using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.Utilities
{
    public static class GraphModelExtensions
    {
        public static RegistryNodeModel CreateRegistryNode(
            this IGraphModel graphModel,
            PlaceholderRegistryKey registryKey, // FIXME
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

        public static RegistryNodeModel CreateRegistryNode(
            this GraphNodeCreationData graphNodeCreationData,
            PlaceholderRegistryKey registryKey, // FIXME
            string nodeName = ""
        )
        {
            return graphNodeCreationData.GraphModel.CreateRegistryNode(
                registryKey,
                nodeName,
                graphNodeCreationData.Position,
                graphNodeCreationData.Guid,
                graphNodeCreationData.SpawnFlags
            );
        }
    }
}
