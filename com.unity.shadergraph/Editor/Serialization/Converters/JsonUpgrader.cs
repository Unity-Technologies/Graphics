using System;
using Newtonsoft.Json;

namespace UnityEditor.ShaderGraph.Serialization
{
    abstract class JsonUpgrader<T> : JsonConverter<T>
    {
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            throw new InvalidOperationException();
        }
    }
}
