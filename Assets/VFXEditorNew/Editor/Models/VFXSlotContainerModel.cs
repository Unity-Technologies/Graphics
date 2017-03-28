using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;

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

        void AddSlot(VFXSlot slot, bool notify = true);
        void RemoveSlot(VFXSlot slot, bool notify = true);

        void Invalidate(VFXModel.InvalidationCause cause);
    }

    class VFXSlotContainerModel<ParentType, ChildrenType> : VFXModel<ParentType, ChildrenType>, IVFXSlotContainer
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public virtual ReadOnlyCollection<VFXSlot> inputSlots  { get { return m_InputSlots.AsReadOnly(); } }
        public virtual ReadOnlyCollection<VFXSlot> outputSlots { get { return m_OutputSlots.AsReadOnly(); } }

        public virtual int GetNbInputSlots()            { return m_InputSlots.Count; }
        public virtual int GetNbOutputSlots()           { return m_OutputSlots.Count; }

        public virtual VFXSlot GetInputSlot(int index)  { return m_InputSlots[index]; }
        public virtual VFXSlot GetOutputSlot(int index) { return m_OutputSlots[index]; }

        public virtual void AddSlot(VFXSlot slot, bool notify = true)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (slot.owner != this)
            {
                if (slot.owner != null)
                    slot.owner.RemoveSlot(slot,notify);

                slotList.Add(slot);
                slot.m_Owner = this;

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }          
        }

        public virtual void RemoveSlot(VFXSlot slot, bool notify = true)
        {
            var slotList = slot.direction == VFXSlot.Direction.kInput ? m_InputSlots : m_OutputSlots;

            if (slot.owner == this)
            {
                slotList.Remove(slot);
                slot.m_Owner = null;

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

        protected VFXSlotContainerModel()
        {
        }

        public override void OnEnable()
        {
            base.OnEnable();
            InitProperties(GetInputPropertiesTypeName(), out m_InputProperties, out m_InputValues, VFXSlot.Direction.kInput);
            InitProperties(GetOutputPropertiesTypeName(), out m_OutputProperties, out m_OutputValues, VFXSlot.Direction.kOutput);
        }

        static private VFXExpression GetExpressionFromObject(object value)
        {
            if (value is float)
            {
                return new VFXValueFloat((float)value, true);
            }
            else if (value is Vector2)
            {
                return new VFXValueFloat2((Vector2)value, true);
            }
            else if (value is Vector3)
            {
                return new VFXValueFloat3((Vector3)value, true);
            }
            else if (value is Vector4)
            {
                return new VFXValueFloat4((Vector4)value, true);
            }
            else if (value is FloatN)
            {
                return (FloatN)value;
            }
            else if (value is AnimationCurve)
            {
                return new VFXValueCurve(value as AnimationCurve, true);
            }
            return null;
        }

        private void InitProperties(string className, out VFXProperty[] properties, out object[] values,VFXSlot.Direction direction)
        {
            System.Type type = GetType().GetNestedType(className);

            if (type != null)
            {
                var fields = type.GetFields().Where(f => !f.IsStatic).ToArray();

                properties = new VFXProperty[fields.Length];
                values = new object[fields.Length];

                var defaultBuffer = System.Activator.CreateInstance(type);

                for (int i = 0; i < fields.Length; ++i)
                {
                    properties[i] = new VFXProperty() { type = fields[i].FieldType, name = fields[i].Name };
                    values[i] = fields[i].GetValue(defaultBuffer);
                }

                // Create slot hierarchy
                for (int i = 0; i < fields.Length; ++i)
                {
                    var property = properties[i];
                    var value = values[i];
                    var slot = VFXSlot.Create(property, direction, value);
                    if (slot != null)
                    {
                        AddSlot(slot,false);
                    }
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

        protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            /*
             * Will be the job of expression
            var allConnectedChildModel = outputSlots.SelectMany(o => o.children.Select(c => c.owner)).Distinct().ToArray();
            if (cause == InvalidationCause.kParamChanged)
            {
                foreach (var slot in inputSlots)
                {
                    if (slot.parent != null && !slot.CanConnect(slot.parent, slot.parentSlotID))
                    {
                        slot.Disconnect();
                    }
                }
            }*/

            base.Invalidate(model, cause);

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
