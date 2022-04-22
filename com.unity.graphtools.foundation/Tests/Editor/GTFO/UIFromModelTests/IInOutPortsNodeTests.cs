using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class HasPortsTests : GtfTestFixture
    {
        IInputOutputPortsNodeModel m_SourceNodeModel;
        IInputOutputPortsNodeModel m_DestinationNodeModel1;
        IInputOutputPortsNodeModel m_DestinationNodeModel2;
        List<PortModel> m_SourcePortModels;
        List<PortModel> m_DestinationPortModels1;
        List<PortModel> m_DestinationPortModels2;

        [SetUp]
        public new void SetUp()
        {
            const int sourceInputPortCount = 0;
            const int sourceOutputPortCount = 2;
            m_SourceNodeModel = CreateNode(sourceInputPortCount, sourceOutputPortCount);
            m_SourcePortModels = m_SourceNodeModel.GetOutputPorts().Cast<PortModel>().ToList();

            const int destinationInputPortCount = 1;
            const int destinationOutputPortCount = 0;
            m_DestinationNodeModel1 = CreateNode(destinationInputPortCount, destinationOutputPortCount, new Vector2(150, -100));
            m_DestinationPortModels1 = m_DestinationNodeModel1.GetInputPorts().Cast<PortModel>().ToList();

            m_DestinationNodeModel2 = CreateNode(destinationInputPortCount, destinationOutputPortCount, new Vector2(150, 100));
            m_DestinationPortModels2 = m_DestinationNodeModel2.GetInputPorts().Cast<PortModel>().ToList();
        }

        IInputOutputPortsNodeModel CreateNode(int inputPortCount, int outputPortCount, Vector2 position = default)
        {
            return GraphModel.CreateNode<IONodeModel>(position: position, initializationCallback: model =>
            {
                model.InputCount = inputPortCount;
                model.OutputCount = outputPortCount;
            });
        }

        IEdgeModel CreateEdge(IPortModel to, IPortModel from)
        {
            return GraphModel.CreateEdge(from, to);
        }

        [UnityTest]
        public IEnumerator RevealOrderableEdgeDoesNothingIfPortIsNotReorderableEdgeReady()
        {
            m_SourcePortModels[0].SetReorderable(false);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_SourceNodeModel));
            yield return null;

            Assert.IsFalse(ShouldShowLabel(port1Edge1));
            Assert.IsFalse(ShouldShowLabel(port1Edge2));
        }

        bool ShouldShowLabel(IEdgeModel edge)
        {
            return EdgeBubblePart.EdgeShouldShowLabel(edge, GraphView.GraphViewModel.SelectionState);
        }

        [UnityTest]
        public IEnumerator EdgeOrderRevealedOnNodeSelection()
        {
            m_SourcePortModels[0].SetReorderable(true);
            m_SourcePortModels[1].SetReorderable(true);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);
            var port2Edge1 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels1[0]);
            var port2Edge2 = CreateEdge(m_SourcePortModels[1], m_DestinationPortModels2[0]);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_SourceNodeModel));
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should always have label \"1\"");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should always have label \"2\"");
            Assert.AreEqual("1", port2Edge1.EdgeLabel, "Port 2 Edge 1 should always have label \"1\"");
            Assert.AreEqual("2", port2Edge2.EdgeLabel, "Port 2 Edge 2 should always have label \"2\"");

            Assert.IsTrue(ShouldShowLabel(port1Edge1));
            Assert.IsTrue(ShouldShowLabel(port1Edge2));
            Assert.IsTrue(ShouldShowLabel(port2Edge1));
            Assert.IsTrue(ShouldShowLabel(port2Edge2));

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, port1Edge1));
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should always have label \"1\"");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should always have label \"2\"");
            Assert.AreEqual("1", port2Edge1.EdgeLabel, "Port 2 Edge 1 should always have label \"1\"");
            Assert.AreEqual("2", port2Edge2.EdgeLabel, "Port 2 Edge 2 should always have label \"2\"");

            Assert.IsTrue(ShouldShowLabel(port1Edge1));
            Assert.IsTrue(ShouldShowLabel(port1Edge2));
            Assert.IsFalse(ShouldShowLabel(port2Edge1));
            Assert.IsFalse(ShouldShowLabel(port2Edge2));

            GraphView.Dispatch(new ClearSelectionCommand());
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should always have label \"1\"");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should always have label \"2\"");
            Assert.AreEqual("1", port2Edge1.EdgeLabel, "Port 2 Edge 1 should always have label \"1\"");
            Assert.AreEqual("2", port2Edge2.EdgeLabel, "Port 2 Edge 2 should always have label \"2\"");

            Assert.IsFalse(ShouldShowLabel(port1Edge1));
            Assert.IsFalse(ShouldShowLabel(port1Edge2));
            Assert.IsFalse(ShouldShowLabel(port2Edge1));
            Assert.IsFalse(ShouldShowLabel(port2Edge2));
        }

        [UnityTest]
        public IEnumerator EdgeLabelPreservedOnReorder()
        {
            m_SourcePortModels[0].SetReorderable(true);
            m_SourcePortModels[1].SetReorderable(true);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);
            var port1Edge2 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels2[0]);

            MarkGraphModelStateDirty();
            yield return null;

            GraphView.Dispatch(new ReframeGraphViewCommand(Vector3.zero, Vector3.one, new List<IGraphElementModel>
                { m_SourceNodeModel, m_DestinationNodeModel1, m_DestinationNodeModel2 }));
            yield return null;

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_SourceNodeModel));
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"1\"");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"2\"");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, port1Edge1));
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"1\"");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"2\"");

            GraphView.Dispatch(new ClearSelectionCommand());
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"1\"");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"2\"");

            GraphView.Dispatch(new ReorderEdgeCommand(port1Edge1, ReorderType.MoveDown));
            yield return null;

            Assert.AreEqual("2", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"2\"");
            Assert.AreEqual("1", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"1\"");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, port1Edge1));
            yield return null;

            Assert.AreEqual("2", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"2\"");
            Assert.AreEqual("1", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"1\"");

            GraphView.Dispatch(new ReorderEdgeCommand(port1Edge1, ReorderType.MoveFirst));
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"1\"");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"2\"");
        }

        [UnityTest]
        public IEnumerator RevealOrderableEdgeDoesNothingIfPortIsReorderableEdgeReadyWithSingleEdge()
        {
            m_SourcePortModels[0].SetReorderable(true);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_SourceNodeModel));
            yield return null;

            Assert.IsFalse(ShouldShowLabel(port1Edge1));
        }
    }
}
