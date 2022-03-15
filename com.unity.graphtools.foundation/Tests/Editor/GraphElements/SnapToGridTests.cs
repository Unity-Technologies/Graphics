using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class SnapToGridHelper
    {
        public const float snapDistance = 8.0f;

        public enum Edge
        {
            None,
            Top,
            Left,
            Bottom,
            Right
        }

        public static float GetSnapDistance(GraphElement node, Edge edge)
        {
            var borderWidth = SnapToGridStrategy.GetBorderWidth(node);

            switch (edge)
            {
                case Edge.Top:
                    return snapDistance + borderWidth.Top;
                case Edge.Right:
                    return snapDistance - borderWidth.Right;
                case Edge.Bottom:
                    return snapDistance - borderWidth.Bottom;
                case Edge.Left:
                    return snapDistance + borderWidth.Left;
                default:
                    return snapDistance;
            }
        }
    }

    class SnapToGridTests : GraphViewSnappingTester
    {
        const float k_Spacing = 200f;
        const float k_HalfSpacing = k_Spacing * 0.5f;
        static readonly Vector2 k_NewSize = new Vector2(k_HalfSpacing, k_HalfSpacing); // To make it easier to test
        static readonly Vector2 k_ReferenceNodePos = new Vector2(k_Spacing, k_Spacing);

        void TestElementPosition(Vector2 offset, bool isSnapping, bool isHorizontalSnapping, SnapToGridHelper.Edge edgeToTest)
        {
            var borderWidth = SnapToGridStrategy.GetBorderWidth(m_SnappedNode);
            float borderOffset;
            switch (edgeToTest)
            {
                case SnapToGridHelper.Edge.Top:
                    borderOffset = -GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Top);
                    break;
                case SnapToGridHelper.Edge.Right:
                    borderOffset = GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Right);
                    break;
                case SnapToGridHelper.Edge.Bottom:
                    borderOffset = GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Bottom);
                    break;
                case SnapToGridHelper.Edge.Left:
                    borderOffset = -GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Left);
                    break;
                default:
                    borderOffset = 0.0f;
                    break;
            }

            if (isSnapping)
            {
                if (isHorizontalSnapping)
                {
                    // X should snap
                    Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x),
                        m_SnappedNode.layout.x + borderOffset);
                    Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + offset.x),
                        m_SnappedNode.layout.x + borderOffset);

                    // Y should be dragged normally
                    Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + offset.y),
                        m_SnappedNode.layout.y);
                }
                else
                {
                    // Y should snap
                    Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y),
                        m_SnappedNode.layout.y + borderOffset);
                    Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + offset.y),
                        m_SnappedNode.layout.y + borderOffset);

                    // X should be dragged normally
                    Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + offset.x),
                        m_SnappedNode.layout.x);
                }
            }
            else
            {
                // X and Y should be dragged normally
                Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + offset.y),
                    m_SnappedNode.layout.y);
                Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + offset.x),
                    m_SnappedNode.layout.x);
            }
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_SnappedNode = null;
            m_SnappingNodePos = Vector2.zero;

            GraphViewSettings.UserSettings.EnableSnapToGrid = true;
            GraphViewSettings.UserSettings.EnableSnapToPort = false;
            GraphViewSettings.UserSettings.EnableSnapToBorders = false;
            GraphViewSettings.UserSettings.EnableSnapToSpacing = false;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldSnapToGridLine()
        {
            // Config
            //           |          |
            //   --------+-------+-----------
            //           | Node1 |  |
            //           +-------+  |
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }
            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(10, SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Top) + 1); // offset is greater than max snap distance
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, false, false, SnapToGridHelper.Edge.Top);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementTopBorderShouldNotSnapToGridLine()
        {
            // Config
            //           |          |
            //   --------+-------+-----------
            //           | Node1 |  |
            //           +-------+  |
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(10, SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Top));
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, true, false, SnapToGridHelper.Edge.Top);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementHorizontalCenterBorderShouldSnapToGridLine()
        {
            // Config
            //           |          |
            //           +-------+  |
            //   --------+ Node1 +-----------
            //           +-------+  |
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y - k_HalfSpacing * 0.5f));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(10, SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.None) + 1);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, false, false, SnapToGridHelper.Edge.None);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementHorizontalCenterBorderShouldNotSnapToGridLine()
        {
            // Config
            //           |          |
            //           +-------+  |
            //   --------+ Node1 +-----------
            //           +-------+  |
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y - k_HalfSpacing * 0.5f));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(10, SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.None));
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, true, false, SnapToGridHelper.Edge.None);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldSnapToGridLine()
        {
            // Config
            //           |          |
            //           +-------+  |
            //           | Node1 |  |
            //   --------+-------+-----------
            //           |          |
            //           |          |
            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y - k_HalfSpacing));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(10, SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Bottom) + 1);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, false, false, SnapToGridHelper.Edge.Bottom);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementBottomBorderShouldNotSnapToGridLine()
        {
            // Config
            //           |          |
            //           +-------+  |
            //           | Node1 |  |
            //   --------+-------+-----------
            //           |          |
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y - k_HalfSpacing));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(10, SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Bottom + 1));
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, true, false, SnapToGridHelper.Edge.Bottom);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementLeftBorderShouldSnapToGridLine()
        {
            // Config
            //           |          |
            //   --------+-------+-----------
            //           | Node1 |  |
            //           +-------+  |
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Left) + 1, 10);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, false, true, SnapToGridHelper.Edge.Left);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementLeftBorderShouldNotSnapToGridLine()
        {
            // Config
            //           |          |
            //   --------+-------+-----------
            //           | Node1 |  |
            //           +-------+  |
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Left) + 1, 10);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, false, true, SnapToGridHelper.Edge.Left);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementVerticalCenterBorderShouldSnapToGridLine()
        {
            // Config
            //            |          |
            //   -----+-------+-------------
            //        | Node1 |      |
            //        +-------+      |
            //            |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x - (k_HalfSpacing * 0.5f), k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.None), 10);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, true, true, SnapToGridHelper.Edge.None);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementVerticalCenterShouldNotSnapToGridLine()
        {
            // Config
            //            |          |
            //   -----+-------+-------------
            //        | Node1 |      |
            //        +-------+      |
            //            |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x - (k_HalfSpacing * 0.5f), k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.None) + 2, 10);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, false, true, SnapToGridHelper.Edge.None);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementRightBorderShouldSnapToGridLine()
        {
            // Config
            //           |          |
            //   --------+--+-------+---------
            //           |  | Node1 |
            //           |  +-------+
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + k_HalfSpacing, k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Right), 10);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, true, true, SnapToGridHelper.Edge.Right);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementRightBorderShouldNotSnapToGridLine()
        {
            // Config
            //           |          |
            //   --------+--+-------+---------
            //           |  | Node1 |
            //           |  +-------+
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + k_HalfSpacing, k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Right) + 1, 10);
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            TestElementPosition(moveOffset, false, true, SnapToGridHelper.Edge.Right);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementShouldSnapToMultipleGridLines()
        {
            // Config
            //           |          |
            //   --------+--+-------+---------
            //           |  | Node1 |
            //           |  +-------+
            //           |          |

            var actions = SetUpUIElements(new Vector2(k_ReferenceNodePos.x + k_HalfSpacing, k_ReferenceNodePos.y));

            while (actions.MoveNext())
            {
                yield return null;
            }

            actions = UpdateUINodeSizes(k_NewSize, k_NewSize, k_NewSize);

            while (actions.MoveNext())
            {
                yield return null;
            }

            Vector2 moveOffset = new Vector2(SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Right), SnapToGridHelper.GetSnapDistance(m_SnappedNode, SnapToGridHelper.Edge.Top));
            actions = MoveElementWithOffset(moveOffset);

            while (actions.MoveNext())
            {
                yield return null;
            }

            // The snapping node's top and right border should snap to the corresponding grid lines
            var borderWidth = SnapToGridStrategy.GetBorderWidth(m_SnappedNode);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.x + borderWidth.Right));
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y),
                m_SnappedNode.layout.y - GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Top));
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                m_SnappedNode.layout.y - GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Top));
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                GraphViewStaticBridge.RoundToPixelGrid(m_SnappedNode.layout.x + borderWidth.Right));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ElementUnderMouseShouldSnapWhenMultipleSelectedElements()
        {
            // Config
            //
            //    |               |
            //----+-------+-----------------
            //    | Node1 | +-----|----+
            //    +-------+ | Placemat |
            //    |         +----------+
            //    |               |

            m_SnappingNodePos = new Vector2(k_ReferenceNodePos.x, k_ReferenceNodePos.y);
            snappingNodeModel = CreateNode("Node1", m_SnappingNodePos);

            Vector2 secondElementPos = new Vector2(m_SnappingNodePos.x + k_HalfSpacing, m_SnappingNodePos.y + 10);
            var secondElementModel = CreatePlacemat(new Rect(secondElementPos, new Vector2(k_HalfSpacing, k_HalfSpacing)), "Placemat");

            MarkGraphViewStateDirty();
            yield return null;

            // Get the UI nodes
            var snappingNode = snappingNodeModel.GetView<Node>(GraphView);
            Placemat secondElement = secondElementModel.GetView<Placemat>(GraphView);
            Assert.IsNotNull(snappingNode);
            Assert.IsNotNull(secondElement);

            // Changing the node' size to make it easier to test the snapping
            SetUINodeSize(ref snappingNode, k_HalfSpacing, k_HalfSpacing);
            yield return null;

            Vector2 worldPosSnappingNode = GraphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 worldPosSecondElement = GraphView.ContentViewContainer.LocalToWorld(secondElementPos);

            Vector2 selectionPosSnappingNode = worldPosSnappingNode + m_SelectionOffset;
            Vector2 selectionPosSecondElement = worldPosSecondElement + m_SelectionOffset;

            // Select placemat by clicking on it and pressing Ctrl
            Helpers.MouseDownEvent(selectionPosSecondElement, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            Helpers.MouseUpEvent(selectionPosSecondElement, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // Move mouse to Node2
            Helpers.MouseMoveEvent(selectionPosSecondElement, selectionPosSnappingNode, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // Select Node1 by clicking on it and pressing Ctrl
            Helpers.MouseDownEvent(selectionPosSnappingNode, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // Move Node1 within snapping distance
            Vector2 moveOffset = new Vector2(10, SnapToGridHelper.GetSnapDistance(snappingNode, SnapToGridHelper.Edge.Top));
            Vector2 end = selectionPosSnappingNode + moveOffset;
            Helpers.MouseDragEvent(selectionPosSnappingNode, end, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            Helpers.MouseUpEvent(end, MouseButton.LeftMouse, TestEventHelpers.multiSelectModifier);
            yield return null;

            // The snapping Node1 top border should snap but X should be dragged normally
            var borderWidth = SnapToGridStrategy.GetBorderWidth(snappingNode);
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y),
                snappingNode.layout.y - GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Top));
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.x + moveOffset.x),
                snappingNode.layout.x);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos.y + moveOffset.y),
                snappingNode.layout.y);

            // placemat should follow the same offset as Node1
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(secondElementPos.y),
                secondElement.layout.y - GraphViewStaticBridge.RoundToPixelGrid(borderWidth.Top));
            Assert.AreEqual(GraphViewStaticBridge.RoundToPixelGrid(secondElementPos.x + moveOffset.x),
                secondElement.layout.x);
            Assert.AreNotEqual(GraphViewStaticBridge.RoundToPixelGrid(secondElementPos.y + moveOffset.y),
                secondElement.layout.y);

            yield return null;
        }
    }
}
