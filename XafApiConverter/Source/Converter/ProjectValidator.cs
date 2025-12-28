using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Utility class for validating converted projects
    /// Implements verification checks from Convert_to_NET.md
    /// </summary>
    internal class ProjectValidator {
        /// <summary>
        /// Validate a converted SDK-style project
        /// </summary>
        public static ValidationResult Validate(string projectPath, ConversionConfig config = null) {
            config ??= ConversionConfig.Default;
            var result = new ValidationResult { ProjectPath = projectPath };

            if (!File.Exists(projectPath)) {
                result.AddError("Project file does not exist");
                return result;
            }

            try {
                var content = File.ReadAllText(projectPath);
                var doc = XDocument.Parse(content);

                // TRANS-001 Verification
                ValidateSdkStyle(doc, result);

                // TRANS-002 Verification
                ValidateTargetFramework(doc, result, config);

                // TRANS-004 Verification
                ValidateAssemblyReferences(doc, result);

                // TRANS-005 Verification
                ValidatePackageReferences(doc, result, config);

                // TRANS-007 Verification
                ValidateEmbeddedResources(doc, result);
            }
            catch (Exception ex) {
                result.AddError($"Validation error: {ex.Message}");
            }

            return result;
        }

        private static void ValidateSdkStyle(XDocument doc, ValidationResult result) {
            // Check first line: '<Project Sdk="Microsoft.NET.Sdk">'
            var project = doc.Root;
            if (project == null || project.Name.LocalName != "Project") {
                result.AddError("Root element is not <Project>");
                return;
            }

            var sdk = project.Attribute("Sdk")?.Value;
            if (string.IsNullOrEmpty(sdk)) {
                result.AddError("Missing Sdk attribute on <Project>");
            }
            else if (!sdk.Contains("Microsoft.NET.Sdk")) {
                result.AddWarning($"Unexpected Sdk value: {sdk}");
            }
            else {
                result.AddSuccess("SDK-style format verified");
            }

            // Check for Import statements (should be removed)
            var imports = doc.Descendants().Where(e => e.Name.LocalName == "Import").ToList();
            if (imports.Any()) {
                result.AddWarning($"Found {imports.Count} <Import> statements (should be removed in SDK-style)");
            }
        }

        private static void ValidateTargetFramework(XDocument doc, ValidationResult result, ConversionConfig config) {
            var targetFramework = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "TargetFramework")
                ?.Value;

            if (string.IsNullOrEmpty(targetFramework)) {
                result.AddError("Missing <TargetFramework> element");
                return;
            }

            var validFrameworks = new[] { config.TargetFramework, config.TargetFrameworkWindows };
            if (validFrameworks.Contains(targetFramework)) {
                result.AddSuccess($"Target framework: {targetFramework}");
            }
            else {
                result.AddWarning($"Unexpected target framework: {targetFramework}");
            }

            // Validate Windows properties
            var isWindowsFramework = targetFramework == config.TargetFrameworkWindows;
            var hasUseWindowsForms = doc.Descendants()
                .Any(e => e.Name.LocalName == "UseWindowsForms" && e.Value == "true");
            var hasImportWindowsDesktop = doc.Descendants()
                .Any(e => e.Name.LocalName == "ImportWindowsDesktopTargets" && e.Value == "true");

            if (isWindowsFramework) {
                if (!hasUseWindowsForms || !hasImportWindowsDesktop) {
                    result.AddWarning("Windows framework detected but missing Windows properties");
                }
            }
            else {
                if (hasUseWindowsForms || hasImportWindowsDesktop) {
                    result.AddWarning("Non-Windows framework but has Windows properties");
                }
            }
        }


        private static void ValidateAssemblyReferences(XDocument doc, ValidationResult result) {
            // TRANS-004: Check for DevExpress and System.Web assembly references (should be removed)
            var assemblyRefs = doc.Descendants()
                .Where(e => e.Name.LocalName == "Reference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => v != null)
                .ToList();

            var devExpressRefs = assemblyRefs.Where(r => r.StartsWith("DevExpress", StringComparison.OrdinalIgnoreCase)).ToList();
            var systemWebRefs = assemblyRefs.Where(r => r.StartsWith("System.Web", StringComparison.OrdinalIgnoreCase)).ToList();

            if (devExpressRefs.Any()) {
                result.AddError($"Found {devExpressRefs.Count} DevExpress assembly references (should be removed): {string.Join(", ", devExpressRefs.Take(3))}");
            }

            if (systemWebRefs.Any()) {
                result.AddError($"Found {systemWebRefs.Count} System.Web assembly references (should be removed): {string.Join(", ", systemWebRefs.Take(3))}");
            }

            if (!devExpressRefs.Any() && !systemWebRefs.Any()) {
                result.AddSuccess("No legacy assembly references found");
            }
        }

        private static void ValidatePackageReferences(XDocument doc, ValidationResult result, ConversionConfig config) {
            var packages = doc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => new {
                    Name = e.Attribute("Include")?.Value,
                    Version = e.Attribute("Version")?.Value
                })
                .Where(p => p.Name != null)
                .ToList();

            if (!packages.Any()) {
                result.AddWarning("No PackageReference elements found");
                return;
            }

            result.AddSuccess($"Found {packages.Count} package references");

            // Check for duplicates
            var duplicates = packages.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any()) {
                result.AddError($"Duplicate package references: {string.Join(", ", duplicates)}");
            }

            // Check DevExpress package versions
            var dxPackages = packages.Where(p => p.Name.StartsWith("DevExpress", StringComparison.OrdinalIgnoreCase)).ToList();
            var wrongVersionDxPackages = dxPackages.Where(p => p.Version != config.DxPackageVersion).ToList();
            
            if (wrongVersionDxPackages.Any()) {
                result.AddWarning($"DevExpress packages with incorrect version: {string.Join(", ", wrongVersionDxPackages.Select(p => $"{p.Name} ({p.Version})"))}");
            }

            // TRANS-008: Check for .Web packages (should be .Blazor)
            var webPackages = packages.Where(p => p.Name.Contains(".Web", StringComparison.OrdinalIgnoreCase) && 
                                                   p.Name.StartsWith("DevExpress", StringComparison.OrdinalIgnoreCase))
                                      .ToList();
            
            if (webPackages.Any()) {
                result.AddWarning($"Found .Web packages (should be .Blazor): {string.Join(", ", webPackages.Select(p => p.Name))}");
            }
        }

        private static void ValidateEmbeddedResources(XDocument doc, ValidationResult result) {
            // TRANS-007: Check for .resx files with DependentUpon (should be removed)
            var embeddedResources = doc.Descendants()
                .Where(e => e.Name.LocalName == "EmbeddedResource")
                .ToList();

            var resxWithDependentUpon = embeddedResources
                .Where(e => {
                    var include = e.Attribute("Include")?.Value;
                    if (string.IsNullOrEmpty(include) || !include.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return e.Descendants().Any(d => d.Name.LocalName == "DependentUpon");
                })
                .ToList();

            if (resxWithDependentUpon.Any()) {
                result.AddWarning($"Found {resxWithDependentUpon.Count} .resx files with DependentUpon (should be removed for SDK-style auto-inclusion)");
            }
        }
    }

    internal class ValidationResult {
        public string ProjectPath { get; set; }
        public List<ValidationMessage> Messages { get; } = new List<ValidationMessage>();
        
        public bool HasErrors => Messages.Any(m => m.Level == ValidationLevel.Error);
        public bool IsValid => !HasErrors;

        public void AddError(string message) {
            Messages.Add(new ValidationMessage(ValidationLevel.Error, message));
        }

        public void AddWarning(string message) {
            Messages.Add(new ValidationMessage(ValidationLevel.Warning, message));
        }

        public void AddSuccess(string message) {
            Messages.Add(new ValidationMessage(ValidationLevel.Success, message));
        }

        public void PrintResults() {
            Console.WriteLine($"\nValidation Results for: {Path.GetFileName(ProjectPath)}");
            Console.WriteLine(new string('-', 60));

            foreach (var message in Messages) {
                var icon = message.Level switch {
                    ValidationLevel.Error => "?",
                    ValidationLevel.Warning => "?",
                    ValidationLevel.Success => "?",
                    _ => " "
                };

                var color = message.Level switch {
                    ValidationLevel.Error => ConsoleColor.Red,
                    ValidationLevel.Warning => ConsoleColor.Yellow,
                    ValidationLevel.Success => ConsoleColor.Green,
                    _ => ConsoleColor.White
                };

                Console.ForegroundColor = color;
                Console.WriteLine($"{icon} {message.Message}");
                Console.ResetColor();
            }

            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"Result: {(IsValid ? "PASSED" : "FAILED")}");
        }
    }

    internal enum ValidationLevel {
        Success,
        Warning,
        Error
    }

    internal class ValidationMessage {
        public ValidationLevel Level { get; }
        public string Message { get; }

        public ValidationMessage(ValidationLevel level, string message) {
            Level = level;
            Message = message;
        }
    }
}
