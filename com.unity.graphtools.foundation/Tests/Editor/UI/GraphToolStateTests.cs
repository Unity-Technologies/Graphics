using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    public class ToolStateTests : BaseUIFixture
    {
        IGraphAssetModel m_Asset1;
        IGraphAssetModel m_Asset2;
        INodeModel m_NodeModel;

        /// <inheritdoc />
        protected override bool CreateGraphOnStartup => false;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Asset1 = GraphAssetCreationHelpers<ClassGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test1");
            m_NodeModel = m_Asset1.GraphModel.CreateNode<NodeModel>();
            m_Asset2 = GraphAssetCreationHelpers<ClassGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test2");
        }

        [Test]
        public void SelectionStateIsTiedToAssetAndView()
        {
            GraphView.Dispatch(new LoadGraphAssetCommand(m_Asset1));
            GraphTool.Update();
            Assert.IsNotNull(GraphView.SelectionState);
            using (var selectionUpdater = GraphView.SelectionState.UpdateScope)
            {
                selectionUpdater.SelectElements(new[] { m_NodeModel }, true);
            }
            Assert.IsTrue(GraphView.SelectionState.IsSelected(m_NodeModel));

            // Load another asset in the same view: node is not selected anymore.
            GraphView.Dispatch(new LoadGraphAssetCommand(m_Asset2));
            GraphTool.Update();
            Assert.IsNotNull(GraphView.SelectionState);
            Assert.IsFalse(GraphView.SelectionState.IsSelected(m_NodeModel));

            // Fetch a selection state for the same asset in another view: node is not selected in this component.
            var assetKey = PersistedState.MakeAssetKey(m_Asset2);
            var otherSelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, Hash128.Compute("otherGraphView"), assetKey);
            Assert.IsNotNull(otherSelectionState);
            Assert.IsFalse(otherSelectionState.IsSelected(m_NodeModel));

            // Reload the original asset in the original view: node is still selected.
            GraphView.Dispatch(new LoadGraphAssetCommand(m_Asset1));
            GraphTool.Update();
            Assert.IsNotNull(GraphView.SelectionState);
            Assert.IsTrue(GraphView.SelectionState.IsSelected(m_NodeModel));
        }
    }
}
