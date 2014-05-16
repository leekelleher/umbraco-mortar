using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace Our.Umbraco.Mortar.Web.Extensions
{
	internal static class ContentTypeServiceExtensions
	{
		public static string GetAliasByGuid(this IContentTypeService contentTypeService, Guid id)
		{
			return (string)ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem(
				"Our.Umbraco.Mortar.Web.Extensions.ContentTypeServiceExtensions.GetAliasById_" + id,
				() => ApplicationContext.Current.DatabaseContext.Database
					.ExecuteScalar<string>("SELECT [cmsContentType].[alias] FROM [cmsContentType] INNER JOIN [umbracoNode] ON [cmsContentType].[nodeId] = [umbracoNode].[id] WHERE [umbracoNode].[uniqueID] = @0", id));

		}
	}
}
