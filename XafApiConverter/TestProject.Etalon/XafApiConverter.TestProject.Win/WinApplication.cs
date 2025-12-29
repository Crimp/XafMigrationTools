using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;
using SecurityDemo.Module;

namespace SecurityDemo.Win
{
    // TODO: The 'SecurityDemoWindowsFormsApplication' class has been marked automatically due to usage of types that have no XAF .NET equivalent.
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule' has no equivalent in XAF .NET
    //     BusinessClassLibraryCustomizationModule has no equivalent in XAF .NET (loaded from removed-api.txt)
public partial class SecurityDemoWindowsFormsApplication : WinApplication
    {
        public SecurityDemoWindowsFormsApplication()
        {
            InitializeComponent();
        }
        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args)
        {
			args.ObjectSpaceProviders.Add(new SecuredObjectSpaceProvider((ISelectDataSecurityProvider)Security, args.ConnectionString, args.Connection));
			args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
        }
        protected override ModelDifferenceStore CreateUserModelDifferenceStoreCore() {
            return null;
        }
    }
}
