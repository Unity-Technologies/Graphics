using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class RectConverter : JsonConverter<Rect>
    {
        public override void WriteJson(JsonWriter writer, Rect value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("width");
            writer.WriteValue(value.width);
            writer.WritePropertyName("height");
            writer.WriteValue(value.height);
            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect value, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            value.x = jObject.Value<float>("x");
            value.y = jObject.Value<float>("y");
            value.width = jObject.Value<float>("width");
            value.height = jObject.Value<float>("height");
            return value;
        }
    }
}
