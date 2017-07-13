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
                    Undo.RecordObject(parameter, "Exposed Name");
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
                    Undo.RecordObject(parameter, "Exposed");
                    parameter.exposed = value;
                }
            }
        }

        public int order
        {
            get { return parameter.order; }
            set
            {
                if (parameter.order!= value)
                {
                    Undo.RecordObject(parameter, "Parameter Order");
                    parameter.order = value;
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
        bool IPropertyRMProvider.editable
        {
            get { return true; }
        }



        public object minValue
        {
            get { return parameter.m_Min == null ? null:parameter.m_Min.Get(); }
            set
            {
                Undo.RecordObject(parameter, "Parameter Min");
                if (value != null)
                {
                    if (parameter.m_Min == null)
                        parameter.m_Min = new VFXSerializableObject(anchorType, value);
                    else
                        parameter.m_Min.Set(value);
                }
                else
                    parameter.m_Min = null;
            }
        }
        public object maxValue
        {
            get { return parameter.m_Max == null ? null:parameter.m_Max.Get(); }
            set
            {
                Undo.RecordObject(parameter, "Parameter Max");
                if (value != null)
                {
                    if (parameter.m_Max == null)
                        parameter.m_Max = new VFXSerializableObject(anchorType, value);
                    else
                        parameter.m_Max.Set(value);
                }
                else
                    parameter.m_Max = null;
            }
        }

        bool IPropertyRMProvider.expandable {get  { return false; } }

        public object value
        {
            get
            {
                return parameter.GetOutputSlot(0).value;
            }
            set
            {
                if(parameter.GetOutputSlot(0).value != value)
                {
                    Undo.RecordObject(parameter, "Change Value");
                    parameter.GetOutputSlot(0).value = value;
                }
                
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
