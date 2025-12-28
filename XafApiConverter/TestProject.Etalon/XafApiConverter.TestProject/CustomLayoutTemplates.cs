using System.Collections.Generic;
using System.Web.UI;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Layout;
using DevExpress.Web;

namespace FeatureCenter.Module.Web.Layout {
    // TODO: The 'CustomLayoutItemTemplate' class has been commented out automatically due to usage of types that have no XAF .NET equivalent.
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.Web.Layout.LayoutItemTemplate' has no equivalent in XAF .NET
    //     LayoutItemTemplate has no Blazor equivalent (Web Forms layout specific)
    //   - Type 'DevExpress.ExpressApp.Web.Layout.LayoutItemTemplateContainer' has no equivalent in XAF .NET
    //     LayoutItemTemplateContainer has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Class 'ASPxHyperLink' has no equivalent in XAF .NET
    //     DevExpress.Web.v25.1 assembly is removed in v25.2
    // ========== COMMENTED OUT CLASS ==========
    // public class CustomLayoutItemTemplate : LayoutItemTemplate {
    //         protected override Control CreateCaptionControl(LayoutItemTemplateContainer layoutItemTemplateContainer) {
    //             Table table = new Table();
    //             table.Rows.Add(new TableRow());
    //             table.Rows[0].Cells.Add(new TableCell());
    //             table.Rows[0].Cells.Add(new TableCell());
    //             Control baseControl = base.CreateCaptionControl(layoutItemTemplateContainer);
    //             table.Rows[0].Cells[0].Controls.Add(baseControl);
    //             ASPxHyperLink anchor = new ASPxHyperLink();
    //             anchor.Text = "?";
    //             anchor.Style.Add(HtmlTextWriterStyle.FontWeight, "bold");
    //             anchor.Style.Add(HtmlTextWriterStyle.TextDecoration, "underline");
    //             anchor.NavigateUrl = "javascript:void(0);";
    //             anchor.ToolTip = string.Format("Description for the '{0}' item", layoutItemTemplateContainer.ViewItem.Caption);
    //             table.Rows[0].Cells[1].Controls.Add(anchor);
    //             return table;
    //         }
    //     }
    // ========================================

    // TODO: The 'CustomLayoutGroupTemplate' class has been commented out automatically due to usage of types that have no XAF .NET equivalent.
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.Web.Layout.LayoutGroupTemplate' has no equivalent in XAF .NET
    //     LayoutGroupTemplate has no Blazor equivalent (Web Forms layout specific)
    //   - Type 'DevExpress.ExpressApp.Web.Layout.LayoutItemTemplateContainerBase' has no equivalent in XAF .NET
    //     LayoutItemTemplateContainerBase has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.ExpressApp.Web.Layout.LayoutGroupTemplateContainer' has no equivalent in XAF .NET
    //     LayoutGroupTemplateContainer has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Class 'ASPxRoundPanel' has no equivalent in XAF .NET
    //     DevExpress.Web.v25.1 assembly is removed in v25.2
    //   - Type 'DevExpress.ExpressApp.Web.ASPxImageHelper' has no equivalent in XAF .NET
    //     ASPxImageHelper has no equivalent in XAF .NET (loaded from removed-api.txt)
    // ========== COMMENTED OUT CLASS ==========
    // public class CustomLayoutGroupTemplate : LayoutGroupTemplate {
    //         private static void AddControls(ControlCollection controlCollection, IEnumerable<Control> controlsToLayout) {
    //             foreach(Control control in controlsToLayout) {
    //                 LayoutItemTemplateContainerBase templateContainer = control as LayoutItemTemplateContainerBase;
    //                 if(templateContainer != null && templateContainer.LayoutManager.DelayedItemsInitialization) {
    //                     templateContainer.Instantiate();
    //                 }
    //                 controlCollection.Add(control);
    //             }
    //         }
    //         protected override void LayoutContentControls(LayoutGroupTemplateContainer layoutGroupTemplateContainer, IList<Control> controlsToLayout) {
    //             if(layoutGroupTemplateContainer.ShowCaption) {
    //                 Panel panel = new Panel();
    //                 panel.Style.Add(HtmlTextWriterStyle.Padding, "10px 5px 10px 5px");
    //                 ASPxRoundPanel roundPanel = new ASPxRoundPanel();
    //                 roundPanel.Width = Unit.Percentage(100);
    //                 roundPanel.HeaderText = layoutGroupTemplateContainer.Caption;
    //                 if(layoutGroupTemplateContainer.HasHeaderImage) {
    //                     ASPxImageHelper.SetImageProperties(roundPanel.HeaderImage, layoutGroupTemplateContainer.HeaderImageInfo);
    //                 }
    //                 AddControls(roundPanel.Controls, controlsToLayout);
    //                 panel.Controls.Add(roundPanel);
    //                 layoutGroupTemplateContainer.Controls.Add(panel);
    //             }
    //             else {
    //                 AddControls(layoutGroupTemplateContainer.Controls, controlsToLayout);
    //             }
    //         }
    //     }
    // ========================================

    // TODO: The 'CustomLayoutTabbedGroupTemplate' class has been commented out automatically due to usage of types that have no XAF .NET equivalent.
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.Web.Layout.TabbedGroupTemplate' has no equivalent in XAF .NET
    //     TabbedGroupTemplate has no Blazor equivalent (Web Forms layout specific)
    //   - Class 'ASPxPageControl' has no equivalent in XAF .NET
    //     DevExpress.Web.v25.1 assembly is removed in v25.2
    //   - Type 'DevExpress.ExpressApp.Web.Layout.TabbedGroupTemplateContainer' has no equivalent in XAF .NET
    //     TabbedGroupTemplateContainer has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Class 'TabPosition' has no equivalent in XAF .NET
    //     DevExpress.Web.v25.1 assembly is removed in v25.2
    // ========== COMMENTED OUT CLASS ==========
    // public class CustomLayoutTabbedGroupTemplate : TabbedGroupTemplate {
    //         protected override ASPxPageControl CreatePageControl(TabbedGroupTemplateContainer tabbedGroupTemplateContainer) {
    //             ASPxPageControl pageControl = base.CreatePageControl(tabbedGroupTemplateContainer);
    //             pageControl.TabPosition = TabPosition.Left;
    //             pageControl.ContentStyle.Paddings.Padding = Unit.Pixel(10);
    //             return pageControl;
    //         }
    //     }
    // ========================================

}
