using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 value, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            value.x = jObject.Value<float>("x");
            value.y = jObject.Value<float>("y");
            value.z = jObject.Value<float>("z");
            return value;
        }
    }
}
