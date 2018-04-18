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
        VFXParameterController m_Parameter;
        //int m_Field;
        int[] m_FieldPath;
        FieldInfo[] m_FieldInfos;

        VFXSubParameterController[] m_Children;

        object[] m_CustomAttributes;
        VFXPropertyAttribute[] m_Attributes;

        public CoordinateSpace space
        {
            get
            {
                //TODOPAUL
                return CoordinateSpace.Local;
            }

            set
            {
                //TODOPAUL
            }
        }

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
            m_CustomAttributes = m_FieldInfos[m_FieldInfos.Length - 1].GetCustomAttributes(true);
            m_Attributes = VFXPropertyAttribute.Create(m_CustomAttributes);
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

        object[] IPropertyRMProvider.customAttributes { get { return m_CustomAttributes; } }

        VFXPropertyAttribute[] IPropertyRMProvider.attributes { get { return m_Attributes; } }

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

    class VFXMinMaxParameterController : IPropertyRMProvider
    {
        public VFXMinMaxParameterController(VFXParameterController owner, bool min)
        {
            m_Owner = owner;
            m_Min = min;
        }

        VFXParameterController m_Owner;
        bool m_Min;
        public bool expanded
        {
            get { return m_Owner.expanded; }
            set { throw new NotImplementedException(); }
        }

        public bool expandable
        {
            get { return m_Owner.expandable; }
        }
        public object value
        {
            get { return m_Min ? m_Owner.minValue : m_Owner.maxValue; }
            set
            {
                if (m_Min)
                    m_Owner.minValue = value;
                else
                    m_Owner.maxValue = value;
            }
        }

        public string name
        {
            get { return m_Min ? "Min" : "Max"; }
        }

        public VFXPropertyAttribute[] attributes
        {
            get { return new VFXPropertyAttribute[] {}; }
        }

        public object[] customAttributes
        {
            get { return null; }
        }

        public Type portType
        {
            get { return m_Owner.portType; }
        }

        public int depth
        {
            get { return m_Owner.depth; }
        }

        public bool editable
        {
            get { return true; }
        }

        public CoordinateSpace space
        {
            get
            {
                //TODOPAUL
                return ((IPropertyRMProvider)m_Owner).space;
            }

            set
            {
                ((IPropertyRMProvider)m_Owner).space = value;
            }
        }

        public void ExpandPath()
        {
            throw new NotImplementedException();
        }

        public void RetractPath()
        {
            throw new NotImplementedException();
        }
    }
    class VFXParameterController : VFXController<VFXParameter>, IPropertyRMProvider
    {
        VFXSubParameterController[] m_SubControllers;

        VFXSlot m_Slot;

        VFXMinMaxParameterController m_MinController;
        public VFXMinMaxParameterController minController
        {
            get
            {
                if (m_MinController == null)
                {
                    m_MinController = new VFXMinMaxParameterController(this, true);
                }
                return m_MinController;
            }
        }
        VFXMinMaxParameterController m_MaxController;
        public VFXMinMaxParameterController maxController
        {
            get
            {
                if (m_MaxController == null)
                {
                    m_MaxController = new VFXMinMaxParameterController(this, false);
                }
                return m_MaxController;
            }
        }

        public VFXParameterController(VFXParameter model, VFXViewController viewController) : base(viewController, model)
        {
            m_Slot = model.outputSlots[0];
            viewController.RegisterNotification(m_Slot, OnSlotChanged);
        }

        public const int ValueChanged = 1;

        void OnSlotChanged()
        {
            if (m_Slot == null)
                return;
            NotifyChange(ValueChanged);
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

        public VFXParameterNodeController GetParameterForLink(VFXSlot slot)
        {
            return m_Controllers.FirstOrDefault(t => t.Value.infos.linkedSlots != null && t.Value.infos.linkedSlots.Any(u => u.inputSlot == slot)).Value;
        }

        public string MakeNameUnique(string name)
        {
            HashSet<string> allNames = new HashSet<string>(viewController.parameterControllers.Where((t, i) => t != this).Select(t => t.exposedName));

            return MakeNameUnique(name, allNames);
        }

        public static string MakeNameUnique(string name, HashSet<string> allNames)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "parameter";
            }
            string candidateName = name.Trim();
            if (candidateName.Length < 1)
            {
                return null;
            }
            string candidateMainPart = null;
            int cpt = 0;
            while (allNames.Contains(candidateName))
            {
                if (candidateMainPart == null)
                {
                    int spaceIndex = candidateName.LastIndexOf(' ');
                    if (spaceIndex == -1)
                    {
                        candidateMainPart = candidateName;
                    }
                    else
                    {
                        if (int.TryParse(candidateName.Substring(spaceIndex + 1), out cpt)) // spaceIndex can't be last char because of Trim()
                        {
                            candidateMainPart = candidateName.Substring(0, spaceIndex);
                        }
                        else
                        {
                            candidateMainPart = candidateName;
                        }
                    }
                }
                ++cpt;

                candidateName = string.Format("{0} {1}", candidateMainPart, cpt);
            }

            return candidateName;
        }

        public void CheckNameUnique(HashSet<string> allNames)
        {
            string candidateName = MakeNameUnique(model.exposedName, allNames);
            if (candidateName != model.exposedName)
            {
                parameter.SetSettingValue("m_exposedName", candidateName);
            }
        }

        public string exposedName
        {
            get { return parameter.exposedName; }

            set
            {
                string candidateName = MakeNameUnique(value);
                if (candidateName != null && candidateName != parameter.exposedName)
                {
                    parameter.SetSettingValue("m_exposedName", candidateName);
                }
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
                parameter.order = value;
            }
        }

        public VFXParameter parameter { get { return model as VFXParameter; } }


        public bool canHaveRange
        {
            get
            {
                return parameter.canHaveRange;
            }
        }

        public bool hasRange
        {
            get { return parameter.hasRange; }

            set
            {
                parameter.hasRange = value;
            }
        }

        static float RangeToFloat(object value)
        {
            if (value != null)
            {
                if (value.GetType() == typeof(float))
                {
                    return (float)value;
                }
                else if (value.GetType() == typeof(int))
                {
                    return (float)(int)value;
                }
                else if (value.GetType() == typeof(uint))
                {
                    return (float)(uint)value;
                }
            }
            return 0.0f;
        }

        public object minValue
        {
            get { return parameter.m_Min != null ? parameter.m_Min.Get() : null; }
            set
            {
                if (value != null)
                {
                    if (parameter.m_Min == null || parameter.m_Min.type != portType)
                        parameter.m_Min = new VFXSerializableObject(portType, value);
                    else
                        parameter.m_Min.Set(value);
                    if (RangeToFloat(this.value) < RangeToFloat(value))
                    {
                        this.value = value;
                    }
                    parameter.Invalidate(VFXModel.InvalidationCause.kUIChanged);
                }
                else
                    parameter.m_Min = null;
            }
        }
        public object maxValue
        {
            get { return parameter.m_Max != null ? parameter.m_Max.Get() : null; }
            set
            {
                if (value != null)
                {
                    if (parameter.m_Max == null || parameter.m_Max.type != portType)
                        parameter.m_Max = new VFXSerializableObject(portType, value);
                    else
                        parameter.m_Max.Set(value);
                    if (RangeToFloat(this.value) > RangeToFloat(value))
                    {
                        this.value = value;
                    }

                    parameter.Invalidate(VFXModel.InvalidationCause.kUIChanged);
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

                if (hasRange)
                {
                    if (RangeToFloat(value) < RangeToFloat(minValue))
                    {
                        value = minValue;
                    }
                    if (RangeToFloat(value) > RangeToFloat(maxValue))
                    {
                        value = maxValue;
                    }
                }

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
        public void DrawGizmos(VisualEffect component)
        {
            if (m_SubControllers != null)
            {
                foreach (var controller in m_SubControllers)
                {
                    VFXValueGizmo.Draw(controller, component);
                }
            }
        }

        Dictionary<int, VFXParameterNodeController> m_Controllers = new Dictionary<int, VFXParameterNodeController>();


        public int nodeCount
        {
            get {return m_Controllers.Count(); }
        }

        protected override void ModelChanged(UnityEngine.Object obj)
        {
            model.ValidateNodes();
            bool controllerListChanged = UpdateControllers();
            if (controllerListChanged)
                viewController.NotifyParameterControllerChange();
            NotifyChange(AnyThing);
        }

        public bool UpdateControllers()
        {
            bool changed = false;
            var nodes = model.nodes.ToDictionary(t => t.id, t => t);

            foreach (var removedController in m_Controllers.Where(t => !nodes.ContainsKey(t.Key)).ToArray())
            {
                removedController.Value.OnDisable();
                m_Controllers.Remove(removedController.Key);
                viewController.RemoveControllerFromModel(parameter, removedController.Value);
                changed = true;
            }

            foreach (var addedController in nodes.Where(t => !m_Controllers.ContainsKey(t.Key)).ToArray())
            {
                VFXParameterNodeController controller = new VFXParameterNodeController(this, addedController.Value, viewController);

                m_Controllers[addedController.Key] = controller;
                viewController.AddControllerToModel(parameter, controller);

                controller.ForceUpdate();
                changed = true;
            }

            return changed;
        }

        public bool expanded
        {
            get
            {
                return !model.collapsed;
            }
            set
            {
                model.collapsed = !value;
            }
        }
        public bool editable
        {
            get { return true; }
        }

        public bool expandable { get { return false; } }

        public string name { get { return "Value"; } }

        public object[] customAttributes { get { return new object[] {}; } }

        public VFXPropertyAttribute[] attributes
        {
            get
            {
                if (canHaveRange)
                {
                    return VFXPropertyAttribute.Create(new object[] { new RangeAttribute(RangeToFloat(minValue), RangeToFloat(maxValue)) });
                }
                return new VFXPropertyAttribute[] {};
            }
        }

        public int depth { get { return 0; } }
        public CoordinateSpace space
        {
            get
            {
                return CoordinateSpace.Local;
            }

            set
            {
                //TODODPAUL
            }
        }

        public void ExpandPath()
        {
            throw new NotImplementedException();
        }

        public void RetractPath()
        {
            throw new NotImplementedException();
        }

        public override void OnDisable()
        {
            if (!object.ReferenceEquals(m_Slot, null))
            {
                viewController.UnRegisterNotification(m_Slot, OnSlotChanged);
            }

            base.OnDisable();
        }
    }
}
