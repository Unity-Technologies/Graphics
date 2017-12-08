using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    abstract class VFXOperatorAnchorPresenter : VFXDataAnchorPresenter
    {
        public new void Init(VFXSlot model, VFXSlotContainerPresenter scPresenter)
        {
            base.Init(model, scPresenter);
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
            if (model.GetExpression() != null)
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


    class VFXInputOperatorAnchorPresenter : VFXOperatorAnchorPresenter
    {
        public override Direction direction
        {
            get
            {
                return Direction.Input;
            }
        }
    }

    class VFXOutputOperatorAnchorPresenter : VFXOperatorAnchorPresenter
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
