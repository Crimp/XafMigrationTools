using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Converts legacy .NET Framework .csproj files to SDK-style format
    /// Implements rules from Convert_to_NET.md
    /// </summary>
    internal class CSprojConverter {
        private readonly ConversionConfig _config;
        private readonly PackageManager _packageManager;

        public CSprojConverter(ConversionConfig config = null) {
            _config = config ?? ConversionConfig.Default;
            _packageManager = new PackageManager(_config);
        }

        ///// <summary>
        ///// Convert a Roslyn Project to SDK-style format
        ///// </summary>
        //public static void Convert(Project project) {
        //    var converter = new CSprojConverter();
        //    converter.ConvertProject(project.FilePath);
        //}

        /// <summary>
        /// Convert a project file by path
        /// </summary>
        public void ConvertProject(string projectPath, bool createBackup) {
            if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath)) {
                Console.WriteLine($"Project file not found: {projectPath}");
                return;
            }

            Console.WriteLine($"Converting project: {projectPath}");

            try {
                // Step 1: Read the original project file
                var originalContent = File.ReadAllText(projectPath);
                var projectDir = Path.GetDirectoryName(projectPath);

                // Step 2: Parse the project file
                var doc = XDocument.Parse(originalContent);

                // Step 3: Check if already SDK-style
                bool isSdkStyle = IsSdkStyleProject(originalContent);
                
                if (isSdkStyle) {
                    // SDK-style project - check if needs TargetFramework/package updates
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Project is already SDK-style, checking for updates...");
                    Console.ResetColor();
                    
                    bool needsUpdate = CheckIfNeedsUpdate(doc, projectPath);
                    
                    if (!needsUpdate) {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"✓ Project is already up-to-date: {projectPath}");
                        Console.ResetColor();
                        return;
                    }
                    
                    // Update existing SDK-style project
                    Console.WriteLine("  Updating TargetFramework and packages...");
                    UpdateSdkStyleProject(doc, projectPath, createBackup);
                    return;
                }

                // Step 4: Analyze project type (legacy .NET Framework project)
                var projectInfo = AnalyzeProject(doc, projectDir);

                // Step 4.5: Process AssemblyInfo.cs if it has Web-specific attributes
                if (projectInfo.HasManualAssemblyInfo && 
                    AssemblyInfoProcessor.HasWebSpecificAttributes(projectDir)) {
                    Console.WriteLine("  Processing AssemblyInfo.cs for Web-specific attributes...");
                    AssemblyInfoProcessor.ProcessAssemblyInfo(projectDir);
                }

                // Step 5: Create new SDK-style project
                var newDoc = CreateSdkStyleProject(doc, projectInfo, projectPath);

                var backupPath = projectPath + ".backup";
                // Step 6: Backup original file
                if (createBackup) {
                    File.Copy(projectPath, backupPath, true);
                    Console.WriteLine($"Backup created at: {backupPath}");
                }

                // Step 7: Save the new project file
                SaveProject(newDoc, projectPath);

                Console.WriteLine($"✓ Successfully converted: {projectPath}");
                if(createBackup) {
                    Console.WriteLine($"  Backup saved to: {backupPath}");
                }
                Console.WriteLine($"  Project type: {GetProjectTypeDescription(projectInfo)}");
            }
            catch (Exception ex) {
                Console.WriteLine($"✗ Error converting project {projectPath}: {ex.Message}");
                throw;
            }
        }

        private bool IsSdkStyleProject(string content) {
            return content.Contains("<Project Sdk=", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Check if SDK-style project needs TargetFramework or package updates.
        /// Returns true if:
        /// - TargetFramework is netstandard* (needs upgrade to net9.0/net9.0-windows)
        /// - DevExpress packages exist but have wrong version
        /// - Web packages need migration to Blazor
        /// </summary>
        private bool CheckIfNeedsUpdate(XDocument doc, string projectPath) {
            // Check TargetFramework
            var targetFramework = ExtractProperty(doc, "TargetFramework");
            var targetFrameworks = ExtractProperty(doc, "TargetFrameworks"); // multi-targeting
            
            var currentTfm = targetFramework ?? targetFrameworks?.Split(';').FirstOrDefault();
            
            if (!string.IsNullOrEmpty(currentTfm)) {
                // Check if it's netstandard or old .NET version
                if (currentTfm.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)) {
                    Console.WriteLine($"    Found TargetFramework: {currentTfm} (needs update to {_config.TargetFramework})");
                    return true;
                }
                
                // Check if it's old .NET Core/5/6/7/8 version (should upgrade to net9.0)
                if (currentTfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) ||
                    currentTfm == "net5.0" || currentTfm == "net6.0" || 
                    currentTfm == "net7.0" || currentTfm == "net8.0") {
                    Console.WriteLine($"    Found TargetFramework: {currentTfm} (needs update to {_config.TargetFramework})");
                    return true;
                }
            }
            
            // Check if DevExpress packages need update or migration
            var packages = doc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => new {
                    Name = e.Attribute("Include")?.Value,
                    Version = e.Attribute("Version")?.Value
                })
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .ToList();
            
            foreach (var package in packages) {
                // Check if Web package needs migration to Blazor
                if (TypeReplacementMap.TryGetPackageReplacement(package.Name, out var replacement) && 
                    replacement.HasEquivalent) {
                    Console.WriteLine($"    Found package that needs migration: {package.Name} → {replacement.NewPackage}");
                    return true;
                }
                
                // Check if package should be removed
                if (TypeReplacementMap.ShouldRemovePackage(package.Name)) {
                    Console.WriteLine($"    Found package that needs removal: {package.Name}");
                    return true;
                }
                
                // Check if DevExpress package version needs update
                if (package.Name.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(package.Version) &&
                    package.Version != _config.DxPackageVersion) {
                    Console.WriteLine($"    Found DevExpress package with old version: {package.Name} {package.Version} (needs {_config.DxPackageVersion})");
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Update existing SDK-style project (TargetFramework and packages)
        /// </summary>
        private void UpdateSdkStyleProject(XDocument doc, string projectPath, bool createBackup) {
            var projectDir = Path.GetDirectoryName(projectPath);
            var projectInfo = AnalyzeProject(doc, projectDir);
            
            bool modified = false;
            
            // Update TargetFramework
            var targetFrameworkElement = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "TargetFramework");
            
            if (targetFrameworkElement != null) {
                var currentTfm = targetFrameworkElement.Value;
                var newTfm = projectInfo.IsWindowsProject 
                    ? _config.TargetFrameworkWindows 
                    : _config.TargetFramework;
                
                if (currentTfm != newTfm) {
                    Console.WriteLine($"    Updating TargetFramework: {currentTfm} → {newTfm}");
                    targetFrameworkElement.Value = newTfm;
                    modified = true;
                }
            }
            
            // Update packages
            var packageReferences = doc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .ToList();
            
            var packagesToRemove = new List<XElement>();
            var packagesToUpdate = new Dictionary<XElement, string>(); // element → new package name
            
            foreach (var packageRef in packageReferences) {
                var packageName = packageRef.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(packageName)) continue;
                
                // Check if package should be removed
                if (TypeReplacementMap.ShouldRemovePackage(packageName)) {
                    packagesToRemove.Add(packageRef);
                    Console.WriteLine($"    Removing: {packageName} (no equivalent)");
                    modified = true;
                    continue;
                }
                
                // Check if package needs migration (Web → Blazor)
                if (TypeReplacementMap.TryGetPackageReplacement(packageName, out var replacement) && 
                    replacement.HasEquivalent) {
                    packagesToUpdate[packageRef] = replacement.NewPackage;
                    Console.WriteLine($"    Migrating: {packageName} → {replacement.NewPackage}");
                    modified = true;
                    // Don't continue here - we still need to update version below
                    packageName = replacement.NewPackage; // Update packageName for version check
                }
                
                // Update DevExpress package version
                if (packageName.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase)) {
                    var versionAttr = packageRef.Attribute("Version");
                    if (versionAttr != null && !_config.UseDirectoryPackages) {
                        if (versionAttr.Value != _config.DxPackageVersion) {
                            Console.WriteLine($"    Updating version: {packageName} from {versionAttr.Value} to {_config.DxPackageVersion}");
                            versionAttr.Value = _config.DxPackageVersion;
                            modified = true;
                        }
                    }
                }
            }
            
            // Apply package removals
            foreach (var package in packagesToRemove) {
                package.Remove();
            }
            
            // Apply package migrations (name changes)
            foreach (var kvp in packagesToUpdate) {
                kvp.Key.Attribute("Include").Value = kvp.Value;
            }
            
            if (modified) {
                var backupPath = projectPath + ".backup";
                if (createBackup) {
                    File.Copy(projectPath, backupPath, true);
                    Console.WriteLine($"  Backup created at: {backupPath}");
                }
                
                SaveProject(doc, projectPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Successfully updated: {projectPath}");
                Console.ResetColor();
                
                if (createBackup) {
                    Console.WriteLine($"  Backup saved to: {backupPath}");
                }
            }
        }

        private ProjectInfo AnalyzeProject(XDocument doc, string projectDir) {
            var projectPath = Path.Combine(projectDir, Path.GetFileName(projectDir) + ".csproj");
            if (!File.Exists(projectPath)) {
                // Try to find .csproj file in directory
                var csprojFiles = Directory.GetFiles(projectDir, "*.csproj");
                if (csprojFiles.Length > 0) {
                    projectPath = csprojFiles[0];
                }
            }

            var info = new ProjectInfo {
                IsWindowsProject = DetectWindowsProject(doc),
                IsWebProject = DetectWebProject(doc, projectDir),
                IsEfProject = PackageManager.IsProjectReferencesEF(projectPath),
                RootNamespace = ExtractProperty(doc, "RootNamespace"),
                AssemblyName = ExtractProperty(doc, "AssemblyName"),
                HasManualAssemblyInfo = HasManualAssemblyInfo(projectDir)
            };

            return info;
        }

        private bool DetectWindowsProject(XDocument doc) {
            // TRANS-002: Check for Windows-specific DevExpress references (both Reference and PackageReference)
            var references = doc.Descendants()
                .Where(e => e.Name.LocalName == "Reference" || e.Name.LocalName == "PackageReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(v => v != null);

            return references.Any(r => 
                r.Contains("DevExpress.ExpressApp.Win", StringComparison.OrdinalIgnoreCase) || 
                r.StartsWith("DevExpress.Win.", StringComparison.OrdinalIgnoreCase) ||
                r.Contains($"DevExpress.ExpressApp.Win.{_config.DxAssemblyVersion}"));
        }

        private bool DetectWebProject(XDocument doc, string projectDir) {
            // Check for Global.asax.cs
            if (File.Exists(Path.Combine(projectDir, "Global.asax.cs"))) {
                return true;
            }

            // Check project name
            var projectName = Path.GetFileName(projectDir);
            return projectName != null && 
                   (projectName.Contains(".Web") || projectName.Contains(".Blazor"));
        }

        private XDocument CreateSdkStyleProject(XDocument originalDoc, ProjectInfo info, string projectPath) {
            // TRANS-001: SDK-Style Conversion
            var project = new XElement("Project");
            project.SetAttributeValue("Sdk", "Microsoft.NET.Sdk");

            // Add main PropertyGroup
            var propertyGroup = CreateMainPropertyGroup(info);
            project.Add(propertyGroup);

            // TRANS-005: Add or update NuGet packages
            AddOrUpdatePackageReferences(project, originalDoc, info);

            // TRANS-007: Add custom embedded resources
            AddCustomEmbeddedResources(project, originalDoc, Path.GetDirectoryName(projectPath));

            // Add other custom items
            AddCustomItems(project, originalDoc);

            return new XDocument(new XDeclaration("1.0", "utf-8", null), project);
        }

        private XElement CreateMainPropertyGroup(ProjectInfo info) {
            var propertyGroup = new XElement("PropertyGroup");

            // TRANS-002: Target Framework Selection
            var targetFramework = info.IsWindowsProject 
                ? _config.TargetFrameworkWindows 
                : _config.TargetFramework;
            propertyGroup.Add(new XElement("TargetFramework", targetFramework));

            // Add namespace and assembly name if present
            if (!string.IsNullOrEmpty(info.RootNamespace)) {
                propertyGroup.Add(new XElement("RootNamespace", info.RootNamespace));
            }
            if (!string.IsNullOrEmpty(info.AssemblyName)) {
                propertyGroup.Add(new XElement("AssemblyName", info.AssemblyName));
            }

            // TRANS-003: Windows Desktop Properties
            if (info.IsWindowsProject) {
                propertyGroup.Add(new XElement("UseWindowsForms", "true"));
                propertyGroup.Add(new XElement("ImportWindowsDesktopTargets", "true"));
            }

            // TRANS-006: AssemblyInfo.cs Handling
            if (info.HasManualAssemblyInfo) {
                propertyGroup.Add(new XElement("GenerateAssemblyInfo", "false"));
            }

            return propertyGroup;
        }

        /// <summary>
        /// Add or update package references.
        /// If DevExpress packages already exist (starting with DevExpress.ExpressApp), 
        /// update their versions instead of adding new packages.
        /// Also handles Web → Blazor package migration and removal of no-equivalent packages.
        /// </summary>
        private void AddOrUpdatePackageReferences(XElement project, XDocument originalDoc, ProjectInfo info) {
            // Check if original project has DevExpress.ExpressApp packages
            var existingPackages = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "PackageReference")
                .Select(e => new {
                    Name = e.Attribute("Include")?.Value,
                    Version = e.Attribute("Version")?.Value,
                    Element = e
                })
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .ToList();

            var hasDevExpressPackages = existingPackages.Any(p => 
                p.Name.StartsWith("DevExpress.ExpressApp", StringComparison.OrdinalIgnoreCase) && !p.Name.StartsWith("DevExpress.ExpressApp.CodeAnalysis", StringComparison.OrdinalIgnoreCase));

            if (hasDevExpressPackages) {
                // Project already has DevExpress packages - update versions and migrate Web → Blazor
                Console.WriteLine("  Found existing DevExpress packages - migrating and updating versions...");
                
                var itemGroup = new XElement("ItemGroup");
                bool anyPackageUpdated = false;
                bool anyPackageMigrated = false;
                var removedPackages = new List<string>();

                foreach (var package in existingPackages) {
                    var packageName = package.Name;
                    
                    // Check if package should be removed
                    if (TypeReplacementMap.ShouldRemovePackage(packageName)) {
                        removedPackages.Add(packageName);
                        Console.WriteLine($"    Removed: {packageName} (no equivalent in .NET)");
                        continue;
                    }
                    
                    // Check if package should be migrated (Web → Blazor)
                    if (TypeReplacementMap.TryGetPackageReplacement(packageName, out var replacement) && replacement.HasEquivalent) {
                        packageName = replacement.NewPackage;
                        Console.WriteLine($"    Migrated: {package.Name} → {packageName}");
                        anyPackageMigrated = true;
                    }
                    
                    var packageRef = new XElement("PackageReference");
                    packageRef.SetAttributeValue("Include", packageName);
                    
                    // Update version for DevExpress packages
                    if (packageName.StartsWith("DevExpress.ExpressApp", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Persistent", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Data", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.EasyTest", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Utils", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Office", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Pdf", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Printing", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Sparkline", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Charts", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.CodeParser", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Drawing", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Images", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Maui", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.RichEdit", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Spreadsheet", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Xpo", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Xpf", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Win.", StringComparison.OrdinalIgnoreCase) ||
                        packageName.StartsWith("DevExpress.Reporting", StringComparison.OrdinalIgnoreCase)) {

                        if(!_config.UseDirectoryPackages) {
                            packageRef.SetAttributeValue("Version", _config.DxPackageVersion);
                            
                            if (package.Version != _config.DxPackageVersion) {
                                Console.WriteLine($"    Updated: {packageName} from {package.Version} to {_config.DxPackageVersion}");
                                anyPackageUpdated = true;
                            }
                        }
                    }
                    else {
                        // Keep original version for non-DevExpress packages
                        if (!_config.UseDirectoryPackages && !string.IsNullOrEmpty(package.Version)) {
                            packageRef.SetAttributeValue("Version", package.Version);
                        }
                    }
                    
                    itemGroup.Add(packageRef);
                }
                
                if (!anyPackageUpdated && !anyPackageMigrated && removedPackages.Count == 0) {
                    Console.WriteLine($"    All DevExpress packages are already at version {_config.DxPackageVersion}");
                }

                project.Add(itemGroup);
            }
            else {
                // No DevExpress packages found - add new packages from PackageManager
                Console.WriteLine("  No existing DevExpress packages found - adding new packages...");
                AddPackageReferences(project, info);
            }
        }

        private void AddPackageReferences(XElement project, ProjectInfo info) {
            var packages = _packageManager.GetPackages(info.IsWindowsProject, info.IsWebProject, info.IsEfProject);

            if (packages.Any()) {
                var itemGroup = new XElement("ItemGroup");
                
                foreach (var package in packages) {
                    var packageRef = new XElement("PackageReference");
                    packageRef.SetAttributeValue("Include", package.Name);
                    
                    if (!_config.UseDirectoryPackages) {
                        packageRef.SetAttributeValue("Version", package.Version);
                    }
                    
                    itemGroup.Add(packageRef);
                }

                project.Add(itemGroup);
            }
        }

        private void AddCustomEmbeddedResources(XElement project, XDocument originalDoc, string projectDir) {
            // TRANS-007: Keep only non-.resx EmbeddedResources and standalone .resx files
            var embeddedResources = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "EmbeddedResource")
                .ToList();

            var customResources = new List<XElement>();

            foreach (var resource in embeddedResources) {
                var include = resource.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(include)) continue;

                // Keep non-.resx files (xml, pdf, svg, etc)
                if (!include.EndsWith(".resx", StringComparison.OrdinalIgnoreCase)) {
                    customResources.Add(new XElement(resource));
                    continue;
                }

                // Skip .resx files with DependentUpon (auto-included by SDK)
                var hasDependentUpon = resource.Descendants()
                    .Any(d => d.Name.LocalName == "DependentUpon");
                
                if (!hasDependentUpon) {
                    // Keep standalone .resx files
                    customResources.Add(new XElement(resource));
                }
            }

            if (customResources.Any()) {
                var itemGroup = new XElement("ItemGroup");
                foreach (var resource in customResources) {
                    itemGroup.Add(resource);
                }
                project.Add(itemGroup);
            }
        }

        private void AddCustomItems(XElement project, XDocument originalDoc) {
            // Add other custom items that SDK doesn't auto-include
            
            // CRITICAL: Preserve ProjectReference elements
            var projectReferences = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "ProjectReference")
                .ToList();

            if (projectReferences.Any()) {
                var projectRefGroup = new XElement("ItemGroup");
                foreach (var projRef in projectReferences) {
                    // Simplify ProjectReference for SDK-style
                    var include = projRef.Attribute("Include")?.Value;
                    if (!string.IsNullOrEmpty(include)) {
                        var newProjRef = new XElement("ProjectReference");
                        newProjRef.SetAttributeValue("Include", include);
                        projectRefGroup.Add(newProjRef);
                    }
                }
                project.Add(projectRefGroup);
            }

            // Get None items with special metadata
            var noneItems = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "None" && e.HasElements)
                .ToList();

            // Get Content items with special properties
            var contentItems = originalDoc.Descendants()
                .Where(e => e.Name.LocalName == "Content" && e.HasElements)
                .ToList();

            if (noneItems.Any() || contentItems.Any()) {
                var itemGroup = new XElement("ItemGroup");
                
                foreach (var item in noneItems) {
                    itemGroup.Add(new XElement(item));
                }
                
                foreach (var item in contentItems) {
                    itemGroup.Add(new XElement(item));
                }
                
                project.Add(itemGroup);
            }
        }

        private string ExtractProperty(XDocument doc, string propertyName) {
            return doc.Descendants()
                .Where(e => e.Name.LocalName == propertyName)
                .Select(e => e.Value)
                .FirstOrDefault();
        }

        private bool HasManualAssemblyInfo(string projectDir) {
            // TRANS-006: Check for manual AssemblyInfo.cs
            var assemblyInfoPath1 = Path.Combine(projectDir, "Properties", "AssemblyInfo.cs");
            var assemblyInfoPath2 = Path.Combine(projectDir, "AssemblyInfo.cs");
            return File.Exists(assemblyInfoPath1) || File.Exists(assemblyInfoPath2);
        }

        private void SaveProject(XDocument doc, string projectPath) {
            var settings = new System.Xml.XmlWriterSettings {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false
            };

            using (var writer = System.Xml.XmlWriter.Create(projectPath, settings)) {
                doc.Save(writer);
            }
        }

        private string GetProjectTypeDescription(ProjectInfo info) {
            var types = new List<string>();
            if (info.IsWindowsProject) types.Add("Windows");
            if (info.IsWebProject) types.Add("Web/Blazor");
            return types.Any() ? string.Join(", ", types) : "Console/Library";
        }

        private class ProjectInfo {
            public bool IsWindowsProject { get; set; }
            public bool IsWebProject { get; set; }
            public bool IsEfProject { get; set; }
            public string RootNamespace { get; set; }
            public string AssemblyName { get; set; }
            public bool HasManualAssemblyInfo { get; set; }
        }
    }
}
