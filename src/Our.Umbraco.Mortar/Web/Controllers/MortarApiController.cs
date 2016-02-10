﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using Our.Umbraco.Mortar.Web.Extensions;
using Umbraco.Core.IO;
using Umbraco.Core.Models;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.Editors;
using Umbraco.Web.Mvc;

namespace Our.Umbraco.Mortar.Web.Controllers
{
	[PluginController("MortarApi")]
	public class MortarApiController : UmbracoAuthorizedJsonController
	{
		[HttpGet]
		public object GetContentTypeAliasByGuid([ModelBinder] Guid guid)
		{
			return new
			{
				alias = Services.ContentTypeService.GetAliasByGuid(guid)
			};
		}

		[HttpGet]
		public IEnumerable<object> GetContentTypes([ModelBinder] string[] allowedContentTypes)
		{
			return Services.ContentTypeService.GetAllContentTypes()
				.Where(x => allowedContentTypes == null || allowedContentTypes.Length == 0 || allowedContentTypes.Any(y => Regex.IsMatch(x.Alias, y)))
				.OrderBy(x => x.Name)
				.Select(x => new
				{
					id = x.Id,
					guid = x.Key,
					name = x.Name,
					alias = x.Alias,
					icon = x.Icon
				});
		}

		[HttpGet]
		public object GetDataTypePreValues(string dtdId)
		{
			Guid guidDtdId;
			int intDtdId;

			IDataTypeDefinition dtd;

			// Parse the ID
			if (int.TryParse(dtdId, out intDtdId))
			{
				// Do nothing, we just want the int ID
				dtd = Services.DataTypeService.GetDataTypeDefinitionById(intDtdId);
			}
			else if (Guid.TryParse(dtdId, out guidDtdId))
			{
				dtd = Services.DataTypeService.GetDataTypeDefinitionById(guidDtdId);
			}
			else
			{
				return null;
			}

			if (dtd == null)
				return null;

			// Convert to editor config
			var preValue = Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtd.Id);
			var propEditor = PropertyEditorResolver.Current.GetByAlias(dtd.PropertyEditorAlias);
			return propEditor.PreValueEditor.ConvertDbToEditor(propEditor.DefaultPreValues, preValue);
		}

		[HttpGet]
		public object GetDocTypePreview([ModelBinder] Guid guid)
		{
			var path = IOHelper.MapPath("~/App_Plugins/Mortar/Views/Previews/{0}.html");
			var view = string.Empty;

			var file = string.Format(path, guid);
			if (System.IO.File.Exists(file))
				view = System.IO.File.ReadAllText(file);

			if (string.IsNullOrWhiteSpace(view))
			{
				var alias = Services.ContentTypeService.GetAliasByGuid(guid);
				var file2 = string.Format(path, alias);
				if (System.IO.File.Exists(file2))
					view = System.IO.File.ReadAllText(file2);
			}

			return new { view = view };
		}
	}
}