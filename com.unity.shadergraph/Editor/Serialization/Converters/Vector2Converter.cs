using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 value, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            value.x = jObject.Value<float>("x");
            value.y = jObject.Value<float>("y");
            return value;
        }
    }
}
