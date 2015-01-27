using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Our.Umbraco.Mortar.Models
{
	public class MortarValue : Dictionary<string, IEnumerable<MortarRow>>
	{
		public MortarValue()
			: base()
		{ }

		public MortarValue(IEqualityComparer<string> comparer)
			: base(comparer)
		{ }

		public MortarValue(int capacity)
			: base(capacity)
		{ }

		public MortarValue(int capacity, IEqualityComparer<string> comparer)
			: base(capacity, comparer)
		{ }

		public MortarValue(IDictionary<string, IEnumerable<MortarRow>> dictionary)
			: base(dictionary)
		{ }

		public MortarValue(IDictionary<string, IEnumerable<MortarRow>> dictionary, IEqualityComparer<string> comparer)
			: base(dictionary, comparer)
		{ }

		public MortarValue(SerializationInfo serializationInfo, StreamingContext streamingContext)
			: base(serializationInfo, streamingContext)
		{ }

		//[JsonProperty("dtdGuid")]
		//public Guid DtdGuid { get; set; }
	}
}