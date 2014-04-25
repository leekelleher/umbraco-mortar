using System.Collections.Generic;
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Our.Umbraco.Mortar.Models
{
	public class MortarRow
	{
		[JsonProperty("layout")]
		public string Layout { get; set; }

		[JsonProperty("items")]
		public ReadOnlyCollection<MortarItem> Items { get; set; }
	}
}