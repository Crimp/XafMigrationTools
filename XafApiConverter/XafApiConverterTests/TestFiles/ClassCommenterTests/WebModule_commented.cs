using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;

namespace FeatureCenter.Module.Web {
    // TODO: The 'FeatureCenterAspNetModule' class has been marked automatically due to usage of types that have no XAF .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.Chart.Web.ChartAspNetModule' has no equivalent in XAF .NET
    //     ChartAspNetModule has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.ExpressApp.PivotGrid.Web.PivotGridAspNetModule' has no equivalent in XAF .NET
    //     PivotGridAspNetModule has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.ExpressApp.Maps.Web.MapsAspNetModule' has no equivalent in XAF .NET
    //     MapsAspNetModule has no Blazor equivalent
    [ToolboxItemFilter("Xaf.Platform.Web")]
    public sealed partial class FeatureCenterAspNetModule : ModuleBase {
        public FeatureCenterAspNetModule() {
            InitializeComponent();
        }
        public override IEnumerable<ModuleUpdater> GetModuleUpdaters(IObjectSpace objectSpace, Version versionFromDB) {
            ModuleUpdater updater = new Updater(objectSpace, versionFromDB);
            return new ModuleUpdater[] { updater };
        }
    }

}
