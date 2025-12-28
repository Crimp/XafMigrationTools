using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System.Text.RegularExpressions;
using XafApiConverter.Converter.CodeAnalysis;

namespace XafApiConverter.Converter {
    internal class TypeMigrationTool {
        private readonly string _solutionPath;
        private Solution _solution;
        private MigrationReport _report;
        private MigrationOptions _options;

        /// <summary>
        /// Cache of original semantic models and syntax trees before any modifications.
        /// Key: Document.FilePath
        /// Value: (SemanticModel, SyntaxTree, Document)
        /// This cache is populated once after LoadSolution and never updated.
        /// Use this for dependency analysis and type resolution on original code.
        /// </summary>
        private SemanticCache _semanticCache;
        /// <summary>
        /// When true, all problematic classes will receive warning comments only (no auto-commenting).
        /// This mode treats ALL classes as protected, allowing manual review and decision-making.
        /// Useful for controlled migration scenarios where developer wants to review each class individually.
        /// Default: false (normal mode with automatic commenting for non-protected classes).
        /// </summary>
        public bool CommentIssuesOnly { get; set; } = false;
        public TypeMigrationTool(string solutionPath, MigrationOptions options) {
            _options = options;
            _solutionPath = solutionPath;
            _report = new MigrationReport { SolutionPath = _solutionPath };
            _semanticCache = new SemanticCache();
        }

        public MigrationReport RunMigration() {
            Console.WriteLine("Starting Type Migration...");
            Console.WriteLine();

            try {
                // Phase 1: Load solution
                Console.WriteLine($"Phase 1: Loading solution {Path.GetFileName(_solutionPath)}...");
                LoadSolution();

                // Phase 1.5: Build semantic cache of original state
                Console.WriteLine("Phase 1.5: Building semantic cache...");
                BuildSemanticCache();

                // Phase 2: Detect and comment out problematic classes FIRST (before changing usings!)
                // This allows using directives analysis to work correctly for namespace resolution
                Console.WriteLine("Phase 2: Detecting and commenting out problematic classes...");
                DetectProblems();
                CommentOutProblematicClasses();

                // Phase 3: Apply automatic replacements (usings + types)
                // Now it's safe to change usings since problematic classes are already commented
                Console.WriteLine("Phase 3: Applying automatic replacements...");
                ApplyAutomaticReplacements();

                // Phase 4: Build project
                Console.WriteLine("Phase 4: Building project...");
                BuildErrorAnalysis.BuildAndAnalyzeErrors(_solutionPath, _report, _solution);

                // Phase 5: Generate report
                Console.WriteLine("Phase 5: Generating report...");
                SaveReport();

                Console.WriteLine();
                Console.WriteLine("[OK] Migration analysis complete!");
                _report.PrintSummary();

                return _report;
            } catch(Exception ex) {
                Console.WriteLine($"[ERROR] Migration failed: {ex.Message}");
                throw;
            }
        }

        private void LoadSolution() {
            var workspace = MSBuildWorkspace.Create();
            _solution = workspace.OpenSolutionAsync(_solutionPath).Result;
            Console.WriteLine($"  Loaded solution: {Path.GetFileName(_solutionPath)}");
            Console.WriteLine($"  Projects: {_solution.Projects.Count()}");
        }

        /// <summary>
        /// Build semantic cache for all C# documents in the solution.
        /// This cache represents the ORIGINAL state before any modifications.
        /// Use this cache for dependency analysis and type resolution.
        /// </summary>
        private void BuildSemanticCache() {
            int totalDocuments = 0;
            int cachedDocuments = 0;

            foreach(var project in _solution.Projects) {
                foreach(var document in project.Documents) {
                    if(!document.FilePath.EndsWith(".cs")) {
                        continue;
                    }

                    totalDocuments++;

                    try {
                        var syntaxTree = document.GetSyntaxTreeAsync().Result;
                        var semanticModel = document.GetSemanticModelAsync().Result;

                        if(syntaxTree != null && semanticModel != null) {
                            _semanticCache.Add(document.FilePath, semanticModel, syntaxTree, document);
                            cachedDocuments++;
                        }
                    } catch(Exception ex) {
                        Console.WriteLine($"    [WARNING] Failed to cache document {Path.GetFileName(document.FilePath)}: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"  Cached {cachedDocuments}/{totalDocuments} documents");
        }

        /// <summary>
        /// Phase 2: Apply automatic namespace and type replacements
        /// TRANS-006, TRANS-007, TRANS-008
        /// </summary>
        private void ApplyAutomaticReplacements() {
            foreach(var project in _solution.Projects) {
                Console.WriteLine($"  Processing project: {project.Name}");

                // Process C# files
                foreach(var document in project.Documents) {
                    var filePath = document.FilePath;
                    var extension = Path.GetExtension(filePath);

                    if(extension == ".cs") {
                        ProcessCSharpFile(document);
                        _report.FilesProcessed++;
                    } else if(extension == ".xafml") {
                        ProcessXafmlFile(filePath);
                        _report.XafmlFilesProcessed++;
                        _report.FilesProcessed++;
                    }
                }
            }
        }

        private void ProcessCSharpFile(Document document) {
            // CRITICAL: Read file from DISK, not from Roslyn document cache!
            // Phase 2 (ClassCommenter) may have modified files on disk,
            // but Roslyn workspace still has old cached syntax trees.
            // We must read the current state from disk to preserve Phase 2 changes.

            var filePath = document.FilePath;
            if(!File.Exists(filePath)) {
                Console.WriteLine($"    [WARNING] File not found: {filePath}");
                return;
            }

            // Read current content from disk (may contain Phase 2 modifications)
            var fileContent = File.ReadAllText(filePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
            var root = syntaxTree.GetRoot();
            var originalRoot = root;

            // Use unified static methods for processing
            int namespacesReplaced = 0;
            int typesReplaced = 0;

            root = ProcessUsingsInRoot(root, ref namespacesReplaced);
            root = ProcessTypesInRoot(root, ref typesReplaced);

            _report.NamespacesReplaced += namespacesReplaced;
            _report.TypesReplaced += typesReplaced;

            // Save if changed
            if(root != originalRoot) {
                File.WriteAllText(filePath, root.ToFullString());
            }
        }

        /// <summary>
        /// Process using directives in syntax root.
        /// Internal static helper for tests and production use.
        /// Handles SqlClient namespace, DevExpress namespaces, and NO_EQUIVALENT namespace removal.
        /// </summary>
        internal static SyntaxNode ProcessUsingsInRoot(SyntaxNode root) {
            int dummy = 0;
            return ProcessUsingsInRoot(root, ref dummy);
        }

        /// <summary>
        /// Process using directives with counter update.
        /// </summary>
        private static SyntaxNode ProcessUsingsInRoot(SyntaxNode root, ref int replacedCount) {
            var compilationUnit = root as CompilationUnitSyntax;
            if(compilationUnit == null) return root;

            var newUsings = new List<UsingDirectiveSyntax>();
            bool modified = false;

            foreach(var usingDirective in compilationUnit.Usings) {
                var namespaceName = usingDirective.Name?.ToString();

                // Check if should be removed (NO_EQUIVALENT namespaces)
                if(ShouldRemoveNamespace(namespaceName)) {
                    modified = true;
                    replacedCount++;
                    continue; // Skip (remove)
                }

                // Check if should be replaced
                var newNamespace = GetNamespaceReplacement(namespaceName);
                if(newNamespace != null) {
                    // Create new using with proper whitespace: "using Namespace;"
                    // We need to copy all trivia from original
                    var newName = SyntaxFactory.ParseName(newNamespace);
                    var newUsing = SyntaxFactory.UsingDirective(newName)
                        .WithUsingKeyword(usingDirective.UsingKeyword) // Keep "using" keyword with its trivia
                        .WithSemicolonToken(usingDirective.SemicolonToken); // Keep semicolon with its trivia

                    newUsings.Add(newUsing);
                    modified = true;
                    replacedCount++;
                    continue;
                }

                // Keep unchanged
                newUsings.Add(usingDirective);
            }

            return modified ? compilationUnit.WithUsings(SyntaxFactory.List(newUsings)) : root;
        }

        /// <summary>
        /// Check if namespace should be removed (NO_EQUIVALENT).
        /// </summary>
        private static bool ShouldRemoveNamespace(string namespaceName) {
            return TypeReplacementMap.NoEquivalentNamespaces.ContainsKey(namespaceName) ||
                   TypeReplacementMap.NoEquivalentNamespaces.Values.Any(ns =>
                       namespaceName.StartsWith(ns.OldNamespace + "."));
        }

        /// <summary>
        /// Get replacement for namespace or null if no replacement needed.
        /// Handles SqlClient and DevExpress namespace migrations.
        /// </summary>
        private static string GetNamespaceReplacement(string namespaceName) {
            // TRANS-006: SqlClient namespace
            if(namespaceName == "System.Data.SqlClient") {
                return "Microsoft.Data.SqlClient";
            }

            // TRANS-007: DevExpress namespaces
            if(TypeReplacementMap.NamespaceReplacements.TryGetValue(namespaceName, out var replacement)) {
                if(replacement.HasEquivalent && replacement.AppliesToFileType(".cs")) {
                    return replacement.NewNamespace;
                }
            }

            return null;
        }

        /// <summary>
        /// Process type replacements in syntax root.
        /// Internal static helper for tests and production use.
        /// Applies all type replacements from TypeReplacementMap.
        /// </summary>
        internal static SyntaxNode ProcessTypesInRoot(SyntaxNode root) {
            int dummy = 0;
            return ProcessTypesInRoot(root, ref dummy);
        }

        /// <summary>
        /// Process type replacements with counter update.
        /// </summary>
        private static SyntaxNode ProcessTypesInRoot(SyntaxNode root, ref int replacedCount) {
            var originalRoot = root;

            // TRANS-008: Type replacements
            foreach(var typeReplacement in TypeReplacementMap.TypeReplacements.Values) {
                if(!typeReplacement.AppliesToFileType(".cs") || !typeReplacement.HasEquivalent) {
                    continue;
                }

                var oldRoot = root;
                var rewriter = new TypeReplaceRewriter(
                    typeReplacement.GetFullOldTypeName(),
                    typeReplacement.GetFullNewTypeName());
                root = rewriter.Visit(root);

                if(root != oldRoot) {
                    replacedCount++;
                }
            }

            return root;
        }

        private void ProcessXafmlFile(string filePath) {
            var content = File.ReadAllText(filePath);
            var originalContent = content;

            // TRANS-007: Namespace replacements in XAFML
            foreach(var nsReplacement in TypeReplacementMap.NamespaceReplacements.Values) {
                if(!nsReplacement.AppliesToFileType(".xafml")) continue;
                if(!nsReplacement.HasEquivalent) continue;

                var oldContent = content;
                content = content.Replace(nsReplacement.OldNamespace, nsReplacement.NewNamespace);
                if(content != oldContent) {
                    _report.NamespacesReplaced++;
                }
            }

            // TRANS-008: Type replacements in XAFML (use full type names)
            foreach(var typeReplacement in TypeReplacementMap.TypeReplacements.Values) {
                if(!typeReplacement.AppliesToFileType(".xafml")) continue;
                if(!typeReplacement.HasEquivalent) continue;

                var oldTypeName = typeReplacement.GetFullOldTypeName();
                var newTypeName = typeReplacement.GetFullNewTypeName();

                var oldContent = content;
                content = content.Replace(oldTypeName, newTypeName);
                if(content != oldContent) {
                    _report.TypesReplaced++;
                }
            }

            // Save if changed
            if(content != originalContent) {
                File.WriteAllText(filePath, content);
            }
        }

        /// <summary>
        /// Phase 3:
        /// TRANS-009: Classes using NO_EQUIVALENT types
        /// Uses cached semantic models from Phase 1.5 to analyze ORIGINAL code before any modifications.
        /// </summary>
        private void DetectProblems() {
            foreach(var project in _solution.Projects) {
                var detector = new ProblemDetector(_solution);

                // NEW: Analyze using cached semantic models (original state before modifications)
                var problematicClasses = new List<ProblematicClass>();

                foreach(var document in project.Documents) {
                    if(!document.FilePath.EndsWith(".cs")) continue;

                    // Get cached semantic model and syntax tree (ORIGINAL state)
                    var cached = _semanticCache.TryGetValue(document.FilePath);
                    if(cached == null) {
                        Console.WriteLine($"    [WARNING] No cached semantic model for {Path.GetFileName(document.FilePath)}, skipping");
                        continue;
                    }

                    var semanticModel = cached.SemanticModel;
                    var syntaxTree = cached.SyntaxTree;
                    var root = syntaxTree.GetRoot();

                    // Extract using directives from ORIGINAL syntax tree
                    var usingDirectives = root.DescendantNodes()
                        .OfType<UsingDirectiveSyntax>()
                        .Select(u => u.Name?.ToString())
                        .Where(n => !string.IsNullOrEmpty(n))
                        .ToHashSet();

                    // Analyze classes using ORIGINAL semantic model and syntax tree
                    var classesInFile = ProblemDetector.AnalyzeClassesInSyntaxTree(
                        document.FilePath,
                        root,
                        semanticModel,
                        usingDirectives);

                    problematicClasses.AddRange(classesInFile);
                }

                // CRITICAL: Check if each problematic class is protected BEFORE cascading
                // This ensures cascade logic knows which classes will be fully commented vs warned
                foreach(var problematicClass in problematicClasses) {
                    bool isProtected = CheckIfClassIsProtected(problematicClass.FilePath, problematicClass.ClassName);
                    problematicClass.IsFullyCommented = !isProtected;  // Protected classes are NOT fully commented
                }

                // Find dependencies for each problematic class using semantic analysis
                foreach(var problematicClass in problematicClasses) {
                    // Use semantic cache and namespace for accurate dependency detection
                    var dependents = detector.FindDependentClasses(
                        project,
                        problematicClass.ClassName,
                        problematicClass.Namespace,
                        _semanticCache);

                    problematicClass.DependentClasses = dependents;
                }

                // NEW: Recursively mark dependent classes as problematic (cascade effect)
                // If class A is problematic and class B depends on A, then B is also problematic
                // BUT: Only cascades if A is fully commented (IsFullyCommented = true)
                var cascadedProblematicClasses = CascadeProblematicClasses(problematicClasses, detector, project);

                _report.ProblematicClasses.AddRange(cascadedProblematicClasses);

                // Detect XAFML problems
                var xafmlProblems = XafmlAnalysis.AnalyzeXafmlFiles(project);
                _report.XafmlProblems.AddRange(xafmlProblems);
            }

            Console.WriteLine($"  Found {_report.ProblematicClasses.Count} problematic classes");
            Console.WriteLine($"  Found {_report.XafmlProblems.Count} XAFML problems");
        }

        /// <summary>
        /// Check if a class is protected (inherits from protected base classes).
        /// Helper method to determine IsFullyCommented flag during DetectProblems phase.
        /// </summary>
        private bool CheckIfClassIsProtected(string filePath, string className) {
            try {
                if(!File.Exists(filePath)) {
                    return false;
                }

                var content = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot();

                // Find the class declaration
                var classDecl = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.Identifier.Text == className);

                if(classDecl == null) {
                    return false;
                }

                // Check base types
                if(classDecl.BaseList == null) {
                    return false;
                }

                foreach(var baseType in classDecl.BaseList.Types) {
                    var baseTypeName = baseType.Type.ToString();

                    // Extract simple name (e.g., "BaseObject" from "Namespace.BaseObject")
                    var lastDot = baseTypeName.LastIndexOf('.');
                    var simpleBaseTypeName = lastDot >= 0
                        ? baseTypeName.Substring(lastDot + 1)
                        : baseTypeName;

                    // Check if this is a protected base class
                    if(TypeReplacementMap.ProtectedBaseClasses.Contains(simpleBaseTypeName)) {
                        return true;
                    }
                }

                return false;
            } catch {
                return false;
            }
        }

        /// <summary>
        /// Cascade problematic class detection.
        /// If class A is problematic and class B depends on A, then B is also problematic.
        /// This process is recursive - if B becomes problematic, then classes depending on B also become problematic.
        /// 
        /// IMPORTANT: Only cascades for FULLY COMMENTED classes (IsFullyCommented = true).
        /// Protected classes with warning comments (IsFullyCommented = false) do NOT cascade,
        /// because they remain active and functional.
        /// </summary>
        /// <param name="initialProblematicClasses">Initial list of problematic classes detected directly</param>
        /// <param name="detector">ProblemDetector instance for dependency analysis</param>
        /// <param name="project">Project to analyze</param>
        /// <returns>Complete list of problematic classes including cascaded dependencies</returns>
        private List<ProblematicClass> CascadeProblematicClasses(
            List<ProblematicClass> initialProblematicClasses,
            ProblemDetector detector,
            Project project) {

            var allProblematicClasses = new List<ProblematicClass>(initialProblematicClasses);
            var problematicClassNames = new HashSet<string>(
                initialProblematicClasses.Select(c => c.FullName),
                StringComparer.OrdinalIgnoreCase);

            // Queue of classes to check for dependents
            // ONLY classes that will be FULLY COMMENTED OUT (IsFullyCommented = true)
            var toProcess = new Queue<ProblematicClass>(
                initialProblematicClasses.Where(c => c.IsFullyCommented));

            while(toProcess.Count > 0) {
                var currentClass = toProcess.Dequeue();

                // CRITICAL CHECK: Only cascade if current class is fully commented
                // Protected classes with warnings (IsFullyCommented = false) do NOT cascade
                if(!currentClass.IsFullyCommented) {
                    continue;
                }

                // Find classes that depend on current problematic class
                var dependents = detector.FindDependentClasses(
                    project,
                    currentClass.ClassName,
                    currentClass.Namespace,
                    _semanticCache);

                foreach(var dependentFullName in dependents) {
                    // Skip if already marked as problematic
                    if(problematicClassNames.Contains(dependentFullName)) {
                        continue;
                    }

                    // Extract class name and namespace from full name
                    var lastDotIndex = dependentFullName.LastIndexOf('.');
                    string dependentClassName;
                    string dependentNamespace;

                    if(lastDotIndex >= 0) {
                        dependentNamespace = dependentFullName.Substring(0, lastDotIndex);
                        dependentClassName = dependentFullName.Substring(lastDotIndex + 1);
                    } else {
                        dependentNamespace = null;
                        dependentClassName = dependentFullName;
                    }

                    // Find the file containing this dependent class
                    string dependentFilePath = null;
                    foreach(var document in project.Documents) {
                        if(!document.FilePath.EndsWith(".cs")) continue;

                        var cached = _semanticCache.TryGetValue(document.FilePath);
                        if(cached == null) continue;

                        var root = cached.SyntaxTree.GetRoot();
                        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                        foreach(var classDecl in classes) {
                            if(classDecl.Identifier.Text == dependentClassName) {
                                var ns = ProblemDetector.GetNamespace(classDecl);
                                if(ns == dependentNamespace ||
                                    (string.IsNullOrEmpty(ns) && string.IsNullOrEmpty(dependentNamespace))) {
                                    dependentFilePath = document.FilePath;
                                    break;
                                }
                            }
                        }

                        if(dependentFilePath != null) break;
                    }

                    if(dependentFilePath == null) {
                        Console.WriteLine($"    [WARNING] Could not find file for dependent class {dependentFullName}");
                        continue;
                    }

                    // Create ProblematicClass entry for the dependent
                    var dependentProblematicClass = new ProblematicClass {
                        ClassName = dependentClassName,
                        Namespace = dependentNamespace,
                        FilePath = dependentFilePath,
                        IsFullyCommented = true,  // Cascaded classes are fully commented by default
                        Problems = new List<TypeProblem> {
                            new TypeProblem {
                                TypeName = currentClass.ClassName,
                                FullTypeName = currentClass.FullName,
                                Reason = $"Depends on problematic class '{currentClass.FullName}' which has no .NET equivalent",
                                Description = $"Class uses '{currentClass.FullName}' which is being commented out due to having no .NET equivalent",
                                Severity = ProblemSeverity.Critical,
                                RequiresCommentOut = true
                            }
                        }
                    };

                    // Add to results
                    allProblematicClasses.Add(dependentProblematicClass);
                    problematicClassNames.Add(dependentFullName);

                    // Add to queue to check its dependents (cascade will continue)
                    toProcess.Enqueue(dependentProblematicClass);

                    Console.WriteLine($"    [CASCADE] Class {dependentFullName} marked as problematic (depends on {currentClass.FullName})");
                }
            }

            return allProblematicClasses;
        }

        



        /// <summary>
        /// Phase 5: Generate and save report
        /// </summary>
        private void SaveReport() {
            var reportPath = Path.Combine(
                Path.GetDirectoryName(_solutionPath),
                "type-migration-report.md");

            _report.SaveToFile(reportPath);
            Console.WriteLine($"  Report saved to: {reportPath}");
        }

        /// <summary>
        /// Phase 6: Comment out problematic classes automatically
        /// Implements TRANS-010 lightweight version
        /// </summary>
        private void CommentOutProblematicClasses() {
            var commenter = new ClassCommenter(_report, _options, _semanticCache);
            var commentedCount = commenter.CommentOutProblematicClasses();

            if(commentedCount > 0) {
                Console.WriteLine($"  Commented out {commentedCount} classes");
                
                // Update report with commented classes
                _report.ClassesCommented = commentedCount;
                _report.CommentedClassNames = commenter.GetCommentedClasses().ToList();
            } else {
                Console.WriteLine("  No classes needed commenting");
            }
        }

        /// <summary>
        /// Get migration statistics
        /// </summary>
        public MigrationReport GetReport() => _report;
    }
}
