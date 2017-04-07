using System;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.Migrations;
using Umbraco.Core.Services;

namespace Our.Umbraco.Mortar.Web
{
	public class Boostrapper : ApplicationEventHandler
	{
		protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
		{
			DataTypeService.Saved += ExpireMortarCache;

			ApplyMigrations(applicationContext, MortarConstants.PackageNameAlias, MortarConstants.CurrentVersion, MortarConstants.ApplicationVersion);
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

		private void ApplyMigrations(ApplicationContext applicationContext, string productName, Version currentVersion, Version targetVersion)
		{
			// TODO: [LK] If Mortar supports a version of Umbraco later than v7.3.0,
			// then we can make use of the MigrationEntryService database table.

			if (currentVersion == null)
				currentVersion = new Version(0, 0, 0);

			if (targetVersion == currentVersion)
				return;

			var migrationRunner = new MigrationRunner(currentVersion, targetVersion, productName);

			try
			{
				migrationRunner.Execute(applicationContext.DatabaseContext.Database);
			}
			catch (System.Web.HttpException)
			{
				// because umbraco runs some other migrations after the migration runner
				// is executed we get HttpException
				// catch this error, but don't do anything
				// fixed in 7.4.2+ see : http://issues.umbraco.org/issue/U4-8077
			}
			catch (Exception ex)
			{
				LogHelper.Error<Boostrapper>("Error running migration.", ex);
			}
		}
	}
}