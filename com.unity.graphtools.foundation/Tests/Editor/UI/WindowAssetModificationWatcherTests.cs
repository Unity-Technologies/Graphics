using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.UIElements;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class WindowAssetModificationWatcherTests : BaseUIFixture
    {
        IGraphAsset m_Asset1;

        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => false;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Asset1 = GraphAssetCreationHelpers<ClassGraphAsset>.CreateGraphAsset(CreatedGraphType, "Test1", "Assets/test1.asset");
            GraphTool.Dispatch(new LoadGraphCommand(m_Asset1.GraphModel));
            GraphTool.Update();
        }

        [TearDown]
        public override void TearDown()
        {
            var path = AssetDatabase.GetAssetPath(m_Asset1 as Object);
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.DeleteAsset(path);

            base.TearDown();
        }

        [Test]
        public void TestDeleteAssetClearsCurrentOpenedGraph()
        {
            var state = Window.GraphTool.ToolState;

            Assert.IsNotNull(state.CurrentGraph.GetGraphAsset());

            var path = AssetDatabase.GetAssetPath(m_Asset1 as Object);
            Assert.IsNotNull(path);
            AssetDatabase.DeleteAsset(path);

            Assert.AreEqual("", state.CurrentGraph.GraphAssetGuid);
        }

        void DisplayToolbarBreadcrumbs()
        {
#if UNITY_2022_2_OR_NEWER
            Window.TryGetOverlay(BreadcrumbsToolbar.toolbarId, out var toolbar);
            toolbar.displayed = true;
#endif
        }

        ToolbarBreadcrumbs GetToolbarBreadcrumbs()
        {
#if UNITY_2022_2_OR_NEWER
            Window.TryGetOverlay(BreadcrumbsToolbar.toolbarId, out var toolbar);
            var toolbarRoot = toolbar == null ? null : GraphViewStaticBridge.GetOverlayRoot(toolbar);
            return toolbarRoot.Q<GraphBreadcrumbs>();
#else
            return  Window.rootVisualElement.Q<ToolbarBreadcrumbs>();
#endif
        }

        [UnityTest]
        public IEnumerator TestRenameAssetUpdatesCurrentGraphName()
        {
            var state = Window.GraphTool.ToolState;

            Assert.IsNotNull(state.CurrentGraph.GetGraphAsset());
            DisplayToolbarBreadcrumbs();

            yield return null;

            var path = AssetDatabase.GetAssetPath(m_Asset1 as Object);
            Assert.IsNotNull(path);
            AssetDatabase.RenameAsset(path, "blah");

            yield return null;

            var firstBreadcrumbButton = GetToolbarBreadcrumbs()?.Children().First() as ToolbarButton;
            Assert.IsNotNull(firstBreadcrumbButton);
            Assert.AreEqual("blah", firstBreadcrumbButton.text);
        }
    }
}
