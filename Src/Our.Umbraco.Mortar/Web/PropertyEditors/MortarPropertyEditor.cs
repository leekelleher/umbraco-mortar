using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ClientDependency.Core;
using Newtonsoft.Json;
using Our.Umbraco.Mortar.Models;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Editors;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web.PropertyEditors;
using Constants = Umbraco.Core.Constants;

namespace Our.Umbraco.Mortar.Web.PropertyEditors
{
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
                {"layout", "<table>\n\t<tr>\n\t\t<td id='main'></td>\n\t\t<td id='sidebar' width='25%'></td>\n\t</tr>\n</table>"}
            };
		}

		#region Pre Value Editor

		protected override PreValueEditor CreatePreValueEditor()
		{
			return new MortarPreValueEditor();
		}

		internal class MortarPreValueEditor : PreValueEditor
		{
			[PreValueField("layout", "Grid Layout", "textarea", Description = "Enter page layout for your Mortar property. This should be in the format of a single HTML table with id attributes on the table cells you want to be able to add bricks to.")]
			public string Layout { get; set; }
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
				var value = JsonConvert.DeserializeObject<MortarValue>(property.Value.ToString());

				foreach (var key in value.Keys)
				{
					foreach (var row in value[key])
					{
						foreach (var item in row.Items)
						{
							if (item != null && !item.RawValue.IsNullOrWhiteSpace())
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
											var propValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.RawValue);
											var propValueKeys = propValues.Keys.ToArray();
											foreach (var propKey in propValueKeys)
											{
												// Lookup the property type on the content type
												var propType = contentType.PropertyTypes.First(x => x.Alias == propKey);

												// Create a fake property using the property abd stored value
												var prop = new Property(propType, item.RawValue);

												// Lookup the property editor
												var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);

												// Get the editor to do it's conversion, and store it back
												propValues[propKey] = propEditor.ValueEditor.ConvertDbToString(prop, propType, dataTypeService);
											}

											// Serialize the dictionary back
											item.RawValue = JsonConvert.SerializeObject(propValues);
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
				var value = JsonConvert.DeserializeObject<MortarValue>(property.Value.ToString());

				foreach (var key in value.Keys)
				{
					foreach (var row in value[key])
					{
						foreach (var item in row.Items)
						{
							if (item != null && !item.RawValue.IsNullOrWhiteSpace())
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
											var propValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.RawValue);
											var propValueKeys = propValues.Keys.ToArray();
											foreach (var propKey in propValueKeys)
											{
												// Lookup the property type on the content type
												var propType = contentType.PropertyTypes.First(x => x.Alias == propKey);
												
												// Create a fake property using the property abd stored value
												var prop = new Property(propType, item.RawValue);

												// Lookup the property editor
												var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);
												
												// Get the editor to do it's conversion
												var newValue = propEditor.ValueEditor.ConvertDbToEditor(prop, propType, dataTypeService);
												
												// Store the value back
												propValues[propKey] = (newValue == null) ? null : newValue.ToString();
											}

											// Serialize the dictionary back
											item.RawValue = JsonConvert.SerializeObject(propValues);
										}
										break;
								}
							}
						}
					}
				}

				return value;
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
							if (item != null && !item.RawValue.IsNullOrWhiteSpace())
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
											var contentType = PublishedContentType.Get(PublishedItemType.Content, docTypeAlias);

											// Loop through doc type properties
											var propValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(item.RawValue);
											var propValueKeys = propValues.Keys.ToArray();
											foreach (var propKey in propValueKeys)
											{
												// Fetch the current property type
												var propType = contentType.GetPropertyType(propKey);

												// Fetch the property types prevalue
												var propPreValues = ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(propType.DataTypeId);
												
												// Lookup the property editor
												var propEditor = PropertyEditorResolver.Current.GetByAlias(propType.PropertyEditorAlias);
												
												// Create a fake content property data object
												var contentPropData = new ContentPropertyData(item.RawValue, propPreValues, new Dictionary<string, object>());

												// Get the property editor to do it's conversion
												var newValue = propEditor.ValueEditor.ConvertEditorToDb(contentPropData, propValues[propKey]);

												// Store the value back
												propValues[propKey] = (newValue == null) ? null : newValue.ToString();
											}

											// Serialize the dictionary back
											item.RawValue = JsonConvert.SerializeObject(propValues);
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
