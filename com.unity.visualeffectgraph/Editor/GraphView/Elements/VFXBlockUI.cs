using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Linq;
using UnityEngine.Profiling;

using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXBlockUI : VFXNodeUI
    {
        public new VFXBlockController controller
        {
            get { return base.controller as VFXBlockController; }
            set { base.controller = value; }
        }

        protected override bool HasPosition()
        {
            return false;
        }

        public VFXContextUI context
        {
            get { return this.GetFirstAncestorOfType<VFXContextUI>(); }
        }

        public VFXBlockUI()
        {
            Profiler.BeginSample("VFXBlockUI.VFXBlockUI");
            this.AddStyleSheetPath("VFXBlock");
            pickingMode = PickingMode.Position;

            capabilities &= ~Capabilities.Ascendable;
            capabilities |= Capabilities.Selectable | Capabilities.Droppable;
            this.AddManipulator(new SelectionDropper());

            Profiler.EndSample();
            style.position = PositionType.Relative;
        }

        // On purpose -- until we support Drag&Drop I suppose
        public override void SetPosition(Rect newPos)
        {
            style.position = PositionType.Relative;
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (controller.model.enabled)
                RemoveFromClassList("block-disabled");
            else
                AddToClassList("block-disabled");

            if (!controller.model.isValid)
                AddToClassList("invalid");
            else
                RemoveFromClassList("invalid");
        }

        public override bool superCollapsed
        {
            get { return false; }
        }
    }
}
