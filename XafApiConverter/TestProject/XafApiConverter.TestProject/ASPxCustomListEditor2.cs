using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp;
using System.Collections;
using System.ComponentModel;
using DevExpress.ExpressApp.Web.Editors.ASPx;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Model;
using System.Web.UI;
using System.IO;
using System.Web;
using DevExpress.Web;
using FeatureCenter.Module.ListEditors;
using DevExpress.ExpressApp.Web.Templates;
using DevExpress.ExpressApp.Web;

namespace FeatureCenter.Module.Web.ListEditors {
    [ListEditor(typeof(IPictureItem))]
    public class ASPxCustomListEditor2 : ListEditor {
        private ASPxCustomListEditorControl2 control;
        private object focusedObject;
        private void control_OnClick(object sender, CustomListEditorClickEventArgs e) {
            this.FocusedObject = e.ItemClicked;
            OnSelectionChanged();
            OnProcessSelectedItem();
        }
        protected override object CreateControlsCore() {
            control = new ASPxCustomListEditorControl2();
            control.ID = "CustomListEditor_control";
            control.OnClick += new EventHandler<CustomListEditorClickEventArgs>(control_OnClick);
            return control;
        }
        protected override void AssignDataSourceToControl(Object dataSource) {
            if(control != null) {
                control.DataSource = ListHelper.GetList(dataSource);
            }
        }
        protected override void OnSelectionChanged() {
            base.OnSelectionChanged();
        }
        public ASPxCustomListEditor2(IModelListView info) : base(info) { }
        public override IList GetSelectedObjects() {
            List<object> selectedObjects = new List<object>();
            if(FocusedObject != null) {
                selectedObjects.Add(FocusedObject);
            }
            return selectedObjects;
        }
        public override void Refresh() {
            if(control != null) control.Refresh();
        }
        public override void SaveModel() {
        }
        public override object FocusedObject {
            get {
                return focusedObject;
            }
            set {
                focusedObject = value;
            }
        }
        public override DevExpress.ExpressApp.Templates.IContextMenuTemplate ContextMenuTemplate {
            get { return null; }
        }
        public override bool AllowEdit {
            get {
                return false;
            }
            set {
            }
        }
        public override SelectionType SelectionType {
            get { return SelectionType.TemporarySelection; }
        }
    }
}
