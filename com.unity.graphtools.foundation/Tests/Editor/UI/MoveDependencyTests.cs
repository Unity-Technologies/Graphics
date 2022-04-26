using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class MoveDependencyTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void DeleteNodeDoesRemoveTheDependency()
        {
            var mgr = new PositionDependenciesManager(GraphView, GraphTool.Preferences);
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            var intModel = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "int", new Vector2(-150, -100));
            var edge = GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);
            mgr.AddPositionDependency(edge);
            mgr.Remove(operatorModel.Guid, intModel.Guid);
            Assert.That(mgr.GetDependencies(operatorModel), Is.Null);
        }

        [UnityTest, Ignore("@theor needs to figure this one out")]
        public IEnumerator EndToEndMoveDependencyWithPanning()
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>(string.Empty, new Vector2(100, -100));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>(string.Empty, new Vector2(100, 100));
            GraphModel.CreateEdge(node1.Input0, node0.Output0);

            MarkGraphModelStateDirty();
            yield return null;
            GraphView.DispatchFrameAllCommand();
            yield return null;

            bool needsMouseUp = false;
            try
            {
                using (var scheduler = GraphView.CreateTimerEventSchedulerWrapper())
                {
                    GraphElement node0UI = node0.GetView<GraphElement>(GraphView);
                    Assert.IsNotNull(node0UI);
                    Vector2 startPos = node0UI.layout.position;
                    Vector2 otherStartPos = node1.Position;
                    Vector2 nodeRect = node0UI.hierarchy.parent.ChangeCoordinatesTo(Window.rootVisualElement, node0UI.layout.center);

                    // Move the movable node.
                    Vector2 pos = nodeRect;
                    Vector2 target = new Vector2(Window.rootVisualElement.layout.xMax - 20, pos.y);
                    needsMouseUp = true;
                    Vector3 pOrig = GraphView.ContentViewContainer.transform.position;
                    Helpers.MouseDownEvent(pos);
                    yield return null;


                    Helpers.MouseMoveEvent(pos, target);
                    Helpers.MouseDragEvent(pos, target);
                    yield return null;

                    scheduler.TimeSinceStartup += GraphView.panInterval;
                    scheduler.UpdateScheduledEvents();

                    Helpers.MouseUpEvent(target);
                    needsMouseUp = false;
                    Vector3 p = GraphView.ContentViewContainer.transform.position;
                    Assume.That(pOrig != p);
                    yield return null;

                    Vector2 delta = node0UI.layout.position - startPos;
                    Assert.That(node1.Position, Is.EqualTo(otherStartPos + delta));
                }
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(Vector2.zero);
            }
        }

        [UnityTest]
        public IEnumerator MovingAFloatingNodeMovesConnectedToken([Values] TestingMode mode)
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "int", new Vector2(-150, -100));
            GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);
            MarkGraphModelStateDirty();
            yield return null;

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { operatorModel },
                expectedMovedDependencies: new INodeModel[] { intModel }
            );
        }

        IEnumerator TestMove(TestingMode mode, Vector2 mouseDelta, INodeModel[] movedNodes,
            INodeModel[] expectedMovedDependencies,
            INodeModel[] expectedUnmovedDependencies = null)
        {
            const float epsilon = 0.00001f;

            Vector2 startMousePos = new Vector2(42, 13);
            List<Vector2> initPositions = expectedMovedDependencies.Select(x => x.Position).ToList();
            List<Vector2> initUnmovedPositions = expectedUnmovedDependencies?.Select(x => x.Position).ToList() ?? new List<Vector2>();

            yield return TestPrereqCommandPostreq(mode,
                () =>
                {
                    for (int i = 0; i < expectedMovedDependencies.Length; i++)
                    {
                        GraphModel.TryGetModelFromGuid(expectedMovedDependencies[i].Guid, out INodeModel model);
                        GraphElement element = model.GetView<GraphElement>(GraphView);

                        Assert.IsNotNull(element);
                        Assert.That(model.Position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                        Assert.That(model.Position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                        Assert.That(element.layout.position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                        Assert.That(element.layout.position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                    }
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            using (var undo = GraphTool.UndoStateComponent.UpdateScope)
                            {
                                undo.SaveSingleState(GraphView.GraphViewModel.GraphModelState, null);
                            }

                            var selectables = movedNodes.ToList();
                            GraphView.PositionDependenciesManager.StartNotifyMove(selectables, startMousePos);
                            GraphView.PositionDependenciesManager.ProcessMovedNodes(startMousePos + mouseDelta);
                            for (int i = 0; i < expectedMovedDependencies.Length; i++)
                            {
                                INodeModel model = expectedMovedDependencies[i];
                                GraphElement element = model.GetView<GraphElement>(GraphView);
                                Assert.IsNotNull(element);
                                Assert.That(model.Position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                                Assert.That(model.Position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                                Assert.That(element.layout.position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                                Assert.That(element.layout.position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                            }
                            return TestPhase.WaitForNextFrame;
                        default:
                            GraphView.PositionDependenciesManager.StopNotifyMove();
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    for (int i = 0; i < expectedMovedDependencies.Length; i++)
                    {
                        GraphModel.TryGetModelFromGuid(expectedMovedDependencies[i].Guid, out INodeModel model);
                        GraphElement element = model.GetView<GraphElement>(GraphView);
                        Assert.IsNotNull(element);
                        Assert.That(model.Position.x, Is.EqualTo(initPositions[i].x + mouseDelta.x).Within(epsilon), () => $"Model {model} was expected to have moved");
                        Assert.That(model.Position.y, Is.EqualTo(initPositions[i].y + mouseDelta.y).Within(epsilon), () => $"Model {model} was expected to have moved");
                        Assert.That(element.layout.position.x, Is.EqualTo(initPositions[i].x + mouseDelta.x).Within(epsilon));
                        Assert.That(element.layout.position.y, Is.EqualTo(initPositions[i].y + mouseDelta.y).Within(epsilon));
                    }

                    if (expectedUnmovedDependencies != null)
                    {
                        for (int i = 0; i < expectedUnmovedDependencies.Length; i++)
                        {
                            GraphModel.TryGetModelFromGuid(expectedUnmovedDependencies[i].Guid, out INodeModel model);
                            GraphElement element = model.GetView<GraphElement>(GraphView);
                            Assert.IsNotNull(element);
                            Assert.That(model.Position.x, Is.EqualTo(initUnmovedPositions[i].x).Within(epsilon));
                            Assert.That(model.Position.y, Is.EqualTo(initUnmovedPositions[i].y).Within(epsilon));
                            Assert.That(element.layout.position.x, Is.EqualTo(initUnmovedPositions[i].x).Within(epsilon));
                            Assert.That(element.layout.position.y, Is.EqualTo(initUnmovedPositions[i].y).Within(epsilon));
                        }
                    }
                }
            );
        }
    }
}
