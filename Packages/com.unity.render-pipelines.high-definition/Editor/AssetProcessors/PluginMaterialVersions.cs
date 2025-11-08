using System;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [Serializable]
    class PluginMaterialVersions : SerializedDictionary<UnityEngine.GUID, int, string, int>
    {
        public override string SerializeKey(UnityEngine.GUID key) => key.ToString();
        public override int SerializeValue(int val) => val;
        public override UnityEngine.GUID DeserializeKey(string key)
        {
            if (!string.IsNullOrEmpty(key) && UnityEngine.GUID.TryParse(key, out UnityEngine.GUID guid))
                return guid;
            else
                return new UnityEngine.GUID();
        }

        public override int DeserializeValue(int val) => val;
    }
}
