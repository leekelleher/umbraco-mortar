using System;
using System.Collections.Generic;
using System.Linq;
using ClientDependency.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Mortar.Helpers;
using Our.Umbraco.Mortar.Models;
using Our.Umbraco.Mortar.Web.Extensions;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web.PropertyEditors;
using Constants = Umbraco.Core.Constants;

namespace Our.Umbraco.Mortar.Web.PropertyEditors
{
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.extensions.js")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.services.js")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.resources.js")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.controllers.js")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.directives.js")]
	[PropertyEditorAsset(ClientDependencyType.Css, "/App_Plugins/Mortar/Css/mortar.css")]
	[PropertyEditor(MortarPropertyEditor.PropertyEditorAlias, "Mortar", "/App_Plugins/Mortar/Views/mortar.html", HideLabel = true, ValueType = "JSON")]
	public class MortarPropertyEditor : PropertyEditor
	{
		public const string PropertyEditorAlias = "Our.Umbraco.Mortar";

		private IDictionary<string, object> _defaultPreValues;
		public override IDictionary<string, object> DefaultPreValues
		{
			get { return _defaultPreValues; }
			set { _defaultPreValues = value; }
		}

		public MortarPropertyEditor()
		{
			// Setup default values
			_defaultPreValues = new Dictionary<string, object>
			{
				{"gridLayout", "<table>\n\t<tr>\n\t\t<td id='main'></td>\n\t\t<td id='sidebar' width='25%'></td>\n\t</tr>\n</table>"},
				{"gridConfig", "{'main':{'layouts':[[50,50],[25,25,25,25]]},'sidebar':{'maxItems':4}}"},
				{"defaultConfig", "{'allowedDocTypes':['Widget$']}"}
			};
		}

		#region Pre Value Editor

		protected override PreValueEditor CreatePreValueEditor()
		{
			return new MortarPreValueEditor();
		}

		internal class MortarPreValueEditor : PreValueEditor
		{
			[PreValueField("gridLayout", "Grid Layout", "textarea", Description = "Enter page layout for your Mortar property. This should be in the format of a single HTML table with id attributes on the table cells you want to be able to add items to.")]
			public string GridLayout { get; set; }

			[PreValueField("gridConfig", "Grid Config", "/App_Plugins/Mortar/Views/mortar.jsonTextarea.html", Description = "Configure each cell of the grid layout. This should be in the form of a JSON object with sub config objects for each grid layout cell with an ID.")]
			public string GridConfig { get; set; }

			[PreValueField("defaultConfig", "Default Config", "/App_Plugins/Mortar/Views/mortar.jsonTextarea.html", Description = "Provide a default config that applies to all grid cells. This should be in the form of a JSON object.")]
			public string DefaultConfig { get; set; }
		}

		#endregion

		#region Value Editor

		protected override PropertyValueEditor CreateValueEditor()
		{
			return new MortarPropertyValueEditor(base.CreateValueEditor());
		}

		internal class MortarPropertyValueEditor : PropertyValueEditorWrapper
		{
			public MortarPropertyValueEditor(PropertyValueEditor wrapped)
				: base(wrapped)
			{ }

			#region DB to String

			public override string ConvertDbToString(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
			{
				if (property.Value == null || string.IsNullOrWhiteSpace(property.Value.ToString()))
					return string.Empty;

				var value = JsonConvert.DeserializeObject<MortarValue>(property.Value.ToString());

				if (value == null)
					return string.Empty;

				foreach (var key in value.Keys)
				{
					var rowOptionsDocTypeAlias = MortarHelper.GetRowOptionsDocType(propertyType.DataTypeDefinitionId, key);

					foreach (var row in value[key])
					{
						row.RawOptions = !string.IsNullOrWhiteSpace(rowOptionsDocTypeAlias)
							? ConvertDbToString_DocType(rowOptionsDocTypeAlias, row.RawOptions)
							: null;

						foreach (var item in row.Items)
						{
							if (item != null && item.RawValue != null)
							{
								switch (item.Type.ToLowerInvariant())
								{
									case "richtext":
										item.RawValue = ConvertDbToString_Fake(Constants.PropertyEditors.TinyMCEAlias, "bodyText", item.RawValue);
										break;
									case "embed":
										item.RawValue = ConvertDbToString_Fake(Constants.PropertyEditors.TextboxMultipleAlias, "embedCode", item.RawValue);
										break;
									case "doctype":
										Guid docTypeGuid;
										if (item.AdditionalInfo.ContainsKey("docType")
											&& item.AdditionalInfo["docType"] != null
											&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace()
											&& Guid.TryParse(item.AdditionalInfo["docType"], out docTypeGuid))
										{
											// Lookup the doctype
											var docTypeAlias = ApplicationContext.Current.Services.ContentTypeService.GetAliasByGuid(docTypeGuid);
											item.RawValue = ConvertDbToString_DocType(docTypeAlias, item.RawValue);
										}
										break;
								}
							}
						}
					}
				}

				// Update the value on the property
				property.Value = JsonConvert.SerializeObject(value);

				// Pass the call down
				return base.ConvertDbToString(property, propertyType, dataTypeService);
			}

			protected object ConvertDbToString_Fake(string propEditorAlias, string propAlias, object value)
			{
				// Create a fake DTD
				var fakeDtd = new DataTypeDefinition(-1, propEditorAlias);

				// Create a fake property type
				var fakePropType = new PropertyType(fakeDtd) { Alias = propAlias };

				// Create a fake property
				var fakeProp = new Property(fakePropType, value);

				// Lookup the property editor
				var fakePropEditor = PropertyEditorResolver.Current.GetByAlias(propEditorAlias);

				return fakePropEditor.ValueEditor.ConvertDbToString(fakeProp, fakePropType, ApplicationContext.Current.Services.DataTypeService);
			}

			protected object ConvertDbToString_DocType(string docTypeAlias, object value)
			{
				var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(docTypeAlias);

				// Loop through properties
				var propValues = ((JObject)value);
				var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
				if (contentType != null && contentType.PropertyTypes != null)
				{
					foreach (var propKey in propValueKeys)
					{
						// Lookup the property type on the content type
						var propType = contentType.PropertyTypes.FirstOrDefault(x => x.Alias == propKey);

						if (propType == null)
						{
							if (propKey != "name")
							{
								// Property missing so just delete the value
								propValues[propKey] = null;
							}
						}
						else
						{
							// Create a fake property using the property abd stored value
							var prop = new Property(propType, propValues[propKey] == null ? null : propValues[propKey].ToString());

							// Lookup the property editor
							var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

							// Get the editor to do it's conversion, and store it back
							propValues[propKey] = propEditor.ValueEditor.ConvertDbToString(prop, propType,
								ApplicationContext.Current.Services.DataTypeService);
						}
					}
				}

				// Serialize the dictionary back
				return propValues;
			}

			#endregion

			#region DB to Editor

			public override object ConvertDbToEditor(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
			{
				if (property.Value == null || string.IsNullOrWhiteSpace(property.Value.ToString()))
					return string.Empty;

				var value = JsonConvert.DeserializeObject<MortarValue>(property.Value.ToString());

				if (value == null)
					return string.Empty;

				foreach (var key in value.Keys)
				{
					// Lookup the row option doc type here so we only do it once per cell
					var rowOptionsDocTypeAlias = MortarHelper.GetRowOptionsDocType(propertyType.DataTypeDefinitionId, key);

					foreach (var row in value[key])
					{
						row.RawOptions = !string.IsNullOrWhiteSpace(rowOptionsDocTypeAlias)
							? ConvertDbToEditor_DocType(rowOptionsDocTypeAlias, row.RawOptions)
							: null;

						foreach (var item in row.Items)
						{
							if (item != null && item.RawValue != null)
							{
								switch (item.Type.ToLowerInvariant())
								{
									case "richtext":
										item.RawValue = ConvertDbToEditor_Fake(Constants.PropertyEditors.TinyMCEAlias, "bodyText", item.RawValue);
										break;
									case "embed":
										item.RawValue = ConvertDbToEditor_Fake(Constants.PropertyEditors.TextboxMultipleAlias, "embedCode", item.RawValue);
										break;
									case "doctype":
										Guid docTypeGuid;
										if (item.AdditionalInfo.ContainsKey("docType")
											&& item.AdditionalInfo["docType"] != null
											&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace()
											&& Guid.TryParse(item.AdditionalInfo["docType"], out docTypeGuid))
										{
											// Lookup the doctype
											var docTypeAlias = ApplicationContext.Current.Services.ContentTypeService.GetAliasByGuid(docTypeGuid);
											item.RawValue = ConvertDbToEditor_DocType(docTypeAlias, item.RawValue);
										}
										break;
								}
							}
						}
					}
				}

				// We serialize back down as we want the editor to handle
				// the data as a generic object type, not our specific classes
				property.Value = JsonConvert.SerializeObject(value);

				return base.ConvertDbToEditor(property, propertyType, dataTypeService);
			}

			protected object ConvertDbToEditor_Fake(string propEditorAlias, string propAlias, object value)
			{
				// Create a fake DTD
				var fakeDtd = new DataTypeDefinition(-1, propEditorAlias);

				// Create a fake property type
				var fakePropType = new PropertyType(fakeDtd) { Alias = propAlias };

				// Create a fake property
				var fakeProp = new Property(fakePropType, value);

				// Lookup the property editor
				var fakePropEditor = PropertyEditorResolver.Current.GetByAlias(propEditorAlias);

				// Get the editor to do it's conversion
				var fakeNewValue = fakePropEditor.ValueEditor.ConvertDbToEditor(fakeProp, fakePropType, ApplicationContext.Current.Services.DataTypeService);

				// Store the value back
				return fakeNewValue == null ? null : fakeNewValue.ToString();
			}

			protected object ConvertDbToEditor_DocType(string docTypeAlias, object value)
			{
				var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(docTypeAlias);

				// Loop through properties
				var propValues = value as JObject;
				if (propValues != null)
				{
					if (contentType != null && contentType.PropertyTypes != null)
					{
						var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
						foreach (var propKey in propValueKeys)
						{
							// Lookup the property type on the content type
							var propType = contentType.PropertyTypes.FirstOrDefault(x => x.Alias == propKey);

							if (propType == null)
							{
								if (propKey != "name")
								{
									// Property missing so just remove the value
									propValues[propKey] = null;
								}
							}
							else
							{
								// Create a fake property using the property abd stored value
								var prop = new Property(propType, propValues[propKey] == null ? null : propValues[propKey].ToString());

								// Lookup the property editor
								var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

								// Get the editor to do it's conversion
								var newValue = propEditor.ValueEditor.ConvertDbToEditor(prop, propType,
									ApplicationContext.Current.Services.DataTypeService);

								// Store the value back
								propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
							}
						}
					}
				}

				return propValues;
			}

			#endregion

			#region Editor to DB

			public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
			{
				if (editorValue.Value == null || string.IsNullOrWhiteSpace(editorValue.Value.ToString()))
					return null;

				var value = JsonConvert.DeserializeObject<MortarValue>(editorValue.Value.ToString());

				if (value == null)
					return null;

				foreach (var key in value.Keys)
				{
					var rowOptionsDocTypeAlias = MortarHelper.GetRowOptionsDocType(editorValue.PreValues, key);

					foreach (var row in value[key])
					{
						row.RawOptions = !string.IsNullOrWhiteSpace(rowOptionsDocTypeAlias)
							? ConvertEditorToDb_DocType(rowOptionsDocTypeAlias, row.RawOptions)
							: null;

						foreach (var item in row.Items)
						{
							if (item != null && item.RawValue != null)
							{
								switch (item.Type.ToLowerInvariant())
								{
									case "richtext":
										item.RawValue = ConvertEditorToDb_Fake(Constants.PropertyEditors.TinyMCEAlias, item.RawValue);
										break;
									case "embed":
										item.RawValue = ConvertEditorToDb_Fake(Constants.PropertyEditors.TextboxMultipleAlias, item.RawValue);
										break;
									case "doctype":
										Guid docTypeGuid;
										if (item.AdditionalInfo.ContainsKey("docType")
											&& item.AdditionalInfo["docType"] != null
											&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace()
											&& Guid.TryParse(item.AdditionalInfo["docType"], out docTypeGuid))
										{
											// Lookup the doctype
											var docTypeAlias = ApplicationContext.Current.Services.ContentTypeService.GetAliasByGuid(docTypeGuid);

											// Serialize the dictionary back
											item.RawValue = ConvertEditorToDb_DocType(docTypeAlias, item.RawValue);
										}
										break;
								}
							}
						}
					}
				}

				return JsonConvert.SerializeObject(value);
			}

			protected object ConvertEditorToDb_Fake(string propEditorAlias, object value)
			{
				// Lookup the property editor
				var fakePropEditor = PropertyEditorResolver.Current.GetByAlias(propEditorAlias);

				// Create a fake content property data object (note, we don't have a prevalue, so passing in null)
				var fakeContentPropData = new ContentPropertyData(value, null, new Dictionary<string, object>());

				// Get the property editor to do it's conversion
				var fakeNewValue = fakePropEditor.ValueEditor.ConvertEditorToDb(fakeContentPropData, value);

				// Store the value back
				return fakeNewValue == null ? null : fakeNewValue.ToString();
			}

			protected object ConvertEditorToDb_DocType(string docTypeAlias, object value)
			{
				var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(docTypeAlias);

				// Loop through doc type properties
				var propValues = ((JObject)value);
				var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
				if (contentType != null && contentType.PropertyTypes != null)
				{
					foreach (var propKey in propValueKeys)
					{
						// Fetch the current property type
						var propType = contentType.PropertyTypes.FirstOrDefault(x => x.Alias == propKey);

						if (propType == null)
						{
							if (propKey != "name")
							{
								// Property missing so just remove the value
								propValues[propKey] = null;
							}
						}
						else
						{
							// Fetch the property types prevalue
							var propPreValues =
								ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(
									propType.DataTypeDefinitionId);

							// Lookup the property editor
							var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

							// Create a fake content property data object
							var contentPropData = new ContentPropertyData(
								propValues[propKey] == null ? null : propValues[propKey].ToString(), propPreValues,
								new Dictionary<string, object>());

							// Get the property editor to do it's conversion
							var newValue = propEditor.ValueEditor.ConvertEditorToDb(contentPropData, propValues[propKey]);

							// Store the value back
							propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
						}
					}
				}

				return propValues;
			}

			#endregion
		}

		#endregion
	}
}
