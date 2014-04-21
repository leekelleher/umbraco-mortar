using System.Collections.Generic;
using Newtonsoft.Json;

namespace Our.Umbraco.Mortar.Models
{
	public class MortarRow
	{
		[JsonProperty("layout")]
		public string Layout { get; set; }

		[JsonProperty("items")]
		public IEnumerable<MortarItem> Items { get; set; }
	}
}