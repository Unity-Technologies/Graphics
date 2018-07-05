using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    public class UpgradeConverter : JsonConverter
    {
        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("Use default serialization.");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var versionToken = jsonObject["$version"];
            var version = versionToken != null ? versionToken.Value<int>() : -1;
            Debug.Log("ReadJson: " + version);
            existingValue = existingValue ?? Activator.CreateInstance(objectType);
            serializer.Populate(jsonObject.CreateReader(), existingValue);
            return existingValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
