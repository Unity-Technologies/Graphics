using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEngine.ShaderGraph
{
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("r");
            writer.WriteValue(value.r);

            writer.WritePropertyName("g");
            writer.WriteValue(value.g);

            writer.WritePropertyName("b");
            writer.WriteValue(value.b);

            writer.WritePropertyName("a");
            writer.WriteValue(value.a);

            writer.WriteEndObject();
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            existingValue.r = jObject["r"].Value<float>();
            existingValue.g = jObject["g"].Value<float>();
            existingValue.b = jObject["b"].Value<float>();
            existingValue.a = jObject["a"].Value<float>();
            return existingValue;
        }
    }
}
