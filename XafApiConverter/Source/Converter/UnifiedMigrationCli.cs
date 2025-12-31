using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Unified options for XAF migration workflow
    /// Supports project conversion, security updates, and type migration
    /// </summary>
    public class MigrationOptions {
        public string SolutionPath { get; set; }
        public string TargetFramework { get; set; }
        public string DxVersion { get; set; }
        public string OutputPath { get; set; }

        // Step selection - user must explicitly specify which steps to execute
        public bool ExecuteSecurityUpdate { get; set; }
        public bool ExecuteTypeMigration { get; set; }
        public bool ExecuteProjectConversion { get; set; }

        // Type migration specific options
        public bool CommentIssuesOnly { get; set; }
        public bool ShowMappings { get; set; }

        // Other options
        public bool CreateBackup { get; set; } = false;
        public bool UseDirectoryPackages { get; set; }
    }

    public class UnifiedMigrationCli {
        /// <summary>
        /// Run unified migration from command line
        /// </summary>
        public static int Run(string[] args) {
            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h")) {
                PrintHelp();
                return 0;
            }

            try {
                var options = ParseArguments(args);
                
                // Show mappings mode
                if (options.ShowMappings) {
                    ShowMappings();
                    return 0;
                }
                
                if (!ValidateOptions(options)) {
                    return 1;
                }

                // Check if at least one step is specified
                if (!options.ExecuteSecurityUpdate && 
                    !options.ExecuteTypeMigration && 
                    !options.ExecuteProjectConversion) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Error: No migration steps specified.");
                    Console.WriteLine();
                    Console.WriteLine("You must explicitly specify which steps to execute:");
                    Console.WriteLine("  security-update      - Update Security types (SecuritySystem* -> PermissionPolicy*)");
                    Console.WriteLine("  migrate-types        - Migrate types (Web -> Blazor)");
                    Console.WriteLine("  project-conversion   - Convert projects (.NET Framework -> .NET)");
                    Console.WriteLine();
                    Console.WriteLine("Examples:");
                    Console.WriteLine("  XafApiConverter.exe MySolution.sln migrate-types");
                    Console.WriteLine("  XafApiConverter.exe MySolution.sln security-update migrate-types project-conversion");
                    Console.WriteLine();
                    Console.WriteLine("Run with --help for more information");
                    Console.ResetColor();
                    return 1;
                }

                return ExecuteMigration(options);
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"\nStack trace:\n{ex.StackTrace}");
                Console.ResetColor();
                return 1;
            }
        }

        private static MigrationOptions ParseArguments(string[] args) {
            var options = new MigrationOptions();

            for (int i = 0; i < args.Length; i++) {
                var arg = args[i];

                switch (arg.ToLowerInvariant()) {
                    // Solution/Directory path
                    case "--solution":
                    case "-s":
                        if (i + 1 < args.Length) {
                            options.SolutionPath = args[++i];
                        }
                        break;

                    // Target framework
                    case "--target-framework":
                    case "-tf":
                        if (i + 1 < args.Length) {
                            options.TargetFramework = args[++i];
                        }
                        break;

                    // DevExpress version
                    case "--dx-version":
                    case "-dx":
                        if (i + 1 < args.Length) {
                            options.DxVersion = args[++i];
                        }
                        break;

                    // Output path for reports
                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length) {
                            options.OutputPath = args[++i];
                        }
                        break;

                    // Step selection - explicit commands
                    case "security-update":
                        options.ExecuteSecurityUpdate = true;
                        break;

                    case "migrate-types":
                        options.ExecuteTypeMigration = true;
                        break;

                    case "project-conversion":
                        options.ExecuteProjectConversion = true;
                        break;

                    // Type migration specific options
                    case "--comment-issues-only":
                    case "-c":
                        options.CommentIssuesOnly = true;
                        break;

                    case "--show-mappings":
                    case "-m":
                        options.ShowMappings = true;
                        break;

                    // Other options
                    case "--backup":
                    case "-b":
                        options.CreateBackup = true;
                        break;

                    case "--directory-packages":
                    case "-dp":
                        options.UseDirectoryPackages = true;
                        break;

                    default:
                        if (!arg.StartsWith("-") && string.IsNullOrEmpty(options.SolutionPath)) {
                            options.SolutionPath = arg;
                        }
                        break;
                }
            }

            return options;
        }

        private static bool ValidateOptions(MigrationOptions options) {
            if (string.IsNullOrEmpty(options.SolutionPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Solution or directory path is required");
                Console.ResetColor();
                Console.WriteLine("\nUsage: XafApiConverter <path> <step1> [step2] [step3] [options]");
                Console.WriteLine("Run with --help for more information");
                return false;
            }

            if (!File.Exists(options.SolutionPath) && !Directory.Exists(options.SolutionPath)) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Path not found: {options.SolutionPath}");
                Console.ResetColor();
                return false;
            }

            return true;
        }

        private static void ShowMappings() {
            Console.WriteLine("===============================================");
            Console.WriteLine("  Type and Namespace Replacement Mappings");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            // Namespace mappings
            Console.WriteLine("## Namespace Replacements (TRANS-006, TRANS-007)");
            Console.WriteLine();
            foreach (var ns in TypeReplacementMap.NamespaceReplacements.Values) {
                Console.WriteLine($"  {ns.OldNamespace}");
                Console.WriteLine($"  -> {ns.NewNamespace}");
                Console.WriteLine($"  ({ns.Description})");
                Console.WriteLine();
            }

            // Type mappings
            Console.WriteLine("## Type Replacements (TRANS-008)");
            Console.WriteLine();
            foreach (var type in TypeReplacementMap.TypeReplacements.Values) {
                Console.WriteLine($"  {type.OldType}");
                Console.WriteLine($"  -> {type.NewType}");
                Console.WriteLine($"  ({type.Description})");
                Console.WriteLine();
            }

            // NO_EQUIVALENT namespaces
            Console.WriteLine("## No Equivalent Namespaces (Require Commenting Out)");
            Console.WriteLine();
            foreach (var ns in TypeReplacementMap.NoEquivalentNamespaces.Values) {
                Console.WriteLine($"  [X] {ns.OldNamespace}");
                Console.WriteLine($"  ({ns.Description})");
                Console.WriteLine();
            }

            // NO_EQUIVALENT types
            Console.WriteLine("## No Equivalent Types (Require Commenting Out)");
            Console.WriteLine();
            foreach (var type in TypeReplacementMap.NoEquivalentTypes.Values) {
                Console.WriteLine($"  [X] {type.OldType}");
                Console.WriteLine($"  ({type.Description})");
                if (type.CommentOutEntireClass) {
                    Console.WriteLine($"  [WARNING] Requires commenting out ENTIRE CLASS");
                }
                Console.WriteLine();
            }

            // Problematic enums
            Console.WriteLine("## Problematic Enums (TRANS-009)");
            Console.WriteLine();
            foreach (var enumEntry in TypeReplacementMap.ProblematicEnums.Values) {
                Console.WriteLine($"  [!] {enumEntry.EnumName}");
                Console.WriteLine($"  Values: {string.Join(", ", enumEntry.ProblematicValues)}");
                Console.WriteLine($"  ({enumEntry.Description})");
                Console.WriteLine();
            }
        }

        private static int ExecuteMigration(MigrationOptions options) {
            PrintHeader();
            PrintOptions(options);

            var solutions = DiscoverSolutions(options.SolutionPath);
            if (!solutions.Any()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No solution files found.");
                Console.ResetColor();
                return 1;
            }

            Console.WriteLine($"\nFound {solutions.Count} solution(s):");
            foreach (var sln in solutions) {
                Console.WriteLine($"  - {Path.GetFileName(sln)}");
            }
            Console.WriteLine();

            int totalErrors = 0;

            foreach (var solutionPath in solutions) {
                Console.WriteLine("===============================================================");
                Console.WriteLine($"Processing: {Path.GetFileName(solutionPath)}");
                Console.WriteLine("===============================================================");
                Console.WriteLine();

                try {
                    int stepNumber = 1;
                    int totalSteps = 
                        (options.ExecuteSecurityUpdate ? 1 : 0) + 
                        (options.ExecuteTypeMigration ? 1 : 0) + 
                        (options.ExecuteProjectConversion ? 1 : 0);

                    // Step 1: Security Types Update
                    if (options.ExecuteSecurityUpdate) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($">> Step {stepNumber}/{totalSteps}: Security Types Update");
                        Console.ResetColor();
                        Console.WriteLine();

                        var securityResult = RunSecurityUpdate(solutionPath, options);
                        if (securityResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[WARNING] Step {stepNumber} completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[OK] Step {stepNumber} completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                        stepNumber++;
                    }

                    TypeMigrationTool typeMigrationTool = null;
                    // Step 2: Type Migration (Web -> Blazor)
                    if (options.ExecuteTypeMigration) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($">> Step {stepNumber}/{totalSteps}: Type Migration (Web -> Blazor)");
                        Console.ResetColor();
                        Console.WriteLine();

                        if(typeMigrationTool == null) {
                            typeMigrationTool = new TypeMigrationTool(solutionPath, options);
                            typeMigrationTool.Initialize();
                        }

                        int resultState = 0;
                        try {
                            resultState = typeMigrationTool.CommentOutProblematicClasses().ProblematicClasses.Any() ? 1 : 0;
                        } catch(Exception ex) {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Type migration failed: {ex.Message}");
                            Console.ResetColor();
                            resultState = 1;
                        }

                        if (resultState != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[WARNING] Step {stepNumber} completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[OK] Step {stepNumber} completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                        stepNumber++;
                    }

                    // Step 3: Project Conversion (.NET Framework -> .NET)
                    if (options.ExecuteProjectConversion) {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($">> Step {stepNumber}/{totalSteps}: Project Conversion (.NET Framework -> .NET)");
                        Console.ResetColor();
                        Console.WriteLine();

                        var conversionResult = RunProjectConversion(solutionPath, options);

                        if(typeMigrationTool == null) {
                            typeMigrationTool = new TypeMigrationTool(solutionPath, options);
                            typeMigrationTool.Initialize();
                        }
                        typeMigrationTool.ApplyingAutomaticReplacements();

                        if (conversionResult != 0) {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"[WARNING] Step {stepNumber} completed with warnings");
                            Console.ResetColor();
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"[OK] Step {stepNumber} completed successfully");
                            Console.ResetColor();
                        }
                        Console.WriteLine();
                    }
                    // Save type migration report if applicable
                    if(typeMigrationTool != null) {
                        var report = typeMigrationTool.GetReport();
                        if (report.ProblematicClasses.Any()) {
                            PrintNextSteps(report);
                        }
                        var reportPath = options.OutputPath ?? Path.Combine(
                            Path.GetDirectoryName(solutionPath),
                            "type-migration-report.md");
                        report.SaveToFile(reportPath);
                    }
                }
                catch (Exception ex) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[ERROR] Error processing solution: {ex.Message}");
                    Console.ResetColor();
                    totalErrors++;
                }
            }

            // Final summary
            PrintFinalSummary(solutions.Count, totalErrors);

            return totalErrors > 0 ? 1 : 0;
        }

        private static List<string> DiscoverSolutions(string path) {
            var solutions = new List<string>();

            if (File.Exists(path) && (path.EndsWith(".sln") || path.EndsWith(".slnx"))) {
                solutions.Add(path);
            }
            else if (Directory.Exists(path)) {
                solutions.AddRange(Directory.GetFiles(path, "*.sln", SearchOption.AllDirectories));
                solutions.AddRange(Directory.GetFiles(path, "*.slnx", SearchOption.AllDirectories));
            }

            return solutions;
        }

        private static int RunProjectConversion(string solutionPath, MigrationOptions options) {
            try {
                // Parse solution to get actual projects
                var solutionDir = Path.GetDirectoryName(solutionPath);
                var projectFiles = ParseSolutionForProjects(solutionPath);

                if (!projectFiles.Any()) {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No projects found in solution");
                    Console.ResetColor();
                    return 0;
                }

                Console.WriteLine($"Found {projectFiles.Count} project(s) in solution");
                Console.WriteLine();

                // Create conversion config
                var config = CreateConversionConfig(options);
                var converter = new CSprojConverter(config);

                int converted = 0;
                int skipped = 0;
                int failed = 0;

                foreach (var projectPath in projectFiles) {
                    try {
                        var projectName = Path.GetFileNameWithoutExtension(projectPath);
                        Console.Write($"  Converting {projectName}...");

                        converter.ConvertProject(projectPath, options.CreateBackup);

                        // Validate
                        var validation = ProjectValidator.Validate(projectPath, config);
                        if (validation.IsValid) {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(" [OK]");
                            Console.ResetColor();
                            converted++;
                        }
                        else {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(" [WARN]");
                            Console.ResetColor();
                            skipped++;
                        }
                    }
                    catch (Exception ex) {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($" [ERROR] ({ex.Message})");
                        Console.ResetColor();
                        failed++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Summary: {converted} converted, {skipped} skipped, {failed} failed");

                return failed > 0 ? 1 : 0;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Project conversion failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        /// <summary>
        /// Parse solution file to get list of projects
        /// </summary>
        private static List<string> ParseSolutionForProjects(string solutionPath) {
            var projects = new List<string>();
            var solutionDir = Path.GetDirectoryName(solutionPath);

            try {
                var solutionContent = File.ReadAllText(solutionPath);
                
                // Parse .sln file for project entries
                // Format: Project("{...}") = "ProjectName", "Path\To\Project.csproj", "{...}"
                var projectPattern = @"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""";
                var matches = System.Text.RegularExpressions.Regex.Matches(solutionContent, projectPattern);

                foreach (System.Text.RegularExpressions.Match match in matches) {
                    if (match.Groups.Count > 1) {
                        var projectRelativePath = match.Groups[1].Value;
                        // Convert to absolute path
                        var projectAbsolutePath = Path.GetFullPath(Path.Combine(solutionDir, projectRelativePath));
                        
                        if (File.Exists(projectAbsolutePath)) {
                            projects.Add(projectAbsolutePath);
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Warning: Could not parse solution file: {ex.Message}");
                Console.WriteLine("Falling back to directory search...");
                Console.ResetColor();
                
                // Fallback: search for .csproj files in solution directory
                projects.AddRange(Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories));
            }

            return projects;
        }

        private static void PrintNextSteps(MigrationReport report) {
            Console.WriteLine("===============================================");
            Console.WriteLine("  Next Steps");
            Console.WriteLine("===============================================");
            Console.WriteLine();

            if (report.ProblematicClasses.Any()) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[!] Manual Review Required:");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("1. Analyze problematic classes:");
                Console.WriteLine("   - Classes marked [COMMENTED] - fully commented out");
                Console.WriteLine("   - Classes marked [WARNING] - require manual fixes");
                Console.WriteLine("2. Build project: dotnet build");
                Console.WriteLine("3. Fix remaining compilation errors");
                Console.WriteLine("4. Test application functionality");
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK] Automatic Changes:");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine($"  * {report.NamespacesReplaced} namespaces migrated");
            Console.WriteLine($"  * {report.TypesReplaced} types replaced");
            Console.WriteLine($"  * {report.FilesProcessed} files processed");
            Console.WriteLine();

            if (!report.BuildSuccessful) {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[BUILD] Next Iteration:");
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("1. Build the project: dotnet build");
                Console.WriteLine("2. Review build errors");
                Console.WriteLine();
            }
        }

        private static int RunSecurityUpdate(string solutionPath, MigrationOptions options) {
            try {
                Console.WriteLine("Processing security types...");

                var result = SecurityTypesUpdater.ProcessSolution(solutionPath);

                Console.WriteLine($"Security types updated: {result.FilesChanged} file(s) changed");

                return 0;
            }
            catch (Exception ex) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Security update failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static ConversionConfig CreateConversionConfig(MigrationOptions options) {
            var config = ConversionConfig.Default;

            if (!string.IsNullOrEmpty(options.TargetFramework)) {
                config.TargetFramework = options.TargetFramework;
                config.TargetFrameworkWindows = $"{options.TargetFramework}-windows";
            }

            if (!string.IsNullOrEmpty(options.DxVersion)) {
                config.DxPackageVersion = options.DxVersion;
                var parts = options.DxVersion.Split('.');
                if (parts.Length >= 2) {
                    config.DxAssemblyVersion = $"v{parts[0]}.{parts[1]}";
                }
            }

            config.UseDirectoryPackages = options.UseDirectoryPackages;

            return config;
        }

        private static void PrintHeader() {
            Console.WriteLine("===============================================================");
            Console.WriteLine("     XAF Migration Tool");
            Console.WriteLine("     .NET Framework -> .NET");
            Console.WriteLine("===============================================================");
            Console.WriteLine();
        }

        private static void PrintOptions(MigrationOptions options) {
            Console.WriteLine("Configuration:");
            Console.WriteLine($"  Path: {options.SolutionPath}");
            if (!string.IsNullOrEmpty(options.TargetFramework)) {
                Console.WriteLine($"  Target Framework: {options.TargetFramework}");
            }
            if (!string.IsNullOrEmpty(options.DxVersion)) {
                Console.WriteLine($"  DevExpress Version: {options.DxVersion}");
            }

            Console.WriteLine();
            Console.WriteLine("Steps to execute:");
            Console.WriteLine($"  {(options.ExecuteSecurityUpdate ? "[X]" : "[ ]")} Security Types Update");
            Console.WriteLine($"  {(options.ExecuteTypeMigration ? "[X]" : "[ ]")} Type Migration (Web -> Blazor)");
            Console.WriteLine($"  {(options.ExecuteProjectConversion ? "[X]" : "[ ]")} Project Conversion (.NET Framework -> .NET)");
            Console.WriteLine();
        }

        private static void PrintFinalSummary(int totalSolutions, int errors) {
            Console.WriteLine("===============================================================");
            Console.WriteLine("                    Final Summary");
            Console.WriteLine("===============================================================");
            Console.WriteLine();
            Console.WriteLine($"Solutions processed: {totalSolutions}");

            if (errors == 0) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[OK] All migrations completed successfully!");
                Console.ResetColor();
            }
            else {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] Completed with {errors} error(s)");
                Console.WriteLine("Review the output above for details");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        private static void PrintHelp() {
            Console.WriteLine(@"
XAF Migration Tool
============================================

Each step requires manual review before proceeding to the next.

Usage:
  XafApiConverter <path> <step1> [step2] [step3] [options]

Arguments:
  <path>                    Path to solution file (.sln) or directory

Migration Steps (execute in order):
  security-update           Update Security types (SecuritySystem* -> PermissionPolicy*)
  migrate-types             Migrate types (Web -> Blazor namespaces and types)
  project-conversion        Convert projects (.NET Framework -> .NET SDK-style)

IMPORTANT: You must explicitly specify which steps to execute!
           After each step, review changes before proceeding to next step.

Examples:
  XafApiConverter.exe MySolution.sln migrate-types
  XafApiConverter.exe MySolution.sln security-update migrate-types project-conversion

Common Options:
  -s, --solution <path>     Solution file or directory path
  -tf, --target-framework   Target .NET version (default: net9.0)
                            Examples: net8.0, net9.0, net10.0
  -dx, --dx-version         DevExpress version (default: 25.1.6)
                            Example: 25.2.3, 26.1.6
  -o, --output <path>       Output directory for reports
  -b, --backup              Create backup files
  -dp, --directory-packages Use Directory.Packages.props

Type Migration Options:
  -c, --comment-issues-only Add warning comments to ALL problematic classes 
                            without commenting them out
                            (Useful for manual review mode)
  -m, --show-mappings       Show all type and namespace mappings and exit

");
        }
    }

    /// <summary>
    /// Result from SecurityTypesUpdater
    /// </summary>
    public class SecurityUpdateResult {
        public int FilesChanged { get; set; }
        public List<string> ChangedFiles { get; set; } = new();
        public bool Success { get; set; }
    }
}
