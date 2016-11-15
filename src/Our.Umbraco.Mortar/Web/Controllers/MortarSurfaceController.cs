using Our.Umbraco.Mortar.Models;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;

namespace Our.Umbraco.Mortar.Web.Controllers
{
	public abstract class MortarSurfaceController : MortarSurfaceController<IPublishedContent>
	{ }

	public abstract class MortarSurfaceController<TModel> : SurfaceController
	{
		public TModel MortarModel
		{
			get { return (TModel)ControllerContext.RouteData.Values["mortarModel"]; }
		}

		public MortarRow MortarRow
		{
			get { return (MortarRow)ControllerContext.RouteData.Values["mortarRow"]; }
		}

		public string MortarViewPath
		{
			get { return ControllerContext.RouteData.Values["mortarViewPath"] as string ?? string.Empty; }
		}
	}
}