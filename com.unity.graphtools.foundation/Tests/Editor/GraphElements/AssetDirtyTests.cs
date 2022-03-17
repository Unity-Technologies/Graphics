using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class AssetDirtyTests : GraphViewTester
    {
        static readonly Vector2 k_NodePos = new Vector2(SelectionDragger.panAreaWidth * 2, SelectionDragger.panAreaWidth * 3);
        static readonly Rect k_MinimapRect = new Rect(100, 100, 100, 100);
        Vector2 m_SelectionOffset = new Vector2(100, 100);

        public AssetDirtyTests() : base(true)
        {}
        INodeModel NodeModel { get; set; }

        IGraphAssetModel AssetModel { get; set; }

        const string k_FilePath = "Assets/AssetDirtyTests_TestFile.asset";
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;

            AssetModel = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(TestStencil), "TestFile", k_FilePath);

            Window.GraphTool.Dispatch(new LoadGraphAssetCommand(AssetModel));
            Window.GraphTool.Update();

            NodeModel = CreateNode("Movable element", k_NodePos, 0, 1);

            // Add the minimap.
            var miniMap = new MiniMap();
            miniMap.style.left = k_MinimapRect.x;
            miniMap.style.top = k_MinimapRect.y;
            miniMap.style.width = k_MinimapRect.width;
            miniMap.style.height = k_MinimapRect.height;
            GraphView.Add(miniMap);
        }

        [TearDown]
        public override void TearDown()
        {
            AssetDatabase.DeleteAsset(k_FilePath);
            base.TearDown();
            GraphViewStaticBridge.SetTimeSinceStartupCallback(null);
        }

        [UnityTest]
        public IEnumerator DocumentSetDirtyAfterCommandThenResetAfterSave()
        {
            MarkGraphViewStateDirty();
            yield return null;

            //Check that the asset is not dirty for the moment
            Assert.IsFalse(AssetModel.Dirty);

            var node = NodeModel.GetView<Node>(GraphView);
            Assert.IsNotNull(node);

            Vector2 worldNodePos = GraphView.ContentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;
            Vector2 moveOffset = new Vector2(10, 10);

            // Move the movable element.
            Helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            Helpers.MouseDragEvent(start, end);
            yield return null;

            Helpers.MouseUpEvent(end);
            yield return null;

            //Check that the asset is dirty
            Assert.IsTrue(AssetModel.Dirty);

            AssetDatabase.SaveAssets();
            //Check that the asset is not dirty after save
            Assert.IsFalse(AssetModel.Dirty);
        }
    }
}
