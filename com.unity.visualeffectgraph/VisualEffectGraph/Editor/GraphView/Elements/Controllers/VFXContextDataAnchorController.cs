using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;

namespace UnityEditor.VFX.UI
{
    abstract class VFXContextDataAnchorController : VFXDataAnchorController
    {
        public VFXContextDataAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override bool expandable
        {
            get { return VFXContextSlotContainerController.IsTypeExpandable(portType); }
        }
    }

    class VFXContextDataInputAnchorController : VFXContextDataAnchorController
    {
        public VFXContextDataInputAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXContextDataOutputAnchorController : VFXContextDataAnchorController
    {
        public VFXContextDataOutputAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public override Direction direction
        {
            get
            {
                return Direction.Output;
            }
        }
    }
}
