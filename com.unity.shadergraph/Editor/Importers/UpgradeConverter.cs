using System;
using Newtonsoft.Json;

namespace UnityEditor.ShaderGraph
{
    public class UpgradeConverter : JsonConverter
    {
        Type m_Type;

        public UpgradeConverter(Type type)
        {
            m_Type = type;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == m_Type;
        }
    }
}
