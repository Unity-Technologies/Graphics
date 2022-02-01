using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphNodeTests : GraphViewTester
    {
        IInputOutputPortsNodeModel m_Node1;
        IInputOutputPortsNodeModel m_Node2;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node1 = CreateNode("Node 1", new Vector2(0, 0), 0, 1);
            m_Node2 = CreateNode("Node 2", new Vector2(300, 300), 1);

            // Add the minimap.
            var miniMap = new MiniMap();
            miniMap.SetPosition(new Rect(10, 100, 100, 100));
            miniMap.MaxWidth = 100;
            miniMap.MaxHeight = 100;
            graphView.Add(miniMap);
        }

        [Test]
        public void CollapseButtonOnlyEnabledWhenNodeHasUnconnectedPorts()
        {
            graphView.RebuildUI();
            List<Node> nodeList = GraphModel.NodeModels.Select(n => n.GetUI<Node>(graphView)).ToList();

            // Nothing is connected. The collapse button should be enabled.
            Assert.AreEqual(2, nodeList.Count);
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.SafeQ<VisualElement>(name: "collapse-button");
                Assert.False(collapseButton.GetDisabledPseudoState());
            }

            var edge = GraphModel.CreateEdge(m_Node1.GetOutputPorts().First(), m_Node2.GetInputPorts().First());
            graphView.RebuildUI();
            nodeList = GraphModel.NodeModels.Select(n => n.GetUI<Node>(graphView)).ToList();

            // Ports are connected. The collapse button should be disabled.
            Assert.AreEqual(2, nodeList.Count);
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.SafeQ<VisualElement>(name: "collapse-button");
                Assert.True(collapseButton.GetDisabledPseudoState());
            }

            // Disconnect the ports of the 2 nodes.
            GraphModel.DeleteEdge(edge);
            graphView.RebuildUI();
            nodeList = GraphModel.NodeModels.Select(n => n.GetUI<Node>(graphView)).ToList();

            // Once more, nothing is connected. The collapse button should be enabled.
            Assert.AreEqual(2, nodeList.Count);
            foreach (Node node in nodeList)
            {
                VisualElement collapseButton = node.SafeQ<VisualElement>(name: "collapse-button");
                Assert.False(collapseButton.GetDisabledPseudoState());
            }
        }

        [UnityTest]
        public IEnumerator SelectedNodeCanBeDeleted()
        {
            MarkGraphViewStateDirty();
            yield return null;

            var nodeList = GraphModel.NodeModels.Select(n => n.GetUI<Node>(graphView)).ToList();
            int initialCount = nodeList.Count();
            Assert.Greater(initialCount, 0);

            Node node = nodeList.First();
            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node.Model));
            yield return null;

            var elementsToRemove = graphView.GetSelection();
            graphView.Dispatch(new DeleteElementsCommand(elementsToRemove.ToList()));
            yield return null;

            Assert.AreEqual(initialCount - 1, GraphModel.NodeModels.Select(n => n.GetUI<Node>(graphView)).Count());
        }

        [UnityTest]
        public IEnumerator SelectedEdgeCanBeDeleted()
        {
            var edge = GraphModel.CreateEdge(m_Node1.GetOutputPorts().First(), m_Node2.GetInputPorts().First());
            MarkGraphViewStateDirty();
            yield return null;

            int initialCount = window.GraphView.GraphModel.EdgeModels.Count;
            Assert.Greater(initialCount, 0);

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, edge));
            yield return null;

            var elementsToRemove = graphView.GetSelection();
            graphView.Dispatch(new DeleteElementsCommand(elementsToRemove.ToList()));
            yield return null;

            Assert.AreEqual(initialCount - 1, window.GraphView.GraphModel.EdgeModels.Count);
        }

        [UnityTest]
        public IEnumerator EdgeColorsMatchCustomPortColors()
        {
            graphView.AddToClassList("EdgeColorsMatchCustomPortColors");

            var edge = GraphModel.CreateEdge(m_Node2.GetInputPorts().First(), m_Node1.GetOutputPorts().First());
            MarkGraphViewStateDirty();
            yield return null;

            var outputPort = m_Node1.GetOutputPorts().First().GetUI<Port>(graphView);
            var inputPort = m_Node2.GetInputPorts().First().GetUI<Port>(graphView);
            var edgeControl = edge.GetUI<Edge>(graphView)?.EdgeControl;

            Assert.IsNotNull(outputPort);
            Assert.IsNotNull(inputPort);
            Assert.IsNotNull(edgeControl);

            Assert.AreEqual(Color.red, inputPort.PortColor);
            Assert.AreEqual(Color.blue, outputPort.PortColor);

            Assert.AreEqual(inputPort.PortColor, edgeControl.InputColor);
            Assert.AreEqual(outputPort.PortColor, edgeControl.OutputColor);
        }
    }
}
