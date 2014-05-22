using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Mortar.Models;
using Our.Umbraco.Mortar.ValueConverters;
using Our.Umbraco.Mortar.Web.PropertyEditors;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Courier.Core;
using Umbraco.Courier.Core.Enums;
using Umbraco.Courier.Core.Helpers;
using Umbraco.Courier.DataResolvers;
using Umbraco.Courier.ItemProviders;
using Umbraco.Web;

namespace Our.Umbraco.Mortar.DataResolvers
{
	public class MortarDataResolver : PropertyDataResolverProvider
	{
		private enum Direction
		{
			Extracting,
			Packaging
		}

		public MortarDataResolver()
		{
			// HACK: Turns out that `PublishedProperty.GetDetached` requires `UmbracoContext.Current`,
			// however `UmbracoContext.Current` is null during the extraction process.
			// So it looks like we have no choice but to fake it.
			// Hat-Tip: https://gist.github.com/sniffdk/7600822
			if (UmbracoContext.Current == null)
			{
				if (HttpContext.Current == null)
					HttpContext.Current = new HttpContext(new HttpRequest("", "http://tempuri.org", ""), new HttpResponse(new StringWriter()));

				UmbracoContext.EnsureContext(new HttpContextWrapper(HttpContext.Current), ApplicationContext.Current, true);
			}
		}

		public override string EditorAlias
		{
			get
			{
				return MortarPropertyEditor.PropertyEditorAlias;
			}
		}

		public override void ExtractingProperty(Item item, ContentProperty propertyData)
		{
			ResolvePropertyData(item, propertyData, Direction.Extracting);
		}

		public override void PackagingProperty(Item item, ContentProperty propertyData)
		{
			ResolvePropertyData(item, propertyData, Direction.Packaging);
		}

		private object ConvertIdentifier(object value, Item item, IdentifierReplaceDirection direction, Guid providerId)
		{
			if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
			{
				var guid = Dependencies.ConvertIdentifier(value.ToString(), direction);

				if (direction == IdentifierReplaceDirection.FromNodeIdToGuid)
					item.Dependencies.Add(guid, providerId);

				return guid;
			}

			return value;
		}

		private PublishedPropertyType CreateFakePropertyType(int dataTypeId, string propertyEditorAlias)
		{
			return new PublishedPropertyType(null, new PropertyType(new DataTypeDefinition(-1, propertyEditorAlias) { Id = dataTypeId }));
		}

		private void ResolvePropertyData(Item item, ContentProperty propertyData, Direction direction)
		{
			if (propertyData == null || propertyData.Value == null)
				return;

			// just look at the amount of dancing around we have to do in order to fake a `PublishedPropertyType`?!
			var dataTypeId = PersistenceManager.Default.GetNodeId(propertyData.DataType, NodeObjectTypes.DataType);
			var fakePropertyType = CreateFakePropertyType(dataTypeId, EditorAlias);

			var converter = new MortarValueConverter();
			var mortarValue = (MortarValue)converter.ConvertDataToSource(fakePropertyType, propertyData.Value, false);

			// create a 'fake' provider, as ultimately only the 'Packaging' enum will be referenced.
			var fakeItemProvider = new PropertyItemProvider();

			if (mortarValue != null)
			{
				foreach (var mortarBlock in mortarValue)
				{
					foreach (var mortarRow in mortarBlock.Value)
					{
						if (mortarRow.Options != null)
							mortarRow.RawOptions = ResolveMultiplePropertyItemData(item, fakeItemProvider, mortarRow.Options, mortarRow.RawOptions, direction);

						foreach (var mortarItem in mortarRow.Items)
						{
							switch (mortarItem.Type.ToUpperInvariant())
							{
								case "DOCTYPE":
									// resolve the doctype alias/guid
									if (mortarItem.AdditionalInfo.ContainsKey("docType"))
									{
										if (direction == Direction.Packaging)
											mortarItem.AdditionalInfo["docType"] = mortarItem.Value.DocumentTypeAlias;
										else if (direction == Direction.Extracting)
											mortarItem.AdditionalInfo["docType"] = PersistenceManager.Default.GetUniqueId(mortarItem.Value.DocumentTypeId, NodeObjectTypes.DocumentType).ToString();
									}

									// resolve the value's properties
									mortarItem.RawValue = ResolveMultiplePropertyItemData(item, fakeItemProvider, mortarItem.Value, mortarItem.RawValue, direction);

									break;

								case "EMBED":
									// we don't need Courier to process the embed code - it's pure HTML
									break;

								case "LINK":
									mortarItem.RawValue = ConvertIdentifier(mortarItem.RawValue, item, IdentifierReplaceDirection.FromNodeIdToGuid, ProviderIDCollection.documentItemProviderGuid);
									break;

								case "MEDIA":
									mortarItem.RawValue = ConvertIdentifier(mortarItem.RawValue, item, IdentifierReplaceDirection.FromNodeIdToGuid, ProviderIDCollection.mediaItemProviderGuid);
									break;

								case "RICHTEXT":
									var property = mortarItem.Value.GetProperty("bodyText");
									var propertyType = mortarItem.Value.ContentType.GetPropertyType(property.PropertyTypeAlias);

									mortarItem.RawValue = ResolvePropertyItemData(item, fakeItemProvider, propertyType, mortarItem.RawValue, Guid.Empty, direction);

									break;

								default:
									break;
							}
						}
					}
				}

				propertyData.Value = JsonConvert.SerializeObject(mortarValue);
			}
		}

		private object ResolvePropertyItemData(Item item, PropertyItemProvider propertyItemProvider, PublishedPropertyType propertyType, object value, Guid dataTypeGuid, Direction direction = Direction.Packaging)
		{
			// create a 'fake' item for Courier to process
			var fakeItem = new ContentPropertyData()
			{
				ItemId = item.ItemId,
				Name = string.Format("{0} [{1}: {2} ({3})]", item.Name, EditorAlias, propertyType.PropertyEditorAlias, propertyType.PropertyTypeAlias),
				Data = new List<ContentProperty>
				{
					new ContentProperty
					{
						Alias = propertyType.PropertyTypeAlias,
						DataType = dataTypeGuid,
						PropertyEditorAlias = propertyType.PropertyEditorAlias,
						Value = value
					}
				}
			};

			if (direction == Direction.Packaging)
			{
				// run the 'fake' item through Courier's data resolvers
				ResolutionManager.Instance.PackagingItem(fakeItem, propertyItemProvider);

				// pass up the dependencies
				if (fakeItem.Dependencies != null && fakeItem.Dependencies.Count > 0)
					item.Dependencies.AddRange(fakeItem.Dependencies);

				// pass up the resources
				if (fakeItem.Resources != null && fakeItem.Resources.Count > 0)
					item.Resources.AddRange(fakeItem.Resources);
			}
			else if (direction == Direction.Extracting)
			{
				// run the 'fake' item through Courier's data resolvers
				ResolutionManager.Instance.ExtractingItem(fakeItem, propertyItemProvider);
			}

			// return the resolved data from the 'fake' item
			if (fakeItem.Data != null && fakeItem.Data.Any())
				return fakeItem.Data.FirstOrDefault().Value;

			return value;
		}

		private JObject ResolveMultiplePropertyItemData(Item item, PropertyItemProvider propertyItemProvider, IPublishedContent content, object rawValue, Direction direction = Direction.Packaging)
		{
			var propertyItemData = new Dictionary<string, object>();
			var propertyValues = ((JObject)rawValue).ToObject<Dictionary<string, object>>();

			foreach (var propertyValue in propertyValues)
			{
				if (propertyValue.Key.InvariantEquals("name"))
				{
					propertyItemData.Add(propertyValue.Key, propertyValue.Value);
					continue;
				}

				var propertyType = content.ContentType.GetPropertyType(propertyValue.Key);
				if (propertyType == null)
					continue;

				var dataTypeGuid = PersistenceManager.Default.GetUniqueId(propertyType.DataTypeId, NodeObjectTypes.DataType);

				var value = ResolvePropertyItemData(item, propertyItemProvider, propertyType, propertyValue.Value, dataTypeGuid, direction);

				propertyItemData.Add(propertyValue.Key, value);
			}

			return JObject.FromObject(propertyItemData);
		}
	}
}