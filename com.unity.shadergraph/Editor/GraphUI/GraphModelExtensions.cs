using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class GraphModelExtensions
    {
        public static RegistryNodeModel CreateRegistryNode(
            this IGraphModel graphModel,
            PlaceholderRegistryKey registryKey,  // FIXME
            string nodeName = "",
            Vector2 position = default,
            SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default
        )
        {
            return graphModel.CreateNode<RegistryNodeModel>(
                nodeName,
                position,
                guid,
                nodeModel => nodeModel.key = registryKey,
                spawnFlags
            );
        }
    }
}
