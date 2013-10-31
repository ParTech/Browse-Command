using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using Sitecore.Data;
using Sitecore.Sites;
using Sitecore.Configuration;
using Sitecore.Web;
using Sitecore.Publishing;
using Sitecore.Text;
using Sitecore.Links;

namespace ParTech.Commands
{
    /// <summary>
    /// Sitecore command that enables the user to browse to an item from the Sitecore tree (without going into Preview mode)
    /// </summary>
    public class Browse : PreviewItem
    {
        /// <summary>
        /// Runs the specified args.
        /// </summary>
        /// <param name="args">The arguments.</param>
        protected new void Run(ClientPipelineArgs args)
        {
            Item item = Database.GetItem(ItemUri.Parse(args.Parameters["uri"]));

            if (item == null)
            {
                SheerResponse.Alert("Item not found.");
            }
            else
            {
                // Use default preview website if site or device could not be determined.
                SiteContext site = Factory.GetSite(Settings.Preview.DefaultSite);

                // Try to find a Site entry that serves this item.
                // We well use that to determine which Device to use for previewing.
                SiteInfo siteInfo = SiteContextFactory.Sites
                    .FirstOrDefault(x => 
                        x.PhysicalFolder.Equals("/") && item.Paths.FullPath.StartsWith(string.Concat(x.RootPath, x.StartItem), StringComparison.InvariantCultureIgnoreCase)
                    );

                DeviceItem defaultDevice = null;

                if (siteInfo != null && !string.IsNullOrEmpty(siteInfo.DefaultDevice) && Context.ContentDatabase != null)
                {
                    site = Factory.GetSite(siteInfo.Name);
                    defaultDevice = Context.ContentDatabase.Resources.Devices[siteInfo.DefaultDevice];
                }

                Assert.IsNotNull(site, "Site \"{0}\" not found", new object[1]
                {
                    Settings.Preview.DefaultSite
                });

                // Check if there is a Layout present, otherwise we can't preview
                if (item.Visualization.Layout == null && (defaultDevice == null || item.Visualization.GetLayout(defaultDevice) == null))
                {
                    SheerResponse.Alert("The current item cannot be browsed to because it has no layout for the current or default device.");
                    return;
                }

                // Determine the hostname to use for previewing the selected item
                Uri siteHostName = new Uri(LinkManager.GetItemUrl(item, new UrlOptions
                {
                    AlwaysIncludeServerUrl = true,
                    SiteResolving = true
                }));

                UrlString webSiteUrl = new UrlString(WebUtil.GetServerUrl(siteHostName, false)); 
                
                // Set preview querystring parameters
                webSiteUrl["sc_itemid"] = item.ID.ToString();
                webSiteUrl["sc_mode"] = "normal";
                webSiteUrl["sc_lang"] = item.Language.ToString();

                WebUtil.SetCookieValue(site.GetCookieKey("sc_date"), string.Empty);
                PreviewManager.StoreShellUser(true);

                // Open the preview
                SheerResponse.CheckModified(false);
                SheerResponse.Eval("window.open('" + webSiteUrl + "', '_blank')");
            }
        }
    }
}