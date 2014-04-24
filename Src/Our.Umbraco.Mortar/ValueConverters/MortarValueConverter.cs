using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Mortar.Models;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Our.Umbraco.Mortar.ValueConverters
{
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
			return propertyType.PropertyEditorAlias == "Our.Umbraco.Mortar";
		}

		public override object ConvertDataToSource(PublishedPropertyType propertyType, object source, bool preview)
		{
			try
			{
				if (source != null && !source.ToString().IsNullOrWhiteSpace() && source.ToString() != "{}")
				{
					var value = JsonConvert.DeserializeObject<MortarValue>(source.ToString());

					var currentPageId = UmbracoContext.Current.PublishedContentRequest != null
						? UmbracoContext.Current.PublishedContentRequest.PublishedContent.Id
						: 0;

					// We get the JSON converter to do some initial conversion, but
					// created the IPublishedContent values requires some context
					// so we have to do them in an additional loop here
					foreach (var key in value.Keys)
					{
						foreach (var row in value[key])
						{
							foreach (var item in row.Items)
							{
								if (item != null && item.RawValue != null)
								{
									switch (item.Type.ToLowerInvariant())
									{
										case "richtext":
											// Do we need to create a fake doctype?
											var rtePropType = new PublishedPropertyType("bodyText", Constants.PropertyEditors.TinyMCEAlias);
											var rteContentType = new PublishedContentType(-1, "MortarRichtext", new[] {rtePropType});
											var rteProp = PublishedProperty.GetDetached(rtePropType.Nested(propertyType), item.RawValue, preview);
											item.Value = new NestedPublishedContent(currentPageId, rteContentType, new[] { rteProp });
											break;
										case "link":
											int nodeId;
											if (int.TryParse(item.RawValue.ToString(), out nodeId))
												item.Value = Umbraco.TypedContent(nodeId);
											break;
										case "doctype":
											if (item.AdditionalInfo.ContainsKey("docType") 
												&& item.AdditionalInfo["docType"] != null
												&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace())
											{
												var docTypeAlias = item.AdditionalInfo["docType"];
												var contentType = PublishedContentType.Get(PublishedItemType.Content, docTypeAlias);
												var properties = new List<IPublishedProperty>();

												var propValues = ((JObject) item.RawValue).ToObject<Dictionary<string, object>>(); // JsonConvert.DeserializeObject<Dictionary<string, object>>(item.RawValue);
												foreach (var jProp in propValues)
												{
													var propType = contentType.GetPropertyType(jProp.Key);
													var prop = PublishedProperty.GetDetached(propType.Nested(propertyType), jProp.Value == null ? "" : jProp.Value.ToString(), preview);
													properties.Add(prop);
												}

												item.Value = new NestedPublishedContent(currentPageId, contentType, properties.ToArray());
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
	}
}
