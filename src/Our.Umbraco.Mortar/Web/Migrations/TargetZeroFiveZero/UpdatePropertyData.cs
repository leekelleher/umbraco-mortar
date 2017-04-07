using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Migrations;

namespace Our.Umbraco.Mortar.Web.Migrations.TargetZeroFiveZero
{
	[Migration("0.5.0", 1, MortarConstants.PackageNameAlias)]
	public sealed class UpdatePropertyData : IMigration
	{
		public void Down()
		{ }

		public void Up()
		{
			if (ApplicationContext.Current == null || ApplicationContext.Current.DatabaseContext == null)
				return;

			var dbCtx = ApplicationContext.Current.DatabaseContext;

			// get a list of all the document type aliases & GUIDs
			var sql = @"SELECT n.uniqueID, c.alias FROM cmsContentType AS c INNER JOIN umbracoNode AS n ON n.id = c.nodeId WHERE n.nodeObjectType = 'A2CB7800-F571-4787-9638-BC48539A0EFB';";
			var contentTypes = dbCtx.Database.Fetch<ContentTypeContainer>(sql);

			if (contentTypes == null || !contentTypes.Any())
				return;

			// get the node IDs for all the content that use Mortar
			var sql2 = @"SELECT c.nodeId, pt.Alias FROM cmsDataType AS dt INNER JOIN cmsPropertyType AS pt ON pt.dataTypeId = dt.nodeId INNER JOIN cmsContent AS c ON c.contentType = pt.contentTypeId WHERE dt.propertyEditorAlias = @0;";
			var contentProperties = dbCtx.Database.Fetch<ContentPropertyContainer>(sql2, MortarConstants.PackageNameAlias);

			if (contentProperties == null || !contentProperties.Any())
				return;

			var replacements = contentTypes.ToDictionary(x => x.UniqueId.ToString("D"), x => x.Alias, StringComparer.InvariantCultureIgnoreCase);

			var contentService = ApplicationContext.Current.Services.ContentService;
			var toBeSaved = new List<IContent>();

			foreach (var contentProperty in contentProperties)
			{
				var content = contentService.GetById(contentProperty.NodeId);

				var value = content.GetValue<string>(contentProperty.PropertyAlias);

				if (string.IsNullOrWhiteSpace(value))
					continue;

				// do a string replace - swapping the GUID with the alias
				var newValue = value.ReplaceMany(replacements);

				if (newValue != value)
				{
					// set the property value
					content.SetValue(contentProperty.PropertyAlias, newValue);

					toBeSaved.Add(content);
				}
			}

			// save the content nodes
			if (toBeSaved.Count > 0)
			{
				contentService.Save(toBeSaved, raiseEvents: false);
			}

			// set the latest version number
			var path = IOHelper.MapPath("~/App_Plugins/Mortar/version");
			System.IO.File.WriteAllText(path, "0.5.0");
		}

		public class ContentTypeContainer
		{
			[Column("uniqueID")]
			public Guid UniqueId { get; set; }

			[Column("alias")]
			public string Alias { get; set; }
		}

		public class ContentPropertyContainer
		{
			[Column("nodeId")]
			public int NodeId { get; set; }

			[Column("Alias")]
			public string PropertyAlias { get; set; }
		}
	}
}