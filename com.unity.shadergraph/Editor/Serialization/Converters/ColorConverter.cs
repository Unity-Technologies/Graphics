using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class ColorConverter : JsonConverter<Color>
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

        public override Color ReadJson(JsonReader reader, Type objectType, Color value, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            value.r = jObject.Value<float>("r");
            value.g = jObject.Value<float>("g");
            value.b = jObject.Value<float>("b");
            value.a = jObject.Value<float>("a");
            return value;
        }
    }
}
