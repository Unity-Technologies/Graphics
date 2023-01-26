using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class SGGraphTestUtils
    {
        /// <summary>
        /// Creates a node by using the node type name displayed in the ItemLibrary and adds it to the graph model.
        /// </summary>
        /// <param name="graphModel">SGGraphModel that the node should be added to.</param>
        /// <param name="name">Type name of the node to create, as displayed in the Item Library.</param>
        /// <param name="position">Node position.</param>
        /// <returns>The created node.</returns>
        internal static SGNodeModel CreateNodeByName(SGGraphModel graphModel, string name, Vector2 position)
        {
            var registry = graphModel.RegistryInstance;

            var versionCounts = new Dictionary<string, int>();
            foreach (var registryKey in registry.Registry.BrowseRegistryKeys())
            {
                versionCounts[registryKey.Name] = versionCounts.GetValueOrDefault(registryKey.Name, 0) + 1;
            }

            foreach (var registryKey in registry.Registry.BrowseRegistryKeys())
            {
                if (graphModel.ShouldBeInSearcher(registryKey))
                {
                    var uiInfo = registry.GetNodeUIDescriptor(registryKey);
                    string searcherItemName = uiInfo.DisplayName;

                    if (versionCounts[registryKey.Name] > 1)
                    {
                        searcherItemName += $" (v{uiInfo.Version})";
                    }

                    if (searcherItemName == name)
                    {
                        return graphModel.CreateGraphDataNode(
                            registryKey,
                            uiInfo.DisplayName,
                            position);
                    }
                }
            }

            return null;
        }

    }
}
