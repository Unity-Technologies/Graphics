using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    sealed class VirtualFunctionNodeDragger : Manipulator
    {
        const float kDragThreshold = 8;

        readonly GraphView m_GraphView;
        readonly VirtualFunctionOutputNode m_Output;

        Vector2 m_PressPosition;
        bool m_Pressed;
        Label m_Ghost;
        AbstractVirtualFunctionNode m_Input;
        AbstractVirtualFunctionNode m_Base;
        AbstractVirtualFunctionNode m_Shown;

        internal VirtualFunctionNodeDragger(GraphView graphView, VirtualFunctionOutputNode output)
        {
            m_GraphView = graphView;
            m_Output = output;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOut);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        AbstractVirtualFunctionNode NodeToCreate(bool actionKey) => actionKey && m_Base != null ? m_Base : m_Input;

        void OnMouseDown(MouseDownEvent evt)
        {
            // Inline input fields sit outside the node's bounds, so presses on them don't start a drag.
            if (evt.button != (int)MouseButton.LeftMouse || evt.altKey || target is not GraphElement node || !node.HitTest(node.WorldToLocal(evt.mousePosition)))
                return;

            // Non-movable nodes must stop the click, or GraphView's rectangle selector clears the selection.
            evt.StopPropagation();
            if (m_Output.isOrphaned || target.panel?.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            m_PressPosition = evt.mousePosition;
            m_Pressed = true;
            target.CaptureMouse();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (!m_Pressed)
                return;

            if (m_Ghost == null)
            {
                if ((evt.mousePosition - m_PressPosition).magnitude < kDragThreshold)
                    return;
                m_Input = new VirtualFunctionInputNode();
                m_Base = AbstractVirtualFunctionNode.HasBase(m_Output.Provider.Definition) ? new VirtualFunctionBaseNode() : null;
                m_Ghost = CreateGhost();
                m_GraphView.Add(m_Ghost);
            }

            m_Shown = NodeToCreate(evt.actionKey);
            m_Ghost.text = m_Shown.TitleFor(m_Output.Provider.Definition);
            var position = m_GraphView.WorldToLocal(evt.mousePosition);
            m_Ghost.style.left = position.x;
            m_Ghost.style.top = position.y;
            evt.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (!m_Pressed || evt.button != (int)MouseButton.LeftMouse)
                return;

            var node = m_Shown;
            End();
            if (node != null && !m_Output.isOrphaned && IsCanvas(m_GraphView.panel.Pick(evt.mousePosition)))
            {
                var graph = m_Output.owner;
                graph.owner?.RegisterCompleteObjectUndo("Add " + node.TitleFor(m_Output.Provider.Definition));
                graph.AddLocalProviderNode(node, m_Output.Provider, m_GraphView.contentViewContainer.WorldToLocal(evt.mousePosition));
            }
            evt.StopPropagation();
        }

        // The blackboard, inspector and preview sit inside the graph view, so they're ruled out by type.
        bool IsCanvas(VisualElement picked) => picked?.GetFirstOfType<GraphView>() == m_GraphView
            && picked.GetFirstOfType<GraphSubWindow>() == null && picked.GetFirstOfType<VirtualFunctionStackView>() == null;

        void OnMouseCaptureOut(MouseCaptureOutEvent evt) => End();

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!m_Pressed || evt.keyCode != KeyCode.Escape)
                return;

            End();
            evt.StopPropagation();
        }

        void End()
        {
            m_Pressed = false;
            m_Ghost?.RemoveFromHierarchy();
            m_Ghost = null;
            m_Input = null;
            m_Base = null;
            m_Shown = null;
            if (target.HasMouseCapture())
                target.ReleaseMouse();
        }

        static Label CreateGhost()
        {
            var ghost = new Label { pickingMode = PickingMode.Ignore };
            ProviderStyleSheets.Add(ghost, ProviderStyleSheets.kVirtualFunctionNodeDragger);
            ghost.AddToClassList("sg-virtual-function-node-ghost");
            return ghost;
        }
    }
}
