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
    class VFXSubParameterController : IPropertyRMProvider, IValueController
    {
        VFXParametersController m_Parameter;
        //int m_Field;
        int[] m_FieldPath;
        FieldInfo[] m_FieldInfos;

        VFXSubParameterController[] m_Children;


        public VFXSubParameterController(VFXParametersController parameter, IEnumerable<int> fieldPath)
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
    class VFXParametersController : VFXController<VFXParameter>
    {
        VFXSubParameterController[] m_SubControllers;

        VFXViewController m_ViewController;

        public VFXParametersController(VFXParameter model, VFXViewController viewController) : base(model)
        {
            m_ViewController = viewController;
            m_CachedMinValue = parameter.m_Min != null ? parameter.m_Min.Get() : null;
            m_CachedMaxValue = parameter.m_Max != null ? parameter.m_Max.Get() : null;
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

        public VFXParameterController GetParameterForLink(VFXSlot slot)
        {
            return m_Controllers.FirstOrDefault(t => t.Value.infos.linkedSlots != null && t.Value.infos.linkedSlots.Contains(slot)).Value;
        }

        public string exposedName
        {
            get { return parameter.exposedName; }

            set
            {
                parameter.SetSettingValue("m_exposedName", value);
            }
        }
        public bool exposed
        {
            get {return parameter.exposed; }
            set
            {
                parameter.SetSettingValue("m_exposed", value);
            }
        }

        public int order
        {
            get { return parameter.order; }

            set
            {
                parameter.SetSettingValue("m_order", value);
            }
        }

        public VFXParameter parameter { get { return model as VFXParameter; } }

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

        public Type portType
        {
            get
            {
                VFXParameter model = this.model as VFXParameter;

                return model.GetOutputSlot(0).property.type;
            }
        }
        public void DrawGizmos(VFXComponent component)
        {
            if (m_SubControllers != null)
            {
                foreach (var controller in m_SubControllers)
                {
                    VFXValueGizmo.Draw(controller, component);
                }
            }
        }

        Dictionary<int, VFXParameterController> m_Controllers = new Dictionary<int, VFXParameterController>();

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            model.ValidateParamInfos();
            bool controllerListChanged = UpdateControllers();
            if (controllerListChanged)
                m_ViewController.NotifyParameterControllerChange();
            NotifyChange(AnyThing);
        }

        public bool UpdateControllers()
        {
            bool changed = false;
            var paramInfos = model.paramInfos.ToDictionary(t => t.id, t => t);

            foreach (var removedController in m_Controllers.Where(t => !paramInfos.ContainsKey(t.Key)).ToArray())
            {
                removedController.Value.OnDisable();
                m_Controllers.Remove(removedController.Key);
                m_ViewController.RemoveControllerFromModel(parameter, removedController.Value);
                changed = true;
            }

            foreach (var addedController in paramInfos.Where(t => !m_Controllers.ContainsKey(t.Key)).ToArray())
            {
                VFXParameterController controller = new VFXParameterController(this, addedController.Value, m_ViewController);

                m_Controllers[addedController.Key] = controller;
                m_ViewController.AddControllerToModel(parameter, controller);

                controller.ForceUpdate();
                changed = true;
            }

            return changed;
        }
    }
}
