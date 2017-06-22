using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;
using Object = UnityEngine.Object;

namespace UnityEditor.VFX
{
    interface IVFXSlotContainer
    {
        ReadOnlyCollection<VFXSlot> inputSlots     { get; }
        ReadOnlyCollection<VFXSlot> outputSlots    { get; }

        int GetNbInputSlots();
        int GetNbOutputSlots();

        VFXSlot GetInputSlot(int index);
        VFXSlot GetOutputSlot(int index);

        void AddSlot(VFXSlot slot);
        void RemoveSlot(VFXSlot slot);

        void Invalidate(VFXModel.InvalidationCause cause);
        void UpdateOutputs();

        void SetSettingValue(string name, object value);

        object settings {get; }

        bool expanded { get; set; }
    }

    abstract class VFXSlotContainerModel<ParentType, ChildrenType> : VFXModel<ParentType, ChildrenType>, IVFXSlotContainer
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public virtual ReadOnlyCollection<VFXSlot> inputSlots  { get { return m_InputSlots.AsReadOnly(); } }
        public virtual ReadOnlyCollection<VFXSlot> outputSlots { get { return m_OutputSlots.AsReadOnly(); } }


        public object settings { get { return m_Settings != null ? m_Settings.Get() : null; } }

        public bool expanded { get; set; }

        [SerializeField]
        private VFXSerializableObject m_Settings;


        public T GetSettings<T>() where T : class
        {
            return m_Settings != null ? m_Settings.Get<T>() : null;
        }

        public virtual int GetNbInputSlots()            { return m_InputSlots.Count; }
        public virtual int GetNbOutputSlots()           { return m_OutputSlots.Count; }

        public virtual VFXSlot GetInputSlot(int index)  { return m_InputSlots[index]; }
        public virtual VFXSlot GetOutputSlot(int index) { return m_OutputSlots[index]; }

        public virtual void AddSlot(VFXSlot slot) { InnerAddSlot(slot, true); }
        private void InnerAddSlot(VFXSlot slot, bool notify)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (!slot.IsMasterSlot())
                throw new ArgumentException();

            if (slot.owner != this as IVFXSlotContainer)
            {
                if (slot.owner != null)
                    slot.owner.RemoveSlot(slot);

                slotList.Add(slot);
                slot.SetOwner(this);
                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public virtual void RemoveSlot(VFXSlot slot) { InnerRemoveSlot(slot, true); }
        private void InnerRemoveSlot(VFXSlot slot, bool notify)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (!slot.IsMasterSlot())
                throw new ArgumentException();

            if (slot.owner == this as IVFXSlotContainer)
            {
                slotList.Remove(slot);
                slot.SetOwner(null);
                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        protected string GetInputPropertiesTypeName()
        {
            return "InputProperties";
        }

        protected string GetOutputPropertiesTypeName()
        {
            return "OutputProperties";
        }

        protected string GetSettingsTypeName()
        {
            return "Settings";
        }

        protected VFXSlotContainerModel()
        {
            InitSettings();
        }

        public override void OnEnable()
        {
            base.OnEnable();

            if (m_InputSlots == null)
            {
                m_InputSlots = new List<VFXSlot>();
                InitSlotsFromProperties(GetInputPropertiesTypeName(), VFXSlot.Direction.kInput);
            }
            else
            {
                int nbRemoved = m_InputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} input slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }

            if (m_OutputSlots == null)
            {
                m_OutputSlots = new List<VFXSlot>();
                InitSlotsFromProperties(GetOutputPropertiesTypeName(), VFXSlot.Direction.kOutput);
            }
            else
            {
                int nbRemoved = m_OutputSlots.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.Log(String.Format("Remove {0} output slot(s) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        public override void CollectDependencies(HashSet<Object> objs)
        {
            base.CollectDependencies(objs);
            foreach (var slot in m_InputSlots.Concat(m_OutputSlots))
            {
                objs.Add(slot);
                slot.CollectDependencies(objs);
            }
        }

        public override T Clone<T>()
        {
            var clone = base.Clone<T>();
            var cloneContainer = clone as VFXSlotContainerModel<ParentType, ChildrenType>;

            cloneContainer.m_InputSlots.Clear();
            cloneContainer.m_OutputSlots.Clear();

            foreach (var input in inputSlots)
            {
                var cloneSlot = input.Clone<VFXSlot>();
                cloneContainer.m_InputSlots.Add(cloneSlot);
                cloneSlot.SetOwner(cloneContainer);
            }

            foreach (var output in outputSlots)
            {
                var cloneSlot = output.Clone<VFXSlot>();
                cloneContainer.m_OutputSlots.Add(cloneSlot);
                cloneSlot.SetOwner(cloneContainer);
            }

            return clone;
        }

        static private VFXExpression GetExpressionFromObject(object value)
        {
            if (value is float)
            {
                return new VFXValue<float>((float)value, true);
            }
            else if (value is Vector2)
            {
                return new VFXValue<Vector2>((Vector2)value, true);
            }
            else if (value is Vector3)
            {
                return new VFXValue<Vector3>((Vector3)value, true);
            }
            else if (value is Vector4)
            {
                return new VFXValue<Vector4>((Vector4)value, true);
            }
            else if (value is FloatN)
            {
                return (FloatN)value;
            }
            else if (value is AnimationCurve)
            {
                return new VFXValue<AnimationCurve>(value as AnimationCurve, true);
            }
            return null;
        }

        static protected VFXSlot[] GenerateSlotFromField(Type type, VFXSlot.Direction direction)
        {
            if (type == null)
            {
                return new VFXSlot[] { };
            }

            var slotList = new List<VFXSlot>();
            var fields = type.GetFields().Where(f => !f.IsStatic).ToArray();
            var properties = new VFXProperty[fields.Length];
            var values = new object[fields.Length];

            var defaultBuffer = System.Activator.CreateInstance(type);
            for (int i = 0; i < fields.Length; ++i)
            {
                properties[i] = new VFXProperty(fields[i].FieldType, fields[i].Name);
                values[i] = fields[i].GetValue(defaultBuffer);
            }

            for (int i = 0; i < fields.Length; ++i)
            {
                var property = properties[i];
                var value = values[i];
                var slot = VFXSlot.Create(property, direction, value);
                if (slot != null)
                {
                    slotList.Add(slot);
                }
            }
            return slotList.ToArray();
        }

        private void InitSlotsFromProperties(string className, VFXSlot.Direction direction)
        {
            var type = GetType().GetNestedType(className);
            var slots = GenerateSlotFromField(type, direction);
            foreach (var slot in slots)
            {
                InnerAddSlot(slot, false);
            }
        }

        public void InitSettings()
        {
            var type = GetType().GetNestedType(GetSettingsTypeName());
            if (type != null)
            {
                if (!type.IsClass)
                {
                    Debug.LogError("Settings type must be a class:" + type.FullName);
                    return;
                }
                m_Settings = new VFXSerializableObject(type);
            }
        }

        public void ExpandPath(string fieldPath)
        {
            m_expandedPaths.Add(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public void RetractPath(string fieldPath)
        {
            m_expandedPaths.Remove(fieldPath);
            Invalidate(InvalidationCause.kParamChanged);
        }

        public bool IsPathExpanded(string fieldPath)
        {
            return m_expandedPaths.Contains(fieldPath);
        }

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);
        }

        public virtual void UpdateOutputs()
        {
            //foreach (var slot in m_InputSlots)
            //    slot.Initialize();
        }

        //[SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();

        [SerializeField]
        List<VFXSlot> m_InputSlots;

        [SerializeField]
        List<VFXSlot> m_OutputSlots;

        public void SetSettingValue(string name, object value)
        {
            object sett = m_Settings.Get();
            if (settings != null)
                settings.GetType().GetField(name).SetValue(sett, value);

            m_Settings.Set(sett);

            Invalidate(InvalidationCause.kParamChanged);
        }
    }
}
