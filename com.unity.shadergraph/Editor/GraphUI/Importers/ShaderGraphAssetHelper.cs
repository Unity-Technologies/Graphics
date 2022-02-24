using System;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    internal class ShaderGraphAssetHelper : ScriptableObject
    {
        public string GraphDeltaJSON;
        public string GTFJSON;
    }
}
