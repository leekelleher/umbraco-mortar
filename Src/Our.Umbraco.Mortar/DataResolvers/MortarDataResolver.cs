using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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
		}

		public override void PackagingProperty(Item item, ContentProperty propertyData)
		{
			base.PackagingProperty(item, propertyData);
		}

		private PublishedPropertyType CreateFakePropertyType(int dataTypeId, string propertyEditorAlias)
		{
			return new PublishedPropertyType(null, new PropertyType(new DataTypeDefinition(-1, propertyEditorAlias) { Id = dataTypeId }));
		}
	}
}