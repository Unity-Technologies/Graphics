using NUnit.Framework;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.GraphUI;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class BaseGraphAssetTest
    {
        ShaderGraphAsset m_Asset;

        protected ShaderGraphAsset Asset => m_Asset;
        protected SGGraphModel GraphModel => m_Asset.SGGraphModel;

        [SetUp]
        public void SetUp()
        {
            m_Asset = ShaderGraphAssetUtils.CreateNewAssetGraph(false, true);

            Assert.IsNotNull(Asset, "Setup: Asset must not be null");
            Assert.IsNotNull(GraphModel, "Setup: Graph model must not be null");
        }
    }
}
