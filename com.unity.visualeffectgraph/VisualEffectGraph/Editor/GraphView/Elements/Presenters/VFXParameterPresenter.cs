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
            var anchor = new VFXParameterOutputDataAnchorPresenter();
            anchor.Init(slot, this);
            anchor.anchorType = slot.property.type;
            anchor.name = slot.property.type.UserFriendlyName();
            return anchor;
        }

    }
    class VFXParameterPresenter : VFXParameterSlotContainerPresenter, IPropertyRMProvider
    {
        [SerializeField]
        private string m_exposedName;
        [SerializeField]
        private bool m_exposed;


        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);
        }

        public string exposedName
        {
            get { return m_exposedName; }
            set
            {
                m_exposedName = value;
                if (parameter.exposedName != m_exposedName)
                {
                    Undo.RecordObject(parameter, "Exposed Name");
                    parameter.exposedName = m_exposedName;
                }
            }
        }
        public bool exposed
        {
            get { return m_exposed; }
            set
            {
                m_exposed = value;
                if (parameter.exposed != m_exposed)
                {
                    Undo.RecordObject(parameter, "Exposed");
                    parameter.exposed = m_exposed;
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

        public Type anchorType
        {
            get
            {
                VFXParameter model = this.model as VFXParameter;

                return model.GetOutputSlot(0).property.type;
            }
        }

        int IPropertyRMProvider.depth { get { return 0; }}
        /*
        protected override void Reset()
        {
            if (parameter != null)
            {
                title = node.outputSlots[0].property.type.UserFriendlyName() + " " + node.m_OnEnabledCount;
                exposed = parameter.exposed;
                exposedName = parameter.exposedName;
            }
            base.Reset();
        }
        */
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
