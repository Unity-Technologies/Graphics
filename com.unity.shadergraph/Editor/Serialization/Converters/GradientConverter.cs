using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Serialization
{
    class GradientConverter : JsonConverter<Gradient>
    {
        public override void WriteJson(JsonWriter writer, Gradient value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("mode");
            serializer.Serialize(writer, value.mode);

            writer.WritePropertyName("alphaKeys");
            writer.WriteStartArray();
            foreach (var alphaKey in value.alphaKeys)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("alpha");
                writer.WriteValue(alphaKey.alpha);

                writer.WritePropertyName("time");
                writer.WriteValue(alphaKey.time);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WritePropertyName("colorKeys");
            writer.WriteStartArray();
            foreach (var colorKey in value.colorKeys)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("color");
                serializer.Serialize(writer, colorKey.color);

                writer.WritePropertyName("time");
                writer.WriteValue(colorKey.time);

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        public override Gradient ReadJson(JsonReader reader, Type objectType, Gradient existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            if (existingValue == null)
            {
                existingValue = new Gradient();
            }

            var alphaKeysJArray = (JArray)jObject["alphaKeys"];
            var alphaKeys = new GradientAlphaKey[alphaKeysJArray.Count];
            for (var i = 0; i < alphaKeys.Length; i++)
            {
                var jToken = alphaKeysJArray[i];
                alphaKeys[i] = new GradientAlphaKey(jToken.Value<float>("alpha"), jToken.Value<float>("time"));
            }

            var colorKeysJArray = (JArray)jObject["colorKeys"];
            var colorKeys = new GradientColorKey[colorKeysJArray.Count];
            for (var i = 0; i < colorKeys.Length; i++)
            {
                var jToken = colorKeysJArray[i];
                colorKeys[i] = new GradientColorKey(serializer.Deserialize<Color>(jToken["color"].CreateReader()), jToken.Value<float>("time"));
            }

            existingValue.mode = serializer.Deserialize<GradientMode>(jObject["mode"].CreateReader());
            existingValue.SetKeys(colorKeys, alphaKeys);
            return existingValue;
        }
    }
}
