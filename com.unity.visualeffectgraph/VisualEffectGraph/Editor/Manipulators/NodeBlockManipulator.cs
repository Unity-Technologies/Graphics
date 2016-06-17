using UnityEngine;
using System;

namespace UnityEditor.Experimental
{
    internal class NodeBlockManipulator : IManipulate
    {
        private EventModifiers m_ActivatorModifiers;
        private int m_ActivatorButton;

        private bool bActuallyDrag = false;
        private float m_SnapDistance = 32.0f;
        private Vector2 m_ClickPosition;
        private VFXEdNodeBlockContainer m_targetDropContainer = null;
        private VFXEdNodeBlockContainer m_initialDropContainer = null;
        private int m_initialDropContainerIndex = 0;
        private VFXEdNodeBlock m_Block;


        public NodeBlockManipulator(VFXEdNodeBlock block)
        {
            m_Block = block;
            m_ActivatorButton = 0;
            m_ActivatorModifiers = EventModifiers.None;
        }

        public NodeBlockManipulator(int button, EventModifiers activator)
        {
            m_ActivatorButton = button;
            m_ActivatorModifiers = activator;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            if (cap == ManipulatorCapability.MultiSelection)
                return true;
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.MouseDrag += MouseDrag;
            element.MouseUp += EndDrag;
            element.MouseDown += StartDrag;
        }


        private bool StartDrag(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
                return false;

            if (element.parent.selected)
                return false;

            if (e.button != m_ActivatorButton || m_ActivatorModifiers != e.modifiers)
            {
                return false;
            }

            if ((canvas as VFXEdCanvas).SelectedNodeBlock != (element as VFXEdNodeBlockDraggable))
            {
                (canvas as VFXEdCanvas).SelectedNodeBlock = (element as VFXEdNodeBlockDraggable);
                (element as VFXEdNodeBlock).Invalidate();
            }

            bActuallyDrag = false;

            if (element is VFXEdNodeBlockDraggable)
            {
                m_ClickPosition = canvas.MouseToCanvas(e.mousePosition);
                canvas.StartCapture(this, element);
                e.Use();
                return true;
            }
            else
                return false;

        }

        private bool EndDrag(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
                return false;

            if (!canvas.IsCaptured(this))
            {
                return false;
            }
            canvas.EndCapture();

            if (!bActuallyDrag)
            {
                return false;
            }

            element.selected = false;

            if (m_targetDropContainer != null)
            {

                if (m_targetDropContainer != m_initialDropContainer)
                {
                    m_targetDropContainer.AcceptDrop(element as VFXEdNodeBlockDraggable);
                }
                else
                {
                    m_initialDropContainer.AcceptDrop(element as VFXEdNodeBlockDraggable);
                }
            }
            else
            {
                m_initialDropContainer.RevertDrop(element as VFXEdNodeBlockDraggable, m_initialDropContainerIndex);
            }

            // Revert forbidden overlays
            foreach (CanvasElement ce in canvas.elements)
            {
                foreach(VFXEdNodeBlockContainer container in ce.FindChildren<VFXEdNodeBlockContainer>()) {
                    canvas.OnOverlay -= container.RenderOverlayForbiddenDrop;
                }
            }

            canvas.Layout();
            canvas.Repaint();

            m_targetDropContainer = null;
            m_initialDropContainer = null;

            element.UpdateModel(UpdateType.Update);
            e.Use();
            return true;
        }

        private bool MouseDrag(CanvasElement element, Event e, Canvas2D canvas)
        {
            if (e.type == EventType.Used)
                return false;

            if (!canvas.IsCaptured(this))
            {
                return false;
            }

            if (!(element is VFXEdNodeBlock))
            {
                return false;
            }


            // Drag Snapping Check
            if (bActuallyDrag)
            {
                float scaleFactorX = element == canvas ? 1.0f : 1.0f / canvas.scale.x;
                float scaleFactorY = element == canvas ? 1.0f : 1.0f / canvas.scale.y;

                Vector3 tx = element.translation;
                tx.x += e.delta.x * scaleFactorX;
                tx.y += e.delta.y * scaleFactorY;
                element.translation = tx;
                element.UpdateModel(UpdateType.Candidate);

                // Query for dropping on NodeBlock Containers
                VFXEdNodeBlockContainer container = null;
                Vector2 dropPosition = canvas.MouseToCanvas(e.mousePosition);
                foreach (CanvasElement ce in canvas.elements)
                {
                    if (container == null)
                        container = FindContainer(ce, dropPosition);
                }

                if (container != null)
                {
                    m_targetDropContainer = container;
                    canvas.Repaint();
                }
                else
                {
                    m_targetDropContainer = null;
                }

                e.Use();

                return true;
            }
            else
            {
                if (Vector2.Distance(canvas.MouseToCanvas(e.mousePosition), m_ClickPosition) > m_SnapDistance)
                {
                    VFXEdNodeBlockDraggable n = element as VFXEdNodeBlockDraggable;
                    CanvasElement parent = n.parent;
                    VFXEdNodeBlockContainer container = parent as VFXEdNodeBlockContainer;
                    m_initialDropContainerIndex = container.GetBlockIndex(n);
                    container.DetachNodeBlock(n);
                    n.SetParent(canvas);
                    n.translation = canvas.MouseToCanvas(e.mousePosition) - new Vector2(n.scale.x / 2, n.scale.y / 2);
                    parent.Invalidate();
                    canvas.Repaint();
                    m_initialDropContainer = parent as VFXEdNodeBlockContainer;
                    n.zIndex = 999;
                    bActuallyDrag = true;
                    n.selected = true;
                }
                return true;
            }
        }


        private VFXEdNodeBlockContainer FindContainer(CanvasElement element, Vector2 canvasHitPosition)
        {

            foreach (CanvasElement e in element.Children())
            {
                if (e is VFXEdNodeBlockContainer)
                {
                    VFXEdNodeBlockContainer container = (e as VFXEdNodeBlockContainer);
                    container.ParentCanvas().OnOverlay -= container.RenderOverlayForbiddenDrop;

                    bool acceptable = container.FindParent<VFXEdNode>().AcceptNodeBlock(m_Block as VFXEdNodeBlockDraggable);
                    if(!acceptable) container.ParentCanvas().OnOverlay += container.RenderOverlayForbiddenDrop;

                    if (container.Contains(canvasHitPosition) && acceptable)
                    {
                        container.CaptureDrop = true;
                        container.UpdateCaptureDrop(canvasHitPosition);
                        container.Layout();
                        container.Invalidate();
                        return e as VFXEdNodeBlockContainer;
                    }
                    else
                    {
                        container.CaptureDrop = false;
                        container.Invalidate();
                    }
                }
                else
                {
                    VFXEdNodeBlockContainer child = FindContainer(e, canvasHitPosition);
                    if (child != null)
                        return child;
                }
            }
            return null;

        }
    };
}
