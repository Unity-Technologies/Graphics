using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using System.Reflection;
using System.Linq;

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
            if (slot.IsMasterSlot())
                anchor.name = slot.property.type.UserFriendlyName();
            return anchor;
        }
    }

    class VFXSubParameterPresenter : IPropertyRMProvider, IValuePresenter
    {
        VFXParameterPresenter m_Parameter;
        //int m_Field;
        FieldInfo m_FieldInfo;


        public  VFXSubParameterPresenter(VFXParameterPresenter parameter, int field)
        {
            m_Parameter = parameter;
            //m_Field = field;

            System.Type type = m_Parameter.anchorType;
            m_FieldInfo = type.GetFields()[field];
        }

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

        bool IPropertyRMProvider.expandable { get { return false; } }

        string IPropertyRMProvider.name
        {
            get { return m_FieldInfo.Name; }
        }

        object[] IPropertyRMProvider.customAttributes { get { return new object[] {}; } }

        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return new VFXPropertyAttribute[] {}; } }

        int IPropertyRMProvider.depth { get { return 1; } }

        void IPropertyRMProvider.ExpandPath()
        {
            throw new NotImplementedException();
        }

        void IPropertyRMProvider.RetractPath()
        {
            throw new NotImplementedException();
        }

        public Type anchorType
        {
            get
            {
                return m_FieldInfo.FieldType;
            }
        }

        public object value
        {
            get
            {
                return m_FieldInfo.GetValue(m_Parameter.value);
            }
            set
            {
                object val = m_Parameter.value;
                m_FieldInfo.SetValue(val, value);

                m_Parameter.value = val;
            }
        }
    }
    class VFXParameterPresenter : VFXParameterSlotContainerPresenter, IPropertyRMProvider, IValuePresenter
    {
        VFXSubParameterPresenter[] m_SubPresenters;
        public override void Init(VFXModel model, VFXViewPresenter viewPresenter)
        {
            base.Init(model, viewPresenter);

            m_CachedMinValue = parameter.m_Min != null ? parameter.m_Min.Get() : null;
            m_CachedMaxValue = parameter.m_Max != null ? parameter.m_Max.Get() : null;
        }

        public override void UpdateTitle()
        {
            title = parameter.outputSlots[0].property.type.UserFriendlyName();
        }

        public int CreateSubPresenters()
        {
            if (m_SubPresenters == null)
            {
                System.Type type = anchorType;

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                int count = fields.Length;

                bool spaceable = typeof(ISpaceable).IsAssignableFrom(type);
                if (spaceable)
                {
                    --count;
                }

                m_SubPresenters = new VFXSubParameterPresenter[count];

                int startIndex = spaceable ? 1 : 0;

                for (int i = startIndex; i < count + startIndex; ++i)
                {
                    m_SubPresenters[i - startIndex] = new VFXSubParameterPresenter(this, i);
                }
            }

            return m_SubPresenters.Length;
        }

        public VFXSubParameterPresenter GetSubPresenter(int i)
        {
            return m_SubPresenters[i];
        }

        public string exposedName
        {
            get { return parameter.exposedName; }
        }
        public bool exposed
        {
            get { return parameter.exposed; }
        }

        public int order
        {
            get { return parameter.order; }
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
            get { return m_CachedMinValue; }
            set
            {
                m_CachedMinValue = value;
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
            get { return m_CachedMaxValue; }
            set
            {
                m_CachedMaxValue = value;
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

        // For the edition of Curve and Gradient to work the value must not be recreated each time. We now assume that changes happen only through the presenter (or, in the case of serialization, before the presenter is created)
        object m_CachedMinValue;
        object m_CachedMaxValue;

        bool IPropertyRMProvider.expandable {get  { return false; } }


        public object value
        {
            get
            {
                return parameter.GetOutputSlot(0).value;
            }
            set
            {
                Undo.RecordObject(parameter, "Change Value");

                VFXSlot slot = parameter.GetOutputSlot(0);

                slot.value = value;
            }
        }

        string IPropertyRMProvider.name { get { return "Value"; } }

        object[] IPropertyRMProvider.customAttributes { get { return new object[] {}; } }

        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return new VFXPropertyAttribute[] {}; }}

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

        public override void DrawGizmos(VFXComponent component)
        {
            VFXValueGizmo.Draw(this, component);

            if (m_SubPresenters != null)
            {
                foreach (var presenter in m_SubPresenters)
                {
                    VFXValueGizmo.Draw(presenter, component);
                }
            }
        }
    }
}
