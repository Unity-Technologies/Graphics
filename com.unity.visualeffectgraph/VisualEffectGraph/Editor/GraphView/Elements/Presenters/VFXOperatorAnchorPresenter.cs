using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    abstract class VFXOperatorAnchorPresenter : VFXDataAnchorPresenter
    {
        public void Init(VFXSlot model, VFXSlotContainerPresenter scPresenter)
        {
            base.Init(model, scPresenter);
        }

        public override void UpdateInfos(bool expanded)
        {
            anchorType = VFXExpression.TypeToType(model.GetExpression().ValueType);//model.property.type;
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
