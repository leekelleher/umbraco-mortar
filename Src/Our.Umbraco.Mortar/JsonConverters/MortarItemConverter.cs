using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Our.Umbraco.Mortar.Models;

namespace Our.Umbraco.Mortar.JsonConverters
{
	internal class MortarItemConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(MortarItem);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			var tempDictionary = new Dictionary<string, object>();

			serializer.Populate(reader, tempDictionary);

			// Make sure keys are in camelCase
			tempDictionary = tempDictionary
				.ToDictionary(x => Char.ToLowerInvariant(x.Key[0]) + x.Key.Substring(1), x => x.Value);

			var item = new MortarItem
			{
				Type = tempDictionary["type"].ToString(),
				RawValue = tempDictionary["value"],
				AdditionalInfo = tempDictionary.Where(x => x.Key != "type" && x.Key != "value")
					.ToDictionary(k => k.Key, v => v.Value.ToString())
			};

			return item;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var item = value as MortarItem;
			if (item != null)
			{
				var jObj = new JObject
				{
					{"type", item.Type}, 
					{"value", JToken.FromObject(item.RawValue) }
				};

				foreach (var key in item.AdditionalInfo.Keys)
				{
					jObj.Add(key, item.AdditionalInfo[key]);
				}

				jObj.WriteTo(writer);
			}
		}
	}
}