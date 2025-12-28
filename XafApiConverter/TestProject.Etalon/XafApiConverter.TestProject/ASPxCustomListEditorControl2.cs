using System;
using System.Collections.Generic;
using System.Text;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp;
using System.Collections;
using System.ComponentModel;
using DevExpress.ExpressApp.Blazor.Editors;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Model;
using System.Web.UI;
using System.IO;
using System.Web;
using DevExpress.Web;
using FeatureCenter.Module.ListEditors;
using DevExpress.ExpressApp.Blazor.Templates;
using DevExpress.ExpressApp.Blazor;

namespace FeatureCenter.Module.Web.ListEditors {
    // TODO: The 'ASPxCustomListEditorControl2' class has been commented out automatically due to usage of types that have no XAF .NET equivalent.
    //       Please review the class and implement necessary changes to ensure compatibility with XAF .NET.
    // NOTE:
    //   - Type 'DevExpress.ExpressApp.Web.Templates.IXafCallbackHandler' has no equivalent in XAF .NET
    //     IXafCallbackHandler has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.ExpressApp.Web.Templates.XafCallbackManager' has no equivalent in XAF .NET
    //     XafCallbackManager has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.ExpressApp.Web.Templates.ICallbackManagerHolder' has no equivalent in XAF .NET
    //     ICallbackManagerHolder has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.ExpressApp.Web.ImageInfoEventArgs' has no equivalent in XAF .NET
    //     ImageInfoEventArgs has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Type 'DevExpress.ExpressApp.Web.ImageResourceHttpHandler' has no equivalent in XAF .NET
    //     ImageResourceHttpHandler has no Blazor equivalent (Web Forms specific HTTP handler)
    //   - Type 'DevExpress.ExpressApp.Web.WebImageHelper' has no equivalent in XAF .NET
    //     WebImageHelper has no equivalent in XAF .NET (loaded from removed-api.txt)
    //   - Class 'Paddings' has no equivalent in XAF .NET
    //     DevExpress.Web.v25.1 assembly is removed in v25.2
    // ========== COMMENTED OUT CLASS ==========
    // public class ASPxCustomListEditorControl2 : Panel, INamingContainer, IXafCallbackHandler {
    //         private IList dataSource;
    //         private Dictionary<string, System.Drawing.Image> images = new Dictionary<string, System.Drawing.Image>();
    //         private void RaiseItemClick(IPictureItem item) {
    //             if(OnClick != null) {
    //                 CustomListEditorClickEventArgs args = new CustomListEditorClickEventArgs();
    //                 args.ItemClicked = item;
    //                 OnClick(this, args);
    //             }
    //         }
    //         private IPictureItem FindItemByID(string ID) {
    //             if(dataSource == null)
    //                 return null;
    //
    //             foreach(IPictureItem item in dataSource) {
    //                 if(item.ID == ID)
    //                     return item;
    //             }
    //             return null;
    //         }
    //         private byte[] ImageToByteArray(System.Drawing.Image image) {
    //             if(image == null) {
    //                 throw new ArgumentNullException("image");
    //             }
    //             using(MemoryStream ms = new MemoryStream()) {
    //                 image.Save(ms, image.RawFormat);
    //                 return ms.ToArray();
    //             }
    //         }
    //         private XafCallbackManager CallbackManager {
    //             get { return Page != null ? ((ICallbackManagerHolder)Page).CallbackManager : null; }
    //         }
    //         private void ImageResourceHttpHandler_QueryImageInfo(object sender, ImageInfoEventArgs e) {
    //             if(e.Url.StartsWith("CLE")) {
    //                 lock(images) {
    //                     if(images.ContainsKey(e.Url)) {
    //                         System.Drawing.Image image = images[e.Url];
    //                         e.ImageInfo = new DevExpress.ExpressApp.Utils.ImageInfo("", image, "");
    //                         images.Remove(e.Url);
    //                     }
    //                 }
    //             }
    //         }
    //         protected override void OnInit(EventArgs e) {
    //             base.OnInit(e);
    //             Refresh();
    //         }
    //         protected override void CreateChildControls() {
    //             base.CreateChildControls();
    //             Refresh();
    //         }
    //         public ASPxCustomListEditorControl2() {
    //             ImageResourceHttpHandler.QueryImageInfo += new EventHandler<ImageInfoEventArgs>(ImageResourceHttpHandler_QueryImageInfo);
    //         }
    //         public void Refresh() {
    //             this.Controls.Clear();
    //             if(Page != null) {
    //                 int i = 0;
    //                 string noImageUrl = ImageLoader.Instance.GetImageInfo("NoImage").ImageUrl;
    //                 ArrayList list = new ArrayList(dataSource);
    //                 list.Sort(new PictureItemComparer());
    //                 foreach(IPictureItem item in list) {
    //                     Table table = new Table();
    //                     table.Style["display"] = "inline-block";
    //                     table.Style["vertical-align"] = "top";
    //                     this.Controls.Add(table);
    //                     table.BorderWidth = 0;
    //                     table.CellPadding = 5;
    //                     table.CellSpacing = 0;
    //                     table.Width = Unit.Pixel(124);
    //
    //                     ASPxCustomListEditorButton img = new ASPxCustomListEditorButton();
    //                     img.ID = this.ID + "_" + (i++).ToString();
    //                     img.PictureID = item.ID;
    //                     if(item.Image != null) {
    //                         string imageKey = "CLE_" + WebImageHelper.GetImageHash(item.Image);
    //                         img.ImageUrl = ImageResourceHttpHandler.GetWebResourceUrl(imageKey);
    //                         if(!images.ContainsKey(imageKey)) {
    //                             images.Add(imageKey, item.Image);
    //                         }
    //                     } else {
    //                         img.ImageUrl = noImageUrl;
    //                     }
    //                     img.Image.AlternateText = item.Text;
    //                     img.Image.Height = 150;
    //                     img.Image.Width = 104;
    //                     img.ToolTip = item.Text;
    //                     img.EnableViewState = false;
    //                     img.Paddings.Assign(new DevExpress.Web.Paddings(new Unit(0)));
    //                     img.FocusRectPaddings.Assign(new DevExpress.Web.Paddings(new Unit(0)));
    //                     img.AutoPostBack = false;
    //                     img.ClientSideEvents.Click = "function(s, e) {" + (CallbackManager != null ? CallbackManager.GetScript(this.UniqueID, string.Format("'{0}'", img.PictureID)) : String.Empty) + "}";
    //                     TableCell cell = new TableCell();
    //                     cell.Controls.Add(img);
    //                     cell.Style["text-align"] = "center";
    //                     table.Rows.Add(new TableRow());
    //                     table.Rows[0].Cells.Add(cell);
    //
    //                     Literal text = new Literal();
    //                     text.Text = item.Text;
    //                     cell = new TableCell();
    //                     cell.Style["font-size"] = "80%";
    //                     cell.Style["text-align"] = "center";
    //                     cell.Style["word-wrap"] = "break-word";
    //                     cell.Style["word-break"] = "break-word";
    //                     cell.Controls.Add(text);
    //                     table.Rows.Add(new TableRow());
    //                     table.Rows[1].Cells.Add(cell);
    //                 }
    //             }
    //         }
    //         public IList DataSource {
    //             get { return dataSource; }
    //             set { dataSource = value; }
    //         }
    //         public event EventHandler<CustomListEditorClickEventArgs> OnClick;
    //         #region IXafCallbackHandler Members
    //         public void ProcessAction(string parameter) {
    //             IPictureItem item = FindItemByID(parameter);
    //             if(item != null) {
    //                 RaiseItemClick(item);
    //             }
    //         }
    //         #endregion
    //     }
    // ========================================

}
