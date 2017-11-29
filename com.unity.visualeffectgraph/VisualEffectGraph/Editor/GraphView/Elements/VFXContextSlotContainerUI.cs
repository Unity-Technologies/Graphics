using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXContextSlotContainerUI : VFXSlotContainerUI, IEdgeDrawerContainer
    {
        public VFXContextSlotContainerUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Ignore;


            AddToClassList("VFXContextSlotContainerUI");
        }

        public override Port InstantiatePort(PortPresenter presenter)
        {
            VFXContextDataAnchorPresenter anchorPresenter = presenter as VFXContextDataAnchorPresenter;

            VFXEditableDataAnchor anchor = VFXBlockDataAnchor.Create(anchorPresenter);

            anchorPresenter.sourceNode.viewPresenter.onRecompileEvent += anchor.OnRecompile;

            return anchor;
        }

        protected override void OnPortRemoved(Port anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                var viewPresenter = GetPresenter<VFXSlotContainerPresenter>().viewPresenter;
                viewPresenter.onRecompileEvent += (anchor as VFXEditableDataAnchor).OnRecompile;
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

        public void EdgeDirty()
        {
            (context as IEdgeDrawerContainer).EdgeDirty();
        }
    }
}
