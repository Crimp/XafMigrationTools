using DevExpress.ExpressApp.Blazor.SystemModule;

namespace Test.Module.Web {
    internal class TestClass_1 {
        DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule systemAspNetModule = new DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule();
        public DevExpress.ExpressApp.Blazor.SystemModule.SystemBlazorModule GetModule() {
            return systemAspNetModule;
        }
    }
}
