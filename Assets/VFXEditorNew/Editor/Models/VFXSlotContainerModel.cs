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
            // TODO
        }

        public virtual void RemoveSlot(VFXSlot slot)
        {
            // TODO
        }

        protected VFXSlotContainerModel()
        {
            InitProperties("InputProperties", out m_InputProperties, out m_InputValues);
            InitProperties("OutputProperties", out m_OutputProperties, out m_OutputValues);
        }

        private void InitProperties(string className, out VFXProperty[] properties, out object[] values)
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

        //[SerializeField]
        HashSet<string> m_expandedPaths = new HashSet<string>();

        private List<VFXSlot> m_InputSlots;
        private List<VFXSlot> m_OutputSlots;
    }
}
