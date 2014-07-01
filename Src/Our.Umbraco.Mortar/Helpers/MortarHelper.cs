using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace Our.Umbraco.Mortar.Helpers
{
	internal static class MortarHelper
	{
		public static string GetRowOptionsDocType(int dtdId, string cellId)
		{
			var preValueCollection = (PreValueCollection)ApplicationContext.Current.ApplicationCache.RuntimeCache.GetCacheItem(
				string.Concat("Our.Umbraco.Mortar.Helpers.MortarHelper.GetRowOptionsDocType_GetPreValuesCollectionByDataTypeId_", dtdId),
				() => ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dtdId));

			return GetRowOptionsDocType(preValueCollection, cellId);
		}

		public static string GetRowOptionsDocType(PreValueCollection preValueCollection, string cellId)
		{
			var preValueDict = preValueCollection.PreValuesAsDictionary.ToDictionary(x => x.Key, x => x.Value.Value);

			// Check the grid config
			if (preValueDict.ContainsKey("gridConfig"))
			{
				var gridConfig = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(
						preValueDict["gridConfig"].ToString(CultureInfo.InvariantCulture));

				if (gridConfig != null && gridConfig.ContainsKey(cellId) && gridConfig[cellId].ContainsKey("rowOptionsDocType"))
				{
					return gridConfig[cellId]["rowOptionsDocType"].ToString();
				}
			}

			// Check the default config
			if (preValueDict.ContainsKey("defaultConfig"))
			{
				var defaultConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(
						preValueDict["defaultConfig"].ToString(CultureInfo.InvariantCulture));

				if (defaultConfig != null && defaultConfig.ContainsKey("rowOptionsDocType"))
				{
					return defaultConfig["rowOptionsDocType"].ToString();
				}
			}

			return null;
		}
	}
}
