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

        public virtual bool isEditable
        {
            get{return false;}
        }
    }

    class VFXUnifiedOperatorControllerBase<T> : VFXOperatorController where T : VFXOperatorNumericNew,IVFXOperatorNumericUnifiedNew
    {
        public VFXUnifiedOperatorControllerBase(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
        }

        public new T model
        {
            get
            {
                return base.model as T;
            }
        }
        public override void WillCreateLink(ref VFXSlot myInput,ref VFXSlot otherOutput)
        {

            if( model.validTypes.Contains(otherOutput.property.type))
            {
                int index = model.GetSlotIndex(myInput);
                model.SetOperandType(index,otherOutput.property.type);

                myInput = model.GetInputSlot(index);
            }
        }

        protected override bool CouldLinkMyInputTo(VFXDataAnchorController myInput,VFXDataAnchorController otherOutput)
        {
            return model.validTypes.Contains(otherOutput.portType);
        }

        public override bool isEditable
        {
            get{return true;}
        }
    }
    class VFXUnifiedOperatorController : VFXUnifiedOperatorControllerBase<VFXOperatorNumericUnifiedNew>
    {
        public VFXUnifiedOperatorController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
        }
    }

    class VFXCascadedOperatorController : VFXUnifiedOperatorControllerBase<VFXOperatorNumericCascadedUnifiedNew>
    {
        public VFXCascadedOperatorController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
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

        public override bool isEditable
        {
            get{return true;}
        }
    }

    class VFXUniformOperatorController : VFXOperatorController
    {
        public VFXUniformOperatorController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
        }
        public new VFXOperatorNumericUniformNew model
        {
            get
            {
                return base.model as VFXOperatorNumericUniformNew;
            }
        }

        protected override bool CouldLinkMyInputTo(VFXDataAnchorController myInput,VFXDataAnchorController otherOutput)
        {
            return model.validTypes.Contains(otherOutput.portType);
        }
        
        public override void WillCreateLink(ref VFXSlot myInput,ref VFXSlot otherOutput)
        {
            //Since every input will change at the same time the metric to change is :
            // if we have no input links yet

            var myInputCopy = myInput;
            bool hasLink = inputPorts.Any(t=>t.model != myInputCopy && t.model.HasLink());
            // The new link is impossible if we don't change (case of a vector3 trying to be linked to a vector4)
            bool linkImpossibleNow = ! myInput.CanLink(otherOutput) || !otherOutput.CanLink(myInput);

            if( (! hasLink || linkImpossibleNow) && model.validTypes.Contains(otherOutput.property.type))
            {
                int index = model.GetSlotIndex(myInput);
                model.SetOperandType(otherOutput.property.type);

                myInput = model.GetInputSlot(index);
            }
        }
        public override bool isEditable
        {
            get{return true;}
        }
    }
}
