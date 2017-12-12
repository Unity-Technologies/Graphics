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
    class VFXContextSlotContainerUI : VFXNodeUI, IEdgeDrawerContainer
    {
        public VFXContextSlotContainerUI()
        {
            forceNotififcationOnAdd = true;
            pickingMode = PickingMode.Ignore;
            capabilities &= ~Capabilities.Selectable;


            AddToClassList("VFXContextSlotContainerUI");
        }

        public override VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            VFXContextDataAnchorController anchorController = controller as VFXContextDataAnchorController;

            VFXEditableDataAnchor anchor = VFXBlockDataAnchor.Create(anchorController, node);

            anchorController.sourceNode.viewController.onRecompileEvent += anchor.OnRecompile;

            return anchor;
        }

        protected override void OnPortRemoved(Port anchor)
        {
            if (anchor is VFXEditableDataAnchor)
            {
                var viewController = controller.viewController;
                viewController.onRecompileEvent += (anchor as VFXEditableDataAnchor).OnRecompile;
            }
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
        }

        protected override void SelfChange()
        {
            base.SelfChange();
        }

        protected override bool HasPosition()
        {
            return false;
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
