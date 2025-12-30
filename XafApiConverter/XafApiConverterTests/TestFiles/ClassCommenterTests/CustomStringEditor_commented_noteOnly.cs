using System;
using System.Globalization;

using DevExpress.Web;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Blazor.Editors;
using FeatureCenter.Module.PropertyEditors;
using DevExpress.ExpressApp.Model;

namespace FeatureCenter.Module.Web.PropertyEditors {
    // TODO: The 'CustomStringEditor' class has been marked automatically due to usage of types that have no XAF .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'ASPxPropertyEditor' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Editors.ASPx)
    //     ASPxPropertyEditor has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'RenderHelper' has no equivalent (inferred from using DevExpress.ExpressApp.Web)
    //     RenderHelper has no equivalent in XAF .NET (loaded from removed-api.txt)
    [PropertyEditor(typeof(String), FeatureCenterEditorAliases.CustomStringEditor, false)]
	public class CustomStringEditor : ASPxPropertyEditor {
        ASPxComboBox dropDownControl = null;

        public CustomStringEditor(Type objectType, IModelMemberViewItem info) : base(objectType, info) { }

		protected override void SetupControl(WebControl control) {
			if(ViewEditMode == ViewEditMode.Edit) {
				foreach(CultureInfo culture in CultureInfo.GetCultures(CultureTypes.InstalledWin32Cultures)) {
					((ASPxComboBox)control).Items.Add(culture.EnglishName + "(" + culture.Name + ")");
				}
			}
		}
		protected override WebControl CreateEditModeControlCore() {
			dropDownControl = RenderHelper.CreateASPxComboBox();
            dropDownControl.ValueChanged += new EventHandler(EditValueChangedHandler);
			return dropDownControl;
		}
        public override void BreakLinksToControl(bool unwireEventsOnly) {
            if(dropDownControl != null) {
                dropDownControl.ValueChanged -= new EventHandler(EditValueChangedHandler);
            }
            base.BreakLinksToControl(unwireEventsOnly);
        }
	}
}