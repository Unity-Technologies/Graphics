using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    static class GraphModelExtensions
    {
        public static SGNodeModel CreateGraphDataNode(
            this GraphModel graphModel,
            RegistryKey registryKey,
            string displayName = "",
            Vector2 position = default,
            SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default
        )
        {
            return graphModel.CreateNode<SGNodeModel>(
                displayName,
                position,
                guid,
                nodeModel =>
                {
                    nodeModel.Initialize(registryKey, spawnFlags);
                },
                spawnFlags
            );
        }

        public static SGContextNodeModel CreateGraphDataContextNode(
            this SGGraphModel graphModel,
            string existingContextName,
            Vector2 position = default,
            SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default
        )
        {
            return graphModel.CreateNode<SGContextNodeModel>(
                existingContextName,
                position,
                guid,
                nodeModel =>
                {
                    nodeModel.graphDataName = existingContextName;
                },
                spawnFlags
            );
        }
    }
}
