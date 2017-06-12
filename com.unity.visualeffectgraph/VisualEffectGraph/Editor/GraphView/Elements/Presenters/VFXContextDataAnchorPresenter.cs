using System;
using System.Collections.Generic;
using UnityEngine;
using UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXContextDataAnchorPresenter : VFXDataAnchorPresenter
    {
        public override bool expandable
        {
            get { return VFXContextSlotContainerPresenter.IsTypeExpandable(anchorType); }
        }


        public void Init(VFXSlot model, VFXContextSlotContainerPresenter scPresenter)
        {
            base.Init(model, scPresenter);
        }
    }

    class VFXContextDataInputAnchorPresenter : VFXContextDataAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXContextDataOutputAnchorPresenter : VFXContextDataAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }
}
