using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Xpo;

namespace SecurityDemo.Win
{
    // TODO: The 'SecurityDemo.Win.SecurityDemoWindowsFormsApplication' class has been marked automatically due to usage of types that have no XAF for .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF for .NET
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.Objects.BusinessClassLibraryCustomizationModule' has no equivalent in XAF for .NET
    //     BusinessClassLibraryCustomizationModule has no equivalent in XAF for .NET (loaded from removed-api.txt)
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
