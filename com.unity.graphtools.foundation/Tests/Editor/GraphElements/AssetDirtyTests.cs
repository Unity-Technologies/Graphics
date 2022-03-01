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
        Vector2 k_SelectionOffset = new Vector2(100, 100);

        public AssetDirtyTests() : base(true)
        {}
        INodeModel m_NodeModel { get; set; }

        IGraphAssetModel m_AssetModel { get; set; }

        const string k_FilePath = "Assets/AssetDirtyTests_TestFile.asset";
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;

            m_AssetModel = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateGraphAsset(typeof(TestStencil), "TestFile", k_FilePath);

            window.GraphTool.Dispatch(new LoadGraphAssetCommand(m_AssetModel));
            window.GraphTool.Update();

            m_NodeModel = CreateNode("Movable element", k_NodePos, 0, 1);

            // Add the minimap.
            var miniMap = new MiniMap();
            miniMap.style.left = k_MinimapRect.x;
            miniMap.style.top = k_MinimapRect.y;
            miniMap.style.width = k_MinimapRect.width;
            miniMap.style.height = k_MinimapRect.height;
            graphView.Add(miniMap);
        }

        [TearDown]
        public override void TearDown()
        {
            AssetDatabase.DeleteAsset(k_FilePath);
            base.TearDown();
            GraphViewStaticBridge.SetTimeSinceStartupCB(null);
        }

        [UnityTest]
        public IEnumerator DocumentSetDirtyAfterCommandThenResetAfterSave()
        {
            MarkGraphViewStateDirty();
            yield return null;

            //Check that the asset is not dirty for the moment
            Assert.IsFalse(m_AssetModel.Dirty);

            var node = m_NodeModel.GetUI<Node>(graphView);
            Assert.IsNotNull(node);

            Vector2 worldNodePos = graphView.ContentViewContainer.LocalToWorld(k_NodePos);
            Vector2 start = worldNodePos + k_SelectionOffset;
            Vector2 moveOffset = new Vector2(10, 10);

            // Move the movable element.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + moveOffset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;

            //Check that the asset is dirty
            Assert.IsTrue(m_AssetModel.Dirty);

            AssetDatabase.SaveAssets();
            //Check that the asset is not dirty after save
            Assert.IsFalse(m_AssetModel.Dirty);
        }
    }
}
