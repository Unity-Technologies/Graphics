using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.GraphElements;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.Utilities
{
    public static class GraphModelExtensions
    {
        public static SearcherPreviewNodeModel CreateSearcherPreview(
            this IGraphModel graphModel,
            RegistryKey registryKey,
            string displayName = "",
            Vector2 position = default,
            SerializableGUID guid = default
        )
        {
            return graphModel.CreateNode<SearcherPreviewNodeModel>(
                displayName,
                position,
                guid,
                nodeModel => nodeModel.registryKey = registryKey,
                SpawnFlags.Orphan
            );
        }

        public static SearcherPreviewNodeModel CreateSearcherPreview(
            this GraphNodeCreationData graphNodeCreationData,
            RegistryKey registryKey,
            string displayName = ""
        )
        {
            return graphNodeCreationData.GraphModel.CreateSearcherPreview(
                registryKey,
                displayName,
                graphNodeCreationData.Position,
                graphNodeCreationData.Guid
            );
        }

        public static GraphDataNodeModel CreateGraphDataNode(
            this IGraphModel graphModel,
            RegistryKey registryKey,
            string displayName = "",
            Vector2 position = default,
            SerializableGUID guid = default,
            SpawnFlags spawnFlags = SpawnFlags.Default
        )
        {
            if (spawnFlags.IsOrphan())
            {
                AssertHelpers.Fail("GraphDataNodeModel should not be orphan");
                return null;
            }

            return graphModel.CreateNode<GraphDataNodeModel>(
                displayName,
                position,
                guid,
                nodeModel =>
                {
                    var registry = ((ShaderGraphStencil) graphModel.Stencil).GetRegistry();
                    ((ShaderGraphModel) graphModel).GraphHandler.AddNode(registryKey, nodeModel.Guid.ToString(), registry);
                },
                spawnFlags
            );
        }

        public static GraphDataNodeModel CreateGraphDataNode(this GraphNodeCreationData graphNodeCreationData,
            RegistryKey registryKey, string displayName)
        {
            return graphNodeCreationData.GraphModel.CreateGraphDataNode(
                registryKey,
                displayName,
                graphNodeCreationData.Position,
                graphNodeCreationData.Guid,
                graphNodeCreationData.SpawnFlags
            );
        }

    }
}
