using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using DevExpress.ExpressApp.Web;
using DevExpress.ExpressApp.Web.Layout;
using DevExpress.Web;

namespace FeatureCenter.Module.Web.Layout {
    public class CustomLayoutItemTemplate : LayoutItemTemplate {
        protected override Control CreateCaptionControl(LayoutItemTemplateContainer layoutItemTemplateContainer) {
            Table table = new Table();
            table.Rows.Add(new TableRow());
            table.Rows[0].Cells.Add(new TableCell());
            table.Rows[0].Cells.Add(new TableCell());
            Control baseControl = base.CreateCaptionControl(layoutItemTemplateContainer);
            table.Rows[0].Cells[0].Controls.Add(baseControl);
            ASPxHyperLink anchor = new ASPxHyperLink();
            anchor.Text = "?";
            anchor.Style.Add(HtmlTextWriterStyle.FontWeight, "bold");
            anchor.Style.Add(HtmlTextWriterStyle.TextDecoration, "underline");
            anchor.NavigateUrl = "javascript:void(0);";
            anchor.ToolTip = string.Format("Description for the '{0}' item", layoutItemTemplateContainer.ViewItem.Caption);
            table.Rows[0].Cells[1].Controls.Add(anchor);
            return table;
        }
    }
    public class CustomLayoutGroupTemplate : LayoutGroupTemplate {
        private static void AddControls(ControlCollection controlCollection, IEnumerable<Control> controlsToLayout) {
            foreach(Control control in controlsToLayout) {
                LayoutItemTemplateContainerBase templateContainer = control as LayoutItemTemplateContainerBase;
                if(templateContainer != null && templateContainer.LayoutManager.DelayedItemsInitialization) {
                    templateContainer.Instantiate();
                }
                controlCollection.Add(control);
            }
        }
        protected override void LayoutContentControls(LayoutGroupTemplateContainer layoutGroupTemplateContainer, IList<Control> controlsToLayout) {
            if(layoutGroupTemplateContainer.ShowCaption) {
                Panel panel = new Panel();
                panel.Style.Add(HtmlTextWriterStyle.Padding, "10px 5px 10px 5px");
                ASPxRoundPanel roundPanel = new ASPxRoundPanel();
                roundPanel.Width = Unit.Percentage(100);
                roundPanel.HeaderText = layoutGroupTemplateContainer.Caption;
                if(layoutGroupTemplateContainer.HasHeaderImage) {
                    ASPxImageHelper.SetImageProperties(roundPanel.HeaderImage, layoutGroupTemplateContainer.HeaderImageInfo);
                }
                AddControls(roundPanel.Controls, controlsToLayout);
                panel.Controls.Add(roundPanel);
                layoutGroupTemplateContainer.Controls.Add(panel);
            }
            else {
                AddControls(layoutGroupTemplateContainer.Controls, controlsToLayout);
            }
        }
    }
    public class CustomLayoutTabbedGroupTemplate : TabbedGroupTemplate {
        protected override ASPxPageControl CreatePageControl(TabbedGroupTemplateContainer tabbedGroupTemplateContainer) {
            ASPxPageControl pageControl = base.CreatePageControl(tabbedGroupTemplateContainer);
            pageControl.TabPosition = TabPosition.Left;
            pageControl.ContentStyle.Paddings.Padding = Unit.Pixel(10);
            return pageControl;
        }
    }
}
