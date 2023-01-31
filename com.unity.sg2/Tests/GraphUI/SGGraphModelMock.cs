using System;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    /// <summary>
    /// This is a mock ShaderGraphModel, intended to be used in unit testing.
    ///
    /// Depending on the use case, the given registry might need some core definitions (like GraphType) to be useful.
    /// See the tool defaults in ShaderGraphRegistry.InitializeDefaults.
    /// </summary>
    class SGGraphModelMock : SGGraphModel
    {
        internal override ShaderGraphRegistry RegistryInstance { get; }

        // Note: Provide a GraphHandler using .Init() before use. See ShaderGraphAssetUtils.CreateNewAssetGraph
        // for how this happens in the tool, or use the CreateWithGraphHandler factory method.
        internal SGGraphModelMock(ShaderGraphRegistry registry)
        {
            RegistryInstance = registry;
            Stencil = new ShaderGraphStencilMock();

            var sgAsset = ScriptableObject.CreateInstance<ShaderGraphAssetMock>();
            Asset = sgAsset;
            sgAsset.SetGraphModel(this);
        }

        /// <summary>
        /// Creates a new SGGraphModelMock and initializes it with an empty GraphHandler using the given registry.
        /// The graph will have a null target and not be a subgraph.
        /// </summary>
        /// <param name="registry">ShaderGraphRegistry containing definitions to use.</param>
        /// <returns>A new SGGraphModelMock using the given registry and initialized with an empty GraphHandler.</returns>
        public static SGGraphModelMock CreateWithGraphHandler(ShaderGraphRegistry registry)
        {
            var model = new SGGraphModelMock(registry);
            var graphHandler = new GraphHandler(registry.Registry);
            model.Init(graphHandler, false, null);
            return model;
        }
    }
}
