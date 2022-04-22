using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class NodeTests : BaseUIFixture
    {
        const float k_AcceptablePortDelta = 0.00005f;

        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        float GetPortYInGraph(Port port)
        {
            var gvPos = new Vector2(GraphView.ViewTransform.position.x, GraphView.ViewTransform.position.y);
            var gvScale = GraphView.ViewTransform.scale.x;
            var connector = port.GetConnector();
            var localCenter = connector.layout.size * .5f;

            return (connector.ChangeCoordinatesTo(GraphView.contentContainer, localCenter - gvPos) / gvScale).y;
        }

        [UnityTest]
        public IEnumerator AlignNodesCommandWorks([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantA", Vector2.zero);
            var constantB = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantB", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.one * 200);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.one * 500);

            GraphModel.CreateEdge(binary0.Input0, constantA.OutputPort);
            GraphModel.CreateEdge(binary1.Input0, binary0.Output0);
            GraphModel.CreateEdge(binary1.Input1, constantB.OutputPort);

            MarkGraphModelStateDirty();
            yield return null;

            void RefreshModelReferences()
            {
                constantA = GraphModel.NodeModels[0] as IConstantNodeModel;
                constantB = GraphModel.NodeModels[1] as IConstantNodeModel;
                binary0 = GraphModel.NodeModels[2] as Type0FakeNodeModel;
                binary1 = GraphModel.NodeModels[3] as Type0FakeNodeModel;

                Assert.IsNotNull(constantA);
                Assert.IsNotNull(constantB);
                Assert.IsNotNull(binary0);
                Assert.IsNotNull(binary1);
            }

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshModelReferences();

                    Assert.That(binary0.Input0, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.Input0, Is.ConnectedTo(binary0.Output0));
                    Assert.That(binary1.Input1, Is.ConnectedTo(constantB.OutputPort));

                    var port0UI = binary0.Output0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreNotEqual(GetPortYInGraph(port0UI), GetPortYInGraph(port1UI));

                    var port2UI = constantA.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreNotEqual(GetPortYInGraph(port2UI), GetPortYInGraph(port3UI));

                    var port4UI = binary1.Input1.GetView<Port>(GraphView);
                    Assert.IsNotNull(port4UI);
                    var port5UI = constantB.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port5UI);
                    Assert.AreNotEqual(GetPortYInGraph(port4UI), GetPortYInGraph(port5UI));
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            GraphView.Dispatch(new AlignNodesCommand(GraphView, false, binary1));
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    RefreshModelReferences();

                    var port0UI = binary0.Output0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreEqual(GetPortYInGraph(port0UI), GetPortYInGraph(port1UI), k_AcceptablePortDelta);

                    var port2UI = constantA.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreNotEqual(GetPortYInGraph(port2UI), GetPortYInGraph(port3UI));

                    var port4UI = binary1.Input1.GetView<Port>(GraphView);
                    Assert.IsNotNull(port4UI);
                    var port5UI = constantB.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port5UI);
                    Assert.AreEqual(GetPortYInGraph(port4UI), GetPortYInGraph(port5UI));
                });
        }

        [UnityTest]
        public IEnumerator AlignNodesHierarchiesCommandWorks([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantA", Vector2.zero);
            var constantB = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantB", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.one * 200);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.one * 500);

            GraphModel.CreateEdge(binary0.Input0, constantA.OutputPort);
            GraphModel.CreateEdge(binary1.Input0, binary0.Output0);
            GraphModel.CreateEdge(binary1.Input1, constantB.OutputPort);

            MarkGraphModelStateDirty();
            yield return null;

            void RefreshModelReferences()
            {
                constantA = GraphModel.NodeModels[0] as IConstantNodeModel;
                constantB = GraphModel.NodeModels[1] as IConstantNodeModel;
                binary0 = GraphModel.NodeModels[2] as Type0FakeNodeModel;
                binary1 = GraphModel.NodeModels[3] as Type0FakeNodeModel;

                Assert.IsNotNull(constantA);
                Assert.IsNotNull(constantB);
                Assert.IsNotNull(binary0);
                Assert.IsNotNull(binary1);
            }

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshModelReferences();

                    Assert.That(binary0.Input0, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.Input0, Is.ConnectedTo(binary0.Output0));
                    Assert.That(binary1.Input1, Is.ConnectedTo(constantB.OutputPort));

                    var port0UI = binary0.Output0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreNotEqual(GetPortYInGraph(port0UI), GetPortYInGraph(port1UI));

                    var port2UI = constantA.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreNotEqual(GetPortYInGraph(port2UI), GetPortYInGraph(port3UI));

                    var port4UI = binary1.Input1.GetView<Port>(GraphView);
                    Assert.IsNotNull(port4UI);
                    var port5UI = constantB.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port5UI);
                    Assert.AreNotEqual(GetPortYInGraph(port4UI), GetPortYInGraph(port5UI));
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            GraphView.Dispatch(new AlignNodesCommand(GraphView, true, binary1));
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    RefreshModelReferences();

                    var port0UI = binary0.Output0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreEqual(GetPortYInGraph(port0UI), GetPortYInGraph(port1UI), k_AcceptablePortDelta);

                    var port2UI = constantA.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetView<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreEqual(GetPortYInGraph(port2UI), GetPortYInGraph(port3UI));

                    var port4UI = binary1.Input1.GetView<Port>(GraphView);
                    Assert.IsNotNull(port4UI);
                    var port5UI = constantB.OutputPort.GetView<Port>(GraphView);
                    Assert.IsNotNull(port5UI);
                    Assert.AreEqual(GetPortYInGraph(port4UI), GetPortYInGraph(port5UI));
                });
        }

        [UnityTest]
        public IEnumerator ConvertConstantNodesToVariableNodesCommandWorks([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantA", Vector2.zero);

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.AreEqual(1, GraphModel.NodeModels.Count(n => n is IConstantNodeModel));
                    Assert.AreEqual(0, GraphModel.NodeModels.Count(n => n is IVariableNodeModel));
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            GraphView.Dispatch(new ConvertConstantNodesAndVariableNodesCommand(new[] { constantA }, null));
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    Assert.AreEqual(0, GraphModel.NodeModels.Count(n => n is IConstantNodeModel));
                    Assert.AreEqual(1, GraphModel.NodeModels.Count(n => n is IVariableNodeModel));
                });
        }

        [UnityTest]
        public IEnumerator ConvertVariableNodesToConstantNodesCommandWorks([Values] TestingMode mode)
        {
            var vdm = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "blah", ModifierFlags.Read, false);
            var variable = GraphModel.CreateVariableNode(vdm, Vector2.zero);

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.AreEqual(0, GraphModel.NodeModels.Count(n => n is IConstantNodeModel));
                    Assert.AreEqual(1, GraphModel.NodeModels.Count(n => n is IVariableNodeModel));
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            GraphView.Dispatch(new ConvertConstantNodesAndVariableNodesCommand(null, new[] { variable }));
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    Assert.AreEqual(1, GraphModel.NodeModels.Count(n => n is IConstantNodeModel));
                    Assert.AreEqual(0, GraphModel.NodeModels.Count(n => n is IVariableNodeModel));
                });
        }
    }
}
