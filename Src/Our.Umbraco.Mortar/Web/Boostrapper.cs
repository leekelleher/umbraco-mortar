using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Our.Umbraco.Mortar.Web
{
	public class Boostrapper : ApplicationEventHandler
	{
		protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{
			DataTypeService.Saved += ExpireMortarCache;
		}

		private void ExpireMortarCache(IDataTypeService sender, SaveEventArgs<IDataTypeDefinition> e)
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