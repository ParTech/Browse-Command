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
                string str = item.ID.ToString();

                if (args.IsPostBack)
                {
                    if (args.Result != "yes")
                    {
                        return;
                    }

                    Item root = Context.ContentDatabase.GetItem(Context.Site.StartPath);

                    if (root == null)
                    {
                        SheerResponse.Alert("Start item not found.", new string[0]);
                        return;
                    }
                    else if (root.Visualization.Layout == null)
                    {
                        SheerResponse.Alert("The start item cannot be browsed to because it has no layout for the current device.\n\\nBrowse cannot be started.", new string[0]);
                        return;
                    }
                    else
                    {
                        str = root.ID.ToString();
                    }
                }
                else if (item.Visualization.Layout == null)
                {
                    SheerResponse.Confirm("The current item cannot be browsed to because it has no layout for the current device.\n\nDo you want to browse to the start Web page instead?");
                    args.WaitForPostBack();
                    return;
                }

                SheerResponse.CheckModified(false);
                SiteContext site = Factory.GetSite(global::Sitecore.Configuration.Settings.Preview.DefaultSite);

                Assert.IsNotNull(site, "Site \"{0}\" not found", new object[1]
                {
                  global::Sitecore.Configuration.Settings.Preview.DefaultSite
                });

                WebUtil.SetCookieValue(site.GetCookieKey("sc_date"), string.Empty);
                PreviewManager.StoreShellUser(true);

                UrlString webSiteUrl = SiteContext.GetWebSiteUrl();
                webSiteUrl["sc_itemid"] = str;
                webSiteUrl["sc_mode"] = "normal";
                webSiteUrl["sc_lang"] = item.Language.ToString();

                SheerResponse.Eval("window.open('" + webSiteUrl + "', '_blank')");
            }
        }
    }
}