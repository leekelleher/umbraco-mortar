using Our.Umbraco.Mortar.Models;
using Umbraco.Core.Models;
using Umbraco.Web.Mvc;

namespace Our.Umbraco.Mortar.Web.Controllers
{
	public abstract class MortarSurfaceController : SurfaceController
	{
		public IPublishedContent MortarModel
		{
			get { return ControllerContext.RouteData.Values["mortarModel"] as IPublishedContent; }
		}

		public MortarRow MortarRow
		{
			get { return ControllerContext.RouteData.Values["mortarRow"] as MortarRow; }
		}

		public string MortarViewPath
		{
			get { return ControllerContext.RouteData.Values["mortarViewPath"] as string ?? string.Empty; }
		}
	}
}