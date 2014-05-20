using System;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Umbraco.Core.Models;

namespace Our.Umbraco.Mortar.Models
{
	public class MortarRow
	{
		[JsonProperty("layout")]
		internal string LayoutString { get; set; }

		[JsonIgnore]
		public ReadOnlyCollection<int> Layout
		{
			get
			{
				return LayoutString.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries)
					.Select(int.Parse)
					.ToList().AsReadOnly();
			}
		}

		[JsonProperty("options")]
		internal object RawOptions { get; set; }

		// Only ever used in Razor views, so can be considered readonly
		[JsonIgnore]
		public IPublishedContent Options { get; internal set; }

		[JsonProperty("items")]
		public ReadOnlyCollection<MortarItem> Items { get; set; }
	}
}