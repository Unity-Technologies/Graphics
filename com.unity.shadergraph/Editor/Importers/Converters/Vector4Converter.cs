using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEngine.ShaderGraph
{
    public class Vector4Converter : JsonConverter<Vector4>
    {
        public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("x");
            writer.WriteValue(value.x);

            writer.WritePropertyName("y");
            writer.WriteValue(value.y);

            writer.WritePropertyName("z");
            writer.WriteValue(value.z);

            writer.WritePropertyName("w");
            writer.WriteValue(value.w);

            writer.WriteEndObject();
        }

        public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            existingValue.x = jObject["x"].Value<float>();
            existingValue.y = jObject["y"].Value<float>();
            existingValue.z = jObject["z"].Value<float>();
            existingValue.w = jObject["w"].Value<float>();
            return existingValue;
        }
    }
}
