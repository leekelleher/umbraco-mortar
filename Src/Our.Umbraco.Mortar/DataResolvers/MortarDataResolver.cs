using System.Collections.Generic;
using System.Linq;
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

namespace Our.Umbraco.Mortar.DataResolvers
{
	public class MortarDataResolver : PropertyDataResolverProvider
	{
		private enum Direction
		{
			Extracting,
			Packaging
		}

		public override string EditorAlias
		{
			get
			{
				return MortarPropertyEditor.PropertyEditorAlias;
			}
		}

		public override void ExtractingDataType(DataType item)
		{
			base.ExtractingDataType(item);
		}

		public override void ExtractingProperty(Item item, ContentProperty propertyData)
		{
			base.ExtractingProperty(item, propertyData);
		}

		public override void PackagingDataType(DataType item)
		{
			base.PackagingDataType(item);

			// AddDataTypeDependencies - get the DocTypeAliases and add them as dependencies
		}

		public override void PackagingProperty(Item item, ContentProperty propertyData)
		{
			if (propertyData == null || propertyData.Value == null)
				return;

			// just look at the amount of dancing around we have to do in order to fake a `PublishedPropertyType`?!
			var dataTypeId = PersistenceManager.Default.GetNodeId(propertyData.DataType, NodeObjectTypes.DataType);
			var fakePropertyType = this.CreateFakePropertyType(dataTypeId, this.EditorAlias);

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
						{
							// TODO: [LK] Process the row options
						}

						foreach (var mortarItem in mortarRow.Items)
						{
							switch (mortarItem.Type.ToUpperInvariant())
							{
								case "DOCTYPE":
									// TODO: [LK] Process the DocType

									if (mortarItem.AdditionalInfo.ContainsKey("docType"))
										mortarItem.AdditionalInfo["docType"] = mortarItem.Value.DocumentTypeAlias;

									// get the DocType schema (from Alias)
									var contentType = mortarItem.Value.ContentType;

									// make a dictionary from RawValue
									var converted = new Dictionary<string, object>();
									var propValues = ((JObject)mortarItem.RawValue).ToObject<Dictionary<string, object>>();

									// loop the properties
									foreach (var jProp in propValues)
									{
										var propType = contentType.GetPropertyType(jProp.Key);

										if (jProp.Key == "name")
										{
											converted.Add("name", jProp.Value);
											continue;
										}

										if (propType == null)
											continue;

										// create fake courier item
										var fakeItem1 = new ContentPropertyData()
										{
											ItemId = item.ItemId,
											Name = string.Format("{0} [{1}: Nested {2} ({3})]", new[] { item.Name, this.EditorAlias, propType.PropertyEditorAlias, propType.PropertyTypeAlias }),
											Data = new List<ContentProperty>
											{
												new ContentProperty
												{
													Alias =  propType.PropertyTypeAlias,
													DataType = PersistenceManager.Default.GetUniqueId(propType.DataTypeId, NodeObjectTypes.DataType),
													PropertyEditorAlias = propType.PropertyEditorAlias,
													Value = jProp.Value
												}
											}
										};

										// run the 'fake' item through Courier's data resolvers
										ResolutionManager.Instance.PackagingItem(fakeItem1, fakeItemProvider);

										// pass up the dependencies and resources
										item.Dependencies.AddRange(fakeItem1.Dependencies);
										item.Resources.AddRange(fakeItem1.Resources);

										// add a dependency for the property's data-type
										//propValues[jProp.Key] = fakeItem1.Data.FirstOrDefault().Value;
										converted.Add(jProp.Key, fakeItem1.Data.FirstOrDefault().Value);
									}

									// store back to RawValue
									mortarItem.RawValue = JObject.FromObject(converted);

									break;

								case "EMBED":
									// we don't need Courier to process the embed code - it's just HTML
									break;

								case "LINK":
									// TODO: [LK] Process the Content Picker
									if (mortarItem.RawValue != null && !string.IsNullOrWhiteSpace(mortarItem.RawValue.ToString()))
									{
										var documentGuid = Dependencies.ConvertIdentifier(mortarItem.RawValue.ToString(), IdentifierReplaceDirection.FromNodeIdToGuid);

										item.Dependencies.Add(documentGuid, ProviderIDCollection.documentItemProviderGuid);

										mortarItem.RawValue = documentGuid;
									}
									break;

								case "MEDIA":
									if (mortarItem.RawValue != null && !string.IsNullOrWhiteSpace(mortarItem.RawValue.ToString()))
									{
										var mediaGuid = Dependencies.ConvertIdentifier(mortarItem.RawValue.ToString(), IdentifierReplaceDirection.FromNodeIdToGuid);

										item.Dependencies.Add(mediaGuid, ProviderIDCollection.mediaItemProviderGuid);

										mortarItem.RawValue = mediaGuid;
									}

									break;

								case "RICHTEXT":

									var bodyText = mortarItem.Value.GetProperty("bodyText");

									// create a 'fake' item for Courier to process
									var fakeItem2 = new ContentPropertyData()
									{
										ItemId = item.ItemId,
										Name = string.Format("{0} [{1}: Nested {2} ({3})]", new[] { item.Name, this.EditorAlias, Constants.PropertyEditors.TinyMCEAlias, bodyText.PropertyTypeAlias }),
										Data = new List<ContentProperty>
										{
											new ContentProperty
											{
												Alias = bodyText.PropertyTypeAlias,
												//DataType = PersistenceManager.Default.GetUniqueId(property.DataTypeId, NodeObjectTypes.DataType),
												PropertyEditorAlias = Constants.PropertyEditors.TinyMCEAlias,
												Value = bodyText.DataValue
											}
										}
									};

									// run the 'fake' item through Courier's data resolvers
									ResolutionManager.Instance.PackagingItem(fakeItem2, fakeItemProvider);

									// pass up the dependencies and resources
									item.Dependencies.AddRange(fakeItem2.Dependencies);
									item.Resources.AddRange(fakeItem2.Resources);

									// add a dependency for the property's data-type
									mortarItem.RawValue = fakeItem2.Data.FirstOrDefault().Value;

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

		private PublishedPropertyType CreateFakePropertyType(int dataTypeId, string propertyEditorAlias)
		{
			return new PublishedPropertyType(null, new PropertyType(new DataTypeDefinition(-1, propertyEditorAlias) { Id = dataTypeId }));
		}
	}
}