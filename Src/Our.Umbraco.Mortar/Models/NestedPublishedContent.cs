using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Models;

namespace Our.Umbraco.Mortar.Models
{
	public class NestedPublishedContent : PublishedContentBase
	{
		private readonly int _parentContentId;
		private readonly PublishedContentType _contentType;
		private readonly IPublishedProperty[] _properties;

		public NestedPublishedContent(int parentContentId,
			PublishedContentType contentType, 
			IPublishedProperty[] properties)
		{
			_parentContentId = parentContentId;
			_contentType = contentType;
			_properties = properties;
		}

		public override PublishedItemType ItemType
		{
			get { return PublishedItemType.Content; }
		}

		public override bool IsDraft
		{
			get { return false; }
		}

		public override IPublishedContent Parent
		{
			get { throw new NotImplementedException(); }
		}

		public override IEnumerable<IPublishedContent> Children
		{
			get { throw new NotImplementedException(); }
		}

		public override ICollection<IPublishedProperty> Properties
		{
			get { return _properties; }
		}

		public override PublishedContentType ContentType
		{
			get { return _contentType; }
		}

		public override int Id
		{
			get { return _parentContentId; } // Because we don't actually exist, share the same ID as the parent page
		}

		public override int TemplateId
		{
			get { throw new NotImplementedException(); }
		}

		public override int SortOrder
		{
			get { throw new NotImplementedException(); }
		}

		public override string Name
		{
			get { throw new NotImplementedException(); }
		}

		public override string UrlName
		{
			get { throw new NotImplementedException(); }
		}

		public override string DocumentTypeAlias
		{
			get { return _contentType.Alias; }
		}

		public override int DocumentTypeId
		{
			get { return _contentType.Id; }
		}

		public override string WriterName
		{
			get { throw new NotImplementedException(); }
		}

		public override string CreatorName
		{
			get { throw new NotImplementedException(); }
		}

		public override int WriterId
		{
			get { throw new NotImplementedException(); }
		}

		public override int CreatorId
		{
			get { throw new NotImplementedException(); }
		}

		public override string Path
		{
			get { throw new NotImplementedException(); }
		}

		public override DateTime CreateDate
		{
			get { throw new NotImplementedException(); }
		}

		public override DateTime UpdateDate
		{
			get { throw new NotImplementedException(); }
		}

		public override Guid Version
		{
			get { throw new NotImplementedException(); }
		}

		public override int Level
		{
			get { throw new NotImplementedException(); }
		}

		#region Properties

		public override IPublishedProperty GetProperty(string alias)
		{
			return _properties.FirstOrDefault(x => x.PropertyTypeAlias.InvariantEquals(alias));
		}

		public override IPublishedProperty GetProperty(string alias, bool recurse)
		{
			if (recurse)
			{
				throw new NotSupportedException();
			}
			return GetProperty(alias);
		}

		#endregion
	}
}
