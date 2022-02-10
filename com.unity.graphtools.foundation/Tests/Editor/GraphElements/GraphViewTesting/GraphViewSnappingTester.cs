using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewSnappingTester : GraphViewTester
    {
        protected const float k_SnapDistance = 8.0f;

        protected Vector2 m_ReferenceNode1Pos;
        protected Vector2 m_ReferenceNode2Pos;
        protected Vector2 m_SnappingNodePos;
        protected Vector2 m_SelectionOffset = new Vector2(25, 25);

        protected IONodeModel snappingNodeModel { get; set; }
        protected IONodeModel referenceNode1Model { get; set; }
        protected IONodeModel referenceNode2Model { get; set; }

        protected IPortModel m_InputPort;
        protected IPortModel m_OutputPort;

        protected Node m_SnappedNode;
        protected Node m_ReferenceNode1;
        protected Node m_ReferenceNode2;

        protected static void SetUINodeSize(ref Node node, float height, float width)
        {
            node.style.height = height;
            node.style.width = width;
        }

        protected IEnumerator UpdateUINodeSizes(Vector2 snappedNodeSize, Vector2 referenceNode1Size, Vector2 referenceNode2Size = default)
        {
            // Changing the nodes' sizes to make it easier to test the snapping
            SetUINodeSize(ref m_SnappedNode, snappedNodeSize.y, snappedNodeSize.x);
            SetUINodeSize(ref m_ReferenceNode1, referenceNode1Size.y, referenceNode1Size.x);
            SetUINodeSize(ref m_ReferenceNode2, referenceNode2Size.y, referenceNode2Size.x);

            yield return null;
        }

        protected IEnumerator SetUpUIElements(Vector2 snappingNodePos, Vector2 referenceNode1Pos = default, Vector2 referenceNode2Pos = default, bool isVerticalPort = false, bool isPortSnapping = false)
        {
            m_SnappingNodePos = snappingNodePos;
            m_ReferenceNode1Pos = referenceNode1Pos;
            m_ReferenceNode2Pos = referenceNode2Pos;

            snappingNodeModel = CreateNode("Snapping Node", GraphViewStaticBridge.RoundToPixelGrid(m_SnappingNodePos), inCount: 1,
                orientation: isVerticalPort ? PortOrientation.Vertical : PortOrientation.Horizontal);
            referenceNode1Model = CreateNode("Reference Node 1", GraphViewStaticBridge.RoundToPixelGrid(m_ReferenceNode1Pos), outCount: 1,
                orientation: isVerticalPort ? PortOrientation.Vertical : PortOrientation.Horizontal);
            referenceNode2Model = CreateNode("Reference Node 2", GraphViewStaticBridge.RoundToPixelGrid(m_ReferenceNode2Pos),
                orientation: isVerticalPort ? PortOrientation.Vertical : PortOrientation.Horizontal);

            if (isPortSnapping)
            {
                m_InputPort = snappingNodeModel.InputsByDisplayOrder[0];
                m_OutputPort = referenceNode1Model.OutputsByDisplayOrder[0];
                Assert.IsNotNull(m_OutputPort);
                Assert.IsNotNull(m_InputPort);

                MarkGraphViewStateDirty();
                yield return null;

                // Connect the ports together
                var actions = ConnectPorts(m_OutputPort, m_InputPort);
                while (actions.MoveNext())
                {
                    yield return null;
                }
            }

            MarkGraphViewStateDirty();
            yield return null;

            // Get the UI nodes
            m_SnappedNode = snappingNodeModel.GetUI<Node>(graphView);
            m_ReferenceNode1 = referenceNode1Model.GetUI<Node>(graphView);
            m_ReferenceNode2 = referenceNode2Model.GetUI<Node>(graphView);

            m_SnappingNodePos = m_SnappedNode?.layout.position ?? Vector2.zero;
            m_ReferenceNode1Pos = m_ReferenceNode1?.layout.position ?? Vector2.zero;
            m_ReferenceNode2Pos = m_ReferenceNode2?.layout.position ?? Vector2.zero;

            Assert.IsNotNull(m_SnappedNode);
            Assert.IsNotNull(m_ReferenceNode1);
            Assert.IsNotNull(m_ReferenceNode2);
        }

        protected IEnumerator MoveElementWithOffset(Vector2 offset)
        {
            Vector2 worldNodePos = graphView.ContentViewContainer.LocalToWorld(m_SnappingNodePos);
            Vector2 start = worldNodePos + m_SelectionOffset;

            // Move the snapping node.
            helpers.MouseDownEvent(start);
            yield return null;

            Vector2 end = start + offset;
            helpers.MouseDragEvent(start, end);
            yield return null;

            helpers.MouseUpEvent(end);
            yield return null;
        }
    }
}
