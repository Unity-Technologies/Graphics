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

            if (input && slot.property.type == typeof(FloatN) && !slot.HasLink() && ((FloatN)slot.value).realSize == 0)
            {
                anchor.name = "Empty";
            }
            return anchor;
        }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);
            title = Operator.name;
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
