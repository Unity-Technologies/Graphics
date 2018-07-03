using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.ShaderGraph
{
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
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

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
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
