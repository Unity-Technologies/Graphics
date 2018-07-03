using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.ShaderGraph
{
    public class BoundsConverter : JsonConverter<Bounds>
    {
        public override void WriteJson(JsonWriter writer, Bounds value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("center");
            serializer.Serialize(writer, value.center);

            writer.WritePropertyName("extents");
            serializer.Serialize(writer, value.extents);

            writer.WriteEndObject();
        }

        public override Bounds ReadJson(JsonReader reader, Type objectType, Bounds existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            JToken centerToken;
            if (jObject.TryGetValue("center", out centerToken) || jObject.TryGetValue("m_Center", out centerToken))
                existingValue.center = centerToken.ToObject<Vector3>(serializer);

            JToken extentsToken;
            if (jObject.TryGetValue("extents", out extentsToken) || jObject.TryGetValue("m_Extent", out extentsToken))
                existingValue.extents = extentsToken.ToObject<Vector3>(serializer);

            return existingValue;
        }
    }
}
