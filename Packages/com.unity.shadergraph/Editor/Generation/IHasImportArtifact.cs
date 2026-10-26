using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    /// <summary>
    /// Lets a target or subtarget contribute an extra sub-asset to the graph's import result.
    /// </summary>
    internal interface IHasImportArtifact
    {
        ScriptableObject GetImportArtifact(GraphData graph, Material material, string assetName);
    }
}
