using System;
using System.Collections;
using System.Collections.Generic;
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

        float GetPortY(Port port)
        {
            return port.parent.LocalToWorld(port.layout.center).y;
        }

        [UnityTest]
        public IEnumerator AlignNodesCommandWorks([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantA", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.one * 200);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.one * 500);
            GraphModel.CreateEdge(binary0.Input0, constantA.OutputPort);
            GraphModel.CreateEdge(binary1.Input0, binary0.Output0);

            MarkGraphViewStateDirty();
            yield return null;

            void RefreshModelReferences()
            {
                constantA = GraphModel.NodeModels[0] as IConstantNodeModel;
                binary0 = GraphModel.NodeModels[1] as Type0FakeNodeModel;
                binary1 = GraphModel.NodeModels[2] as Type0FakeNodeModel;

                Assert.IsNotNull(constantA);
                Assert.IsNotNull(binary0);
                Assert.IsNotNull(binary1);
            }

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshModelReferences();

                    Assert.That(binary0.Input0, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.Input0, Is.ConnectedTo(binary0.Output0));

                    var port0UI = binary0.Output0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreNotEqual(GetPortY(port0UI), GetPortY(port1UI));

                    var port2UI = constantA.OutputPort.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreNotEqual(GetPortY(port2UI), GetPortY(port3UI));
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

                    var port0UI = binary0.Output0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreEqual(GetPortY(port0UI), GetPortY(port1UI), k_AcceptablePortDelta);

                    var port2UI = constantA.OutputPort.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreNotEqual(GetPortY(port2UI), GetPortY(port3UI));
                });
        }

        [UnityTest]
        public IEnumerator AlignNodesHierarchiesCommandWorks([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "constantA", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.one * 200);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.one * 500);
            GraphModel.CreateEdge(binary0.Input0, constantA.OutputPort);
            GraphModel.CreateEdge(binary1.Input0, binary0.Output0);

            MarkGraphViewStateDirty();
            yield return null;

            void RefreshModelReferences()
            {
                constantA = GraphModel.NodeModels[0] as IConstantNodeModel;
                binary0 = GraphModel.NodeModels[1] as Type0FakeNodeModel;
                binary1 = GraphModel.NodeModels[2] as Type0FakeNodeModel;

                Assert.IsNotNull(constantA);
                Assert.IsNotNull(binary0);
                Assert.IsNotNull(binary1);
            }

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    RefreshModelReferences();

                    Assert.That(binary0.Input0, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.Input0, Is.ConnectedTo(binary0.Output0));

                    var port0UI = binary0.Output0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreNotEqual(GetPortY(port0UI), GetPortY(port1UI));

                    var port2UI = constantA.OutputPort.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreNotEqual(GetPortY(port2UI), GetPortY(port3UI));
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

                    var port0UI = binary0.Output0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port0UI);
                    var port1UI = binary1.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port1UI);
                    Assert.AreEqual(GetPortY(port0UI), GetPortY(port1UI), k_AcceptablePortDelta);

                    var port2UI = constantA.OutputPort.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port2UI);
                    var port3UI = binary0.Input0.GetUI<Port>(GraphView);
                    Assert.IsNotNull(port3UI);
                    Assert.AreEqual(GetPortY(port2UI), GetPortY(port3UI), k_AcceptablePortDelta);
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
            var vdm = GraphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "blah", ModifierFlags.ReadOnly, false);
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
