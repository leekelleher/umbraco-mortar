using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

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

		[JsonProperty("items")]
		public ReadOnlyCollection<MortarItem> Items { get; set; }
	}
}