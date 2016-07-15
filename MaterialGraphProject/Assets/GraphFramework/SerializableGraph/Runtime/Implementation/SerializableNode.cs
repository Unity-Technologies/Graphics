using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Graphing
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
        private DrawingData m_DrawData;

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

        public DrawingData drawState
        {
            get { return m_DrawData; }
            set { m_DrawData = value; }
        }

        public virtual bool hasError { get; protected set; }

  
        public SerializableNode()
        {
            m_DrawData.expanded = true;
            m_Guid = Guid.NewGuid();
        }

        public Guid RewriteGuid()
        {
            m_Guid = Guid.NewGuid();
            return m_Guid; 
        }

        public virtual void ValidateNode()
        { }

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

            m_Slots.RemoveAll(x => x.name == slot.name);
            m_Slots.Add(slot);
        }

        public void RemoveSlot(string name)
        {
            //Remove edges that use this slot
            var edges = owner.GetEdges(GetSlotReference(name));

            foreach (var edge in edges.ToArray())
                owner.RemoveEdge(edge);
            
            //remove slots
            m_Slots.RemoveAll(x => x.name == name);
        }

        public void RemoveSlotsNameNotMatching(IEnumerable<string> slotNames)
        {
            var invalidSlots = m_Slots.Select(x => x.name).Except(slotNames);

            foreach (var invalidSlot in invalidSlots.ToArray())
            {
                Debug.LogFormat("Removing Invalid MaterialSlot: {0}", invalidSlot);
                RemoveSlot(invalidSlot);
            }
        }

        public SlotReference GetSlotReference(string name)
        {
            var slot = FindSlot<ISlot>(name);
            if (slot == null)
                return null;
            return new SlotReference(guid, name);
        }

        public T FindSlot<T>(string name) where T: ISlot
        {
            var slot = GetSlots<T>().FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Input Slot: {0} could be found on node {1}", name, this);
            return slot;
        }

        public T FindInputSlot<T>(string name) where T : ISlot
        {
            var slot = GetInputSlots<T>().FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Input Slot: {0} could be found on node {1}", name, this);
            return slot;
        }

        public T FindOutputSlot<T>(string name) where T : ISlot
        {
            var slot = GetOutputSlots<T>().FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Output Slot: {0} could be found on node {1}", name, this);
            return slot;
        }
        
        public virtual void OnBeforeSerialize()
        {
            m_GuidSerialized = m_Guid.ToString();
            m_SerializableSlots = SerializationHelper.Serialize(m_Slots);
        }
        
        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_GuidSerialized)) 
                m_Guid = new Guid(m_GuidSerialized);
            else
                m_Guid = Guid.NewGuid();

            m_Slots = SerializationHelper.Deserialize<ISlot>(m_SerializableSlots, new object[] {});
            m_SerializableSlots = null; 
            UpdateNodeAfterDeserialization();
        }

        public virtual IEnumerable<ISlot> GetInputsWithNoConnection() 
        {
            return GetInputSlots<ISlot>().Where(x => !owner.GetEdges(GetSlotReference(x.name)).Any());
        }

        public virtual void UpdateNodeAfterDeserialization()
        {}
    }
}
