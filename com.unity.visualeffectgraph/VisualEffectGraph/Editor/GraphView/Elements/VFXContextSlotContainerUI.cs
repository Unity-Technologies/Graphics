using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class VFXContextSlotContainerUI : VFXNodeUI
    {
        public VFXContextSlotContainerUI()
        {
            pickingMode = PickingMode.Ignore;
            capabilities &= ~Capabilities.Selectable;


            AddToClassList("VFXContextSlotContainerUI");
        }

        public override VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            VFXContextDataAnchorController anchorController = controller as VFXContextDataAnchorController;

            VFXEditableDataAnchor anchor = VFXBlockDataAnchor.Create(anchorController, node);
            return anchor;
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
        }

        protected override bool HasPosition()
        {
            return false;
        }

        public VFXContextUI context
        {
            get {return this.GetFirstAncestorOfType<VFXContextUI>(); }
        }
    }


    class VFXOwnContextSlotContainerUI : VFXContextSlotContainerUI
    {
        public VFXOwnContextSlotContainerUI()
        {
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            visible = inputContainer.childCount > 0 || (settingsContainer != null && settingsContainer.childCount > 0);
        }

        public override bool hasSettingDivider
        {
            get { return false; }
        }
    }
}
