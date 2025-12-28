using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Reflection;

namespace XafApiConverter.Converter {
    /// <summary>
    /// Type and namespace replacement mappings from UpdateTypes.md
    /// TRANS-006, TRANS-007, TRANS-008, TRANS-009
    /// </summary>
    internal class TypeReplacementMap {
        private static readonly object _lockObject = new();
        private static bool _isInitialized = false;

        /// <summary>
        /// Namespace replacements (TRANS-006, TRANS-007)
        /// </summary>
        public static readonly Dictionary<string, NamespaceReplacement> NamespaceReplacements = new() {
            // TRANS-006: SqlClient
            { "System.Data.SqlClient", new NamespaceReplacement(
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "SqlClient namespace migration",
                new[] { ".cs" }) },


            // TRANS-007: DevExpress Web → Blazor
            { "DevExpress.ExpressApp.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web",
                "DevExpress.ExpressApp.Blazor",
                "Web to Blazor migration",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Editors", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Editors",
                "DevExpress.ExpressApp.Blazor.Editors",
                "Web editors to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Templates", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Templates",
                "DevExpress.ExpressApp.Blazor.Templates",
                "Web templates to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Layout", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Layout",
                "DevExpress.ExpressApp.Blazor.Layout",
                "Web layout to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.Editors.ASPx", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Editors.ASPx",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPx editors to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Web.SystemModule", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.Blazor.SystemModule",
                "Web system module to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Validation.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Validation.Web",
                "DevExpress.ExpressApp.Validation.Blazor",
                "Validation Web to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Scheduler.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Scheduler.Web",
                "DevExpress.ExpressApp.Scheduler.Blazor",
                "Scheduler Web to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Office.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Office.Web",
                "DevExpress.ExpressApp.Office.Blazor",
                "Office Web to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.ReportsV2.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.ReportsV2.Web",
                "DevExpress.ExpressApp.ReportsV2.Blazor",
                "Reports Web to Blazor",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Dashboards.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Dashboards.Web",
                "DevExpress.ExpressApp.Dashboards.Blazor",
                "Dashboards ASP.NET to Blazor",
                new[] { ".cs" }) }
        };

        /// <summary>
        /// Namespace that have NO Blazor equivalent and should be commented out
        /// </summary>
        public static readonly Dictionary<string, NamespaceReplacement> NoEquivalentNamespaces = new() {
            { "DevExpress.ExpressApp.PivotChart.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.PivotChart.Web",
                null,
                "PivotChart.Web has no Blazor equivalent",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Maps.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "Maps.Web has no Blazor equivalent",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.Kpi", new NamespaceReplacement(
                "DevExpress.ExpressApp.Kpi",
                null,
                "DevExpress.ExpressApp.Kpi has no .NET equivalent",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.ScriptRecorder.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.ScriptRecorder.Web",
                null,
                "ScriptRecorder.Web has no Blazor equivalent",
                new[] { ".cs", ".xafml" }) },

            { "DevExpress.ExpressApp.ScriptRecorder", new NamespaceReplacement(
                "DevExpress.ExpressApp.ScriptRecorder",
                null,
                "ScriptRecorder has no .NET equivalent",
                new[] { ".cs", ".xafml" }) },

            // NEW: Additional no-equivalent namespaces
            { "System.Web.UI.WebControls", new NamespaceReplacement(
                "System.Web.UI.WebControls",
                null,
                "System.Web.UI.WebControls has no equivalent in .NET (Web Forms specific)",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.Web.TestScripts", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.TestScripts",
                null,
                "DevExpress.ExpressApp.Web.TestScripts has no Blazor equivalent",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.Web.Templates.ActionContainers", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Templates.ActionContainers",
                null,
                "DevExpress.ExpressApp.Web.Templates.ActionContainers has no Blazor equivalent",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.Web.Templates.ActionContainers.Menu", new NamespaceReplacement(
                "DevExpress.ExpressApp.Web.Templates.ActionContainers.Menu",
                null,
                "DevExpress.ExpressApp.Web.Templates.ActionContainers.Menu has no Blazor equivalent",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.PivotGrid.Web", new NamespaceReplacement(
                "DevExpress.ExpressApp.PivotGrid.Web",
                null,
                "DevExpress.ExpressApp.PivotGrid.Web has no Blazor equivalent",
                new[] { ".cs" }) },

            { "DevExpress.ExpressApp.Workflow", new NamespaceReplacement(
                "DevExpress.ExpressApp.Workflow",
                null,
                "DevExpress.ExpressApp.Workflow has no equivalent in .NET",
                new[] { ".cs" }) }

            
        };

        /// <summary>
        /// Type replacements (TRANS-008)
        /// </summary>
        public static readonly Dictionary<string, TypeReplacement> TypeReplacements = new() {
            // Application Types
            { "WebApplication", new TypeReplacement(
                "WebApplication",
                "BlazorApplication",
                "DevExpress.ExpressApp.Web",
                "DevExpress.ExpressApp.Blazor",
                "Web to Blazor application") },

            // Editor Types
            //{ "ASPxGridListEditor", new TypeReplacement(
            //    "ASPxGridListEditor",
            //    "DxGridListEditor",
            //    "DevExpress.ExpressApp.Web.Editors.ASPx",
            //    "DevExpress.ExpressApp.Blazor.Editors",
            //    "ASPx grid to Dx grid") },

            //{ "ASPxLookupPropertyEditor", new TypeReplacement(
            //    "ASPxLookupPropertyEditor",
            //    "LookupPropertyEditor",
            //    "DevExpress.ExpressApp.Web.Editors.ASPx",
            //    "DevExpress.ExpressApp.Blazor.Editors",
            //    "ASPx lookup to Blazor lookup") },

            // Controller Types
            { "WebModificationsController", new TypeReplacement(
                "WebModificationsController",
                "BlazorModificationsController",
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.Blazor.SystemModule",
                "Web modifications controller to Blazor") },

            // NEW: ListView Controller replacement
            { "ListViewController", new TypeReplacement(
                "ListViewController",
                "ListViewControllerBase",
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.SystemModule",
                "Web ListViewController to base ListViewControllerBase") },

            // Module Types
            { "SystemAspNetModule", new TypeReplacement(
                "SystemAspNetModule",
                "SystemBlazorModule",
                "DevExpress.ExpressApp.Web.SystemModule",
                "DevExpress.ExpressApp.Blazor.SystemModule",
                "System ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "ValidationAspNetModule", new TypeReplacement(
                "ValidationAspNetModule",
                "ValidationBlazorModule",
                "DevExpress.ExpressApp.Validation.Web",
                "DevExpress.ExpressApp.Validation.Blazor",
                "Validation ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "SchedulerAspNetModule", new TypeReplacement(
                "SchedulerAspNetModule",
                "SchedulerBlazorModule",
                "DevExpress.ExpressApp.Scheduler.Web",
                "DevExpress.ExpressApp.Scheduler.Blazor",
                "Scheduler ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "OfficeAspNetModule", new TypeReplacement(
                "OfficeAspNetModule",
                "OfficeBlazorModule",
                "DevExpress.ExpressApp.Office.Web",
                "DevExpress.ExpressApp.Office.Blazor",
                "Office ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "ReportsAspNetModuleV2", new TypeReplacement(
                "ReportsAspNetModuleV2",
                "ReportsBlazorModuleV2",
                "DevExpress.ExpressApp.ReportsV2.Web",
                "DevExpress.ExpressApp.ReportsV2.Blazor",
                "Reports ASP.NET to Blazor",
                new[] { ".cs" }) },

            { "DashboardsAspNetModule", new TypeReplacement(
                "DashboardsAspNetModule",
                "DashboardsBlazorModule",
                "DevExpress.ExpressApp.Dashboards.Web",
                "DevExpress.ExpressApp.Dashboards.Blazor",
                "Dashboards ASP.NET to Blazor",
                new[] { ".cs" }) }
        };

        /// <summary>
        /// Types with NO XAF .NET equivalent (TRANS-009)
        /// These types have no equivalents in XAF .NET and require commenting out entire classes.
        /// This collection is dynamically populated from removed-api.txt and can be manually extended.
        /// </summary>
        public static readonly Dictionary<string, TypeReplacement> NoEquivalentTypes = new() {
            // Manually defined types with detailed descriptions
            { "AnalysisControlWeb", new TypeReplacement(
                "AnalysisControlWeb",
                null,
                "DevExpress.ExpressApp.PivotChart.Web",
                null,
                "AnalysisControlWeb has no Blazor equivalent",
                commentOutEntireClass: true) },

            { "MapsAspNetModule", new TypeReplacement(
                "MapsAspNetModule",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "MapsAspNetModule has no Blazor equivalent",
                commentOutEntireClass: true) },

            { "ScriptRecorderAspNetModule", new TypeReplacement(
                "ScriptRecorderAspNetModule",
                null,
                "DevExpress.ExpressApp.ScriptRecorder.Web",
                null,
                "ScriptRecorderAspNetModule has no Blazor equivalent",
                commentOutEntireClass: true) },

            { "ScriptRecorderModuleBase", new TypeReplacement(
                "ScriptRecorderModuleBase",
                null,
                "DevExpress.ExpressApp.ScriptRecorder",
                null,
                "ScriptRecorderModuleBase has no .NET equivalent",
                commentOutEntireClass: true) },

            { "KpiModule", new TypeReplacement(
                "KpiModule",
                null,
                "DevExpress.ExpressApp.Kpi",
                null,
                "KpiModule has no .NET equivalent",
                commentOutEntireClass: true) },

            { "WebMapsPropertyEditor", new TypeReplacement(
                "WebMapsPropertyEditor",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "WebMapsPropertyEditor has no Blazor equivalent",
                commentOutEntireClass: true) },

            { "WebMapsListEditor", new TypeReplacement(
                "WebMapsListEditor",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "WebMapsListEditor has no Blazor equivalent",
                commentOutEntireClass: true) },

            { "WebVectorMapsListEditor", new TypeReplacement(
                "WebVectorMapsListEditor",
                null,
                "DevExpress.ExpressApp.Maps.Web",
                null,
                "WebVectorMapsListEditor has no Blazor equivalent",
                commentOutEntireClass: true) },

            { "ASPxRichTextPropertyEditor", new TypeReplacement(
                "ASPxRichTextPropertyEditor",
                null,
                "DevExpress.ExpressApp.Office.Web",
                null,
                "ASPxRichTextPropertyEditor has no direct Blazor equivalent",
                commentOutEntireClass: true) },

            // NEW: Additional no-equivalent types
            { "ImageResourceHttpHandler", new TypeReplacement(
                "ImageResourceHttpHandler",
                null,
                "DevExpress.ExpressApp.Web",
                null,
                "ImageResourceHttpHandler has no Blazor equivalent (Web Forms specific HTTP handler)",
                commentOutEntireClass: true) },

            { "IXafHttpHandler", new TypeReplacement(
                "IXafHttpHandler",
                null,
                "DevExpress.ExpressApp.Web",
                null,
                "IXafHttpHandler has no Blazor equivalent (Web Forms specific interface)",
                commentOutEntireClass: true) },

            { "IJScriptTestControl", new TypeReplacement(
                "IJScriptTestControl",
                null,
                "DevExpress.ExpressApp.Web.TestScripts",
                null,
                "IJScriptTestControl has no Blazor equivalent (Test framework specific)",
                commentOutEntireClass: true) },

            { "LayoutItemTemplate", new TypeReplacement(
                "LayoutItemTemplate",
                null,
                "DevExpress.ExpressApp.Web.Layout",
                null,
                "LayoutItemTemplate has no Blazor equivalent (Web Forms layout specific)",
                commentOutEntireClass: true) },

            { "LayoutGroupTemplate", new TypeReplacement(
                "LayoutGroupTemplate",
                null,
                "DevExpress.ExpressApp.Web.Layout",
                null,
                "LayoutGroupTemplate has no Blazor equivalent (Web Forms layout specific)",
                commentOutEntireClass: true) },

            { "TabbedGroupTemplate", new TypeReplacement(
                "TabbedGroupTemplate",
                null,
                "DevExpress.ExpressApp.Web.Layout",
                null,
                "TabbedGroupTemplate has no Blazor equivalent (Web Forms layout specific)",
                commentOutEntireClass: true) }
        };

        /// <summary>
        /// Types that have XAF .NET equivalents but require manual conversion (TRANS-010)
        /// These types cannot be automatically converted
        /// </summary>
        public static readonly Dictionary<string, TypeReplacement> ManualConversionRequiredTypes = new() {
            { "WebPropertyEditor", new TypeReplacement(
                "WebPropertyEditor",
                "BlazorPropertyEditorBase",
                "DevExpress.ExpressApp.Web.Editors",
                "DevExpress.ExpressApp.Blazor.Editors",
                "WebPropertyEditor has Blazor equivalent (BlazorPropertyEditorBase) but automatic conversion is not possible. Manual refactoring required.",
                new[] { ".cs" },
                commentOutEntireClass: true) },

            { "ASPxPropertyEditor", new TypeReplacement(
                "ASPxPropertyEditor",
                "BlazorPropertyEditorBase",
                "DevExpress.ExpressApp.Web.Editors.ASPx",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPxPropertyEditor has Blazor equivalent (BlazorPropertyEditorBase) but automatic conversion is not possible. Manual refactoring required.",
                new[] { ".cs" },
                commentOutEntireClass: true) },

            { "ASPxDateTimePropertyEditor", new TypeReplacement(
                "ASPxDateTimePropertyEditor",
                "DateTimePropertyEditor",
                "DevExpress.ExpressApp.Web.Editors",
                "DevExpress.ExpressApp.Blazor.Editors",
                "ASPxDateTimePropertyEditor has Blazor equivalent (DateTimePropertyEditor) but automatic conversion is not possible. Manual refactoring required.",
                new[] { ".cs" },
                commentOutEntireClass: true) }
        };

        /// <summary>
        /// Assemblies that removed in v25.2
        /// </summary>
        public static readonly HashSet<string> RemovedAssemblies = new();

        /// <summary>
        /// Enum types that require commenting out entire class (TRANS-009)
        /// </summary>
        public static readonly Dictionary<string, EnumReplacement> ProblematicEnums = new() {
            { "TemplateType", new EnumReplacement(
                "TemplateType",
                new[] { "TemplateType.Horizontal", "TemplateType.Vertical" },
                "DevExpress.ExpressApp.Web.Templates",
                "TemplateType enum has no Blazor equivalent") }
        };

        /// <summary>
        /// Protected base classes - classes that inherit from these receive WARNING comments but are NOT automatically commented out.
        /// 
        /// CRITICAL BEHAVIOR:
        /// ==================
        /// These are critical XAF base classes that must be preserved for manual refactoring because:
        /// 1. They are essential for application structure (ModuleBase, XafApplication, etc.)
        /// 2. They contain business logic that cannot be automatically migrated
        /// 3. Commenting them out would break the application completely
        /// 
        /// When a class inheriting from these base classes uses types with NO_EQUIVALENT:
        /// - A WARNING comment is added above the class declaration
        /// - The class itself is NOT commented out
        /// - Developer must manually review and refactor
        /// 
        /// Example output:
        /// <code>
        /// // NOTE: Class has no XAF .NET equivalent
        /// //   - Type 'ScriptRecorderAspNetModule' has no equivalent in XAF .NET
        /// //   - Type 'KpiModule' has no equivalent in XAF .NET
        /// // TODO: It is necessary to test the application's behavior and, if necessary, develop a new solution.
        /// public class MyModule : ModuleBase { ... }  // ← Class remains active!
        /// </code>
        /// 
        /// IMPORTANT NOTES:
        /// ================
        /// 1. Case-insensitive matching is used (StringComparer.OrdinalIgnoreCase)
        /// 2. Generic types are matched without type parameters (e.g., "ViewController" matches "ViewController<T>")
        /// 3. Matching is done on base class names ONLY, not on full type paths
        /// 4. If a protected class inherits from a no-equivalent type, only the no-equivalent parts get warnings
        /// 5. This collection is dynamically populated from ProtectedTypes.txt embedded resource
        /// 
        /// TESTING:
        /// ========
        /// See ClassCommenterTests.TestProtectedBaseClass_NotCommented_WithWarning for validation
        /// </summary>
        public static readonly HashSet<string> ProtectedBaseClasses = new(StringComparer.OrdinalIgnoreCase) {
            // Manually defined protected base classes - these are always protected
            // XAF Module base classes - NEVER comment out, only warn
            "ModuleBase",           // Core XAF module infrastructure
            "ModuleUpdater",        // Database update logic

            // XAF Business Object base classes - NEVER comment out, only warn
            "BaseObject",           // Core XAF business object
            
            // XAF Controller base classes - CURRENTLY DISABLED
            // These are commented out because:
            // 1. Controllers are more granular and can often be safely commented out
            // 2. Developer can recreate controller logic more easily than module structure
            // 3. Web-specific controllers (e.g., WebModificationsController) should be replaced, not preserved
            //
            // To enable protection for controllers, uncomment the lines below:
            //"ViewController",
            //"ViewController<TViewType>",
            //"ObjectViewController",
            //"ObjectViewController<TViewType, TObjectType>",
            //"ListViewController",
            //"DetailViewController",
            //"WindowController",
            //"DialogController",
            
            // XAF Editor base classes - CURRENTLY DISABLED
            // These are commented out because:
            // 1. Custom editors inheriting from Web-specific classes should be replaced
            // 2. ASPxPropertyEditor and WebPropertyEditor have no direct Blazor equivalents
            // 3. BlazorPropertyEditorBase requires complete rewrite of editor logic
            //
            // To enable protection for editors, uncomment the lines below:
            //"PropertyEditor",
            //"ListEditor",
            //"BlazorPropertyEditorBase",
            
            // XAF Application classes - NEVER comment out, only warn
            "XafApplication",        // Core application class
            "BlazorApplication",     // Blazor-specific application
            "WinApplication"         // WinForms application (for mixed solutions)
            
            // Other critical base classes - CURRENTLY DISABLED
            // These are commented out for the same reasons as controllers above
            //"Controller",
            //"ViewControllerBase",
            //"ActionBase",
            //"SimpleAction",
            //"SingleChoiceAction",
            //"ParametrizedAction"
            
            // Additional protected types are loaded from ProtectedTypes.txt embedded resource
            // See LoadProtectedTypes() method for dynamic loading
        };

        static TypeReplacementMap() {
            // Automatically initialize on first access
            EnsureInitialized();
        }

        /// <summary>
        /// Ensures that NoEquivalentTypes dictionary and ProtectedBaseClasses set are initialized
        /// </summary>
        private static void EnsureInitialized() {
            if (_isInitialized) return;

            lock (_lockObject) {
                if (_isInitialized) return;

                LoadRemovedApiTypes();
                LoadProtectedTypes();
                LoadRemovedAssemblies();
                _isInitialized = true;
            }
        }

        /// <summary>
        /// Loads removed API types from removed-api.txt embedded resource and populates NoEquivalentTypes dictionary
        /// </summary>
        private static void LoadRemovedApiTypes() {
            try {
                List<string> removedTypeNames = ReadTypeNamesFromResource("removed-api.txt");
                HashSet<string> typesExistingBothForEF6AndEFCore = ReadTypeNamesFromResource("TypesExistingBothEF6AndEFCore.txt").ToHashSet();
                removedTypeNames = removedTypeNames
                    .Where(t => !typesExistingBothForEF6AndEFCore.Contains(t))
                    .ToList();

                foreach (string fullTypeName in removedTypeNames) {
                    // Extract namespace and type name
                    int lastDotIndex = fullTypeName.LastIndexOf('.');
                    if (lastDotIndex == -1) {
                        // No namespace, skip
                        continue;
                    }

                    string namespaceName = fullTypeName.Substring(0, lastDotIndex);
                    string typeName = fullTypeName.Substring(lastDotIndex + 1);

                    // Only add if not already in the dictionary (manually defined entries take precedence)
                    if (!NoEquivalentTypes.ContainsKey(typeName)) {
                        NoEquivalentTypes[typeName] = new TypeReplacement(
                            typeName,
                            null, // No equivalent
                            namespaceName,
                            null,
                            $"{typeName} has no equivalent in XAF .NET (loaded from removed-api.txt)",
                            new[] { ".cs" },
                            commentOutEntireClass: true);
                    }
                }

                // Step 2: Remove types that have equivalents in TypeReplacements
                var typesToRemove = new List<string>();
                foreach (var kvp in NoEquivalentTypes) {
                    if (TypeReplacements.ContainsKey(kvp.Key)) {
                        typesToRemove.Add(kvp.Key);
                    }
                }

                foreach (var typeToRemove in typesToRemove) {
                    NoEquivalentTypes.Remove(typeToRemove);
                }

            } catch (Exception ex) {
                // Log error or handle gracefully - don't crash the application
                Console.WriteLine($"Warning: Failed to load removed-api.txt from embedded resources: {ex.Message}");
            }
        }

        static List<string> ReadTypeNamesFromResource(string resourceName) {
            // Get the assembly containing this class
            var assembly = Assembly.GetExecutingAssembly();

            // Try to find the resource with different possible names
            var resourceNames = assembly.GetManifestResourceNames();
            var matchingResource = resourceNames.FirstOrDefault(r => r.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

            var result = new List<string>();
            if (matchingResource == null) {
                // Resource not found, skip loading (NoEquivalentTypes will only contain manually defined entries)
                Console.WriteLine($"Warning: {resourceName} resource not found. Available resources: {string.Join(", ", resourceNames)}");
                return result;
            }

            // Read the embedded resource
            using (Stream stream = assembly.GetManifestResourceStream(matchingResource)) {
                if (stream == null) {
                    Console.WriteLine($"Warning: Could not open stream for resource '{matchingResource}'");
                    return result;
                }

                using (StreamReader reader = new StreamReader(stream)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        string trimmedLine = line.Trim();

                        // Skip empty lines and comments
                        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#")) {
                            continue;
                        }

                        // Parse full type name (e.g., "DevExpress.ExpressApp.Security.ClientPermissionRequestProcessor")
                        string fullTypeName = trimmedLine;
                        result.Add(fullTypeName);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Loads protected types from ProtectedTypes.txt embedded resource and adds them to ProtectedBaseClasses set.
        /// Protected types are business object base classes and interfaces that should receive WARNING comments
        /// but should NOT be automatically commented out.
        /// 
        /// File format:
        /// - Lines starting with '#' are treated as comments (ignored)
        /// - Empty lines are ignored
        /// - Each non-comment line contains a full type name: "Namespace.TypeName"
        /// - Only the simple type name (after last dot) is added to ProtectedBaseClasses
        /// 
        /// Example:
        /// # DevExpress.Persistent.Base.v25.2
        /// DevExpress.Persistent.Base.General.ITask
        /// DevExpress.Persistent.BaseImpl.Task
        /// 
        /// Result: "ITask" and "Task" are added to ProtectedBaseClasses
        /// </summary>
        private static void LoadProtectedTypes() {
            try {
                List<string> protectedTypes = ReadTypeNamesFromResource("ProtectedTypes.txt");
                
                int loadedCount = 0;

                foreach (string fullTypeName in protectedTypes) {
                    // Extract simple type name (after last dot)
                    int lastDotIndex = fullTypeName.LastIndexOf('.');
                    if (lastDotIndex == -1) {
                        // No namespace - use as-is
                        if (!ProtectedBaseClasses.Contains(fullTypeName)) {
                            ProtectedBaseClasses.Add(fullTypeName);
                            loadedCount++;
                        }
                        continue;
                    }

                    string typeName = fullTypeName.Substring(lastDotIndex + 1);

                    // Add to ProtectedBaseClasses if not already present
                    // (manually defined entries take precedence due to case-insensitive comparison)
                    if (!ProtectedBaseClasses.Contains(typeName)) {
                        ProtectedBaseClasses.Add(typeName);
                        loadedCount++;
                    }
                }
                
                if (loadedCount > 0) {
                    Console.WriteLine($"Info: Loaded {loadedCount} protected types from ProtectedTypes.txt");
                }

            } catch (Exception ex) {
                // Log error or handle gracefully - don't crash the application
                Console.WriteLine($"Warning: Failed to load ProtectedTypes.txt from embedded resources: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads removed assembly names removed-assemblies.txt embedded resource
        /// </summary>
        private static void LoadRemovedAssemblies() {
            try {
                // Get the assembly containing this class
                var assembly = Assembly.GetExecutingAssembly();

                // Get the resource name - typically namespace + filename
                // Expected format: "XafApiConverter.Converter.removed-assemblies.txt"
                string resourceName = "XafApiConverter.Converter.removed-assemblies.txt";

                // Try to find the resource with different possible names
                var resourceNames = assembly.GetManifestResourceNames();
                var matchingResource = resourceNames.FirstOrDefault(r => r.EndsWith("removed-assemblies.txt", StringComparison.OrdinalIgnoreCase));

                if (matchingResource == null) {
                    // Resource not found, skip loading (NoEquivalentTypes will only contain manually defined entries)
                    Console.WriteLine($"Warning: removed-assemblies.txt resource not found. Available resources: {string.Join(", ", resourceNames)}");
                    return;
                }

                // Read the embedded resource
                using (Stream stream = assembly.GetManifestResourceStream(matchingResource)) {
                    if (stream == null) {
                        Console.WriteLine($"Warning: Could not open stream for resource '{matchingResource}'");
                        return;
                    }

                    using (StreamReader reader = new StreamReader(stream)) {
                        string line;
                        while ((line = reader.ReadLine()) != null) {
                            RemovedAssemblies.Add(line);
                        }
                    }
                }
            }
            catch (Exception ex) {
                // Log error or handle gracefully - don't crash the application
                Console.WriteLine($"Warning: Failed to load removed-api.txt from embedded resources: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all namespace replacements (both normal and NO_EQUIVALENT)
        /// </summary>
        public static IEnumerable<NamespaceReplacement> GetAllNamespaceReplacements() {
            EnsureInitialized();
            return NamespaceReplacements.Values.Concat(NoEquivalentNamespaces.Values);
        }

        /// <summary>
        /// Get all type replacements (normal, NO_EQUIVALENT, and MANUAL_CONVERSION_REQUIRED)
        /// </summary>
        public static IEnumerable<TypeReplacement> GetAllTypeReplacements() {
            EnsureInitialized();
            return TypeReplacements.Values
                .Concat(NoEquivalentTypes.Values)
                .Concat(ManualConversionRequiredTypes.Values);
        }

        /// <summary>
        /// Check if a type requires commenting out entire class
        /// </summary>
        public static bool RequiresCommentOutClass(string typeName) {
            EnsureInitialized();
            return NoEquivalentTypes.TryGetValue(typeName, out var replacement) && 
                   replacement.CommentOutEntireClass;
        }

        /// <summary>
        /// Check if a type requires manual conversion
        /// </summary>
        public static bool RequiresManualConversion(string typeName) {
            EnsureInitialized();
            return ManualConversionRequiredTypes.ContainsKey(typeName);
        }

        /// <summary>
        /// Check if an enum usage requires commenting out entire class
        /// </summary>
        public static bool IsProblematicEnum(string enumName) {
            EnsureInitialized();
            return ProblematicEnums.ContainsKey(enumName);
        }

        /// <summary>
        /// Force reload of removed-api.txt (useful for testing)
        /// </summary>
        public static void ReloadRemovedApiTypes() {
            lock (_lockObject) {
                _isInitialized = false;
                // Clear dynamically loaded entries (keep manually defined ones)
                var manualEntries = NoEquivalentTypes
                    .Where(kvp => !kvp.Value.Description.Contains("loaded from removed-api.txt"))
                    .ToList();
                
                NoEquivalentTypes.Clear();
                foreach (var entry in manualEntries) {
                    NoEquivalentTypes[entry.Key] = entry.Value;
                }

                EnsureInitialized();
            }
        }

        /// <summary>
        /// Force reload of both removed-api.txt and ProtectedTypes.txt (useful for testing)
        /// </summary>
        public static void ReloadAllTypes() {
            lock (_lockObject) {
                _isInitialized = false;
                
                // Clear dynamically loaded entries from NoEquivalentTypes
                var manualNoEquivEntries = NoEquivalentTypes
                    .Where(kvp => !kvp.Value.Description.Contains("loaded from removed-api.txt"))
                    .ToList();
                
                NoEquivalentTypes.Clear();
                foreach (var entry in manualNoEquivEntries) {
                    NoEquivalentTypes[entry.Key] = entry.Value;
                }
                
                // Clear dynamically loaded protected types
                var manualProtectedTypes = new[] {
                    "ModuleBase", "ModuleUpdater", "BaseObject",
                    "XafApplication", "BlazorApplication", "WinApplication"
                };
                
                var toRemove = ProtectedBaseClasses
                    .Where(t => !manualProtectedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                
                foreach (var type in toRemove) {
                    ProtectedBaseClasses.Remove(type);
                }

                EnsureInitialized();
            }
        }

        /// <summary>
        /// Gets statistics about loaded types (for debugging)
        /// </summary>
        public static (int manuallyDefined, int loadedFromFile, int total) GetLoadedTypesStatistics() {
            EnsureInitialized();
            
            int manuallyDefined = NoEquivalentTypes.Count(kvp => !kvp.Value.Description.Contains("loaded from removed-api.txt"));
            int loadedFromFile = NoEquivalentTypes.Count(kvp => kvp.Value.Description.Contains("loaded from removed-api.txt"));
            int total = NoEquivalentTypes.Count;

            return (manuallyDefined, loadedFromFile, total);
        }

        /// <summary>
        /// Gets statistics about protected types (for debugging)
        /// </summary>
        public static (int manuallyDefined, int loadedFromFile, int total) GetProtectedTypesStatistics() {
            EnsureInitialized();
            
            // Manually defined protected types (core types)
            var manualTypes = new[] {
                "ModuleBase", "ModuleUpdater", "BaseObject",
                "XafApplication", "BlazorApplication", "WinApplication"
            };
            
            int manuallyDefined = ProtectedBaseClasses.Count(t => 
                manualTypes.Contains(t, StringComparer.OrdinalIgnoreCase));
            int total = ProtectedBaseClasses.Count;
            int loadedFromFile = total - manuallyDefined;

            return (manuallyDefined, loadedFromFile, total);
        }
    }

    /// <summary>
    /// Namespace replacement information
    /// </summary>
    internal class NamespaceReplacement {
        public string OldNamespace { get; }
        public string NewNamespace { get; }
        public string Description { get; }
        public string[] ApplicableFileTypes { get; }
        public bool HasEquivalent => NewNamespace != null;

        public NamespaceReplacement(
            string oldNamespace,
            string newNamespace,
            string description,
            string[] applicableFileTypes = null) {
            OldNamespace = oldNamespace;
            NewNamespace = newNamespace;
            Description = description;
            ApplicableFileTypes = applicableFileTypes ?? new[] { ".cs", ".xafml" };
        }

        public bool AppliesToFileType(string fileExtension) {
            return ApplicableFileTypes.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Type replacement information
    /// </summary>
    internal class TypeReplacement {
        public string OldType { get; }
        public string NewType { get; }
        public string OldNamespace { get; }
        public string NewNamespace { get; }
        public string Description { get; }
        public string[] ApplicableFileTypes { get; }
        public bool CommentOutEntireClass { get; }
        public bool HasEquivalent => NewType != null;

        public TypeReplacement(
            string oldType,
            string newType,
            string oldNamespace,
            string newNamespace,
            string description,
            string[] applicableFileTypes = null,
            bool commentOutEntireClass = false) {
            OldType = oldType;
            NewType = newType;
            OldNamespace = oldNamespace;
            NewNamespace = newNamespace;
            Description = description;
            ApplicableFileTypes = applicableFileTypes ?? new[] { ".cs", ".xafml" };
            CommentOutEntireClass = commentOutEntireClass;
        }

        public bool AppliesToFileType(string fileExtension) {
            return ApplicableFileTypes.Contains(fileExtension, StringComparer.OrdinalIgnoreCase);
        }

        public string GetFullOldTypeName() {
            return string.IsNullOrEmpty(OldNamespace) ? OldType : $"{OldNamespace}.{OldType}";
        }

        public string GetFullNewTypeName() {
            return string.IsNullOrEmpty(NewNamespace) ? NewType : $"{NewNamespace}.{NewType}";
        }
    }

    /// <summary>
    /// Enum replacement information
    /// </summary>
    internal class EnumReplacement {
        public string EnumName { get; }
        public string[] ProblematicValues { get; }
        public string Namespace { get; }
        public string Description { get; }

        public EnumReplacement(
            string enumName,
            string[] problematicValues,
            string @namespace,
            string description) {
            EnumName = enumName;
            ProblematicValues = problematicValues;
            Namespace = @namespace;
            Description = description;
        }
    }
}
