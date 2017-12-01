using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Graphing
{
    [Serializable]
    public class SerializableNode : INode, ISerializationCallbackReceiver
    {
        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private DrawState m_DrawState;

        [NonSerialized]
        private List<ISlot> m_Slots = new List<ISlot>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSlots = new List<SerializationHelper.JSONSerializedElement>();

        public IGraph owner { get; set; }

        public Guid guid
        {
            get { return m_Guid; }
        }

        public string name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public virtual bool canDeleteNode
        {
            get { return true; }
        }

        public DrawState drawState
        {
            get { return m_DrawState; }
            set
            {
                m_DrawState = value;
                if (onModified != null)
                    onModified(this, ModificationScope.Node);
            }
        }

        public virtual bool hasError { get; protected set; }

        public SerializableNode()
        {
            m_DrawState.expanded = true;
            m_Guid = Guid.NewGuid();
        }

        public Guid RewriteGuid()
        {
            m_Guid = Guid.NewGuid();
            return m_Guid;
        }

        public virtual void ValidateNode()
        {}

        public OnNodeModified onModified { get; set; }

        public IEnumerable<T> GetInputSlots<T>() where T : ISlot
        {
            return GetSlots<T>().Where(x => x.isInputSlot);
        }

        public IEnumerable<T> GetOutputSlots<T>() where T : ISlot
        {
            return GetSlots<T>().Where(x => x.isOutputSlot);
        }

        public IEnumerable<T> GetSlots<T>() where T : ISlot
        {
            return m_Slots.OfType<T>();
        }

        public virtual void AddSlot(ISlot slot)
        {
            if (slot == null)
                return;

            m_Slots.RemoveAll(x => x.id == slot.id);
            m_Slots.Add(slot);
            slot.owner = this;

            if (onModified != null)
            {
                onModified(this, ModificationScope.Topological);
            }
        }

        public void RemoveSlot(int slotId)
        {
            // Remove edges that use this slot
            // no owner can happen after creation
            // but before added to graph
            if (owner != null)
            {
                var edges = owner.GetEdges(GetSlotReference(slotId));

                foreach (var edge in edges.ToArray())
                    owner.RemoveEdge(edge);
            }

            //remove slots
            m_Slots.RemoveAll(x => x.id == slotId);

            if (onModified != null)
            {
                onModified(this, ModificationScope.Topological);
            }
        }

        public void RemoveSlotsNameNotMatching(IEnumerable<int> slotIds)
        {
            var invalidSlots = m_Slots.Select(x => x.id).Except(slotIds);

            foreach (var invalidSlot in invalidSlots.ToArray())
            {
                Debug.LogFormat("Removing Invalid MaterialSlot: {0}", invalidSlot);
                RemoveSlot(invalidSlot);
            }
        }

        public SlotReference GetSlotReference(int slotId)
        {
            var slot = FindSlot<ISlot>(slotId);
            if (slot == null)
                return null;
            return new SlotReference(guid, slotId);
        }

        public T FindSlot<T>(int slotId) where T : ISlot
        {
            return GetSlots<T>().FirstOrDefault(x => x.id == slotId);
        }

        public T FindInputSlot<T>(int slotId) where T : ISlot
        {
            return GetInputSlots<T>().FirstOrDefault(x => x.id == slotId);
        }

        public T FindOutputSlot<T>(int slotId) where T : ISlot
        {
            return GetOutputSlots<T>().FirstOrDefault(x => x.id == slotId);
        }

        public virtual IEnumerable<ISlot> GetInputsWithNoConnection()
        {
            return GetInputSlots<ISlot>().Where(x => !owner.GetEdges(GetSlotReference(x.id)).Any());
        }

        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
            m_SerializableSlots = SerializationHelper.Serialize<ISlot>(m_Slots);
        }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized))
                m_Guid = new Guid(m_GuidSerialized);
            else
                m_Guid = Guid.NewGuid();

            m_Slots = SerializationHelper.Deserialize<ISlot>(m_SerializableSlots, null);
            m_SerializableSlots = null;
            foreach (var s in m_Slots)
                s.owner = this;
            UpdateNodeAfterDeserialization();
        }

        public virtual void UpdateNodeAfterDeserialization()
        {}
    }
}
