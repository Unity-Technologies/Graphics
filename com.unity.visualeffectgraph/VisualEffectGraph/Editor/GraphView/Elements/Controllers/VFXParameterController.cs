using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor.Experimental.UIElements.GraphView;

using UnityEngine;
using UnityEngine.Experimental.VFX;
using UnityEngine.Experimental.UIElements;


namespace UnityEditor.VFX.UI
{
    class VFXParameterOutputDataAnchorController : VFXDataAnchorController
    {
        public VFXParameterOutputDataAnchorController(VFXSlot model, VFXSlotContainerController sourceNode, bool hidden) : base(model, sourceNode, hidden)
        {
        }

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
        int[] m_FieldPath;
        FieldInfo[] m_FieldInfos;

        VFXSubParameterController[] m_Children;


        public VFXSubParameterController(VFXParameterController parameter, IEnumerable<int> fieldPath)
        {
            m_Parameter = parameter;
            //m_Field = field;

            System.Type type = m_Parameter.portType;
            m_FieldPath = fieldPath.ToArray();

            m_FieldInfos = new FieldInfo[m_FieldPath.Length];

            for (int i = 0; i < m_FieldPath.Length; ++i)
            {
                FieldInfo info = type.GetFields(BindingFlags.Public | BindingFlags.Instance)[m_FieldPath[i]];
                m_FieldInfos[i] = info;
                type = info.FieldType;
            }
        }

        public VFXSubParameterController[] children
        {
            get
            {
                if (m_Children == null)
                {
                    m_Children = m_Parameter.ComputeSubControllers(portType, m_FieldPath);
                }
                return m_Children;
            }
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
            get { return m_FieldInfos[m_FieldInfos.Length - 1].Name; }
        }

        object[] IPropertyRMProvider.customAttributes { get { return new object[] {}; } }

        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return new VFXPropertyAttribute[] {}; } }

        int IPropertyRMProvider.depth { get { return m_FieldPath.Length; } }

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
                return m_FieldInfos[m_FieldInfos.Length - 1].FieldType;
            }
        }

        public object value
        {
            get
            {
                object value = m_Parameter.value;

                foreach (var fieldInfo in m_FieldInfos)
                {
                    value = fieldInfo.GetValue(value);
                }

                return value;
            }
            set
            {
                object val = m_Parameter.value;

                List<object> objectStack = new List<object>();
                foreach (var fieldInfo in m_FieldInfos.Take(m_FieldInfos.Length - 1))
                {
                    objectStack.Add(fieldInfo.GetValue(val));
                }


                object targetValue = value;
                for (int i = objectStack.Count - 1; i >= 0; --i)
                {
                    m_FieldInfos[i + 1].SetValue(objectStack[i], targetValue);
                    targetValue = objectStack[i];
                }

                m_FieldInfos[0].SetValue(val, targetValue);

                m_Parameter.value = val;
            }
        }
    }
    class VFXParameterController : VFXSlotContainerController, IPropertyRMProvider, IValueController
    {
        VFXSubParameterController[] m_SubControllers;

        IDataWatchHandle m_SlotHandle;

        public VFXParameterController(VFXModel model, VFXViewController viewController) : base(model, viewController)
        {
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
            var anchor = new VFXParameterOutputDataAnchorController(slot, this, hidden);
            anchor.portType = slot.property.type;
            return anchor;
        }

        public override string title
        {
            get { return parameter.outputSlots[0].property.type.UserFriendlyName(); }
        }

        public VFXSubParameterController[] ComputeSubControllers(Type type, IEnumerable<int> fieldPath)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            int count = fields.Length;

            bool spaceable = typeof(ISpaceable).IsAssignableFrom(type) && fields[0].FieldType == typeof(CoordinateSpace);
            if (spaceable)
            {
                --count;
            }

            var subControllers = new VFXSubParameterController[count];

            int startIndex = spaceable ? 1 : 0;

            for (int i = startIndex; i < count + startIndex; ++i)
            {
                subControllers[i - startIndex] = new VFXSubParameterController(this, fieldPath.Concat(Enumerable.Repeat(i, 1)));
            }

            return subControllers;
        }

        VFXSubParameterController[] m_SubController;

        public VFXSubParameterController[] GetSubControllers(List<int> fieldPath)
        {
            if (m_SubControllers == null)
            {
                m_SubControllers = ComputeSubControllers(portType, fieldPath);
            }
            VFXSubParameterController[] currentArray = m_SubControllers;

            foreach (int value in fieldPath)
            {
                currentArray = currentArray[value].children;
            }

            return currentArray;
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
