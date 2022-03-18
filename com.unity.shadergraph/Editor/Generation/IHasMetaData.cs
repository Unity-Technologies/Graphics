using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    interface IHasMetadata
    {
        string identifier { get; }
        ScriptableObject GetMetadataObject(GraphDataReadOnly graph);
    }
}
