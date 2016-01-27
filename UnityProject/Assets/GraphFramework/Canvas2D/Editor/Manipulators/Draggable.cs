using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class Draggable : IManipulate
    {
        private EventModifiers m_ActivatorModifiers;
        private int m_ActivatorButton;

        public Draggable()
        {
            m_ActivatorButton = 0;
            m_ActivatorModifiers = EventModifiers.None;
        }

        public Draggable(int button, EventModifiers activator)
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

            if (e.button != m_ActivatorButton || m_ActivatorModifiers != e.modifiers)
            {
                return false;
            }

            canvas.StartCapture(this, element);

            e.Use();
            return true;
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

            if (canvas.selection.Count == 0)
            {
                canvas.AddToSelection(element);
            }

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

            float scaleFactorX = element == canvas ? 1.0f : 1.0f / canvas.scale.x;
            float scaleFactorY = element == canvas ? 1.0f : 1.0f / canvas.scale.y;

            Vector3 tx = element.translation;
            tx.x += e.delta.x * scaleFactorX;
            tx.y += e.delta.y * scaleFactorY;
            element.translation = tx;
            element.UpdateModel(UpdateType.Candidate);
            e.Use();

            return true;
        }
    };
}
