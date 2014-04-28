using System.Collections.Generic;
using System.Linq;
using ClientDependency.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Mortar.Models;
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
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.resources.js")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.controllers.js")]
	[PropertyEditorAsset(ClientDependencyType.Javascript, "/App_Plugins/Mortar/Js/mortar.directives.js")]
	[PropertyEditorAsset(ClientDependencyType.Css, "/App_Plugins/Mortar/Css/mortar.css")]
	[PropertyEditor("Our.Umbraco.Mortar", "Mortar", "/App_Plugins/Mortar/Views/mortar.html", 
		HideLabel = true, ValueType = "JSON")]
	public class MortarPropertyEditor : PropertyEditor
	{
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
				{"defaultConfig", "{'allowedDoctypes':['Widget$']}"}
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

			public override string ConvertDbToString(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
			{
				if (property.Value == null)
					return string.Empty;

				var value = JsonConvert.DeserializeObject<MortarValue>(property.Value.ToString());

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
										// Create a fake DTD
										var rteDtd = new DataTypeDefinition(-1, Constants.PropertyEditors.TinyMCEAlias);

										// Create a fake property type
										var rtePropType = new PropertyType(rteDtd) { Alias = "bodyText" };

										// Create a fake property
										var rteProp = new Property(rtePropType, item.RawValue);

										// Lookup the property editor
										var rtePropEditor = PropertyEditorResolver.Current.GetByAlias(Constants.PropertyEditors.TinyMCEAlias);

										// Get the editor to do it's conversion, and store the value back
										item.RawValue = rtePropEditor.ValueEditor.ConvertDbToString(rteProp, rtePropType, dataTypeService);
										break;
									case "doctype":
										if (item.AdditionalInfo.ContainsKey("docType")
											&& item.AdditionalInfo["docType"] != null
											&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace())
										{
											// Lookup the doctype
											var docTypeAlias = item.AdditionalInfo["docType"];
											var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(docTypeAlias);

											// Loop through properties
											var propValues = ((JObject)item.RawValue);
											var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
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
													propValues[propKey] = propEditor.ValueEditor.ConvertDbToString(prop, propType, dataTypeService);
												}
											}

											// Serialize the dictionary back
											item.RawValue = propValues;
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

			public override object ConvertDbToEditor(Property property, PropertyType propertyType, IDataTypeService dataTypeService)
			{
				if (property.Value == null)
					return string.Empty;

				var value = JsonConvert.DeserializeObject<MortarValue>(property.Value.ToString());

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
										// Create a fake DTD
										var rteDtd = new DataTypeDefinition(-1, Constants.PropertyEditors.TinyMCEAlias);
										
										// Create a fake property type
										var rtePropType = new PropertyType(rteDtd) { Alias = "bodyText" };
										
										// Create a fake property
										var rteProp = new Property(rtePropType, item.RawValue);

										// Lookup the property editor
										var rtePropEditor = PropertyEditorResolver.Current.GetByAlias(Constants.PropertyEditors.TinyMCEAlias);
										
										// Get the editor to do it's conversion
										var rteNewValue = rtePropEditor.ValueEditor.ConvertDbToEditor(rteProp, rtePropType, dataTypeService);

										// Store the value back
										item.RawValue = rteNewValue == null ? null : rteNewValue.ToString();
										break;
									case "doctype":
										if (item.AdditionalInfo.ContainsKey("docType")
											&& item.AdditionalInfo["docType"] != null
											&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace())
										{
											// Lookup the doctype
											var docTypeAlias = item.AdditionalInfo["docType"];
											var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(docTypeAlias);

											// Loop through properties
											var propValues = item.RawValue as JObject;
											if (propValues != null)
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
														var newValue = propEditor.ValueEditor.ConvertDbToEditor(prop, propType, dataTypeService);

														// Store the value back
														propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
													}
												}

												// Serialize the dictionary back
												item.RawValue = propValues;
											}
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

			public override object ConvertEditorToDb(ContentPropertyData editorValue, object currentValue)
			{
				if (editorValue.Value == null)
					return null;

				var value = JsonConvert.DeserializeObject<MortarValue>(editorValue.Value.ToString());

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
										// Lookup the property editor
										var rtePropEditor = PropertyEditorResolver.Current.GetByAlias(Constants.PropertyEditors.TinyMCEAlias);
										
										// Create a fake content property data object (note, we don't have a prevalue, so passing in null)
										var rteContentPropData = new ContentPropertyData(item.RawValue, null, new Dictionary<string, object>());

										// Get the property editor to do it's conversion
										var rteNewValue = rtePropEditor.ValueEditor.ConvertEditorToDb(rteContentPropData, item.RawValue);

										// Store the value back
										item.RawValue = rteNewValue == null ? null : rteNewValue.ToString();
										break;
									case "doctype":
										if (item.AdditionalInfo.ContainsKey("docType")
											&& item.AdditionalInfo["docType"] != null
											&& !item.AdditionalInfo["docType"].IsNullOrWhiteSpace())
										{
											// Ftech the doc type
											var docTypeAlias = item.AdditionalInfo["docType"];
											var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(docTypeAlias);

											// Loop through doc type properties
											var propValues = ((JObject)item.RawValue);
											var propValueKeys = propValues.Properties().Select(x => x.Name).ToArray();
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
													var propPreValues = ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(propType.DataTypeDefinitionId);

													// Lookup the property editor
													var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

													// Create a fake content property data object
													var contentPropData = new ContentPropertyData(propValues[propKey] == null ? null : propValues[propKey].ToString(), propPreValues, new Dictionary<string, object>());

													// Get the property editor to do it's conversion
													var newValue = propEditor.ValueEditor.ConvertEditorToDb(contentPropData, propValues[propKey]);

													// Store the value back
													propValues[propKey] = (newValue == null) ? null : JToken.FromObject(newValue);
												}
											}

											// Serialize the dictionary back
											item.RawValue = propValues;
										}
										break;
								}
							}
						}
					}
				}

				return JsonConvert.SerializeObject(value);
			}
		}

		#endregion
	}
}
