using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    internal class VFXEdge : Edge
    {
        public virtual void OnDisplayChanged()
        {
        }
    }

    internal class VFXDataEdge : VFXEdge, IControlledElement<VFXDataEdgeController>
    {
        VFXDataEdgeController m_Controller;
        Controller IControlledElement.controller
        {
            get { return m_Controller; }
        }
        public VFXDataEdgeController controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                {
                    m_Controller.UnregisterHandler(this);
                }
                m_Controller = value;
                if (m_Controller != null)
                {
                    m_Controller.RegisterHandler(this);
                }
            }
        }
        public VFXDataEdge()
        {
            RegisterCallback<ControllerChangedEvent>(OnChange);

            RegisterCallback<MouseEnterEvent>(OnMouseEnter);
            RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        }

        bool m_Hovered;

        void OnMouseEnter(MouseEnterEvent e)
        {
            m_Hovered = true;
            e.PreventDefault();
        }

        void OnMouseLeave(MouseLeaveEvent e)
        {
            m_Hovered = false;
            e.PreventDefault();
        }

        protected virtual void OnChange(ControllerChangedEvent e)
        {
            if (e.controller == controller)
            {
                SelfChange();
            }
        }

        bool m_Selected;

        protected override void DrawEdge()
        {
            if (!UpdateEdgeControl())
                return;

            if (m_Selected)
            {
                if (isGhostEdge)
                    Debug.Log("Selected Ghost Edge: this should never be");

                edgeControl.inputColor = selectedColor;
                edgeControl.outputColor = selectedColor;
                edgeControl.edgeWidth = 4;

                if (input != null)
                    input.capColor = selectedColor;

                if (output != null)
                    output.capColor = selectedColor;
            }
            else
            {
                if (input != null)
                    input.UpdateCapColor();

                if (output != null)
                    output.UpdateCapColor();

                edgeControl.inputColor = input == null ? output.portColor : input.portColor;
                edgeControl.outputColor = output == null ? input.portColor : output.portColor;
                edgeControl.edgeWidth = m_Hovered ? 4 : 2;

                edgeControl.toCapColor = input == null ? output.portColor : input.portColor;
                edgeControl.fromCapColor = output == null ? input.portColor : output.portColor;

                if (isGhostEdge)
                {
                    edgeControl.inputColor = new Color(edgeControl.inputColor.r, edgeControl.inputColor.g, edgeControl.inputColor.b, 0.5f);
                    edgeControl.outputColor = new Color(edgeControl.outputColor.r, edgeControl.outputColor.g, edgeControl.outputColor.b, 0.5f);
                }
            }
        }

        protected virtual void SelfChange()
        {
            if (controller != null)
            {
                VFXView view = GetFirstAncestorOfType<VFXView>();

                var newInput = view.GetDataAnchorByController(controller.input);

                if (base.input != newInput)
                {
                    if (base.input != null)
                    {
                        base.input.Disconnect(this);
                    }
                    base.input = newInput;
                    base.input.Connect(this);
                }

                var newOutput = view.GetDataAnchorByController(controller.output);

                if (base.output != newOutput)
                {
                    if (base.output != null)
                    {
                        base.output.Disconnect(this);
                    }
                    base.output = newOutput;
                    base.output.Connect(this);
                }

                UpdateEdgeControl();
            }
        }

        public new VFXDataAnchor input
        {
            get { return base.input as VFXDataAnchor; }
        }
        public new VFXDataAnchor output
        {
            get { return base.output as VFXDataAnchor; }
        }

        public override void OnPortChanged(bool isInput)
        {
            base.OnPortChanged(isInput);
        }

        public override void OnSelected()
        {
            base.OnSelected();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
        }
    }
}
