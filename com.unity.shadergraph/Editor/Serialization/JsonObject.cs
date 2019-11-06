using System;

namespace UnityEditor.ShaderGraph.Serialization
{
    public abstract class JsonObject
    {
        internal string jsonId { get; set; } = Guid.NewGuid().ToString();

        internal int changeVersion { get; set; }

        // TODO: Is this one necessary?
//        internal JsonStore store { get; set; }

        internal virtual void OnDeserializing()
        {}

        internal virtual void OnDeserialized(string json)
        {}

        internal virtual void OnStoreDeserialized(string json)
        {}
    }
}
