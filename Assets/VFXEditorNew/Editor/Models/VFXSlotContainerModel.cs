using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    interface IVFXSlotContainer
    {
        IEnumerable<VFXSlot> inputSlots     { get; }
        IEnumerable<VFXSlot> outputSlots    { get; }

        int GetNbInputSlots();
        int GetNbOutputSlots();

        VFXSlot GetInputSlot(int index);
        VFXSlot GetOutputSlot(int index);

        void AddSlot(VFXSlot slot);
        void RemoveSlot(VFXSlot slot);
    }

    class VFXSlotContainerModel<ParentType, ChildrenType> : VFXModel<ParentType, ChildrenType>, IVFXSlotContainer
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public virtual IEnumerable<VFXSlot> inputSlots  { get { return m_InputSlots; } }
        public virtual IEnumerable<VFXSlot> outputSlots { get { return m_OutputSlots; } }

        public virtual int GetNbInputSlots()            { return m_InputSlots.Count; }
        public virtual int GetNbOutputSlots()           { return m_OutputSlots.Count; }

        public virtual VFXSlot GetInputSlot(int index)  { return m_InputSlots[index]; }
        public virtual VFXSlot GetOutputSlot(int index) { return m_OutputSlots[index]; }

        public virtual void AddSlot(VFXSlot slot)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (slot.owner != this)
            {
                if (slot.owner != null)
                    slot.owner.RemoveSlot(slot);

                slotList.Add(slot);
                slot.m_Owner = this;
                Invalidate(InvalidationCause.kStructureChanged);
            }          
        }

        public virtual void RemoveSlot(VFXSlot slot)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (slot.owner == this)
            {
                slotList.Remove(slot);
                slot.m_Owner = null;
                Invalidate(InvalidationCause.kStructureChanged);
            }
        } 

        protected VFXSlotContainerModel()
        {
            InitProperties("InputProperties", out m_InputProperties, out m_InputValues,VFXSlot.Direction.kInput);
            InitProperties("OutputProperties", out m_OutputProperties, out m_OutputValues,VFXSlot.Direction.kOutput);
        }

        private void InitProperties(string className, out VFXProperty[] properties, out object[] values,VFXSlot.Direction direction)
        {
            System.Type type = GetType().GetNestedType(className);

            if (type != null)
            {
                var fields = type.GetFields();

                properties = new VFXProperty[fields.Length];
                values = new object[fields.Length];

                var defaultBuffer = System.Activator.CreateInstance(type);

                for (int i = 0; i < fields.Length; ++i)
                {
                    properties[i] = new VFXProperty() { type = fields[i].FieldType, name = fields[i].Name };
                    values[i] = fields[i].GetValue(defaultBuffer);
                }

                // Create slot hierarchy
                foreach (var property in properties)
                {
                    var slot = VFXSlot.Create(property, direction);
                    if (slot != null)
                        AddSlot(slot);
                }
            }
            else
            {
                properties = new VFXProperty[0];
                values = new object[0];
            }
        }

        protected VFXProperty[] m_InputProperties;
        protected object[] m_InputValues;

        protected VFXProperty[] m_OutputProperties;
        protected object[] m_OutputValues;

        public VFXProperty[] GetProperties()
        {
            return m_InputProperties;
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


        public object[] GetCurrentPropertiesValues()
        {
            return m_InputValues;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            m_SerializableInputSlots = SerializationHelper.Serialize<VFXSlot>(m_InputSlots);
            m_SerializableOutputSlots = SerializationHelper.Serialize<VFXSlot>(m_OutputSlots);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
           
            m_InputSlots = SerializationHelper.Deserialize<VFXSlot>(m_SerializableInputSlots, null);
            m_OutputSlots = SerializationHelper.Deserialize<VFXSlot>(m_SerializableOutputSlots, null);
            
            foreach (var slot in m_InputSlots)
                slot.m_Owner = this;
            foreach (var slot in m_OutputSlots)
                slot.m_Owner = this;
            
            m_SerializableInputSlots = null;
            m_SerializableOutputSlots = null;
        }

        //[SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();

        private List<VFXSlot> m_InputSlots = new List<VFXSlot>();
        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializableInputSlots = null;

        private List<VFXSlot> m_OutputSlots = new List<VFXSlot>();
        [SerializeField]
        private List<SerializationHelper.JSONSerializedElement> m_SerializableOutputSlots = null;
    }
}
