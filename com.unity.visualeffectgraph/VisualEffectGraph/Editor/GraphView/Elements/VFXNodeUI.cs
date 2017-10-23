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

        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
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

        protected override void OnAnchorRemoved(NodeAnchor anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                GetPresenter<VFXSlotContainerPresenter>().viewPresenter.onRecompileEvent -= (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }
    }
}
