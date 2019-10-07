using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEditor.ShaderGraph.Serialization
{
    [JsonObject(IsReference = true)]
    interface IJsonObject
    {
    }

    struct DeserializationPair
    {
        public IJsonObject instance { get; }
        public JObject jObject { get; }

        public DeserializationPair(IJsonObject instance, JObject jObject)
        {
            this.instance = instance;
            this.jObject = jObject;
        }

        public void Deconstruct(out IJsonObject instance, out JObject jObject)
        {
            instance = this.instance;
            jObject = this.jObject;
        }
    }

    interface IOnDeserialized
    {
        void OnDeserialized(JObject jObject, List<DeserializationPair> jObjects);
    }
}
