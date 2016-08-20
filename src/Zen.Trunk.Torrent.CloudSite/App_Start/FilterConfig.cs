using System.Web;
using System.Web.Mvc;

namespace Zen.Trunk.Torrent.CloudSite
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}