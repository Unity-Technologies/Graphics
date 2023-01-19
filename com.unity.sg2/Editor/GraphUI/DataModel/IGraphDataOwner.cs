using System;
using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// Base interface for GraphElementModel that can be backed by data in the CLDS layer.
    /// </summary>
    interface IGraphDataOwner
    {
        /// <summary>
        /// The identifier/unique name used to represent this entity and retrieve info. regarding it from CLDS.
        /// </summary>
        public string graphDataName { get; }

        /// <summary>
        /// The <see cref="RegistryKey"/> that represents the concrete type within the Registry, of this object.
        /// </summary>
        public RegistryKey registryKey { get; }

        /// <summary>
        /// Whether or not this object is actually backed by data in the CLDS layer, or whether it is a "fake" entity, like searcher previews for nodes.
        /// </summary>
        public bool existsInGraphData { get; }
    }

    /// <summary>
    /// This interface should be implemented by any GraphElementModel that can be backed by data in the CLDS layer
    /// </summary>
    interface IGraphDataOwner<T> : IGraphDataOwner where T : AbstractNodeModel, IGraphDataOwner<T>
    {
        /// <summary>
        /// The <see cref="GraphHandler"/> for this <see cref="GraphElementModel"/>.
        /// </summary>
        GraphHandler graphHandler => (((GraphElementModel)this).GraphModel as SGGraphModel)?.GraphHandler;

        /// <summary>
        /// The <see cref="ShaderGraphRegistry"/> for this <see cref="GraphElementModel"/>.
        /// </summary>
        ShaderGraphRegistry registry => ((ShaderGraphStencil)((GraphElementModel)this).GraphModel.Stencil).GetRegistry();

        /// <summary>
        /// Whether or not this IGraphDataOwner is actually backed by data in the CLDS layer, or whether it is a "fake" entity, like searcher previews for nodes
        /// </summary>
        bool IGraphDataOwner.existsInGraphData => graphDataName != null && TryGetNodeHandler(out _);

        /// <summary>
        /// Attempts to get a node handler for this <see cref="GraphElementModel"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        bool TryGetNodeHandler(out NodeHandler reader)
        {
            try
            {
                if (graphDataName == null)
                {
                    // This is a preview node. Return a reader to the registry node.
                    reader = registry.GetDefaultTopology(registryKey);
                }
                else
                {
                    reader = graphHandler.GetNode(graphDataName);
                }
            }
            catch (Exception exception)
            {
                AssertHelpers.Fail("Failed to retrieve node due to exception:" + exception);
                reader = null;
            }

            return reader != null;
        }
    }
}
