using System.Collections.Generic;
using System.Web.UI;
using DevExpress.ExpressApp.Blazor;
using DevExpress.ExpressApp.Blazor.Layout;
using DevExpress.Web;

namespace FeatureCenter.Module.Web.Layout {
    // TODO: The 'FeatureCenter.Module.Web.Layout.CustomLayoutItemTemplate' class has been commented out automatically due to usage of types that have no XAF for .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF for .NET
    // NOTE:
    //   - Type 'LayoutItemTemplate' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Layout)
    //     LayoutItemTemplate has no Blazor equivalent (Web Forms layout specific)
    //   - Type 'LayoutItemTemplateContainer' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Layout)
    //     LayoutItemTemplateContainer has no equivalent in XAF for .NET (loaded from removed-api.txt)
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

    // TODO: The 'FeatureCenter.Module.Web.Layout.CustomLayoutGroupTemplate' class has been commented out automatically due to usage of types that have no XAF for .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF for .NET
    // NOTE:
    //   - Type 'LayoutGroupTemplate' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Layout)
    //     LayoutGroupTemplate has no Blazor equivalent (Web Forms layout specific)
    //   - Type 'LayoutItemTemplateContainerBase' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Layout)
    //     LayoutItemTemplateContainerBase has no equivalent in XAF for .NET (loaded from removed-api.txt)
    //   - Type 'LayoutGroupTemplateContainer' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Layout)
    //     LayoutGroupTemplateContainer has no equivalent in XAF for .NET (loaded from removed-api.txt)
    //   - Type 'ASPxImageHelper' has no equivalent (inferred from using DevExpress.ExpressApp.Web)
    //     ASPxImageHelper has no equivalent in XAF for .NET (loaded from removed-api.txt)
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

    // TODO: The 'FeatureCenter.Module.Web.Layout.CustomLayoutTabbedGroupTemplate' class has been commented out automatically due to usage of types that have no XAF for .NET equivalent.
    //       Breaking Change https://supportcenter.devexpress.com/ticket/details/t1312589
    //       Please review the class and implement necessary changes to ensure compatibility with XAF for .NET
    // NOTE:
    //   - Type 'TabbedGroupTemplate' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Layout)
    //     TabbedGroupTemplate has no Blazor equivalent (Web Forms layout specific)
    //   - Type 'TabbedGroupTemplateContainer' has no equivalent (inferred from using DevExpress.ExpressApp.Web.Layout)
    //     TabbedGroupTemplateContainer has no equivalent in XAF for .NET (loaded from removed-api.txt)
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