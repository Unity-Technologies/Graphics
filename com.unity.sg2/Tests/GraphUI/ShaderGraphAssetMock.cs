using System;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class ShaderGraphAssetMock : ShaderGraphAsset
    {
        GraphModel m_MockGraphModel;

        public override bool Dirty { get; set; }
        public override GraphModel GraphModel => m_MockGraphModel;

        public void SetGraphModel(GraphModel mockGraphModel) => m_MockGraphModel = mockGraphModel;
    }
}
