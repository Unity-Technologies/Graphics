using System;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    class PluginMaterialVersions : SerializedDictionary<GUID, int, string, int>
    {
        public override string SerializeKey(GUID key) => key.ToString();
        public override int SerializeValue(int val) => val;
        public override GUID DeserializeKey(string key)
        {
            if (!string.IsNullOrEmpty(key) && GUID.TryParse(key, out GUID guid))
                return guid;
            else
                return new GUID();
        }

        public override int DeserializeValue(int val) => val;
    }
}
