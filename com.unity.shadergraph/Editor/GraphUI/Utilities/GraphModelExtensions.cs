using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.Utilities
{
    public static class GraphModelExtensions
    {
        public static RegistryNodeModel CreateRegistryNode(
            this IGraphModel graphModel,
            RegistryKey registryKey,
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
                nodeModel => nodeModel.registryKey = registryKey,
                spawnFlags
            );
        }

        public static RegistryNodeModel CreateRegistryNode(
            this GraphNodeCreationData graphNodeCreationData,
            RegistryKey registryKey,
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
