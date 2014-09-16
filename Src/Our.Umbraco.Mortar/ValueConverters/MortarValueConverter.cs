﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Mortar.Extensions;
using Our.Umbraco.Mortar.Helpers;
using Our.Umbraco.Mortar.Models;
using Our.Umbraco.Mortar.Web.Extensions;
using Our.Umbraco.Mortar.Web.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Our.Umbraco.Mortar.ValueConverters
{
	[PropertyValueType(typeof(MortarValue))]
	[PropertyValueCache(PropertyCacheValue.All, PropertyCacheLevel.Content)]
	public class MortarValueConverter : PropertyValueConverterBase
	{
		private UmbracoHelper _umbraco;
		internal UmbracoHelper Umbraco
		{
			get { return _umbraco ?? (_umbraco = new UmbracoHelper(UmbracoContext.Current)); }
		}

		public override bool IsConverter(PublishedPropertyType propertyType)
		{
			return propertyType.PropertyEditorAlias.InvariantEquals(MortarPropertyEditor.PropertyEditorAlias);
		}

		public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
		{
			try
			{
				if (source != null && !source.ToString().IsNullOrWhiteSpace() && source.ToString() != "{}")
				{
					var value = JsonConvert.DeserializeObject<MortarValue>(source.ToString());

					// We get the JSON converter to do some initial conversion, but
					// created the IPublishedContent values requires some context
					// so we have to do them in an additional loop here
					foreach (var key in value.Keys)
					{
						var rowOptionsDocTypeAlias = MortarHelper.GetRowOptionsDocType(propertyType.DataTypeId, key);

						foreach (var row in value[key])
						{
							if (!string.IsNullOrWhiteSpace(rowOptionsDocTypeAlias))
								row.Options = ConvertDataToSource_DocType(propertyType, rowOptionsDocTypeAlias, row.RawOptions, preview);

							foreach (var item in row.Items)
							{
								if (item != null && item.RawValue != null)
								{
									switch (item.Type.ToLowerInvariant())
									{
										case "richtext":
											item.Value = ConvertDataToSource_Fake(propertyType, "MortarRichtext", Constants.PropertyEditors.TinyMCEAlias, "bodyText", item.RawValue, preview); 
											break;
										case "embed":
											item.Value = ConvertDataToSource_Fake(propertyType, "MortarEmbed", Constants.PropertyEditors.TextboxMultipleAlias, "embedCode", item.RawValue, preview);
											break;
										case "link":
											item.Value = ConvertDataToSource_Link(propertyType, item.RawValue, preview);
											break;
										case "media":
											item.Value = ConvertDataToSource_Media(propertyType, item.RawValue, preview);
											break;
										case "doctype":
											Guid docTypeGuid;
											if (item.AdditionalInfo.ContainsKey("docType")
												&& item.AdditionalInfo["docType"] != null
												&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace())
											{
												// Lookup the doctype
												var docTypeAlias = item.AdditionalInfo["docType"];

												// We make an assumption that the docTypeAlias is a Guid and attempt to parse it,
												// failing that we assume that the docTypeAlias is the actual alias.
												if (Guid.TryParse(docTypeAlias, out docTypeGuid))
													docTypeAlias = ApplicationContext.Current.Services.ContentTypeService.GetAliasByGuid(docTypeGuid);

												item.Value = ConvertDataToSource_DocType(propertyType, docTypeAlias, item.RawValue, preview);
											}
											break;
									}
								}
							}
						}
					}

					return value;
				}
			}
			catch (Exception e)
			{
				LogHelper.Error<MortarValueConverter>("Error converting value", e);
			}

			return null;
		}

		protected IPublishedContent ConvertDataToSource_Fake(PublishedPropertyType propertyType, string docTypeAlias, string propEditorAlias, string propAlias, object value, bool preview)
		{
			var fakePropType = (PublishedPropertyType) typeof(PublishedPropertyType).GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				null, new[] {typeof (string), typeof (string)}, null)
				.Invoke(new object[] { propAlias, propEditorAlias });

			var fakeContentType = (PublishedContentType)typeof(PublishedContentType).GetConstructor(
				BindingFlags.NonPublic | BindingFlags.Instance,
				null, new[] { typeof(int), typeof(string), typeof(IEnumerable<PublishedPropertyType>) }, null)
				.Invoke(new object[] { -1, docTypeAlias, new[] { fakePropType } });

			var fakeNestedPropType = fakePropType.ExecuteMethod<PublishedPropertyType>("Nested",
				propertyType);

			var fakeProp = typeof(PublishedProperty).ExecuteMethod<IPublishedProperty>("GetDetached",
				fakeNestedPropType,
				(value == null ? "" : value.ToString()) as object,
				preview);

			return new DetachedPublishedContent(null, fakeContentType, new[] { fakeProp });
		}

		protected IPublishedContent ConvertDataToSource_Link(PublishedPropertyType propertyType, object value, bool preview)
		{
			int nodeId;
			return int.TryParse(value.ToString(), out nodeId) 
				? Umbraco.TypedContent(nodeId) 
				: null;
		}

		protected IPublishedContent ConvertDataToSource_Media(PublishedPropertyType propertyType, object value, bool preview)
		{
			int nodeId;
			return int.TryParse(value.ToString(), out nodeId)
				? Umbraco.TypedMedia(nodeId)
				: null;
		}

		protected IPublishedContent ConvertDataToSource_DocType(PublishedPropertyType propertyType, string docTypeAlias, object value, bool preview)
		{
			var contentType = PublishedContentType.Get(PublishedItemType.Content, docTypeAlias);
			var properties = new List<IPublishedProperty>();

			// Convert all the properties
			var propValues = ((JObject)value).ToObject<Dictionary<string, object>>();
			foreach (var jProp in propValues)
			{
				var propType = contentType.GetPropertyType(jProp.Key);
				if (propType != null)
				{
					//var prop = PublishedProperty.GetDetached(propType.Nested(propertyType),
					//	jProp.Value == null ? "" : jProp.Value.ToString(), preview);
					//properties.Add(prop);

					var nestedPropType = propType.ExecuteMethod<PublishedPropertyType>("Nested",
						propertyType);
					var prop = typeof(PublishedProperty).ExecuteMethod<IPublishedProperty>("GetDetached",
						nestedPropType,
						(jProp.Value == null ? "" : jProp.Value.ToString()) as object,
						preview);
					properties.Add(prop);
				}
			}

			// Parse out the name manually
			object nameObj = null;
			if (propValues.TryGetValue("name", out nameObj))
			{
				// Do nothing, we just want to parse out the name if we can
			}

			return new DetachedPublishedContent(nameObj == null ? null : nameObj.ToString(), contentType, properties.ToArray());
		}
	}
}