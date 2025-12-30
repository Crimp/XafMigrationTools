using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Automatically comments out classes with NO_EQUIVALENT types and their dependencies
    /// Implements TRANS-010: Build-Fix-Comment Iteration (lightweight version)
    /// </summary>
    internal class ClassCommenter {
        private readonly MigrationReport _report;
        private readonly HashSet<string> _commentedClasses = new();
        private readonly HashSet<string> _warningAddedClasses = new();
        private readonly MigrationOptions _options;
        private readonly SemanticCache _semanticCache;

        public ClassCommenter(MigrationReport report, MigrationOptions options, SemanticCache semanticCache) {
            this._options = options;
            _report = report;
            _semanticCache = semanticCache;
        }

        public static string GetTodoClassCommentedComment(string className) {
            var sb = new StringBuilder();
            sb.AppendLine($"// TODO: The '{className}' class has been commented out automatically due to usage of types that have no XAF .NET equivalent.");
            sb.AppendLine("//       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589");
            sb.AppendLine("//       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.");
            //sb.AppendLine("//       Refer to the migration documentation for guidance on handling such cases.");
            return sb.ToString();
        }

        public static string GetTodoClassWithIssuesComment(string className) {
            var sb = new StringBuilder();
            sb.AppendLine($"// TODO: The '{className}' class has been marked automatically due to usage of types that have no XAF .NET equivalent.");
            sb.AppendLine("//       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589");
            sb.AppendLine("//       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.");
            //sb.AppendLine("//       Refer to the migration documentation for guidance on handling such cases.");
            return sb.ToString();
        }

        /// <summary>
        /// Comment out all problematic classes and their dependencies
        /// </summary>
        public int CommentOutProblematicClasses() {
            int commentedCount = 0;

            foreach (var problematicClass in _report.ProblematicClasses) {
                // Skip if already commented (duplicate)
                if (_commentedClasses.Contains(problematicClass.ClassName) ||
                    _warningAddedClasses.Contains(problematicClass.ClassName)) {
                    continue;
                }

                // Check if this is a protected class first
                bool isProtected = CheckIfProtectedClass(problematicClass.FilePath, problematicClass.ClassName);
                
                if (isProtected) {
                    // Protected class: Add warning comment only, do NOT comment out
                    if (AddWarningCommentToProtectedClass(problematicClass.FilePath, problematicClass.ClassName, problematicClass)) {
                        _warningAddedClasses.Add(problematicClass.ClassName);
                        problematicClass.IsFullyCommented = false;  // ← Class is NOT fully commented!
                        Console.WriteLine($"    [WARNING ADDED] {problematicClass.ClassName} in {Path.GetFileName(problematicClass.FilePath)} (protected class)");
                    }
                    continue;
                }

                // Not protected: Comment out the entire class
                bool success = CommentOutSingleClass(
                    problematicClass.FilePath,
                    problematicClass.ClassName,
                    problematicClass);

                if (success) {
                    _commentedClasses.Add(problematicClass.ClassName);
                    problematicClass.IsFullyCommented = true;  // ← Class IS fully commented!
                    commentedCount++;

                    string status = problematicClass.DependentClasses.Count > 0 ? " (has dependents)" : "";
                    Console.WriteLine($"    [COMMENTED] {problematicClass.FullName} in {Path.GetFileName(problematicClass.FilePath)}{status}");
                }
            }

            return commentedCount;
        }

        /// <summary>
        /// Check if a class is protected (inherits from ModuleBase, etc.) without modifying it.
        /// 
        /// IMPORTANT: In CommentIssuesOnly mode, this method ALWAYS returns true,
        /// treating all classes as protected (only warning comments will be added).
        /// </summary>
        private bool CheckIfProtectedClass(string filePath, string className) {
            // Comment Issues Only mode: treat ALL classes as protected
            if (_options.CommentIssuesOnly) {
                return true;
            }
            
            try {
                if (!File.Exists(filePath)) {
                    return false;
                }

                // Try to get semantic model from cache first (preferred)
                var cached = _semanticCache?.TryGetValue(filePath);
                if (cached != null) {
                    // Use semantic model for accurate type resolution
                    var root = cached.SyntaxTree.GetRoot() as CompilationUnitSyntax;
                    if (root != null) {
                        var classDecl = root.DescendantNodes()
                            .OfType<ClassDeclarationSyntax>()
                            .Where(c => c.Identifier.Text == className)
                            .Where(c => !IsClassInsideComment(c, cached.SyntaxTree.ToString()))
                            .FirstOrDefault();

                        if (classDecl != null) {
                            return IsProtectedClassUsingSemanticModel(classDecl, cached.SemanticModel);
                        }
                    }
                }

                // Fallback: parse content and use syntax-only check
                var content = File.ReadAllText(filePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root2 = syntaxTree.GetRoot() as CompilationUnitSyntax;

                if (root2 == null) {
                    return false;
                }

                var classDecl2 = root2.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text == className)
                    .Where(c => !IsClassInsideComment(c, content))
                    .FirstOrDefault();

                if (classDecl2 == null) {
                    return false;
                }

                // Syntax-only fallback (less reliable)
                return IsProtectedClassSyntaxOnly(classDecl2, filePath, content);
            }
            catch {
                return false;
            }
        }

        /// <summary>
        /// Add warning comment above a protected class without commenting it out
        /// </summary>
        private bool AddWarningCommentToProtectedClass(string filePath, string className, ProblematicClass problematicClass) {
            try {
                if (!File.Exists(filePath)) {
                    Console.WriteLine($"      [ERROR] File not found: {filePath}");
                    return false;
                }

                // Load content
                var content = File.ReadAllText(filePath);
                
                // Check if warning already exists
                if (HasWarningComment(content, className)) {
                    Console.WriteLine($"      [WARNING] Class {className} already has warning comment, skipping");
                    return false;
                }
                
                // Parse syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

                if (root == null) {
                    Console.WriteLine($"      [ERROR] Could not parse file: {filePath}");
                    return false;
                }

                // Find the class
                var classDecl = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text == className)
                    .Where(c => !IsClassInsideComment(c, content))
                    .FirstOrDefault();
                
                if (classDecl == null) {
                    Console.WriteLine($"      [WARNING] Class {className} not found in file: {filePath}");
                    return false;
                }

                // Check if partial class
                bool isPartial = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
                
                // Build warning comment
                var warningComment = BuildProtectedClassWarningComment(problematicClass, isPartial);

                // Get position to insert comment (before any attributes or the class itself)
                var insertPosition = GetCommentInsertPosition(classDecl, content);

                // Extract base indentation
                var baseIndent = ExtractIndentation(classDecl);

                // Build the formatted comment
                var formattedComment = FormatWarningComment(warningComment, baseIndent);

                // Insert the comment and fix whitespace after it
                var beforeComment = content.Substring(0, insertPosition);
                var afterCommentStart = insertPosition;
                
                // Skip any whitespace (tabs/spaces) that was the original indentation
                while (afterCommentStart < content.Length) {
                    var ch = content[afterCommentStart];
                    if (ch != ' ' && ch != '\t') {
                        break;
                    }
                    afterCommentStart++;
                }
                
                var afterComment = content.Substring(afterCommentStart);
                // Add back the correct indentation (spaces instead of tabs)
                var newContent = beforeComment + formattedComment + baseIndent + afterComment;

                // Save file
                File.WriteAllText(filePath, newContent, Encoding.UTF8);
                
                Console.WriteLine($"      [SUCCESS] Warning comment added to protected class {className}");
                
                // If partial class, add warnings to other parts too
                if (isPartial) {
                    var partialParts = FindPartialClassParts(className, filePath);
                    foreach (var part in partialParts) {
                        AddWarningCommentToProtectedClass(part.FilePath, className, problematicClass);
                    }
                }
                
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"      [ERROR] Failed to add warning comment to class {className}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a class already has a warning comment
        /// </summary>
        private bool HasWarningComment(string content, string className) {
            var patterns = new[] {
                $"// TODO: The '{className}' class"
            };

            // Search in the area before where class might be
            foreach (var pattern in patterns) {
                var index = content.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
                if (index >= 0) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Build warning comment for protected classes
        /// </summary>
        private string BuildProtectedClassWarningComment(ProblematicClass problematicClass, bool isPartial) {
            var sb = new StringBuilder();
            
            var prefix = isPartial ? "Partial class" : "Class";
            sb.AppendLine(GetTodoClassWithIssuesComment(problematicClass.ClassName));
            sb.AppendLine($"// NOTE:");
            
            var reasons = problematicClass.Problems
                .Where(p => p.RequiresCommentOut)
                .Distinct();
            
            foreach (var problem in reasons) {
                // Add the main reason
                sb.AppendLine($"//   - {problem.Reason}");
                
                // NEW: Add detailed description if available
                if (!string.IsNullOrEmpty(problem.Description)) {
                    sb.AppendLine($"//     {problem.Description}");
                }
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Get the position where comment should be inserted (before attributes or class)
        /// </summary>
        private int GetCommentInsertPosition(ClassDeclarationSyntax classDecl, string content) {
            // If class has attributes, insert before first attribute
            if (classDecl.AttributeLists.Count > 0) {
                var firstAttribute = classDecl.AttributeLists[0];
                // Find the start of the line containing the attribute
                var attributeStart = firstAttribute.SpanStart;
                var lineStart = FindLineStart(content, attributeStart);
                return lineStart;
            }

            // Otherwise, find the start of the line containing the class declaration
            var classStart = classDecl.SpanStart;
            return FindLineStart(content, classStart);
        }

        /// <summary>
        /// Find the start of the line containing the given position
        /// </summary>
        private int FindLineStart(string content, int position) {
            // Move backwards to find the newline character before this position
            var lineStart = position;
            while (lineStart > 0) {
                var ch = content[lineStart - 1];
                if (ch == '\n') {
                    // Found newline, return position after it
                    return lineStart;
                }
                if (ch != ' ' && ch != '\t' && ch != '\r') {
                    // Hit non-whitespace, shouldn't happen in valid code
                    // but return current position as fallback
                    break;
                }
                lineStart--;
            }
            return lineStart;
        }

        /// <summary>
        /// Extract indentation from class declaration
        /// </summary>
        private string ExtractIndentation(ClassDeclarationSyntax classDecl) {
            var leadingTrivia = classDecl.GetLeadingTrivia();
            var lastWhitespace = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            
            if (lastWhitespace != default(SyntaxTrivia)) {
                // Convert tabs to spaces (tab = 4 spaces) for consistent formatting
                return lastWhitespace.ToString().Replace("\t", "    ");
            }

            return string.Empty;
        }

        /// <summary>
        /// Format warning comment with proper indentation
        /// </summary>
        private string FormatWarningComment(string comment, string baseIndent) {
            var sb = new StringBuilder();
            
            var lines = comment.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines) {
                sb.Append(baseIndent);
                sb.AppendLine(line);
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Comment out a single class in a file with full syntax tree reload
        /// </summary>
        private bool CommentOutSingleClass(string filePath, string className, ProblematicClass problematicClass) {
            try {
                if (!File.Exists(filePath)) {
                    Console.WriteLine($"      [ERROR] File not found: {filePath}");
                    return false;
                }

                // STEP 1: Load fresh content from disk
                var content = File.ReadAllText(filePath);
                
                // STEP 2: Check if class is already commented out (BEFORE parsing!)
                if (IsClassAlreadyCommented(content, className)) {
                    Console.WriteLine($"      [WARNING] Class {className} appears to be already commented out, skipping");
                    return false;
                }
                
                // STEP 3: Parse syntax tree
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;

                if (root == null) {
                    Console.WriteLine($"      [ERROR] Could not parse file: {filePath}");
                    return false;
                }

                // STEP 4: Find the specific class by name (only active, uncommented classes!)
                var allClasses = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text == className)
                    .Where(c => !IsClassInsideComment(c, content))  // NEW: Filter out classes inside comments!
                    .ToList();
                
                if (!allClasses.Any()) {
                    Console.WriteLine($"      [WARNING] Class {className} not found as active code in file: {filePath}");
                    return false;
                }
                
                // If multiple classes with same name found (shouldn't happen in valid C#), take the first
                var classDecl = allClasses.First();
                
                // STEP 4.5: CRITICAL CHECK - Verify class doesn't inherit from protected base class
                // Do this BEFORE checking for partial classes to prevent any parts from being commented
                bool isProtected = false;
                
                // Try semantic model first (preferred)
                var cached = _semanticCache?.TryGetValue(filePath);
                if (cached != null) {
                    var classFromCachedModel = cached.SyntaxTree.GetRoot().DescendantNodes() // get ClassDeclarationSyntax from cached model, to avoid 'Syntax node is not within syntax tree' exceptions
                            .OfType<ClassDeclarationSyntax>()
                            .Where(c => c.Identifier.Text == className)
                            .Where(c => !IsClassInsideComment(c, content))
                            .FirstOrDefault();
                    if (classFromCachedModel != null) {
                        isProtected = IsProtectedClassUsingSemanticModel(classFromCachedModel, cached.SemanticModel);
                    }
                    else {
                        // Fallback to syntax-only check
                        isProtected = IsProtectedClassSyntaxOnly(classDecl, filePath, content);
                    }
                } else {
                    // Fallback to syntax-only check
                    isProtected = IsProtectedClassSyntaxOnly(classDecl, filePath, content);
                }
                
                if (isProtected) {
                    Console.WriteLine($"      [CRITICAL] Class {className} inherits from protected base class (e.g., ModuleBase)");
                    Console.WriteLine($"      [CRITICAL] This class MUST be preserved for manual refactoring!");
                    Console.WriteLine($"      [CRITICAL] Skipping automatic commenting - please review manually");
                    return false;
                }
                
                // STEP 4.6: Check if this is a partial class and find all its parts
                bool isPartial = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));
                if (isPartial) {
                    var partialParts = FindPartialClassParts(className, filePath);
                    if (partialParts.Any()) {
                        Console.WriteLine($"      [INFO] Class {className} is partial with {partialParts.Count} additional part(s)");
                        Console.WriteLine($"      [WARNING] Partial class detected. All parts must be processed together:");
                        Console.WriteLine($"         - {Path.GetFileName(filePath)} (current)");
                        foreach (var part in partialParts) {
                            Console.WriteLine($"         - {Path.GetFileName(part.FilePath)}");
                        }
                        
                        // Check if ANY part is protected - if so, ALL parts should get warnings
                        bool anyPartProtected = false;
                        foreach (var part in partialParts) {
                            if (CheckIfProtectedClass(part.FilePath, className)) {
                                anyPartProtected = true;
                                break;
                            }
                        }
                        
                        if (anyPartProtected) {
                            Console.WriteLine($"      [CRITICAL] One or more partial class parts inherit from protected base class");
                            Console.WriteLine($"      [CRITICAL] ALL parts of this partial class will receive warning comments only");
                            
                            // Process all parts with warnings
                            foreach (var part in partialParts) {
                                var partContent = File.ReadAllText(part.FilePath);
                                if (!HasWarningComment(partContent, className)) {
                                    var partTree = CSharpSyntaxTree.ParseText(partContent);
                                    var partRoot = partTree.GetRoot() as CompilationUnitSyntax;
                                    if (partRoot != null) {
                                        var partClassDecl = partRoot.DescendantNodes()
                                            .OfType<ClassDeclarationSyntax>()
                                            .Where(c => c.Identifier.Text == className)
                                            .Where(c => !IsClassInsideComment(c, partContent))
                                            .FirstOrDefault();
                                        
                                        if (partClassDecl != null) {
                                            var warningComment = BuildProtectedClassWarningComment(problematicClass, isPartial: true);
                                            var insertPosition = GetCommentInsertPosition(partClassDecl, partContent);
                                            var baseIndent = ExtractIndentation(partClassDecl);
                                            var formattedComment = FormatWarningComment(warningComment, baseIndent);
                                            
                                            var beforeComment = partContent.Substring(0, insertPosition);
                                            var afterComment = partContent.Substring(insertPosition);
                                            var newPartContent = beforeComment + formattedComment + afterComment;
                                            
                                            File.WriteAllText(part.FilePath, newPartContent, Encoding.UTF8);
                                            Console.WriteLine($"         - {Path.GetFileName(part.FilePath)}: warning comment added");
                                        }
                                    }
                                }
                            }
                            
                            // Add warning to current part too
                            var currentWarningComment = BuildProtectedClassWarningComment(problematicClass, isPartial: true);
                            var currentInsertPosition = GetCommentInsertPosition(classDecl, content);
                            var currentBaseIndent = ExtractIndentation(classDecl);
                            var currentFormattedComment = FormatWarningComment(currentWarningComment, currentBaseIndent);
                            
                            var beforeCurrentComment = content.Substring(0, currentInsertPosition);
                            var afterCurrentComment = content.Substring(currentInsertPosition);
                            var newCurrentContent = beforeCurrentComment + currentFormattedComment + afterCurrentComment;
                            
                            File.WriteAllText(filePath, newCurrentContent, Encoding.UTF8);
                            Console.WriteLine($"         - {Path.GetFileName(filePath)}: warning comment added (current part)");
                            

                            // Register in _warningAddedClasses
                            _warningAddedClasses.Add(className);
                            return false; // Don't comment out, just return false to indicate no commenting happened
                        }
                        
                        Console.WriteLine($"      [ACTION] Commenting out all parts of partial class {className}...");
                        
                        // Comment out all other parts first
                        foreach (var part in partialParts) {
                            if (!CommentOutPartialClassPart(part.FilePath, className, problematicClass)) {
                                Console.WriteLine($"      [ERROR] Failed to comment out partial class part in {Path.GetFileName(part.FilePath)}");
                                return false;
                            }
                        }
                        
                        // Then comment out the current part (this file)
                        // Continue with normal flow below...
                    }
                }

                // STEP 5: Additional check - verify the class is not inside a comment
                // by checking if it's part of a trivia (comment)
                var classPosition = classDecl.SpanStart;
                var textBeforeClass = content.Substring(Math.Max(0, classPosition - 100), Math.Min(100, classPosition));
                
                // If there's a // ========== marker shortly before, it's already commented
                if (textBeforeClass.Contains("// ========== COMMENTED OUT CLASS", StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine($"      [WARNING] Class {className} is inside a commented block, skipping");
                    return false;
                }

                // STEP 6: Build comment header
                var comment = BuildClassComment(problematicClass);
                if (isPartial) {
                    comment = comment.Replace("// NOTE:", "// NOTE: Partial class");
                }

                // STEP 7: Get ONLY the class code directly from file content
                // Use direct substring instead of GetText() to avoid Roslyn capturing commented code
                var classStartPosition = classDecl.SpanStart;
                var classLength = classDecl.Span.Length;
                var classText = content.Substring(classStartPosition, classLength);
                
                // Get leading trivia separately for indentation calculation
                var leadingTrivia = classDecl.GetLeadingTrivia();

                // STEP 8: Build commented version
                var commentedClass = BuildCommentedClassText(comment, classText, leadingTrivia);

                // STEP 9: Replace in content using SPAN positions
                // Find the start of the line to remove original whitespace
                var lineStart = FindLineStart(content, classStartPosition);
                var beforeClass = content.Substring(0, lineStart);
                var afterClass = content.Length > classStartPosition + classLength
                    ? content.Substring(classStartPosition + classLength)
                    : string.Empty;

                var newContent = beforeClass + commentedClass + afterClass;

                // STEP 10: Save file (this invalidates all syntax nodes)
                File.WriteAllText(filePath, newContent, Encoding.UTF8);
                
                Console.WriteLine($"      [SUCCESS] Class {className} commented out successfully{(isPartial ? " (partial)" : "")}");
                
                // STEP 11: Syntax tree is now stale - will be reloaded on next iteration
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"      [ERROR] Failed to comment out class {className} in {filePath}: {ex.Message}");
                Console.WriteLine($"      Stack trace: {ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Comment out a single part of a partial class
        /// </summary>
        private bool CommentOutPartialClassPart(string filePath, string className, ProblematicClass problematicClass) {
            try {
                var content = File.ReadAllText(filePath);
                
                // Check if already commented
                if (IsClassAlreadyCommented(content, className)) {
                    Console.WriteLine($"         - {Path.GetFileName(filePath)}: already commented, skipping");
                    return true;
                }
                
                // Check if already has warning comment
                if (HasWarningComment(content, className)) {
                    Console.WriteLine($"         - {Path.GetFileName(filePath)}: already has warning comment, skipping");
                    return true;
                }
                
                var syntaxTree = CSharpSyntaxTree.ParseText(content);
                var root = syntaxTree.GetRoot() as CompilationUnitSyntax;
                
                if (root == null) return false;
                
                var classDecl = root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text == className)
                    .Where(c => !IsClassInsideComment(c, content))
                    .FirstOrDefault();
                
                if (classDecl == null) {
                    Console.WriteLine($"         - {Path.GetFileName(filePath)}: class not found, skipping");
                    return true;
                }
                
                // CRITICAL CHECK: Verify this partial class part doesn't inherit from protected base class
                bool isProtected = false;
                
                // Try semantic model first (preferred)
                var cached = _semanticCache?.TryGetValue(filePath);
                if (cached != null) {
                    var classFromCachedModel = cached.SyntaxTree.GetRoot().DescendantNodes() // get ClassDeclarationSyntax from cached model, to avoid 'Syntax node is not within syntax tree' exceptions
                            .OfType<ClassDeclarationSyntax>()
                            .Where(c => c.Identifier.Text == className)
                            .Where(c => !IsClassInsideComment(c, content))
                            .FirstOrDefault();
                    if (classFromCachedModel != null) {
                        isProtected = IsProtectedClassUsingSemanticModel(classFromCachedModel, cached.SemanticModel);
                    }
                    else {
                        // Fallback to syntax-only check
                        isProtected = IsProtectedClassSyntaxOnly(classDecl, filePath, content);
                    }
                } else {
                    // Fallback to syntax-only check
                    isProtected = IsProtectedClassSyntaxOnly(classDecl, filePath, content);
                }
                
                if (isProtected) {
                    Console.WriteLine($"         - {Path.GetFileName(filePath)}: [PROTECTED] inherits from protected base class");
                    Console.WriteLine($"         - {Path.GetFileName(filePath)}: Adding warning comment instead of commenting out");
                    
                    // Add warning comment instead of commenting out
                    var warningComment = BuildProtectedClassWarningComment(problematicClass, isPartial: true);
                    var insertPosition = GetCommentInsertPosition(classDecl, content);
                    var baseIndent = ExtractIndentation(classDecl);
                    var formattedComment = FormatWarningComment(warningComment, baseIndent);
                    
                    var beforeComment = content.Substring(0, insertPosition);
                    var afterComment = content.Substring(insertPosition);
                    var newContentWithWarning = beforeComment + formattedComment + afterComment;
                    
                    File.WriteAllText(filePath, newContentWithWarning, Encoding.UTF8);
                    Console.WriteLine($"         - {Path.GetFileName(filePath)}: warning comment added successfully");
                    return true;
                }
                
                var comment = BuildClassComment(problematicClass);
                comment = comment.Replace("// NOTE:", "// NOTE: Partial class part");
                
                var classStartPosition = classDecl.SpanStart;
                var classLength = classDecl.Span.Length;
                var classText = content.Substring(classStartPosition, classLength);
                var leadingTrivia = classDecl.GetLeadingTrivia();
                
                var commentedClass = BuildCommentedClassText(comment, classText, leadingTrivia);
                
                // Find the start of the line to remove original whitespace
                var lineStart = FindLineStart(content, classStartPosition);
                var beforeClass = content.Substring(0, lineStart);
                var afterClass = content.Length > classStartPosition + classLength
                    ? content.Substring(classStartPosition + classLength)
                    : string.Empty;
                
                var newContent = beforeClass + commentedClass + afterClass;
                File.WriteAllText(filePath, newContent, Encoding.UTF8);
                
                Console.WriteLine($"         - {Path.GetFileName(filePath)}: commented successfully");
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"         - {Path.GetFileName(filePath)}: ERROR - {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if class is already commented out
        /// </summary>
        private bool IsClassAlreadyCommented(string content, string classFullName) {
            // Extract simple class name from full name (e.g., "MyApp.Models.Message" -> "Message")
            var className = classFullName.Contains('.') 
                ? classFullName.Substring(classFullName.LastIndexOf('.') + 1)
                : classFullName;
            
            // Look for multiple patterns to catch already commented classes:
            // Pattern 1: // ========== COMMENTED OUT CLASS ==========
            // Pattern 2: // public class ClassName (with any indentation)
            // Pattern 3: //     public class ClassName
            // Pattern 4: // internal class ClassName
            
            var commentMarker = "// ========== COMMENTED OUT CLASS ==========";
            
            // CRITICAL FIX: Find ALL commented blocks in the file, not just the first one!
            // This handles multiple classes in one file correctly.
            var currentIndex = 0;
            while (true) {
                // Find next comment marker
                var commentIndex = content.IndexOf(commentMarker, currentIndex, StringComparison.OrdinalIgnoreCase);
                if (commentIndex < 0) {
                    // No more comment markers found
                    break;
                }
                
                // Find the end of this commented block (next closing marker or end of file)
                var closingMarker = "// ========================================";
                var closingIndex = content.IndexOf(closingMarker, commentIndex + commentMarker.Length, StringComparison.OrdinalIgnoreCase);
                if (closingIndex < 0) {
                    // No closing marker - assume rest of file
                    closingIndex = content.Length;
                }
                
                // Extract the commented block content
                var blockLength = closingIndex - commentIndex;
                var commentedBlock = content.Substring(commentIndex, blockLength);
                
                // CRITICAL: Use word boundary check to avoid matching substrings!
                // For example, "ASPxCustomListEditor" should NOT match "ASPxCustomListEditorControl"
                // We check for word boundaries: space, colon, curly brace, newline, etc.
                var patterns = new[] {
                    $"// public class {className} ",          // Space after (e.g., class Foo extends Bar)
                    $"// public class {className}:",         // Colon after (e.g., class Foo : Bar)
                    $"// public class {className}{{",        // Curly brace after (e.g., class Foo {)
                    $"// public class {className}\r",        // Newline after
                    $"// public class {className}\n",        // Newline after
                    $"//     public class {className} ",     // With indent + space
                    $"//     public class {className}:",     // With indent + colon
                    $"//     public class {className}{{",    // With indent + brace
                    $"// internal class {className} ",       // Internal + space
                    $"// internal class {className}:",       // Internal + colon
                    $"// internal class {className}{{",      // Internal + brace
                    $"//     internal class {className} ",   // Internal + indent + space
                    $"//     internal class {className}:",   // Internal + indent + colon
                    $"//\tpublic class {className} ",        // Tab + space
                    $"//\tpublic class {className}:",        // Tab + colon
                    $"//  public class {className} ",        // Double space + space
                    $"//    public class {className} ",      // 4 spaces + space
                    $"//private class {className} ",         // Private + space
                    $"//protected class {className} ",       // Protected + space
                };
                
                foreach (var pattern in patterns) {
                    if (commentedBlock.Contains(pattern, StringComparison.OrdinalIgnoreCase)) {
                        // Found the specific class in this commented block!
                        return true;
                    }
                }
                
                // Continue searching after this block
                currentIndex = closingIndex + closingMarker.Length;
            }
            
            // Class not found in any commented blocks
            return false;
        }
        
        /// <summary>
        /// Check if a class declaration is inside commented code
        /// by analyzing the actual file content at the class position
        /// </summary>
        private bool IsClassInsideComment(ClassDeclarationSyntax classDecl, string fileContent) {
            // Get the actual position of the class in the file
            var classPosition = classDecl.SpanStart;
            
            // Look backwards from the class position to find the start of the line
            var lineStart = classPosition;
            while (lineStart > 0 && fileContent[lineStart - 1] != '\n' && fileContent[lineStart - 1] != '\r') {
                lineStart--;
            }
            
            // Get the text from line start to class start
            var linePrefix = fileContent.Substring(lineStart, classPosition - lineStart);
            
            // If the line starts with //, the class is commented out
            var trimmedPrefix = linePrefix.TrimStart(' ', '\t');
            if (trimmedPrefix.StartsWith("//", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            
            // Additional check: look for comment marker nearby (within 200 chars before)
            var checkStart = Math.Max(0, classPosition - 200);
            var checkRegion = fileContent.Substring(checkStart, classPosition - checkStart);
            
            // If we find the comment marker recently, this class is inside commented block
            if (checkRegion.Contains("// ========== COMMENTED OUT CLASS ==========", StringComparison.OrdinalIgnoreCase)) {
                // Check if there's a closing marker between the comment start and this class
                var closingMarker = "// ========================================";
                var commentStartIndex = checkRegion.LastIndexOf("// ========== COMMENTED OUT CLASS ==========", StringComparison.OrdinalIgnoreCase);
                var afterCommentStart = checkRegion.Substring(commentStartIndex);
                
                // If no closing marker found after comment start, we're still inside the comment
                if (!afterCommentStart.Contains(closingMarker, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
            
            return false;
        }
        
        private bool IsProtectedClassUsingSemanticModel(ClassDeclarationSyntax classDecl, SemanticModel semanticModel) {
            // Comment Issues Only mode: treat ALL classes as protected
            if (_options.CommentIssuesOnly) {
                return true;
            }

            try {
                // Get the symbol for this class
                var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                if (classSymbol == null) {
                    return false;
                }

                // Traverse the base type chain
                var currentBase = classSymbol.BaseType;
                var visitedTypes = new HashSet<string>();

                while (currentBase != null) {
                    // Get simple type name
                    var baseTypeName = currentBase.Name;

                    // Check for circular reference (shouldn't happen but be safe)
                    if (visitedTypes.Contains(baseTypeName)) {
                        break;
                    }
                    visitedTypes.Add(baseTypeName);

                    // Check if this base type is protected
                    if (TypeReplacementMap.ProtectedBaseClasses.Contains(baseTypeName)) {
                        return true;
                    }

                    // Move to next base type
                    currentBase = currentBase.BaseType;
                }

                return false;
            }
            catch {
                // If semantic analysis fails, return false (will use syntax fallback)
                return false;
            }
        }

        private bool IsProtectedClassSyntaxOnly(ClassDeclarationSyntax classDecl, string filePath, string fileContent) {
            // Comment Issues Only mode: treat ALL classes as protected
            if (_options.CommentIssuesOnly) {
                return true;
            }
            
            if (classDecl.BaseList == null || classDecl.BaseList.Types.Count == 0) {
                return false;
            }

            // Extract all base type names from the base list
            // Example: "class Foo : Bar, IBaz" → ["Bar", "IBaz"]
            foreach (var baseType in classDecl.BaseList.Types) {
                if (IsProtectedBaseType(baseType.Type, filePath, fileContent, visitedTypes: new HashSet<string>())) {
                    return true;
                }
            }
            
            // No protected base classes found
            return false;
        }

        private List<(string FilePath, string ClassName)> FindPartialClassParts(string className, string currentFilePath) {
            var parts = new List<(string, string)>();
            
            // Get the directory of the current file
            var directory = Path.GetDirectoryName(currentFilePath);
            if (string.IsNullOrEmpty(directory)) {
                return parts;
            }
            
            // Search for potential partial class files
            // Common patterns: ClassName.cs, ClassName.Designer.cs, ClassName.Generated.cs, etc.
            var potentialFiles = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase));
            
            foreach (var file in potentialFiles) {
                try {
                    var content = File.ReadAllText(file);
                    var tree = CSharpSyntaxTree.ParseText(content);
                    var root = tree.GetRoot() as CompilationUnitSyntax;
                    
                    if (root != null) {
                        // Find partial classes with the same name
                        var partialClasses = root.DescendantNodes()
                            .OfType<ClassDeclarationSyntax>()
                            .Where(c => c.Identifier.Text == className && 
                                       c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                            .Where(c => !IsClassInsideComment(c, content));
                        
                        foreach (var partialClass in partialClasses) {
                            parts.Add((file, className));
                        }
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine($"      [WARNING] Error checking file {file} for partial class: {ex.Message}");
                }
            }
            
            return parts;
        }

        /// <summary>
        /// Recursively check if a base type is protected or inherits from a protected type.
        /// Uses both direct name matching and transitive inheritance chain analysis.
        /// </summary>
        /// <param name="baseTypeSyntax">The base type syntax node</param>
        /// <param name="currentFilePath">Path to the file being analyzed (for finding base class definitions)</param>
        /// <param name="fileContent">Content of the current file</param>
        /// <param name="visitedTypes">Set of already visited type names to prevent infinite recursion</param>
        /// <returns>True if the type is protected or inherits from a protected type</returns>
        private bool IsProtectedBaseType(TypeSyntax baseTypeSyntax, string currentFilePath, string fileContent, HashSet<string> visitedTypes) {
            // Extract simple name without generic parameters
            string baseTypeName = ExtractSimpleTypeName(baseTypeSyntax);
            
            if (string.IsNullOrEmpty(baseTypeName)) {
                return false;
            }

            // Check for circular reference
            if (visitedTypes.Contains(baseTypeName)) {
                return false;
            }
            visitedTypes.Add(baseTypeName);

            // STEP 1: Direct check - is this type name in the protected list?
            if (TypeReplacementMap.ProtectedBaseClasses.Contains(baseTypeName)) {
                return true;
            }

            // STEP 2: Transitive check - find the base type definition and check its inheritance
            // Try to find the class definition in the same file first
            var baseClassDecl = FindClassDefinitionInFile(baseTypeName, fileContent);
            
            if (baseClassDecl != null) {
                // Found in same file - check its base classes recursively
                if (baseClassDecl.BaseList != null) {
                    foreach (var transitiveBase in baseClassDecl.BaseList.Types) {
                        if (IsProtectedBaseType(transitiveBase.Type, currentFilePath, fileContent, visitedTypes)) {
                            return true;
                        }
                    }
                }
            } else if (!string.IsNullOrEmpty(currentFilePath)) {
                // Not found in same file - try to find in other files in the same directory
                var directory = Path.GetDirectoryName(currentFilePath);
                if (!string.IsNullOrEmpty(directory)) {
                    var otherFiles = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly)
                        .Where(f => !f.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase));
                    
                    foreach (var file in otherFiles) {
                        try {
                            var content = File.ReadAllText(file);
                            var foundClass = FindClassDefinitionInFile(baseTypeName, content);
                            
                            if (foundClass != null && foundClass.BaseList != null) {
                                // Found the base class - check its inheritance recursively
                                foreach (var transitiveBase in foundClass.BaseList.Types) {
                                    if (IsProtectedBaseType(transitiveBase.Type, file, content, visitedTypes)) {
                                        return true;
                                    }
                                }
                                break; // Found the class, no need to search further
                            }
                        } catch {
                            // Ignore errors reading other files
                            continue;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Extract simple type name from TypeSyntax, handling generics and qualified names
        /// </summary>
        private string ExtractSimpleTypeName(TypeSyntax typeSyntax) {
            switch (typeSyntax) {
                case GenericNameSyntax genericName:
                    // Generic type: ViewController<T>
                    return genericName.Identifier.Text;
                
                case IdentifierNameSyntax identifierName:
                    // Simple type: ModuleBase
                    return identifierName.Identifier.Text;
                
                case QualifiedNameSyntax qualifiedName:
                    // Qualified type: DevExpress.ExpressApp.ModuleBase
                    // Extract the rightmost part
                    var rightName = qualifiedName.Right;
                    if (rightName is GenericNameSyntax rightGeneric) {
                        return rightGeneric.Identifier.Text;
                    }
                    return rightName.Identifier.Text;
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// Find a class definition by name in the given file content
        /// </summary>
        private ClassDeclarationSyntax FindClassDefinitionInFile(string className, string fileContent) {
            try {
                var syntaxTree = CSharpSyntaxTree.ParseText(fileContent);
                var root = syntaxTree.GetRoot();

                return root.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .Where(c => c.Identifier.Text == className)
                    .Where(c => !IsClassInsideComment(c, fileContent))
                    .FirstOrDefault();
            } catch {
                return null;
            }
        }
        
        /// <summary>
        /// Build comment header for class
        /// </summary>
        private string BuildClassComment(ProblematicClass problematicClass) {
            var sb = new StringBuilder();
            sb.AppendLine(GetTodoClassCommentedComment(problematicClass.ClassName));
            sb.AppendLine("// NOTE:");
            
            var reasons = problematicClass.Problems
                .Where(p => p.RequiresCommentOut)
                .Distinct();
            
            foreach (var problem in reasons) {
                // Add the main reason
                sb.AppendLine($"//   - {problem.Reason}");
                
                // NEW: Add detailed description if available
                if (!string.IsNullOrEmpty(problem.Description)) {
                    sb.AppendLine($"//     {problem.Description}");
                }
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Build commented class text from original class
        /// </summary>
        private string BuildCommentedClassText(string commentHeader, string classText, SyntaxTriviaList leadingTrivia) {
            var sb = new StringBuilder();
            
            // Extract base indentation from the leading trivia
            var baseIndent = string.Empty;
            var lastWhitespace = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
            if (lastWhitespace != default(SyntaxTrivia)) {
                // Convert tabs to spaces (tab = 4 spaces) for consistent formatting
                baseIndent = lastWhitespace.ToString().Replace("\t", "    ");
            }

            // Add comment header
            foreach (var line in commentHeader.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                sb.Append(baseIndent);
                sb.AppendLine(line);
            }
            
            // Add separator
            sb.Append(baseIndent);
            sb.AppendLine("// ========== COMMENTED OUT CLASS ==========");
            
            // Comment out each line of the class
            var lines = classText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++) {
                var line = lines[i];
                
                // Skip the last empty line if it exists
                if (i == lines.Length - 1 && string.IsNullOrWhiteSpace(line)) {
                    continue;
                }
                
                if (string.IsNullOrWhiteSpace(line)) {
                    sb.Append(baseIndent);
                    sb.AppendLine("//");
                } else {
                    sb.Append(baseIndent);
                    sb.Append("// ");
                    sb.AppendLine(line);
                }
            }
            
            // Add closing separator
            sb.Append(baseIndent);
            sb.AppendLine("// ========================================");
            
            return sb.ToString();
        }

        /// <summary>
        /// Get list of all commented classes
        /// </summary>
        public IReadOnlyCollection<string> GetCommentedClasses() => _commentedClasses;
    }
}
