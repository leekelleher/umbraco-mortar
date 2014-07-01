using Our.Umbraco.Mortar.Models;

namespace Our.Umbraco.Mortar.Web.ViewModels
{
	public class RenderMortarItemViewModel
	{
		public RenderMortarItemViewModel(MortarRow row, MortarItem item, int index)
		{
			Index = index;
			Item = item;
			Row = row;
		}

		public int Index { get; private set; }

		public MortarRow Row { get; private set; }

		public MortarItem Item { get; private set; }

		public int Width
		{
			get { return Row.Layout[Index]; }
		}
	}
}