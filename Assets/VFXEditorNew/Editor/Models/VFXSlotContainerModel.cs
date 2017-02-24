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

        private List<VFXSlot> m_InputSlots;
        private List<VFXSlot> m_OutputSlots;
    }
}
