using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.MaterialGraph
{
    [Serializable]
    public class SerializableNode : ISerializationCallbackReceiver
    {
        public delegate void NeedsRepaint();
        public NeedsRepaint onNeedsRepaint;
        
        private const int kPreviewWidth = 64;
        private const int kPreviewHeight = 64;

        [NonSerialized]
        private Guid m_Guid;

        [SerializeField]
        private string m_GuidSerialized;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private Rect m_Position;

        [NonSerialized]
        private List<SerializableSlot> m_Slots = new List<SerializableSlot>();

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableSlots = new List<SerializationHelper.JSONSerializedElement>();

        public SerializableGraph owner { get; set; }

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

        protected virtual int previewWidth
        {
            get { return kPreviewWidth; }
        }

        protected virtual int previewHeight
        {
            get { return kPreviewHeight; }
        }

        public Rect position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public IEnumerable<SerializableSlot> inputSlots
        {
            get { return m_Slots.Where(x => x.isInputSlot); }
        }

        public IEnumerable<SerializableSlot> outputSlots
        {
            get { return m_Slots.Where(x => x.isOutputSlot); }
        }

        public IEnumerable<SerializableSlot> slots
        {
            get { return m_Slots; }
        }

        public SerializableNode(SerializableGraph theOwner)
        {
            owner = theOwner;
            m_Guid = Guid.NewGuid();
        }

        public virtual void AddSlot(SerializableSlot slot)
        {
            if (slot == null)
                return;

            m_Slots.RemoveAll(x => x.name == slot.name);
            m_Slots.Add(slot);
        }

        public void RemoveSlot(string name)
        {
            m_Slots.RemoveAll(x => x.name == name);
        }

        public void RemoveSlotsNameNotMatching(string[] slotNames)
        {
            var invalidSlots = m_Slots.Select(x => x.name).Except(slotNames);

            foreach (var invalidSlot in invalidSlots.ToList())
            {
                Debug.LogFormat("Removing Invalid MaterialSlot: {0}", invalidSlot);
                RemoveSlot(invalidSlot);
            }
        }

        public SlotReference GetSlotReference(string name)
        {
            return new SlotReference(guid, name);
        }
        public SerializableSlot FindSlot(string name)
        {
            var slot = slots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Input Slot: {0} could be found on node {1}", name, this);
            return slot;
        }

        public SerializableSlot FindInputSlot(string name)
        {
            var slot = inputSlots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Input Slot: {0} could be found on node {1}", name, this);
            return slot;
        }

        public SerializableSlot FindOutputSlot(string name)
        {
            var slot = outputSlots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Output Slot: {0} could be found on node {1}", name, this);
            return slot;
        }

        // CollectDependentNodes looks at the current node and calculates
        // which nodes further up the tree (parents) would be effected if this node was changed
        // it also includes itself in this list
        public IEnumerable<SerializableNode> CollectDependentNodes()
        {
            var nodeList = new List<SerializableNode>();
            NodeUtils.CollectDependentNodes(nodeList, this);
            return nodeList;
        }

        // CollectDependentNodes looks at the current node and calculates
        // which child nodes it depends on for it's calculation.
        // Results are returned depth first so by processing each node in
        // order you can generate a valid code block.
        public List<SerializableNode> CollectChildNodesByExecutionOrder(List<SerializableNode> nodeList, SerializableSlot slotToUse = null, bool includeSelf = true)
        {
            if (slotToUse != null && !m_Slots.Contains(slotToUse))
            {
                Debug.LogError("Attempting to collect nodes by execution order with an invalid MaterialSlot on: " + name);
                return nodeList;
            }

            NodeUtils.CollectChildNodesByExecutionOrder(nodeList, this, slotToUse);

            if (!includeSelf)
                nodeList.Remove(this);

            return nodeList;
        }

        public virtual float GetNodeUIHeight(float width)
        {
            return 0;
        }

        public virtual GUIModificationType NodeUI(Rect drawArea)
        {
            return GUIModificationType.None;
        }

        public virtual bool OnGUI()
        {
            GUILayout.Label("MaterialSlot Defaults", EditorStyles.boldLabel);
            var modified = false;
            foreach (var slot in inputSlots)
            {
                if (!owner.GetEdges(GetSlotReference(slot.name)).Any())
                    modified |= DoSlotUI(this, slot);
            }

            return modified;
        }

        public static bool DoSlotUI(SerializableNode node, SerializableSlot slot)
        {
            GUILayout.BeginHorizontal( /*EditorStyles.inspectorBig*/);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("MaterialSlot " + slot.name, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            return slot.OnGUI();
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

            m_Slots = SerializationHelper.Deserialize<SerializableSlot>(m_SerializableSlots, new object[] { this });
            m_SerializableSlots = null; 
        }
    }
}
