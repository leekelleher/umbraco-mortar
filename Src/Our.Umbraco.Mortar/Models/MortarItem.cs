using System.Collections.Generic;
using Newtonsoft.Json;
using Our.Umbraco.Mortar.JsonConverters;
using Umbraco.Core.Models;

namespace Our.Umbraco.Mortar.Models
{
	[JsonConverter(typeof(MortarItemConverter))]
	public class MortarItem
	{
		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("value")]
		internal object RawValue { get; set; }

		internal IDictionary<string, string> AdditionalInfo { get; set; }

		// Only ever used in Razor views, so can be concidered readonly
		[JsonIgnore]
		public IPublishedContent Value { get; internal set; }
	}
}