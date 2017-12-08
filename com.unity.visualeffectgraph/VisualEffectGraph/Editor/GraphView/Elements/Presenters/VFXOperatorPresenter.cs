using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorPresenter : VFXSlotContainerPresenter
    {
        protected override VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot, bool input)
        {
            VFXOperatorAnchorPresenter anchor;
            if (input)
            {
                anchor = CreateInstance<VFXInputOperatorAnchorPresenter>();
            }
            else
            {
                anchor = CreateInstance<VFXOutputOperatorAnchorPresenter>();
            }
            anchor.Init(slot, this);

            anchor.portType = VFXOperatorAnchorPresenter.GetDisplayAnchorType(slot);

            if (slot.GetExpression() == null)
            {
                anchor.name = "Empty";
            }
            return anchor;
        }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);
        }

        public VFXOperator Operator
        {
            get
            {
                return model as VFXOperator;
            }
        }
    }
}
