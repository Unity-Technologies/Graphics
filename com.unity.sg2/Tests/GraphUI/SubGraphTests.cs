using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Assert = UnityEngine.Assertions.Assert;

namespace UnityEditor.ShaderGraph.GraphUI.UnitTests
{
    class SubGraphTests : BaseGraphWindowTest
    {
        protected override string testAssetPath => $"Assets\\{ShaderGraphStencil.DefaultSubGraphAssetName}.{ShaderGraphStencil.SubGraphExtension}";
        ModelInspectorView m_InspectorView;

        /// <inheritdoc />
        protected override GraphInstantiation GraphToInstantiate => GraphInstantiation.MemorySubGraph;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            FindInspectorView();
        }

        private void FindInspectorView()
        {
            const string viewFieldName = "m_InspectorView";

            var found = m_MainWindow.TryGetOverlay(k_InspectorOverlayId, out var inspectorOverlay);
            Assert.IsTrue(found, "Inspector overlay was not found");

            m_InspectorView = (ModelInspectorView)inspectorOverlay.GetType()
                .GetField(viewFieldName, BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(inspectorOverlay);
            Assert.IsNotNull(m_InspectorView, "Inspector view was not found");
        }
    }
}
