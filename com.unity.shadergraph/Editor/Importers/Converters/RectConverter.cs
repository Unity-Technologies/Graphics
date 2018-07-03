using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEngine.ShaderGraph
{
    public class RectConverter : JsonConverter<Rect>
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
//            writer.WritePropertyName("center");
//            serializer.Serialize(writer, value.center);
//
//            writer.WritePropertyName("extents");
//            serializer.Serialize(writer, value.extents);

            writer.WriteEndObject();
        }

        public override Rect ReadJson(JsonReader reader, Type objectType, Rect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return JObject.Load(reader).ToObject<Rect>();
        }
    }
}
