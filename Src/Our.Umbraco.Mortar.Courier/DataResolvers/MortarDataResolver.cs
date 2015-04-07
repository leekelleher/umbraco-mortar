using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Mortar.Helpers;
using Our.Umbraco.Mortar.Models;
using Our.Umbraco.Mortar.Web.PropertyEditors;
using Umbraco.Core;
using Umbraco.Courier.Core;
using Umbraco.Courier.Core.Enums;
using Umbraco.Courier.Core.Helpers;
using Umbraco.Courier.Core.Logging;
using Umbraco.Courier.Core.ProviderModel;
using Umbraco.Courier.DataResolvers;
using Umbraco.Courier.ItemProviders;

namespace Our.Umbraco.Mortar.Courier.DataResolvers
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

		public override void ExtractingProperty(Item item, ContentProperty propertyData)
		{
			ResolvePropertyData(item, propertyData, Direction.Extracting);
		}

		public override void PackagingProperty(Item item, ContentProperty propertyData)
		{
			ResolvePropertyData(item, propertyData, Direction.Packaging);
		}

		private object ConvertIdentifier(object value, Item item, Direction direction, Guid providerId, string itemType)
		{
			if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
			{
				if (direction == Direction.Packaging)
				{
					var guid = Dependencies.ConvertIdentifier(value.ToString(), IdentifierReplaceDirection.FromNodeIdToGuid);

					// add dependency for the item
					var name = string.Concat(itemType, " from picker");
					var dependency = new Dependency(name, guid, providerId);
					item.Dependencies.Add(dependency);

					return guid;
				}
				else if (direction == Direction.Extracting)
				{
					return Dependencies.ConvertIdentifier(value.ToString(), IdentifierReplaceDirection.FromGuidToNodeId);
				}
			}

			return value;
		}

		private void ResolvePropertyData(Item item, ContentProperty propertyData, Direction direction)
		{
			if (propertyData == null || propertyData.Value == null)
				return;

			// create a fake `PublishedPropertyType`
			var dataTypeId = ExecutionContext.DatabasePersistence.GetNodeId(propertyData.DataType, NodeObjectTypes.DataType);

			// deserialize the current Property's value into a 'MortarValue'
            var mortarValue = JsonConvert.DeserializeObject<MortarValue>(propertyData.Value.ToString());

            // get the `PropertyItemProvider` from the collection.
            var propertyItemProvider = ItemProviderCollection.Instance.GetProvider(ProviderIDCollection.propertyDataItemProviderGuid, this.ExecutionContext);

            if (mortarValue != null)
            {
                foreach (var mortarBlock in mortarValue)
                {
                    foreach (var mortarRow in mortarBlock.Value)
                    {
                        var rowOptionsDocTypeAlias = MortarHelper.GetRowOptionsDocType(dataTypeId, mortarBlock.Key);
                        if (!string.IsNullOrWhiteSpace(rowOptionsDocTypeAlias))
                        {
                            mortarRow.RawOptions = ResolveMultiplePropertyItemData(item, propertyItemProvider,
                                mortarRow.RawOptions, rowOptionsDocTypeAlias, direction);
                        }

                        foreach (var mortarItem in mortarRow.Items)
                        {
                            if (mortarItem == null)
                            {
                                CourierLogHelper.Warn<MortarDataResolver>("MortarItem appears to be null, (from '{0}' block)", () => mortarBlock.Key);
                                continue;
                            }

                            if (mortarItem.Type == null)
                            {
                                CourierLogHelper.Warn<MortarDataResolver>("MortarItem did not contain a value for Type, (from '{0}' block)", () => mortarBlock.Key);
                                continue;
                            }

                            switch (mortarItem.Type.ToUpperInvariant())
                            {
                                case "DOCTYPE":
                                    // resolve the doctype alias/guid
                                    string docTypeAlias = string.Empty;
                                    if (mortarItem.AdditionalInfo.ContainsKey("docType"))
                                    {
                                        docTypeAlias = mortarItem.AdditionalInfo["docType"];
                                        DocumentType docType;
                                        Guid docTypeGuid;
                                        if (Guid.TryParse(docTypeAlias, out docTypeGuid))
                                        {
                                            docType = ExecutionContext.DatabasePersistence.RetrieveItem<DocumentType>(
                                                new ItemIdentifier(docTypeGuid.ToString(),
                                                    ProviderIDCollection.documentTypeItemProviderGuid));
                                            docTypeAlias = docType.Alias;
                                        }
                                        else
                                        {
                                            docType = ExecutionContext.DatabasePersistence.RetrieveItem<DocumentType>(
                                                new ItemIdentifier(docTypeAlias,
                                                    ProviderIDCollection.documentTypeItemProviderGuid));
                                            docTypeGuid = docType.UniqueId;
                                        }

                                        if (direction == Direction.Packaging)
                                        {
                                            mortarItem.AdditionalInfo["docType"] = docTypeAlias;

                                            // add dependency for the DocType
                                            var name = string.Concat("Document type: ", docTypeAlias);
                                            var dependency = new Dependency(name, docTypeAlias, ProviderIDCollection.documentTypeItemProviderGuid);
                                            item.Dependencies.Add(dependency);
                                        }
                                        else if (direction == Direction.Extracting)
                                        {
                                            mortarItem.AdditionalInfo["docType"] = docTypeGuid.ToString();
                                        }
                                    }

                                    // resolve the value's properties
                                    mortarItem.RawValue = ResolveMultiplePropertyItemData(item, propertyItemProvider, mortarItem.RawValue, docTypeAlias, direction);

                                    break;

                                case "EMBED":
                                    // we don't need Courier to process the embed code - it's pure HTML
                                    break;

                                case "LINK":
                                    mortarItem.RawValue = ConvertIdentifier(mortarItem.RawValue, item, direction, ProviderIDCollection.documentItemProviderGuid, "Document");
                                    break;

                                case "MEDIA":
                                    mortarItem.RawValue = ConvertIdentifier(mortarItem.RawValue, item, direction, ProviderIDCollection.mediaItemProviderGuid, "Media");
                                    break;

                                case "RICHTEXT":
                                    //From the 'MortarValueConverter' it appears that the DocumentTypeAlias, PropertyEditorAlias and PropertyTypeAlias are 
                                    //all constants, so we reuse them here.
                                    mortarItem.RawValue = ResolvePropertyItemData(item, propertyItemProvider, mortarItem.RawValue,
                                        Constants.PropertyEditors.TinyMCEAlias, "bodyText", Guid.Empty, direction);
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

        private object ResolvePropertyItemData(Item item, ItemProvider itemProvider, object value, string propertyEditorAlias, 
            string propertyTypeAlias, Guid dataTypeGuid, Direction direction = Direction.Packaging)
		{
			// create a 'fake' item for Courier to process
			var fakeItem = new ContentPropertyData
			{
				ItemId = item.ItemId,
				Name = string.Format("{0} [{1}: {2} ({3})]", item.Name, EditorAlias, propertyEditorAlias, propertyTypeAlias),
				Data = new List<ContentProperty>
				{
					new ContentProperty
					{
						Alias = propertyTypeAlias,
						DataType = dataTypeGuid,
						PropertyEditorAlias = propertyEditorAlias,
						Value = value
					}
				}
			};

			if (direction == Direction.Packaging)
			{
				try
				{
					// run the 'fake' item through Courier's data resolvers
					ResolutionManager.Instance.PackagingItem(fakeItem, itemProvider);
				}
				catch (Exception ex)
				{
					CourierLogHelper.Error<MortarDataResolver>(string.Concat("Error packaging data value: ", fakeItem.Name), ex);
				}

				// pass up the dependencies
				if (fakeItem.Dependencies != null && fakeItem.Dependencies.Count > 0)
					item.Dependencies.AddRange(fakeItem.Dependencies);

				// pass up the resources
				if (fakeItem.Resources != null && fakeItem.Resources.Count > 0)
					item.Resources.AddRange(fakeItem.Resources);
			}
			else if (direction == Direction.Extracting)
			{
				try
				{
					// run the 'fake' item through Courier's data resolvers
					ResolutionManager.Instance.ExtractingItem(fakeItem, itemProvider);
                    item.Status = ItemStatus.NeedPostProcessing;
                    item.PostProcess = true;
				}
				catch (Exception ex)
				{
					CourierLogHelper.Error<MortarDataResolver>(string.Concat("Error extracting data value: ", fakeItem.Name), ex);
				}
			}

			// return the resolved data from the 'fake' item
			if (fakeItem.Data != null && fakeItem.Data.Any())
				return fakeItem.Data.FirstOrDefault().Value;

			return value;
		}

	    private JObject ResolveMultiplePropertyItemData(Item item, ItemProvider itemProvider,
	        object rawValue, string docTypeAlias, Direction direction)
	    {
            var propertyItemData = new Dictionary<string, object>();
            var propertyValues = ((JObject)rawValue).ToObject<Dictionary<string, object>>();

	        var documentType = ExecutionContext.DatabasePersistence.RetrieveItem<DocumentType>(
	            new ItemIdentifier(docTypeAlias, ProviderIDCollection.documentTypeItemProviderGuid));
	        if (documentType == null)
                return ((JObject)rawValue);

	        foreach (var propertyValue in propertyValues)
	        {
                if (propertyValue.Key.InvariantEquals("name"))
                {
                    propertyItemData.Add(propertyValue.Key, propertyValue.Value);
                    continue;
                }

	            var propertyType = documentType.Properties.FirstOrDefault(x => x.Alias.Equals(propertyValue.Key));
                if(propertyType == null)
                    continue;

	            var dataType = ExecutionContext.DatabasePersistence.RetrieveItem<DataType>(
	                new ItemIdentifier(propertyType.DataTypeDefinitionId.ToString(),
	                    ProviderIDCollection.dataTypeItemProviderGuid));
                if(dataType == null)
                    continue;

	            var dataTypeGuid = propertyType.DataTypeDefinitionId;
	            var propertyTypeAlias = propertyType.Alias;
	            var propertyEditorAlias = dataType.PropertyEditorAlias;
                var value = ResolvePropertyItemData(item, itemProvider, propertyValue.Value, propertyEditorAlias,
                    propertyTypeAlias, dataTypeGuid, direction);

                propertyItemData.Add(propertyValue.Key, value);
	        }

            return JObject.FromObject(propertyItemData);
	    }
	}
}