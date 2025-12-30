using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using XafApiConverter.SyntaxConverters;

namespace XafApiConverter.Converter {
    internal class ProblemDetector {
        private readonly Solution _solution;

        public ProblemDetector(Solution solution) {
            _solution = solution;
        }

        /// <summary>
        /// Find all classes that use types with no Blazor equivalent
        /// </summary>
        public List<ProblematicClass> FindClassesWithNoEquivalentTypes(Project project) {
            var problematicClasses = new List<ProblematicClass>();

            foreach(var document in project.Documents) {
                if(!document.FilePath.EndsWith(".cs")) continue;

                var syntaxTree = document.GetSyntaxTreeAsync().Result;
                if(syntaxTree == null) continue;

                var root = syntaxTree.GetRoot();
                var semanticModel = document.GetSemanticModelAsync().Result;
                if(semanticModel == null) continue;

                // Find class declarations
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach(var classDecl in classes) {
                    var problems = AnalyzeClass(classDecl, semanticModel, document.FilePath);
                    if(problems.Any()) {
                        problematicClasses.Add(CreateProblematicClassDescription(classDecl, document.FilePath, problems));
                    }
                }
            }

            return problematicClasses;
        }

        static ProblematicClass CreateProblematicClassDescription(ClassDeclarationSyntax classDecl, string filePath, List<TypeProblem> issues) {
            // Extract namespace from the class declaration
            var namespaceName = GetNamespace(classDecl);

            return new ProblematicClass {
                ClassName = classDecl.Identifier.Text,
                Namespace = namespaceName,
                FilePath = filePath,
                Problems = issues
            };
        }

        /// <summary>
        /// Extract namespace from a class declaration.
        /// Handles both traditional namespace syntax and file-scoped namespaces (C# 10+).
        /// </summary>
        /// <param name="classDecl">Class declaration syntax node</param>
        /// <returns>Namespace string or null if class is in global namespace</returns>
        public static string GetNamespace(ClassDeclarationSyntax classDecl) {
            var namespaceDecl = classDecl.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var fileScopedNamespaceDecl = classDecl.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();

            if(namespaceDecl != null) {
                return namespaceDecl.Name.ToString();
            } else if(fileScopedNamespaceDecl != null) {
                return fileScopedNamespaceDecl.Name.ToString();
            }

            return null;
        }

        /// <summary>
        /// Analyze classes in a syntax tree for problematic types.
        /// This is a stateless helper method that can be used by tests.
        /// </summary>
        /// <param name="filePath">Path to the file being analyzed</param>
        /// <param name="root">Syntax tree root</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <param name="usingDirectives">Using directives for fallback namespace resolution</param>
        /// <returns>List of problematic classes found</returns>
        internal static List<ProblematicClass> AnalyzeClassesInSyntaxTree(
            string filePath,
            SyntaxNode root,
            SemanticModel semanticModel,
            HashSet<string> usingDirectives) {

            var problematicClasses = new List<ProblematicClass>();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach(var classDecl in classes) {
                // Use unified analysis logic
                var problems = AnalyzeSingleClass(classDecl, semanticModel, usingDirectives);

                if(problems.Any(p => p.RequiresCommentOut)) {
                    problematicClasses.Add(CreateProblematicClassDescription(classDecl, filePath, problems));
                }
            }

            return problematicClasses;
        }

        /// <summary>
        /// Core analysis logic for a single class.
        /// Used by both production code and tests to ensure consistency.
        /// </summary>
        /// <param name="classDecl">Class declaration to analyze</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <param name="usingDirectives">Using directives for fallback namespace resolution (can be null)</param>
        /// <returns>List of type problems found in the class</returns>
        internal static List<TypeProblem> AnalyzeSingleClass(
            ClassDeclarationSyntax classDecl,
            SemanticModel semanticModel,
            HashSet<string> usingDirectives) {

            var problems = new List<TypeProblem>();

            // 1. Check base classes
            if(classDecl.BaseList != null) {
                foreach(var baseType in classDecl.BaseList.Types) {
                    var typeName = baseType.Type.ToString();
                    if (CheckTypeAgainstMaps(baseType.Type, semanticModel, problems)) {
                        // Semantic model resolved
                    }
                    else if (usingDirectives != null) {
                        // Fallback to using directives
                        CheckTypeUsingDirectives(typeName, usingDirectives, problems);
                    }
                }
            }

            // 2. Check for problematic enum usages (e.g., TemplateType)
            var memberAccess = classDecl.DescendantNodes().OfType<MemberAccessExpressionSyntax>();
            foreach(var access in memberAccess) {
                var text = access.ToString();

                if(text.Contains("TemplateType.Horizontal") || text.Contains("TemplateType.Vertical")) {
                    problems.Add(new TypeProblem {
                        TypeName = "TemplateType",
                        FullTypeName = "DevExpress.ExpressApp.Web.Templates.TemplateType",
                        Reason = "Uses TemplateType enum which has no Blazor equivalent",
                        Description = "TemplateType enum has no Blazor equivalent",
                        Severity = ProblemSeverity.Critical,
                        RequiresCommentOut = true
                    });
                    break; // Only add once
                }
            }

            // 3. Check for typeof() expressions (e.g., typeof(MapsAspNetModule))
            var typeofExpressions = classDecl.DescendantNodes().OfType<TypeOfExpressionSyntax>();
            foreach(var typeofExpr in typeofExpressions) {
                if (CheckTypeAgainstMaps(typeofExpr.Type, semanticModel, problems)) {
                    // Semantic model resolved - use full type name and assembly name
                }
                else {
                    // Fallback: semantic model couldn't resolve (common in tests without full assembly references)
                    // Parse the type expression text to extract namespace and type name
                    var typeText = typeofExpr.Type.ToString();

                    // Try to resolve using text parsing:
                    // Example 1: "DevExpress.ExpressApp.Maps.Web.MapsAspNetModule"
                    // Example 2: "MapsAspNetModule" (with using directive)

                    var lastDot = typeText.LastIndexOf('.');
                    string typeName;
                    string possibleNamespace = null;

                    if (lastDot >= 0) {
                        // Fully qualified type name
                        possibleNamespace = typeText.Substring(0, lastDot);
                        typeName = typeText.Substring(lastDot + 1);
                    }
                    else {
                        // Simple type name - rely on using directives
                        typeName = typeText;
                    }

                    // Check if this type is in our NoEquivalentTypes or ManualConversionRequiredTypes
                    if (TypeReplacementMap.NoEquivalentTypes.TryGetValue(typeName, out var noEquivType)) {
                        // Found a matching no-equivalent type
                        var fullTypeName = noEquivType.GetFullOldTypeName();

                        // Verify namespace matches if we have both
                        if (possibleNamespace == null ||
                            string.IsNullOrEmpty(noEquivType.OldNamespace) ||
                            possibleNamespace.Equals(noEquivType.OldNamespace, StringComparison.Ordinal) ||
                            (usingDirectives != null && usingDirectives.Contains(noEquivType.OldNamespace))) {

                            problems.Add(new TypeProblem {
                                TypeName = typeName,
                                FullTypeName = fullTypeName,
                                Reason = $"Type '{fullTypeName}' has no equivalent in XAF for .NET",
                                Description = noEquivType.Description,
                                Severity = ProblemSeverity.Critical,
                                RequiresCommentOut = noEquivType.CommentOutEntireClass
                            });
                        }
                    }
                    else if (TypeReplacementMap.ManualConversionRequiredTypes.TryGetValue(typeName, out var manualType)) {
                        // Found a matching manual-conversion-required type
                        var fullTypeName = manualType.GetFullOldTypeName();

                        // Verify namespace matches if we have both
                        if (possibleNamespace == null ||
                            string.IsNullOrEmpty(manualType.OldNamespace) ||
                            possibleNamespace.Equals(manualType.OldNamespace, StringComparison.Ordinal) ||
                            (usingDirectives != null && usingDirectives.Contains(manualType.OldNamespace))) {

                            problems.Add(new TypeProblem {
                                TypeName = typeName,
                                FullTypeName = fullTypeName,
                                Reason = $"Type '{fullTypeName}' requires manual conversion",
                                Description = manualType.Description,
                                Severity = ProblemSeverity.High,
                                RequiresCommentOut = manualType.CommentOutEntireClass
                            });
                        }
                    }
                }
            }

            // 4. Check for NO_EQUIVALENT and MANUAL_CONVERSION_REQUIRED types used in code
            var identifierCollector = new IdentifierNameSyntaxCollector();
            identifierCollector.Visit(classDecl);
            foreach (var identifier in identifierCollector.Identifiers) {
                if (CheckTypeAgainstMaps(identifier, semanticModel, problems)) {
                    // Semantic model resolved
                }
                else if (usingDirectives != null) {
                    // Fallback to using directives for identifiers
                    var typeName = identifier.Identifier.Text;
                    if (TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName) ||
                        TypeReplacementMap.ManualConversionRequiredTypes.ContainsKey(typeName)) {
                        CheckTypeUsingDirectives(typeName, usingDirectives, problems);
                    }
                }
            }

            // Deduplicate problems by FullTypeName
            return problems
                .GroupBy(p => p.FullTypeName)
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Check type against TypeReplacementMap using Semantic Model
        /// </summary>
        /// <returns>true, if Semantic model resolved, otherwise false</returns>
        private static bool CheckTypeAgainstMaps(IdentifierNameSyntax identifier, SemanticModel semanticModel, List<TypeProblem> problems) {
            var typeName = identifier.Identifier.Text;
            var typeSymbol = semanticModel.GetSymbolInfo(identifier).Symbol as INamedTypeSymbol;
            return CheckTypeAgainstMaps(typeName, typeSymbol, semanticModel, problems);
        }

        /// <summary>
        /// Check type against TypeReplacementMap using Semantic Model
        /// </summary>
        /// <returns>true, if Semantic model resolved, otherwise false</returns>
        private static bool CheckTypeAgainstMaps(TypeSyntax typeSyntax, SemanticModel semanticModel, List<TypeProblem> problems) {
            var typeName = typeSyntax.ToString();
            var typeSymbol = semanticModel.GetSymbolInfo(typeSyntax).Symbol as INamedTypeSymbol;
            return CheckTypeAgainstMaps(typeName, typeSymbol, semanticModel, problems);
        }

        /// <summary>
        /// Check type against TypeReplacementMap using Semantic Model
        /// </summary>
        /// <returns>true, if Semantic model resolved, otherwise false</returns>
        private static bool CheckTypeAgainstMaps(string typeName, INamedTypeSymbol typeSymbol, SemanticModel semanticModel, List<TypeProblem> problems) {
            if (typeSymbol != null && !typeSymbol.ToDisplayString().StartsWith("?")) {
                // Semantic model resolved - use full type name and containing assembly
                string fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    .Replace("global::", "");
                string containingAssemblyName = typeSymbol.ContainingAssembly?.Name;
                CheckTypeAgainstMaps(typeName, fullTypeName, containingAssemblyName, problems);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check type against TypeReplacementMap using full type name
        /// </summary>
        private static void CheckTypeAgainstMaps(string typeName, string fullTypeName, string assemblyName, List<TypeProblem> problems) {
            // Check NoEquivalentTypes
            var matchingNoEquiv = TypeReplacementMap.NoEquivalentTypes.Values
                .FirstOrDefault(t => {
                    var expectedFullName = t.GetFullOldTypeName();
                    return fullTypeName.Equals(expectedFullName, StringComparison.OrdinalIgnoreCase) ||
                           fullTypeName.EndsWith($".{expectedFullName}", StringComparison.OrdinalIgnoreCase);
                });

            if(matchingNoEquiv != null) {
                problems.Add(new TypeProblem {
                    TypeName = typeName,
                    FullTypeName = fullTypeName,
                    Reason = $"Type '{fullTypeName}' has no equivalent in XAF for .NET",
                    Description = matchingNoEquiv.Description,
                    Severity = ProblemSeverity.Critical,
                    RequiresCommentOut = matchingNoEquiv.CommentOutEntireClass
                });
                return;
            }

            // Check ManualConversionRequiredTypes
            var matchingManual = TypeReplacementMap.ManualConversionRequiredTypes.Values
                .FirstOrDefault(t => {
                    var expectedFullName = t.GetFullOldTypeName();
                    return fullTypeName.Equals(expectedFullName, StringComparison.OrdinalIgnoreCase) ||
                           fullTypeName.EndsWith($".{expectedFullName}", StringComparison.OrdinalIgnoreCase);
                });

            if (matchingManual != null) {
                problems.Add(new TypeProblem {
                    TypeName = typeName,
                    FullTypeName = fullTypeName,
                    Reason = $"Type '{fullTypeName}' has equivalent but requires manual conversion",
                    Description = matchingManual.Description,
                    Severity = ProblemSeverity.High,
                    RequiresCommentOut = matchingManual.CommentOutEntireClass
                });
            }

            // Check removed assemblies
            if (assemblyName != null && fullTypeName.StartsWith("DevExpress.")) {
                string assemblyNameWithoutVersion = GetAssemblyNameWithoutVersion(assemblyName);
                if (TypeReplacementMap.RemovedAssemblies.Contains(assemblyNameWithoutVersion)) {
                    problems.Add(new TypeProblem {
                        TypeName = typeName,
                        FullTypeName = fullTypeName,
                        Reason = $"Class '{typeName}' has no equivalent in XAF for .NET",
                        Description = $"{assemblyName} assembly was removed in v25.2 or is not supported in XAF for .NET",
                        Severity = ProblemSeverity.Critical,
                        RequiresCommentOut = true
                    });
                    return;
                }
            }
        }

        static string GetAssemblyNameWithoutVersion(string assemblyName) {
            if (assemblyName.EndsWith(".dll"))
                assemblyName = assemblyName.Replace(".dll", "");
            if (!assemblyName.StartsWith("DevExpress.") || assemblyName.LastIndexOf(".v") != assemblyName.Length - 6)
                return assemblyName;
            return assemblyName.Substring(0, assemblyName.Length - 6);
        }

        /// <summary>
        /// Check type using directives fallback for namespace resolution
        /// </summary>
        private static void CheckTypeUsingDirectives(string typeName, HashSet<string> usingDirectives, List<TypeProblem> problems) {
            var candidateTypes = TypeReplacementMap.NoEquivalentTypes.Values
                .Concat(TypeReplacementMap.ManualConversionRequiredTypes.Values)
                .Where(t => t.OldType == typeName)
                .ToList();

            foreach(var candidateType in candidateTypes) {
                if(!string.IsNullOrEmpty(candidateType.OldNamespace) &&
                    usingDirectives.Contains(candidateType.OldNamespace)) {
                    var isNoEquiv = TypeReplacementMap.NoEquivalentTypes.ContainsKey(typeName);

                    problems.Add(new TypeProblem {
                        TypeName = typeName,
                        FullTypeName = candidateType.GetFullOldTypeName(),
                        Reason = isNoEquiv
                            ? $"Type '{typeName}' has no equivalent (inferred from using {candidateType.OldNamespace})"
                            : $"Type '{typeName}' requires manual conversion (inferred from using {candidateType.OldNamespace})",
                        Description = candidateType.Description,
                        Severity = isNoEquiv ? ProblemSeverity.Critical : ProblemSeverity.High,
                        RequiresCommentOut = candidateType.CommentOutEntireClass
                    });
                    break;
                }
            }
        }

        /// <summary>
        /// Private wrapper for production use with Solution/Project.
        /// Extracts using directives and delegates to unified analysis logic.
        /// </summary>
        private List<TypeProblem> AnalyzeClass(
            ClassDeclarationSyntax classDecl,
            SemanticModel semanticModel,
            string filePath) {

            // Extract using directives from the file
            var root = classDecl.SyntaxTree.GetRoot();
            var usingDirectives = root.DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet();

            // Use unified analysis logic
            return AnalyzeSingleClass(classDecl, semanticModel, usingDirectives);
        }

        /// <summary>
        /// Find all classes that depend on a specific class using semantic analysis.
        /// This method uses semantic model to accurately detect type usage, avoiding false positives
        /// from simple string matching (e.g., "Message" in a comment or string literal).
        /// </summary>
        /// <param name="project">The project to search in</param>
        /// <param name="targetClassName">Simple class name to find dependencies for</param>
        /// <param name="targetNamespace">Namespace of the target class (optional, for disambiguation)</param>
        /// <param name="semanticCache">Optional semantic cache to avoid reloading documents</param>
        /// <returns>List of class names that depend on the target class</returns>
        public List<string> FindDependentClasses(
            Project project,
            string targetClassName,
            string targetNamespace = null,
            SemanticCache semanticCache = null) {

            var dependents = new List<string>();
            var targetFullName = string.IsNullOrEmpty(targetNamespace) ? targetClassName : $"{targetNamespace}.{targetClassName}";

            foreach(var document in project.Documents) {
                if(!document.FilePath.EndsWith(".cs")) continue;

                try {
                    // Try to get from cache first
                    SemanticModel semanticModel;
                    SyntaxTree syntaxTree;

                    var cached = (semanticCache != null) ? semanticCache.TryGetValue(document.FilePath) : null;
                    if(cached != null) {
                        semanticModel = cached.SemanticModel;
                        syntaxTree = cached.SyntaxTree;
                    } else {
                        // Fallback: load directly
                        syntaxTree = document.GetSyntaxTreeAsync().Result;
                        if(syntaxTree == null) continue;

                        semanticModel = document.GetSemanticModelAsync().Result;
                        if(semanticModel == null) continue;
                    }

                    var root = syntaxTree.GetRoot();
                    var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                    foreach(var classDecl in classes) {
                        var className = classDecl.Identifier.Text;

                        // Skip the target class itself
                        if(className == targetClassName) {
                            continue;
                        }

                        // Check if already in dependents list
                        if(dependents.Contains(className)) {
                            continue;
                        }

                        // Use semantic analysis to check if this class uses the target type
                        if(ClassDependsOnType(classDecl, targetClassName, targetFullName, semanticModel)) {
                            string fullClassName = $"{GetNamespace(classDecl)}.{className}";
                            dependents.Add(fullClassName);
                        }
                    }
                } catch(Exception ex) {
                    Console.WriteLine($"    [WARNING] Error analyzing dependencies in {Path.GetFileName(document.FilePath)}: {ex.Message}");
                }
            }

            return dependents;
        }

        /// <summary>
        /// Check if a class declaration semantically depends on a target type.
        /// Uses semantic model to resolve type symbols, avoiding false positives from string matching.
        /// </summary>
        /// <param name="classDecl">Class declaration to analyze</param>
        /// <param name="targetClassName">Simple name of the target class</param>
        /// <param name="targetFullName">Full name of the target class (with namespace)</param>
        /// <param name="semanticModel">Semantic model for type resolution</param>
        /// <returns>True if the class depends on the target type</returns>
        private bool ClassDependsOnType(
            ClassDeclarationSyntax classDecl,
            string targetClassName,
            string targetFullName,
            SemanticModel semanticModel) {

            // 1. Check base types
            // 2. Check field and property types
            // 3. Check method parameter types and return types
            // 4. Check local variable declarations and object creation expressions
            // 5. Check object creation expressions (new TargetType())
            // 6. Check typeof() expressions
            var typeSyntaxFinder = new TypeSyntaxCollector();
            typeSyntaxFinder.Visit(classDecl);
            foreach(TypeSyntax typeSyntax in typeSyntaxFinder.Types) {
                var typeSymbol = semanticModel.GetSymbolInfo(typeSyntax).Symbol as INamedTypeSymbol;
                if (typeSymbol != null) {
                    var fullTypeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        .Replace("global::", "");

                    // Check if this base type matches the target
                    if (TypeMatchesTarget(typeSymbol.Name, fullTypeName, targetClassName, targetFullName)) {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if a type matches the target type.
        /// Handles both simple name and full name comparisons.
        /// </summary>
        private bool TypeMatchesTarget(string typeName, string fullTypeName, string targetClassName, string targetFullName) {
            // Simple name match
            if(typeName.Equals(targetClassName, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            // Full name match
            if(fullTypeName.Equals(targetFullName, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }

            // Full name ends with target (e.g., "System.Collections.Generic.List`1" ends with "List")
            if(fullTypeName.EndsWith($".{targetClassName}", StringComparison.OrdinalIgnoreCase)) {
                // Additional check: make sure namespaces match if we have full target name
                if(!string.IsNullOrEmpty(targetFullName) && targetFullName.Contains(".")) {
                    return fullTypeName.Contains(targetFullName);
                }
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Represents a class with problematic code
    /// </summary>
    internal class ProblematicClass {
        /// <summary>
        /// Simple class name without namespace (e.g., "MyModule")
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Namespace of the class (e.g., "FeatureCenter.Module.Web")
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Full type name including namespace (e.g., "FeatureCenter.Module.Web.MyModule")
        /// </summary>
        public string FullName => string.IsNullOrEmpty(Namespace) ? ClassName : $"{Namespace}.{ClassName}";

        /// <summary>
        /// Path to the file containing this class
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// List of type problems found in this class
        /// </summary>
        public List<TypeProblem> Problems { get; set; }

        /// <summary>
        /// List of other class names that depend on this class
        /// </summary>
        public List<string> DependentClasses { get; set; } = new List<string>();

        /// <summary>
        /// Indicates whether this class will be fully commented out or only receive a warning comment.
        /// 
        /// True:  Class is fully commented out (entire class is non-functional)
        /// False: Class receives only a warning comment but remains active (e.g., protected base classes)
        /// 
        /// This flag is critical for cascade dependency detection:
        /// - If a class is fully commented (IsFullyCommented = true), classes depending on it should also be commented
        /// - If a class is only warned (IsFullyCommented = false), classes depending on it should NOT be cascaded
        /// 
        /// Example:
        /// - TaskWithNotifications: IsFullyCommented = false (protected, only warning)
        /// - TaskWithNotificationsController: Should NOT be cascaded because TaskWithNotifications is still active
        /// </summary>
        public bool IsFullyCommented { get; set; } = true;  // Default: true (will be commented out)
    }

    /// <summary>
    /// Represents a type-related problem
    /// </summary>
    internal class TypeProblem {
        public string TypeName { get; set; }
        public string FullTypeName { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public ProblemSeverity Severity { get; set; }
        public bool RequiresCommentOut { get; set; }
    }

    internal enum ProblemSeverity {
        Low,
        Medium,
        High,
        Critical
    }
}
