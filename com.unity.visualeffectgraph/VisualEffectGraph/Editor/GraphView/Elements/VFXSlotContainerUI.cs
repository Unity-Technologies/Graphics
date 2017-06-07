using System.Collections.Generic;
using UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXContextSlotContainerUI : VFXSlotContainerUI
    {
        public VFXContextSlotContainerUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Position;

            leftContainer.alignContent = Align.Stretch;

            AddToClassList("VFXContextSlotContainerUI");
        }

        public override NodeAnchor InstantiateNodeAnchor(NodeAnchorPresenter presenter)
        {
            VFXContextDataAnchorPresenter anchorPresenter = presenter as VFXContextDataAnchorPresenter;

            VFXEditableDataAnchor anchor = VFXBlockDataAnchor.Create<VFXDataEdgePresenter>(anchorPresenter);

            anchorPresenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

            return anchor;
        }

        protected override void OnAnchorRemoved(NodeAnchor anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                GetPresenter<VFXParameterPresenter>().viewPresenter.onRecompileEvent += (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
        }



        public override void OnDataChanged()
        {
            base.OnDataChanged();
            var presenter = GetPresenter<VFXContextSlotContainerPresenter>();

            if (presenter == null)
                return;

            SetPosition(presenter.position);
        }

        public VFXContextUI context
        {
            get {return this.GetFirstAncestorOfType<VFXContextUI>(); }
        }
    }
}
