using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorController : VFXSlotContainerController
    {
        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
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
            anchor.Init(slot, this, hidden);

            anchor.portType = VFXOperatorAnchorPresenter.GetDisplayAnchorType(slot);

            if (slot.GetExpression() == null)
            {
                //TRISTAN
                //anchor.name = "Empty";
            }
            return anchor;
        }

        public override void Init(VFXModel model, VFXViewController viewPresenter)
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
