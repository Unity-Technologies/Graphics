using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.ShaderGraph
{
    public class UnityConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var json = JsonUtility.ToJson(value);
            writer.WriteRawValue(json);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var json = jObject.ToString(Formatting.None);
            JsonUtility.FromJsonOverwrite(json, existingValue);
            return existingValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
