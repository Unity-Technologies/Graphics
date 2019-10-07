using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class Matrix4x4Converter : JsonConverter<Matrix4x4>
    {
        public override void WriteJson(JsonWriter writer, Matrix4x4 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
            {
                writer.WritePropertyName($"e{i}{j}");
                writer.WriteValue(value[i,j]);
            }
            writer.WriteEndObject();
        }

        public override Matrix4x4 ReadJson(JsonReader reader, Type objectType, Matrix4x4 value, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            for (var i = 0; i < 4; i++)
            for (var j = 0; j < 4; j++)
            {
                value[i, j] = jObject.Value<float>($"e{i}{j}");
            }
            return value;
        }
    }
}
