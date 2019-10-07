using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Serialization
{
    class UnityObjectConverter : JsonConverter<Object>
    {
        [Serializable]
        class ObjectHelper
        {
            public Object obj;
        }

        public override void WriteJson(JsonWriter writer, Object value, JsonSerializer serializer)
        {
            var helper = new ObjectHelper { obj = value };
            var json = EditorJsonUtility.ToJson(helper);
            var jObject = JObject.Parse(json);
            jObject["obj"].WriteTo(writer);
        }

        public override Object ReadJson(JsonReader reader, Type objectType, Object existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JToken.ReadFrom(reader);
            jObject = new JObject { { "obj", jObject } };
            var json = jObject.ToString(Formatting.None);
            var helper = new ObjectHelper();
            EditorJsonUtility.FromJsonOverwrite(json, helper);
            return helper.obj != null ? helper.obj : null;
        }
    }
}
