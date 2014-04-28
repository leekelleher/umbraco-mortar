using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using System.Web.WebPages;
using Our.Umbraco.Mortar.Models;
using Our.Umbraco.Mortar.Web.ViewModels;
using Umbraco.Web;

namespace Our.Umbraco.Mortar.Web.Extensions
{
	public static class MortarExtensions
	{
		public static HelperResult RenderMortarItems(this HtmlHelper helper,
			MortarRow row,
			Func<RenderMortarItemViewModel, HelperResult> template)
		{
			return new HelperResult(writer =>
			{
				var count = 0;
				foreach (var item in row.Items)
				{
					template(new RenderMortarItemViewModel(row, item, count++))
						.WriteTo(writer);
				}
			});
		}

		public static HtmlString RenderMortarItem(this HtmlHelper helper, RenderMortarItemViewModel item, 
			string viewPath = "",
			string actionName = "")
		{
			if (!string.IsNullOrWhiteSpace(viewPath))
				viewPath = viewPath.TrimEnd('/') + "/";

			if (string.IsNullOrWhiteSpace(actionName))
				actionName = item.Item.Value.DocumentTypeAlias;

			var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);
			if (umbracoHelper.SurfaceControllerExists(item.Item.Value.DocumentTypeAlias + "Surface", actionName))
			{
				return helper.Action(actionName, 
					item.Item.Value.DocumentTypeAlias + "Surface",
					new
					{
						mortarModel = item.Item.Value, 
						mortarRow = item.Row,
						mortarViewPath = viewPath
					});
			}

			return helper.Partial(viewPath + item.Item.Value.DocumentTypeAlias, item.Item.Value);
		}
	}

	
}
