using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXParameterOutputDataAnchorController : VFXDataAnchorController
    {
        public override Direction direction
        { get { return Direction.Output; } }
        public override string name
        {
            get
            {
                if (model.IsMasterSlot())
                {
                    return model.property.type.UserFriendlyName();
                }
                return base.name;
            }
        }
    }

    class VFXSubParameterController : IPropertyRMProvider, IValueController
    {
        VFXParameterController m_Parameter;
        //int m_Field;
        FieldInfo m_FieldInfo;


        public  VFXSubParameterController(VFXParameterController parameter, int field)
        {
            m_Parameter = parameter;
            //m_Field = field;

            System.Type type = m_Parameter.portType;
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

        public Type portType
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
    class VFXParameterController : VFXSlotContainerController, IPropertyRMProvider, IValueController
    {
        VFXSubParameterController[] m_SubControllers;

        IDataWatchHandle m_SlotHandle;

        public override void Init(VFXModel model, VFXViewController viewController)
        {
            base.Init(model, viewController);

            m_CachedMinValue = parameter.m_Min != null ? parameter.m_Min.Get() : null;
            m_CachedMaxValue = parameter.m_Max != null ? parameter.m_Max.Get() : null;

            m_SlotHandle = DataWatchService.sharedInstance.AddWatch(parameter.outputSlots[0], OnSlotChanged);
        }

        void OnSlotChanged(UnityEngine.Object obj)
        {
            NotifyChange(AnyThing);
        }

        protected override VFXDataAnchorController AddDataAnchor(VFXSlot slot, bool input, bool hidden)
        {
            var anchor = VFXParameterOutputDataAnchorController.CreateInstance<VFXParameterOutputDataAnchorController>();
            anchor.Init(slot, this, hidden);
            anchor.portType = slot.property.type;
            return anchor;
        }

        public override string title
        {
            get { return parameter.outputSlots[0].property.type.UserFriendlyName(); }
        }

        public int CreateSubControllers()
        {
            if (m_SubControllers == null)
            {
                System.Type type = portType;

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                int count = fields.Length;

                bool spaceable = typeof(ISpaceable).IsAssignableFrom(type);
                if (spaceable)
                {
                    --count;
                }

                m_SubControllers = new VFXSubParameterController[count];

                int startIndex = spaceable ? 1 : 0;

                for (int i = startIndex; i < count + startIndex; ++i)
                {
                    m_SubControllers[i - startIndex] = new VFXSubParameterController(this, i);
                }
            }

            return m_SubControllers.Length;
        }

        public VFXSubParameterController GetSubController(int i)
        {
            return m_SubControllers[i];
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
                        parameter.m_Min = new VFXSerializableObject(portType, value);
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
                        parameter.m_Max = new VFXSerializableObject(portType, value);
                    else
                        parameter.m_Max.Set(value);
                }
                else
                    parameter.m_Max = null;
            }
        }

        // For the edition of Curve and Gradient to work the value must not be recreated each time. We now assume that changes happen only through the controller (or, in the case of serialization, before the controller is created)
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

        public Type portType
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

        public override void DrawGizmos(VFXComponent component)
        {
            VFXValueGizmo.Draw(this, component);

            if (m_SubControllers != null)
            {
                foreach (var controller in m_SubControllers)
                {
                    VFXValueGizmo.Draw(controller, component);
                }
            }
        }
    }
}
