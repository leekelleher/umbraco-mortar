using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;

namespace Our.Umbraco.Mortar.InternalsProxy
{
    internal static class InternalsUmbracoHelper
    {
	    public static PublishedPropertyType CreatePublishedPropertyType(string propertyAlias, 
			string propertyEditorAlias)
	    {
			return new PublishedPropertyType(propertyAlias, propertyEditorAlias);
	    }

	    public static PublishedContentType CreatePublishedContentType(string docTypeAlias, 
			PublishedPropertyType propertyType)
	    {
			return new PublishedContentType(-1, docTypeAlias, new[] { propertyType });
	    }

	    public static IPublishedProperty GetDetachedPublishedProperty(PublishedPropertyType propertyType,
			PublishedPropertyType containerPropertyType,
			object value, bool preview)
	    {
			return PublishedProperty.GetDetached(propertyType.Nested(containerPropertyType), value, preview);
	    }
    }
}
