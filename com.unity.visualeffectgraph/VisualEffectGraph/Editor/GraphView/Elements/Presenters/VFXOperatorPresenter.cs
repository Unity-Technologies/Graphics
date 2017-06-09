using UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{
    class VFXOperatorSlotContainerPresenter : VFXSlotContainerPresenter
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

            var expression = slot.GetExpression();
            anchor.anchorType = expression == null ? typeof(float) : VFXExpression.TypeToType(expression.ValueType);
            if (expression == null)
            {
                anchor.name = "Empty";
            }
            return anchor;
        }
    }


    class VFXOperatorPresenter : VFXOperatorSlotContainerPresenter
    {
        [SerializeField]
        private object m_settings;
        public object settings
        {
            get
            {
                return Operator.settings;
            }
            set
            {
                Undo.RecordObject(Operator, "Settings");
                Operator.settings = value;
            }
        }

        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);
            title = Operator.name + " " + Operator.m_OnEnabledCount;
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
