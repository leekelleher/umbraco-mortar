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

		public static HtmlString RenderMortarItem(this HtmlHelper helper, MortarRow row,
			MortarItem item,
			string viewPath = "",
			string actionName = "",
			object model = null)
		{
			if (item == null)
				return new HtmlString(string.Empty);

			if (!string.IsNullOrWhiteSpace(viewPath))
				viewPath = viewPath.TrimEnd('/') + "/";

			if (string.IsNullOrWhiteSpace(actionName))
				actionName = item.Value.DocumentTypeAlias;

			var controllerName = string.Concat(item.Value.DocumentTypeAlias, "Surface");
			var umbracoHelper = new UmbracoHelper(UmbracoContext.Current);

			if (umbracoHelper.SurfaceControllerExists(controllerName, actionName, true))
			{
				return helper.Action(actionName,
					controllerName,
					new
					{
						mortarModel = model ?? item.Value,
						mortarRow = row,
						mortarViewPath = viewPath
					});
			}

			return helper.Partial(viewPath + item.Value.DocumentTypeAlias, model ?? item.Value);
		}

		public static HtmlString RenderMortarItem(this HtmlHelper helper, RenderMortarItemViewModel item,
			string viewPath = "",
			string actionName = "",
			object model = null)
		{
			return helper.RenderMortarItem(item.Row, item.Item, viewPath, actionName, model);
		}
	}
}
