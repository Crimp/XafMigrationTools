using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XafApiConverter.Converter.CodeAnalysis;

namespace XafApiConverter.Converter {
    internal class MigrationReport {
        public string SolutionPath { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.Now;
        
        // Statistics
        public int NamespacesReplaced { get; set; }
        public int TypesReplaced { get; set; }
        public int FilesProcessed { get; set; }
        public int XafmlFilesProcessed { get; set; }
        
        // Problems detected
        public List<ProblematicClass> ProblematicClasses { get; set; } = new();
        public List<XafmlProblem> XafmlProblems { get; set; } = new();
        
        // Build results
        public bool BuildSuccessful { get; set; }
        public List<FixableError> FixableErrors { get; set; } = new();
        public List<UnfixableError> UnfixableErrors { get; set; } = new();
        
        // Dependency analysis
        public Dictionary<string, List<string>> ClassDependencies { get; set; } = new();

        // NEW: Automatic commenting results
        public int ClassesCommented { get; set; }
        public List<string> CommentedClassNames { get; set; } = new();

        /// <summary>
        /// Generate markdown report for LLM
        /// </summary>
        public string ToMarkdown() {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("# Type Migration Report");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Solution:** {Path.GetFileName(SolutionPath)}");
            sb.AppendLine();

            // Executive Summary
            sb.AppendLine("## Executive Summary");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value |");
            sb.AppendLine("|--------|-------|");
            sb.AppendLine($"| Files Processed | {FilesProcessed} |");
            sb.AppendLine($"| XAFML Files Processed | {XafmlFilesProcessed} |");
            sb.AppendLine($"| Namespaces Replaced | {NamespacesReplaced} |");
            sb.AppendLine($"| Types Replaced | {TypesReplaced} |");
            sb.AppendLine($"| Build Status | {(BuildSuccessful ? "✅ Success" : "❌ Failed")} |");
            sb.AppendLine($"| Problematic Classes | {ProblematicClasses.Count} |");
            sb.AppendLine($"| XAFML Problems | {XafmlProblems.Count} |");
            sb.AppendLine($"| Fixable Errors | {FixableErrors.Count} |");
            sb.AppendLine($"| Unfixable Errors | {UnfixableErrors.Count} |");
            sb.AppendLine($"| Classes Commented Out | {ClassesCommented} |");
            sb.AppendLine();

            // Automatic Changes
            sb.AppendLine("## ✅ Automatic Changes Applied");
            sb.AppendLine();
            sb.AppendLine($"The migration tool has automatically applied {NamespacesReplaced} namespace replacements ");
            sb.AppendLine($"and {TypesReplaced} type replacements across {FilesProcessed} files.");
            sb.AppendLine();
            sb.AppendLine("**Namespace Migrations:**");
            sb.AppendLine("- `System.Data.SqlClient` → `Microsoft.Data.SqlClient`");
            sb.AppendLine("- `DevExpress.ExpressApp.Web.*` → `DevExpress.ExpressApp.Blazor.*`");
            sb.AppendLine();
            sb.AppendLine("**Type Replacements:**");
            sb.AppendLine("- `WebApplication` → `BlazorApplication`");
            //sb.AppendLine("- `ASPxGridListEditor` → `DxGridListEditor`");
            //sb.AppendLine("- `ASPxLookupPropertyEditor` → `LookupPropertyEditor`");
            sb.AppendLine("- `*AspNetModule` → `*BlazorModule`");
            sb.AppendLine();

            // NEW: Automatic Commenting
            if (ClassesCommented > 0) {
                sb.AppendLine("## 🤖 Automatic Actions Taken");
                sb.AppendLine();
                sb.AppendLine($"The tool automatically commented out **{ClassesCommented} classes** that use types ");
                sb.AppendLine("with no XAF for .NET equivalents (TRANS-010 lightweight implementation).");
                sb.AppendLine();
                sb.AppendLine("**Commented Classes:**");
                foreach (var className in CommentedClassNames.OrderBy(c => c)) {
                    sb.AppendLine($"- `{className}`");
                }
                sb.AppendLine();
                sb.AppendLine("**Format Used:**");
                sb.AppendLine("```csharp");
                sb.AppendLine($"{ClassCommenter.GetTodoClassCommentedComment("ClassName")}");
                sb.AppendLine("// NOTE: Class commented out due to types having no XAF for .NET equivalent");
                sb.AppendLine("//   - [Reason for each problematic type]");
                sb.AppendLine("/*");
                sb.AppendLine("public class ClassName { ... }");
                sb.AppendLine("*/");
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("**Next Steps:**");
                sb.AppendLine("1. Review each commented class");
                sb.AppendLine("2. Determine if functionality is critical");
                sb.AppendLine("3. Options:");
                sb.AppendLine("   - Remove commented code if not needed");
                sb.AppendLine("   - Find alternative Blazor implementation");
                sb.AppendLine("   - Implement custom solution");
                sb.AppendLine();
            }

            // Problematic Classes
            if (ProblematicClasses.Any()) {
                var noEquivalentClasses = ProblematicClasses
                    .Where(c => c.Problems.Any(p => p.RequiresCommentOut))
                    .ToList();

                var manualConversionClasses = ProblematicClasses
                    .Where(c => c.Problems.Any(p => !p.RequiresCommentOut))
                    .ToList();

                if (noEquivalentClasses.Any()) {
                    sb.AppendLine("## ⚠️ Classes with Types Having no XAF for .NET Equivalent");
                    sb.AppendLine();
                    sb.AppendLine($"Found **{noEquivalentClasses.Count} classes** that use types with no XAF for .NET equivalent.");
                    sb.AppendLine("These require commenting out or complete refactoring:");
                    sb.AppendLine();

                    foreach (var problematicClass in noEquivalentClasses.OrderByDescending(c => c.Problems.Count(p => p.RequiresCommentOut))) {
                        sb.AppendLine($"### Class: `{problematicClass.ClassName}`");
                        sb.AppendLine();
                        sb.AppendLine($"**File:** `{Path.GetFileName(problematicClass.FilePath)}`");
                        sb.AppendLine();
                        sb.AppendLine("**Problems:**");
                        foreach (var problem in problematicClass.Problems.Where(p => p.RequiresCommentOut)) {
                            var severity = problem.Severity switch {
                                ProblemSeverity.Critical => "🔴 CRITICAL",
                                ProblemSeverity.High => "🟠 HIGH",
                                ProblemSeverity.Medium => "🟡 MEDIUM",
                                _ => "🟢 LOW"
                            };
                            sb.AppendLine($"- {severity}: {problem.Reason}");
                            sb.AppendLine($"  - Type: `{problem.FullTypeName}`");
                            sb.AppendLine($"  - **Action Required:** Comment out entire class");
                        }
                        sb.AppendLine();

                        if (problematicClass.DependentClasses.Any()) {
                            sb.AppendLine("**⚠️ Dependent Classes (will also need to be commented out):**");
                            foreach (var dependent in problematicClass.DependentClasses) {
                                sb.AppendLine($"- `{dependent}`");
                            }
                            sb.AppendLine();
                        }

                        sb.AppendLine("**Suggested Actions:**");
                        sb.AppendLine("1. Review class functionality and business logic");
                        sb.AppendLine("2. Determine if functionality is critical or optional");
                        sb.AppendLine("3. Options:");
                        sb.AppendLine("   - Comment out class if functionality is optional");
                        sb.AppendLine("   - Find alternative Blazor implementation if critical");
                        sb.AppendLine("   - Consult XAF Blazor documentation for equivalents");
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();
                    }
                }

                if (manualConversionClasses.Any()) {
                    sb.AppendLine("## 🔧 Classes with Types Having XAF .NET Equivalents (Manual Conversion Required)");
                    sb.AppendLine();
                    sb.AppendLine($"Found **{manualConversionClasses.Count} classes** that use types with XAF for .NET equivalents ");
                    sb.AppendLine("but automatic conversion is not possible. These require manual refactoring:");
                    sb.AppendLine();

                    foreach (var problematicClass in manualConversionClasses.OrderByDescending(c => c.Problems.Count(p => !p.RequiresCommentOut))) {
                        sb.AppendLine($"### Class: `{problematicClass.ClassName}`");
                        sb.AppendLine();
                        sb.AppendLine($"**File:** `{Path.GetFileName(problematicClass.FilePath)}`");
                        sb.AppendLine();
                        sb.AppendLine("**Manual Conversion Required:**");
                        foreach (var problem in problematicClass.Problems.Where(p => !p.RequiresCommentOut)) {
                            var severity = problem.Severity switch {
                                ProblemSeverity.Critical => "🔴 CRITICAL",
                                ProblemSeverity.High => "🟠 HIGH",
                                ProblemSeverity.Medium => "🟡 MEDIUM",
                                _ => "🟢 LOW"
                            };
                            sb.AppendLine($"- {severity}: {problem.Reason}");
                            sb.AppendLine($"  - Old Type: `{problem.FullTypeName}`");
                        }
                        sb.AppendLine();

                        sb.AppendLine("**Suggested Actions:**");
                        sb.AppendLine("1. Review the Blazor equivalent type documentation");
                        sb.AppendLine("2. Manually refactor class to use the new Blazor type");
                        sb.AppendLine("3. Test thoroughly after conversion");
                        sb.AppendLine("4. Consider commenting out temporarily if conversion is complex");
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();
                    }
                }
            }

            // XAFML Problems
            if (XafmlProblems.Any()) {
                sb.AppendLine("## ⚠️ XAFML Files Requiring Attention");
                sb.AppendLine();
                sb.AppendLine($"Found **{XafmlProblems.Count} XAFML problems** in model files.");
                sb.AppendLine();

                var groupedByFile = XafmlProblems.GroupBy(p => p.FilePath);
                foreach (var fileGroup in groupedByFile) {
                    sb.AppendLine($"### File: `{Path.GetFileName(fileGroup.Key)}`");
                    sb.AppendLine();
                    foreach (var problem in fileGroup) {
                        sb.AppendLine($"- **Type:** `{problem.FullTypeName}`");
                        sb.AppendLine($"  - **Issue:** {problem.Reason}");
                        sb.AppendLine($"  - **Action:** Comment out XML elements using `<!-- ... -->`");
                    }
                    sb.AppendLine();
                }
            }

            // Build Errors
            if (!BuildSuccessful) {
                sb.AppendLine("## 🔧 Build Errors Analysis");
                sb.AppendLine();

                if (FixableErrors.Any()) {
                    sb.AppendLine($"### ✅ Fixable Errors ({FixableErrors.Count})");
                    sb.AppendLine();
                    sb.AppendLine("These errors can be fixed automatically or with simple changes:");
                    sb.AppendLine();

                    var groupedFixable = FixableErrors.GroupBy(e => e.Code);
                    foreach (var errorGroup in groupedFixable) {
                        sb.AppendLine($"#### Error {errorGroup.Key} ({errorGroup.Count()} occurrences)");
                        sb.AppendLine();
                        foreach (var error in errorGroup.Take(5)) {
                            sb.AppendLine($"- **File:** `{Path.GetFileName(error.FilePath)}:{error.Line}`");
                            sb.AppendLine($"  - **Message:** {error.Message}");
                            sb.AppendLine($"  - **Fix:** {error.SuggestedFix}");
                        }
                        if (errorGroup.Count() > 5) {
                            sb.AppendLine($"  - ... and {errorGroup.Count() - 5} more");
                        }
                        sb.AppendLine();
                    }
                }

                if (UnfixableErrors.Any()) {
                    sb.AppendLine($"### ❌ Unfixable Errors ({UnfixableErrors.Count})");
                    sb.AppendLine();
                    sb.AppendLine("These errors require manual intervention or commenting out code:");
                    sb.AppendLine();

                    var groupedUnfixable = UnfixableErrors.GroupBy(e => e.Code);
                    foreach (var errorGroup in groupedUnfixable) {
                        sb.AppendLine($"#### Error {errorGroup.Key} ({errorGroup.Count()} occurrences)");
                        sb.AppendLine();
                        foreach (var error in errorGroup.Take(5)) {
                            sb.AppendLine($"- **File:** `{Path.GetFileName(error.FilePath)}:{error.Line}`");
                            sb.AppendLine($"  - **Message:** {error.Message}");
                            sb.AppendLine($"  - **Reason:** {error.Reason}");
                        }
                        if (errorGroup.Count() > 5) {
                            sb.AppendLine($"  - ... and {errorGroup.Count() - 5} more");
                        }
                        sb.AppendLine();
                    }
                }
            }

            // Recommendations
            sb.AppendLine("## 📋 Next Steps for LLM");
            sb.AppendLine();
            sb.AppendLine("### Phase 1: Review Problematic Classes");
            if (ProblematicClasses.Any()) {
                sb.AppendLine($"1. Analyze {ProblematicClasses.Count} classes with NO_EQUIVALENT types");
                sb.AppendLine("2. For each class, determine:");
                sb.AppendLine("   - Is the functionality critical?");
                sb.AppendLine("   - Can it be rewritten for Blazor?");
                sb.AppendLine("   - Should it be commented out?");
                sb.AppendLine("3. Consider dependency cascade effects");
            }
            else {
                sb.AppendLine("✅ No problematic classes detected!");
            }
            sb.AppendLine();

            sb.AppendLine("### Phase 2: Fix Build Errors");
            if (FixableErrors.Any()) {
                sb.AppendLine($"1. Apply suggested fixes for {FixableErrors.Count} fixable errors");
            }
            if (UnfixableErrors.Any()) {
                sb.AppendLine($"2. Review and manually fix {UnfixableErrors.Count} unfixable errors");
            }
            if (BuildSuccessful) {
                sb.AppendLine("✅ Project builds successfully!");
            }
            sb.AppendLine();

            sb.AppendLine("### Phase 3: XAFML Updates");
            if (XafmlProblems.Any()) {
                sb.AppendLine($"1. Update {XafmlProblems.Count} XAFML problems");
                sb.AppendLine("2. Comment out elements with NO_EQUIVALENT types");
                sb.AppendLine("3. Test application behavior after XAFML changes");
            }
            else {
                sb.AppendLine("✅ No XAFML problems detected!");
            }
            sb.AppendLine();

            // Standard Comment Format
            sb.AppendLine("## 📝 Standard Comment Format");
            sb.AppendLine();
            sb.AppendLine("When commenting out code, use this format:");
            sb.AppendLine();
            sb.AppendLine("**For C# files:**");
            sb.AppendLine("```csharp");
            sb.AppendLine("// NOTE: [Type/Feature] has no .NET equivalent");
            sb.AppendLine($"{ClassCommenter.GetTodoClassCommentedComment("ClassName")}");
            sb.AppendLine("// [commented out code]");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("**For XAFML files:**");
            sb.AppendLine("```xml");
            sb.AppendLine("<!-- NOTE: [Type/Feature] has no .NET equivalent -->");
            sb.AppendLine("<!-- TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution. -->");
            sb.AppendLine("<!-- [commented out code] -->");
            sb.AppendLine("```");
            sb.AppendLine();

            // Footer
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("**Report generated by XafApiConverter TypeMigrationTool**");
            sb.AppendLine($"**Time:** {GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Save report to file
        /// </summary>
        public void SaveToFile(string filePath) {
            //TODO: Review report generation

            //var markdown = ToMarkdown();
            //File.WriteAllText(filePath, markdown, Encoding.UTF8);
            //Console.WriteLine($"  Report saved to: {filePath}");
        }

        /// <summary>
        /// Print brief summary to console
        /// </summary>
        public void PrintSummary() {
            Console.WriteLine();
            Console.WriteLine("===============================================");
            Console.WriteLine("      Type Migration Report Summary");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[OK] Automatic Changes:");
            Console.ResetColor();
            Console.WriteLine($"   - Namespaces replaced: {NamespacesReplaced}");
            Console.WriteLine($"   - Types replaced: {TypesReplaced}");
            Console.WriteLine($"   - Files processed: {FilesProcessed}");
            Console.WriteLine($"   - XAFML files: {XafmlFilesProcessed}");
            Console.WriteLine();

            if (ProblematicClasses.Any() || XafmlProblems.Any()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[!] Requires LLM Analysis:");
                Console.ResetColor();
                if (ProblematicClasses.Any()) {
                    Console.WriteLine($"   - Problematic classes: {ProblematicClasses.Count}");
                    var totalDependencies = ProblematicClasses.Sum(c => c.DependentClasses.Count);
                    if (totalDependencies > 0) {
                        Console.WriteLine($"   - Dependent classes: {totalDependencies}");
                    }
                }
                if (XafmlProblems.Any()) {
                    Console.WriteLine($"   - XAFML problems: {XafmlProblems.Count}");
                }
                Console.WriteLine();
            }

            if (!BuildSuccessful) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[BUILD] Build Errors:");
                Console.ResetColor();
                Console.WriteLine($"   - Fixable: {FixableErrors.Count}");
                Console.WriteLine($"   - Unfixable: {UnfixableErrors.Count}");
                Console.WriteLine();
            }
            else {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[OK] Project builds successfully!");
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.WriteLine("===============================================");
            Console.WriteLine();
        }
    }
}
