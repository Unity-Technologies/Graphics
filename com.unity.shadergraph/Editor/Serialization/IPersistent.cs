using System;
using Newtonsoft.Json;

namespace UnityEditor.ShaderGraph.Serialization
{
    [JsonObject(IsReference = true)]
    interface IPersistent
    {
//        [JsonProperty(PropertyName = "$id", Order = int.MinValue)]
//        string id { get; set; }
    }
}
