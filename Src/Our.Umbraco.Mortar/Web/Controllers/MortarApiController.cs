using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;

namespace Our.Umbraco.Mortar.Web.Controllers
{
	[PluginController("MortarApi")]
	public class MortarApiController : UmbracoAuthorizedJsonController
	{
		public IEnumerable<object> GetContentTypes()
		{
			return Services.ContentTypeService.GetAllContentTypes()
				.OrderBy(x => x.SortOrder)
				.Select(x => new
				{
					id = x.Id,
					guid = x.Key,
					name = x.Name,
					alias = x.Alias,
					icon = x.Icon
				});
		}
	}
}
