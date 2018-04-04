using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace UnityEditor.VFX.UI
{

    class VFXOperatorController : VFXNodeController
    {
        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            VFXOperatorAnchorController anchor;
            if (input)
            {
                anchor = new VFXInputOperatorAnchorController(slot, this, hidden);
            }
            else
            {
                anchor = new VFXOutputOperatorAnchorController(slot, this, hidden);
            }

            anchor.portType = VFXOperatorAnchorController.GetDisplayAnchorType(slot);

            return anchor;
        }

        public VFXOperatorController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
        }

        public new VFXOperator model
        {
            get
            {
                return base.model as VFXOperator;
            }
        }
    }

    class VFXCascadedOperatorController : VFXOperatorController
    {
        public VFXCascadedOperatorController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
        }

        public new VFXOperatorNumericCascadedUnifiedNew model
        {
            get
            {
                return base.model as VFXOperatorNumericCascadedUnifiedNew;
            }
        }

        VFXUpcommingDataAnchorController m_UpcommingDataAnchor;
        protected override void NewInputSet()
        {
            if( m_UpcommingDataAnchor == null)
            {
                m_UpcommingDataAnchor = new VFXUpcommingDataAnchorController(this,false);
            }
            m_InputPorts.Add(m_UpcommingDataAnchor);
        }
    }
}
