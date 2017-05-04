using System;
using UIElements.GraphView;
using UnityEngine;

namespace UnityEditor.VFX.UI
{
    class VFXParameterPresenter : VFXNodePresenter, IVFXPresenter,IPropertyRMProvider
    {
        [SerializeField]
        private string m_exposedName;
        [SerializeField]
        private bool m_exposed;

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

        private VFXParameter parameter { get { return node as VFXParameter; } }

        bool IPropertyRMProvider.expanded
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool IPropertyRMProvider.expandable {get  { return false; } }

        object IPropertyRMProvider.value {
            get {
                VFXParameter model = this.model as VFXParameter;

                return model.GetOutputSlot(0).value;

            }
            set {
                VFXParameter model = this.model as VFXParameter;

                model.GetOutputSlot(0).value = value;
            }
        }

        string IPropertyRMProvider.name { get{ return "Value"; } }

        Type IPropertyRMProvider.anchorType {get {

                VFXParameter model = this.model as VFXParameter;

                return model.GetOutputSlot(0).property.type;
            }
        }

        int IPropertyRMProvider.depth { get { return 0; }}

        protected override NodeAnchorPresenter CreateAnchorPresenter(VFXSlot slot, Direction direction)
        {
            var anchor = base.CreateAnchorPresenter(slot, direction);
            anchor.anchorType = slot.property.type;
            anchor.name = slot.property.type.Name;
            return anchor;
        }

        protected override void Reset()
        {
            if (parameter != null)
            {
                title = node.outputSlots[0].property.type.Name + " " + node.m_OnEnabledCount;
                exposed = parameter.exposed;
                exposedName = parameter.exposedName;
            }
            base.Reset();
        }

        void IPropertyRMProvider.ExpandPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.RetractPath()
        {
            throw new NotImplementedException();
        }
    }
}
