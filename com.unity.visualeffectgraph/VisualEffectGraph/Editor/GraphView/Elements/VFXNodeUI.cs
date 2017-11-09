using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXNodeUI : Node
    {
        public VFXNodeUI()
        {
            m_CollapseButton.visible = false;

            Insert(0, titleContainer);
            rightContainer.Insert(0, new VisualElement() { name = "rightBackground", pickingMode = PickingMode.Ignore });
            leftContainer.Insert(0, new VisualElement() { name = "leftBackground", pickingMode = PickingMode.Ignore });
            AddToClassList("VFXNodeUI");
        }

        public override Port InstantiatePort(PortPresenter presenter)
        {
            if (presenter.direction == Direction.Input)
            {
                VFXDataAnchorPresenter anchorPresenter = presenter as VFXDataAnchorPresenter;
                VFXEditableDataAnchor anchor = VFXEditableDataAnchor.Create(anchorPresenter);
                anchorPresenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

                return anchor;
            }
            else
            {
                return VFXOutputDataAnchor.Create(presenter as VFXDataAnchorPresenter);
            }
        }

        protected override void OnPortRemoved(Port anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                GetPresenter<VFXSlotContainerPresenter>().viewPresenter.onRecompileEvent -= (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }

        protected override void OnStyleResolved(ICustomStyle style)
        {
            base.OnStyleResolved(style);

            float labelWidth = 30;
            float controlWidth = 50;

            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                port.OnDataChanged();
                float portLabelWidth = port.GetPreferredLabelWidth();
                float portControlWidth = port.GetPreferredControlWidth();

                if (labelWidth < portLabelWidth)
                {
                    labelWidth = portLabelWidth;
                }
                if (controlWidth < portControlWidth)
                {
                    controlWidth = portControlWidth;
                }
            }

            foreach (var port in GetPorts(true, false).Cast<VFXEditableDataAnchor>())
            {
                port.SetLabelWidth(labelWidth);
            }

            inputContainer.style.width = labelWidth + controlWidth + 20;
        }
    }
}
