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

    internal class VFXDataEdge : VFXEdge
    {
        public VFXDataEdge()
        {
        }

        public override void OnAnchorChanged(bool isInput)
        {
            base.OnAnchorChanged(isInput);

            //UpdateColor();
        }

        public virtual void UpdateColor()
        {
            if (selected)
            {
                edgeControl.inputColor = edgeControl.outputColor = selectedColor;
            }
            else
            {
                if (input != null)
                {
                    edgeControl.inputColor = (input as VFXDataAnchor).anchorColor;
                }
                else if (output != null)
                {
                    edgeControl.inputColor = (output as VFXDataAnchor).anchorColor;
                }
                if (output != null)
                {
                    edgeControl.outputColor = (output as VFXDataAnchor).anchorColor;
                }
                else if (input != null)
                {
                    edgeControl.outputColor = (input as VFXDataAnchor).anchorColor;
                }
            }
        }

        public override void OnSelected()
        {
            base.OnSelected();
            UpdateColor();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            UpdateColor();
        }

        /*
        protected override EdgeControl CreateEdgeControl()
        {
            return new VFXEdgeControl
            {
                capRadius = 4,
                interceptWidth = 3
            };
        }*/

        protected override void DrawEdge()
        {
            EdgePresenter edgePresenter = GetPresenter<EdgePresenter>();

            UpdateEdgeControl();
        }
    }
}
