using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;

namespace SecurityDemo.Module.Web
{
    // TODO: The 'SecurityDemoAspNetModule' class has been marked automatically due to usage of types that have no XAF for .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF for .NET
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.TreeListEditors.Web.TreeListEditorsAspNetModule' has no equivalent in XAF for .NET
    //     TreeListEditorsAspNetModule has no equivalent in XAF for .NET (loaded from removed-api.txt)
    [ToolboxItemFilter("Xaf.Platform.Web")]
    public sealed partial class SecurityDemoAspNetModule : ModuleBase
    {
        public SecurityDemoAspNetModule()
        {
            InitializeComponent();
        }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
            return ModuleUpdater.EmptyModuleUpdaters;
        }
    }
}
