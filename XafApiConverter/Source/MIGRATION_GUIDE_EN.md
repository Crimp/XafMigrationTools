# XAF Application Migration Guide: ASP.NET Web Forms to Blazor

## Table of Contents

1. [Overview](#1-overview)
2. [Migration Preparation](#2-migration-preparation)
3. [Security API Update](#3-security-api-update)
4. [Application Code Migration](#4-application-code-migration)
5. [Business Objects Refactoring](#5-business-objects-refactoring)
6. [Converting Project to Blazor](#6-converting-project-to-blazor)
7. [Known Limitations](#7-known-limitations)
8. [Frequently Asked Questions](#8-frequently-asked-questions)

---

## 1. Overview

### What Changed in XAF v25.2

XAF v25.2 discontinued support for:
- **.NET Framework** - all projects must be migrated to .NET 6/8/9+
- **ASP.NET Web Forms** - replaced with **Blazor**
- **EF 6** - migration to **EF Core** required

### What XafApiConverter Does

**XafApiConverter** is an automated tool that performs the following tasks:

1. **Security API Update Assistance** (SecuritySystem* → PermissionPolicy*)
2. **Automatic Commenting** of problematic classes
3. **Project Conversion** (.NET Framework → .NET SDK-style) + **Type and Namespace Migration** where possible (Web Forms → Blazor)

**⚠️ IMPORTANT**: First, you need to update your project to **XAF v25.1**, then perform the .NET Framework → .NET migration on v25.1 using XafApiConverter. Verify the application works correctly. Only after that proceed to v25.2.

**Why through v25.1?** XafApiConverter uses semantic tree analysis, which depends on the DevExpress version. If you try to migrate directly to v25.2, the tool won't be able to correctly recognize types.

**Where to get the tool:** https://github.com/DevExpress-Examples/XafMigrationTools.git

---

## 2. Migration Preparation

### 2.1. Update to XAF v25.1

Before migrating to v25.2, update your project to **XAF v25.1**.

Verify that after the update:
- ✅ Project compiles successfully
- ✅ Application runs without errors
- ✅ All features work correctly

### 2.2. Backup

**Must create database backup**

### 2.3. Version Control System

**Strongly recommended** to use Git for:
- Tracking automatically made changes
- Ability to rollback changes
- Reviewing changes before commit

---

## 3. Security API Update

### 3.1. When Required

This step is **mandatory** if your application:
- ❌ Uses user types other than `PermissionPolicyUser` and `PermissionPolicyRole`
- ❌ Uses old **SHA-512** password hashing algorithm
- ❌ Uses deprecated Security types (`SecuritySystemUser`, `SecuritySystemRole`)

### 3.2. What to Do

Perform migration according to official documentation:
- [XAF Security Types Migration](https://supportcenter.devexpress.com/ticket/details/t1312589)

**Main steps:**
1. Update user and role types to `PermissionPolicyUser` / `PermissionPolicyRole`
2. Update password hashing algorithm
3. Perform data migration in DB

---

## 4. Application Code Migration

This step can be performed using XafApiConverter utility or **manually**.

XafApiConverter is designed to automate routine tasks and speed up getting a minimal CRUD Blazor application. The utility will highlight problematic areas that require intervention while still on 25.1, which will make it easier to handle situations with types removed in 25.2.

### 4.1. Using XafApiConverter

**⚠️ IMPORTANT**: You must **explicitly specify** which steps to execute!

The tool does not execute all steps automatically - this is intentional, as each step requires manual review and application fixes.

**Recommended sequence:**

```bash
# STEP 1: Update Security types (if required)
XafApiConverter.exe MySolution.sln security-update
# → Review changes in Git → Commit → Testing

# STEP 2: Type migration (after reviewing Step 1)
XafApiConverter.exe MySolution.sln migrate-types
# → Fix errors → Commit → Testing

# STEP 3: Project conversion (after reviewing Step 2)
XafApiConverter.exe MySolution.sln project-conversion
# → Review .csproj → Build → Commit → Create Blazor project
```

**Full syntax**:
```bash
XafApiConverter.exe <path> <step1> [step2] [step3] [options]

Arguments:
  <path>                    Path to .sln file or project folder

Migration steps (executed in order):
  security-update           Update Security types (SecuritySystem* -> PermissionPolicy*)
  migrate-types             Migrate types (Web -> Blazor namespaces and types)
  project-conversion        Convert projects (.NET Framework -> .NET SDK-style)

Main options:
  -tf, --target-framework   Target .NET version (default: net9.0)
                            Examples: net8.0, net9.0, net10.0
  -dx, --dx-version         DevExpress version (default: 25.1.6)
                            Example: 25.2.2, 26.1.6
  -o, --output <path>       Folder for saving reports
  -b, --backup              Create backup files
  -dp, --directory-packages Use Directory.Packages.props

Type migration options:
  -c, --comment-issues-only Add comments to ALL problematic classes
                            without automatic commenting out
                            (Manual review mode)

Other options:
  -m, --show-mappings       Show all type and namespace replacements
  -h, --help                Show help

Examples:
  # Execute one step
  XafApiConverter.exe MySolution.sln migrate-types
  
  # Execute two steps
  XafApiConverter.exe MySolution.sln security-update migrate-types
  
  # Execute all steps with version specification
  XafApiConverter.exe MySolution.sln security-update migrate-types project-conversion -tf net10.0
```

### 4.2. What Each Step Does

**Step 1: Security Types Update**
- Replaces `SecuritySystem*` → `PermissionPolicy*`
- Removes obsolete feature toggles
- Adds `PermissionPolicyRoleExtensions`
- Updates permission state setters

**Step 2: Type Migration (Web → Blazor)**

**Automatically:**
- Mark or comment out problematic classes

**Requires manual analysis:**
- Classes using types with no equivalents (Page, TemplateType, etc.)
- Commenting out problematic classes
- Dependency analysis
- Manual code review

**Step 3: Project Conversion (.NET Framework → .NET SDK-style)**
- Converts `.csproj` to SDK-style format
- Updates target framework to .NET 9/10
- Adds NuGet packages (BASE/WINDOWS/BLAZOR_WEB) if needed
- Removes legacy assembly references
- Validates converted projects
- 
- Migrate `System.Data.SqlClient` → `Microsoft.Data.SqlClient`
- Migrate `DevExpress.ExpressApp.Web.*` → `Blazor.*`
- Replace types (`WebApplication` → `BlazorApplication`, etc.)
- Process `.cs` and `.xafml` files

### 4.3. Type Migration Modes

**1. Full migration (default)**:
```bash
XafApiConverter.exe MySolution.sln migrate-types
```
- Automatically applies replacements
- Comments out problematic classes
- Protected classes (ModuleBase, BaseObject) only get warnings comments

**2. Review mode (--comment-issues-only)**:
```bash
XafApiConverter.exe MySolution.sln migrate-types --comment-issues-only
```
- Automatically applies replacements
- **ALL** problematic classes only get warning comments
- Developer decides what to comment out

### 4.4. Reference Information

Complete list of removed APIs, modules, assemblies:
- [Breaking Change T1312589](https://supportcenter.devexpress.com/ticket/details/t1312589)

---

## 5. Business Objects Refactoring

### 5.1. Removed Base Classes

If you use base business object classes removed in v25.2:

**Solution**: Copy their implementation from XAF v25.1 source code to your project.

**List of removed base classes:**
```csharp
// DevExpress.Persistent.BaseImpl (removed in v25.2)
- Address
- Country
- Note
- Organization
- Person
- PhoneNumber
- State
// ... and others
```

**Where to find source code:**
- Installed XAF v25.1: `C:\Program Files\DevExpress 25.1\Components\Sources\`

### 5.2. Migration from EF 6 to EF Core

#### 5.2.1. When Required

If your application uses **Entity Framework 6**.

#### 5.2.2. Reference Materials

**Official Microsoft documentation:**
- [Porting from EF6 to EF Core](https://learn.microsoft.com/ef/core/miscellaneous/porting/)
- [EF6 and EF Core Differences](https://learn.microsoft.com/ef/efcore-and-ef6/)

**XAF examples:**
- Demo application: `MainDemo.EFCore` (included in XAF installation)
- Template Kit: create new project with EF Core

---

## 6. Converting Project to Blazor

### Step 1: Convert Assembly References to NuGet (recommended)

**⚠️ Important**: This step should be performed while staying on XAF v25.1.

If your project uses **assembly references** instead of NuGet packages:

1. Open **Project Converter** version 25.1 (included in XAF installation)
2. Select your solution
3. In additional settings enable:
   - ☑️ **Convert DevExpress assembly to NuGet packages**
4. Run conversion

### Step 2: Convert .csproj Files

Execute command:
```bash
XafApiConverter.exe C:\Projects\MyXafApp\MyXafApp.sln project-conversion
```

**What will be done:**
- ✅ Convert `.csproj` to SDK-style format
- ✅ Update target framework to `net9.0`
- ✅ Add XAF NuGet packages
- ✅ Remove legacy assembly references
- ✅ Update `AssemblyInfo.cs`

### Step 3: Create Blazor Project

**XafApiConverter does not create Blazor project automatically!**

Create new Blazor project manually using **Template Kit**:

1. Launch **Template Kit**
2. Select **XAF Application (.NET)**
3. Settings:
   - **Platform**: Blazor
   - **ORM**: Same as in old project (XPO/EF Core)
   - **Security**: PermissionPolicy
   - **Modules**: Same as in old project
4. Create project
5. Update connection string:
   ```json
   // appsettings.json
   {
     "ConnectionStrings": {
       "ConnectionString": "Your connection string here"
     }
   }
   ```
6. Add XAF Blazor project from new solution to old one

**Reference**: This step is very similar to adding Middle Tier project to existing XAF Win solution:
- [Add Middle Tier to Existing Application](https://docs.devexpress.com/eXpressAppFramework/404391/data-security-and-safety/security-system/security-tiers/middle-tier-security-ef-core/add-middle-tier-to-existing-app)

---

## 7. Limitations

### 7.1. Removed Modules (no equivalents)

- ❌ **Maps** (`DevExpress.ExpressApp.Maps.Web`)
- ❌ **KPI** (`DevExpress.ExpressApp.Kpi`)
- ❌ **Script Recorder** (`DevExpress.ExpressApp.ScriptRecorder`)
- ❌ **Pivot Chart** (`DevExpress.ExpressApp.PivotChart.Web`)
- ❌ **Workflow** (`DevExpress.ExpressApp.Workflow`)

### 7.2. Web Forms Specific Components

- ❌ `Page` (System.Web.UI)
- ❌ `LayoutItemTemplate`, `LayoutGroupTemplate`
- ❌ `ASPxGridView`, `ASPxButton` and other ASPx controls
- ❌ HTTP Handlers (`IXafHttpHandler`)

### 7.3. Blazor Limitations

- ⚠️ Blazor doesn't support everything from Web Forms
- ⚠️ Some scenarios require logic rewriting
- ⚠️ Custom Web Forms controls need to be rewritten as Blazor components if required

---

## 8. Frequently Asked Questions

### Q1: Can I migrate from v25.1 directly to v25.2?

**A:** No. First you need to update project to **XAF v25.1**, then perform migration on v25.1 using XafApiConverter. Verify the application works correctly. Only after that proceed to v25.2.

XafApiConverter uses semantic tree analysis, which depends on the DevExpress version. If you try to migrate directly to v25.2, the tool won't be able to correctly recognize types.

---

### Q2: What to do if XafApiConverter commented out critical class?

**A:** 
1. If class uses removed type - find alternative in Blazor API
2. Rewrite class for Blazor
3. If no alternative exists - need to reconsider business logic

---

### Q3: Can XafApiConverter be used on production code?

**A:** **Only with backup!** The tool automates routine work, but:
- ✅ Always backup database
- ✅ Use Git to track changes
- ✅ Review all automatically made changes
- ✅ Test application after migration

---

### Q4: What to do with custom ASPx editors?

**A:** ASPx editors are Web Forms specific and don't work in Blazor:
1. **Remove** if functionality not critical
2. **Rewrite** as Blazor components
3. **Use standard** Blazor editors if suitable

Migration example:
```csharp
// ❌ Was (Web Forms)
public class CustomDateEditor : ASPxDateTimePropertyEditor {
    // Web Forms specific code
}

// ✅ Now (Blazor)
public class CustomDateEditor : DateTimePropertyEditor {
    // Blazor specific code
}
```

---

### Q5: Can the tool be used for WinForms applications?

**A:** Yes! WinForms migration is **significantly simpler**:
```bash
XafApiConverter.exe C:\Projects\WinFormsApp\WinFormsApp.sln project-conversion
```

WinForms API practically unchanged when moving to .NET Core/8+.

---

### Q6: What if build succeeds but application doesn't start?

**A:** Typical reasons:
1. **Connection string not configured** - check `appsettings.json`
2. **Module not registered** - check `Program.cs` / `Startup.cs`
3. **DB migration not executed** - run `UpdateDatabase`
4. **Security not configured** - check `SecurityStrategy` in configuration

---

### Q7: How to find out which types were removed in v25.2?

**A:** 
1. Read Breaking Change: [T1312589](https://supportcenter.devexpress.com/ticket/details/t1312589)
2. Open file: `XafApiConverter\Source\Converter\removed-api.txt`
3. Run: `XafApiConverter.exe --show-mappings`


---

### Q9: Where to find Blazor code examples?

**A:** Official sources:
1. **MainDemo.Blazor** - installed with XAF
2. **Template Kit** - create new project
3. **GitHub**: https://github.com/DevExpress-Examples/XAF_Blazor_Demo
4. **Documentation**: https://docs.devexpress.com/eXpressAppFramework/

---