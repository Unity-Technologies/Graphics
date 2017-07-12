using System;
using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXParameterOutputDataAnchorPresenter : VFXDataAnchorPresenter
    {
        public override Direction direction
        { get { return Direction.Output; } }
    }
    class VFXParameterSlotContainerPresenter : VFXSlotContainerPresenter
    {
        protected override VFXDataAnchorPresenter AddDataAnchor(VFXSlot slot, bool input)
        {
            var anchor = VFXParameterOutputDataAnchorPresenter.CreateInstance<VFXParameterOutputDataAnchorPresenter>();
            anchor.Init(slot, this);
            anchor.anchorType = slot.property.type;
            anchor.name = slot.property.type.UserFriendlyName();
            return anchor;
        }
    }
    class VFXParameterPresenter : VFXParameterSlotContainerPresenter, IPropertyRMProvider
    {
        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);
            title = parameter.outputSlots[0].property.type.UserFriendlyName() + " " + model.m_OnEnabledCount;

            exposed = parameter.exposed;
            exposedName = parameter.exposedName;
        }

        public string exposedName
        {
            get { return parameter.exposedName; }
            set
            {
                if (parameter.exposedName != value)
                {
                    parameter.exposedName = value;
                }
            }
        }
        public bool exposed
        {
            get { return parameter.exposed; }
            set
            {
                if (parameter.exposed != value)
                {
                    parameter.exposed = value;
                }
            }
        }

        private VFXParameter parameter { get { return model as VFXParameter; } }

        bool IPropertyRMProvider.expanded
        {
            get
            {
                return false;
            }
        }

        bool IPropertyRMProvider.expandable {get  { return false; } }

        object IPropertyRMProvider.value
        {
            get
            {
                return parameter.GetOutputSlot(0).value;
            }
            set
            {
                Undo.RecordObject(parameter, "Change Value");
                parameter.GetOutputSlot(0).value = value;
            }
        }

        string IPropertyRMProvider.name { get { return "Value"; } }
        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return null; } }

        public Type anchorType
        {
            get
            {
                VFXParameter model = this.model as VFXParameter;

                return model.GetOutputSlot(0).property.type;
            }
        }

        int IPropertyRMProvider.depth { get { return 0; }}

        void IPropertyRMProvider.ExpandPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.RetractPath()
        {
            throw new NotImplementedException();
        }

        public override UnityEngine.Object[] GetObjectsToWatch()
        {
            return new UnityEngine.Object[] { this, model, parameter.outputSlots[0] };
        }
    }
}
