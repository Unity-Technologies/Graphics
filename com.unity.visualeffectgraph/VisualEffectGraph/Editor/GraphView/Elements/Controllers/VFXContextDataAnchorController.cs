using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXContextDataAnchorController : VFXDataAnchorController
    {
        public override bool expandable
        {
            get { return VFXContextSlotContainerController.IsTypeExpandable(portType); }
        }
    }

    class VFXContextDataInputAnchorPresenter : VFXContextDataAnchorController
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXContextDataOutputAnchorPresenter : VFXContextDataAnchorController
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
