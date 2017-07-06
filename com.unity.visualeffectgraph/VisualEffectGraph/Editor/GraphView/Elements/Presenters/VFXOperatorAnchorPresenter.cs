using UIElements.GraphView;
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

        public override void UpdateInfos()
        {
            if (model.GetExpression() != null)
            {
                var newAnchorType = VFXExpression.TypeToType(model.GetExpression().ValueType);//model.property.type;
                if (newAnchorType != anchorType)
                {
                    this.sourceNode.viewPresenter.UnregisterDataAnchorPresenter(this);
                    anchorType = newAnchorType;
                    this.sourceNode.viewPresenter.RegisterDataAnchorPresenter(this);
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
