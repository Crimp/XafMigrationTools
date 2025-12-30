using System;
using System.Collections.Generic;
using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.PivotGrid.Web;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.Base;

namespace FeatureCenter.Module.Web {
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
