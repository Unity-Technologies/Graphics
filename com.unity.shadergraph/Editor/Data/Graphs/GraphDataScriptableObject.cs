using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [System.Serializable]
    internal class GraphDataScriptableObject : ScriptableObject
    {
        [SerializeField]
        internal GraphData GraphData;
    }
}
