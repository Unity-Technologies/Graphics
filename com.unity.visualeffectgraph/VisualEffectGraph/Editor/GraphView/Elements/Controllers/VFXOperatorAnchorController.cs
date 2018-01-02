using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    abstract class VFXOperatorAnchorController : VFXDataAnchorController
    {
        public VFXOperatorAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

        public static System.Type GetDisplayAnchorType(VFXSlot slot)
        {
            System.Type newAnchorType = slot.property.type;

            if (newAnchorType != typeof(Color) && slot.GetExpression() != null)
                newAnchorType = VFXExpression.TypeToType(slot.GetExpression().valueType);//model.property.type;

            return newAnchorType;
        }

        public override void UpdateInfos()
        {
            if (model.direction == VFXSlot.Direction.kInput && model.GetExpression() != null)
            {
                System.Type newAnchorType = GetDisplayAnchorType(model);

                if (newAnchorType != portType)
                {
                    portType = newAnchorType;
                }
            }
            else
                base.UpdateInfos();
        }
    }


    class VFXInputOperatorAnchorController : VFXOperatorAnchorController
    {
        public VFXInputOperatorAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
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

    class VFXOutputOperatorAnchorController : VFXOperatorAnchorController
    {
        public VFXOutputOperatorAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
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
