using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class IHasPortsTests : GtfTestFixture
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
            m_DestinationNodeModel1 = CreateNode(destinationInputPortCount, destinationOutputPortCount);
            m_DestinationPortModels1 = m_DestinationNodeModel1.GetInputPorts().Cast<PortModel>().ToList();

            m_DestinationNodeModel2 = CreateNode(destinationInputPortCount, destinationOutputPortCount);
            m_DestinationPortModels2 = m_DestinationNodeModel2.GetInputPorts().Cast<PortModel>().ToList();
        }

        IInputOutputPortsNodeModel CreateNode(int inputPortCount, int outputPortCount)
        {
            return GraphModel.CreateNode<IONodeModel>(initializationCallback: model =>
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

            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeLabel));
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeLabel));
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

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.AreEqual("1", port2Edge1.EdgeLabel, "Port 2 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port2Edge2.EdgeLabel, "Port 2 Edge 2 should have label \"2\" once revealed");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, port1Edge1));
            yield return null;

            Assert.AreEqual("1", port1Edge1.EdgeLabel, "Port 1 Edge 1 should have label \"1\" once revealed");
            Assert.AreEqual("2", port1Edge2.EdgeLabel, "Port 1 Edge 2 should have label \"2\" once revealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeLabel), "Port 2 Edge 1 should have no label since not targeted");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeLabel), "Port 2 Edge 2 should have no label since not targeted");

            GraphView.Dispatch(new ClearSelectionCommand());
            yield return null;

            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeLabel), "Port 1 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port1Edge2.EdgeLabel), "Port 1 Edge 2 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge1.EdgeLabel), "Port 2 Edge 1 should have no label when unrevealed");
            Assert.IsTrue(string.IsNullOrEmpty(port2Edge2.EdgeLabel), "Port 2 Edge 2 should have no label when unrevealed");
        }

        [UnityTest]
        public IEnumerator RevealOrderableEdgeDoesNothingIfPortIsReorderableEdgeReadyWithSingleEdge()
        {
            m_SourcePortModels[0].SetReorderable(true);
            var port1Edge1 = CreateEdge(m_SourcePortModels[0], m_DestinationPortModels1[0]);

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, m_SourceNodeModel));
            yield return null;

            Assert.IsTrue(string.IsNullOrEmpty(port1Edge1.EdgeLabel));
        }
    }
}
