using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine.TestTools;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class DragTokenTests : BaseUIFixture
    {
        static readonly Vector2 k_TokenPos = new Vector2(SelectionDragger.panAreaWidth * 0.3f, 0);
        static readonly Vector2 k_NodePos = new Vector2(SelectionDragger.panAreaWidth * 0.8f, 0);

        TestEventHelpers m_Helpers;
        TestNodeModel NodeModel { get; set; }
        IConstantNodeModel TokenModel { get; set; }

        protected override bool CreateGraphOnStartup => true;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            NodeModel = GraphModel.CreateNode<TestNodeModel>("OtherNode", k_NodePos);
            TokenModel = GraphModel.CreateConstantNode(TypeHandle.Float, "Constant", k_TokenPos);
            MarkGraphModelStateDirty();
            m_Helpers = new TestEventHelpers(Window);
        }

        [UnityTest]
        public IEnumerator DraggingTokenToPortConnectsAndIsStillMoveable([Values] TestingMode testingMode)
        {
            yield return null;
            var token = TokenModel.GetView<Node>(GraphView);
            Assert.NotNull(token);
            var port = NodeModel.InputsByDisplayOrder.Single().GetView<Port>(GraphView);
            Assert.NotNull(port);

            Vector2 worldTokenPos = token.worldBound.min + new Vector2(5, 5);
            Vector2 worldPortPos = port.worldBound.center;

            yield return TestPrereqCommandPostreq(testingMode,
                () =>
                {
                    Assert.True(TokenModel.HasCapability(Capabilities.Movable));
                    port = NodeModel.InputsByDisplayOrder.Single().GetView<Port>(GraphView);
                    Assert.NotNull(port);
                    Assert.False(port.PortModel.IsConnected());
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            {
                                m_Helpers.MouseDownEvent(worldTokenPos);
                                return TestPhase.WaitForNextFrame;
                            }
                        case 1:
                            {
                                m_Helpers.MouseDragEvent(worldTokenPos, worldPortPos);
                                return TestPhase.WaitForNextFrame;
                            }
                        case 2:
                            m_Helpers.MouseUpEvent(worldPortPos);
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    var nodeModel = GraphModel.NodeModels.OfType<TestNodeModel>().Single();
                    var portModel = nodeModel.InputsByDisplayOrder.Single();
                    Assert.NotNull(portModel);
                    Assert.True(portModel.IsConnected());
                    Assert.True(TokenModel.HasCapability(Capabilities.Movable));
                });
        }
    }
}
