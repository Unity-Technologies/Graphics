using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    class NodeModelPortsOrderTests : BaseFixture<NoUIGraphViewTestGraphTool>
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void DefinePortsInNewOrderReusesExistingPorts()
        {
            var node = GraphModel.CreateNode<PortOrderTestNodeModel>("test", Vector2.zero,
                initializationCallback: model => model.MakePortsFromNames(new List<string> { "A", "B", "C" }));
            Assert.That(node.InputsById.Count, Is.EqualTo(3));

            var a = node.InputsById["A"];
            var b = node.InputsById["B"];
            var c = node.InputsById["C"];

            Assert.That(a, Is.Not.Null);
            Assert.That(b, Is.Not.Null);
            Assert.That(c, Is.Not.Null);

            Assert.That(node.IsSorted, Is.True);
            node.RandomizePorts();
            Assert.That(node.IsSorted, Is.False);

            node.DefineNode();
            Assert.That(node.InputsById.Count, Is.EqualTo(3));
            Assert.That(ReferenceEquals(a, node.InputsById["A"]), Is.True);
            Assert.That(ReferenceEquals(b, node.InputsById["B"]), Is.True);
            Assert.That(ReferenceEquals(c, node.InputsById["C"]), Is.True);
        }

        [Test]
        public void RemovingAndAddingPortsPreservesExistingPorts()
        {
            var node = GraphModel.CreateNode<PortOrderTestNodeModel>("test", Vector2.zero,
                initializationCallback: model => model.MakePortsFromNames(new List<string> { "A", "B", "C" }));
            Assert.That(node.InputsById.Count, Is.EqualTo(3));

            var a = node.InputsById["A"];
            var b = node.InputsById["B"];
            var c = node.InputsById["C"];

            node.MakePortsFromNames(new List<string> { "A", "D", "B" });
            node.DefineNode();
            Assert.That(node.InputsById.Count, Is.EqualTo(3));

            Assert.That(ReferenceEquals(a, node.InputsById["A"]), Is.True);
            Assert.That(ReferenceEquals(b, node.InputsById["B"]), Is.True);
            Assert.That(ReferenceEquals(c, node.InputsById["D"]), Is.False);
        }

        [Test]
        public void ShufflingPortsPreserveConnections()
        {
            var node = GraphModel.CreateNode<PortOrderTestNodeModel>("test", Vector2.zero,
                initializationCallback: model => model.MakePortsFromNames(new List<string> { "A", "B", "C" }));

            var decl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Int, "myInt", ModifierFlags.None, true);
            var nodeA = GraphModel.CreateVariableNode(decl, Vector2.up);
            var nodeB = GraphModel.CreateVariableNode(decl, Vector2.zero);
            var nodeC = GraphModel.CreateVariableNode(decl, Vector2.down);

            GraphModel.CreateEdge(node.InputsById["A"], nodeA.OutputPort);
            GraphModel.CreateEdge(node.InputsById["B"], nodeB.OutputPort);
            GraphModel.CreateEdge(node.InputsById["C"], nodeC.OutputPort);

            Assert.That(nodeA.OutputPort, Is.ConnectedTo(node.InputsById["A"]));
            Assert.That(nodeB.OutputPort, Is.ConnectedTo(node.InputsById["B"]));
            Assert.That(nodeC.OutputPort, Is.ConnectedTo(node.InputsById["C"]));

            Assert.That(node.IsSorted, Is.True);
            node.RandomizePorts();
            Assert.That(node.IsSorted, Is.False);

            node.DefineNode();

            Assert.That(nodeA.OutputPort, Is.ConnectedTo(node.InputsById["A"]));
            Assert.That(nodeB.OutputPort, Is.ConnectedTo(node.InputsById["B"]));
            Assert.That(nodeC.OutputPort, Is.ConnectedTo(node.InputsById["C"]));
        }

        [Test]
        public void ConnectingADifferentNodePreservesConnections([Values] TestingMode mode)
        {
            const string nodeName = "Node0";

            {
                var iDecl = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Int, "myInt", ModifierFlags.None, true);
                GraphModel.CreateVariableNode(iDecl, Vector2.up);

                var vDecl = GraphModel.CreateGraphVariableDeclaration(typeof(Vector3).GenerateTypeHandle(), "myVec", ModifierFlags.None, true);
                var myVec = GraphModel.CreateVariableNode(vDecl, Vector2.left);
                var getProperty = GraphModel.CreateNode<Type0FakeNodeModel>(nodeName, Vector2.zero);
                GraphModel.CreateEdge(getProperty.Input0, myVec.OutputPort);

                var log1 = GraphModel.CreateNode<Type0FakeNodeModel>("log1");
                var log2 = GraphModel.CreateNode<Type0FakeNodeModel>("log2");

                GraphModel.CreateEdge(log1.Input0, getProperty.Output0);
                GraphModel.CreateEdge(log2.Input0, getProperty.Output1);
            }

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    var log1 = GraphModel.NodeModels[3] as Type0FakeNodeModel;
                    var log2 = GraphModel.NodeModels[4] as Type0FakeNodeModel;
                    var myInt = GetAllNodes().OfType<VariableNodeModel>().Single(n => n.GetDataType() == TypeHandle.Int);
                    var getProperty = GetAllNodes().OfType<Type0FakeNodeModel>().First(n => n.Title == nodeName);

                    Assert.That(myInt.OutputPort.IsConnected, Is.False);
                    Assert.That(log1?.Input0, Is.ConnectedTo(getProperty.Output0));
                    Assert.That(log2?.Input0, Is.ConnectedTo(getProperty.Output1));
                    return new CreateEdgeCommand(log1?.Input0, myInt.OutputPort);
                },
                () =>
                {
                    var log1 = GraphModel.NodeModels[3] as Type0FakeNodeModel;
                    var log2 = GraphModel.NodeModels[4] as Type0FakeNodeModel;
                    var myInt = GetAllNodes().OfType<VariableNodeModel>().Single(n => n.GetDataType() == TypeHandle.Int);
                    var getProperty = GetAllNodes().OfType<Type0FakeNodeModel>().First(n => n.Title == nodeName);

                    Assert.That(myInt.OutputPort.IsConnected, Is.True);
                    Assert.That(getProperty.Output0.IsConnected, Is.False);
                    Assert.That(log1?.Input0, Is.ConnectedTo(myInt.OutputPort));
                    Assert.That(log2?.Input0, Is.ConnectedTo(getProperty.Output1));
                });
        }
    }
}
