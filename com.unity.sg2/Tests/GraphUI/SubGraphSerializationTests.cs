using System;
using System.Collections;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class SubGraphSerializationTests : BaseGraphWindowTest
    {
        protected override string testAssetPath => $"Assets\\{ShaderGraphStencil.DefaultSubGraphAssetName}.{ShaderGraphStencil.SubGraphExtension}";
        ModelInspectorView m_InspectorView;

        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.DiskSubGraph;

        [UnityTest]
        public IEnumerator TestSaveSubGraph()
        {
            yield return m_TestInteractionHelper.AddNodeFromSearcherAndValidate("Add");
            yield return SaveAndReopenGraph();

            // Wait till the graph model is loaded back up
            while (m_MainWindow.GraphView.GraphModel == null)
                yield return null;

            Assert.IsNotNull(m_MainWindow.GetNodeModelFromGraphByName("Add"));
        }
    }
}
