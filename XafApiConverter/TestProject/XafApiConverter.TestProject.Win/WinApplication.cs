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
