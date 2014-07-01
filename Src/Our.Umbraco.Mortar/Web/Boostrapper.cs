using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace Our.Umbraco.Mortar.Web
{
	public class Boostrapper : IApplicationEventHandler
	{
		public void OnApplicationInitialized(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{ }

		public void OnApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{ }

		public void OnApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{
			DataTypeService.Saved += ExpireMortarCache;
		}

		private void ExpireMortarCache(IDataTypeService sender, global::Umbraco.Core.Events.SaveEventArgs<global::Umbraco.Core.Models.IDataTypeDefinition> e)
		{
			foreach (var dataType in e.SavedEntities)
			{
				ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheItem(
					string.Concat("Our.Umbraco.Mortar.Web.Extensions.ContentTypeServiceExtensions.GetAliasById_", dataType.Key));

				ApplicationContext.Current.ApplicationCache.RuntimeCache.ClearCacheItem(
					string.Concat("Our.Umbraco.Mortar.Helpers.MortarHelper.GetRowOptionsDocType_GetPreValuesCollectionByDataTypeId_", dataType.Id));
			}
		}
	}
}
