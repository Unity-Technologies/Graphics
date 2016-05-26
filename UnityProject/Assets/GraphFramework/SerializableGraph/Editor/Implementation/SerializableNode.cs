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

        public IEnumerable<ISlot> inputSlots
        {
            get { return m_Slots.Where(x => x.isInputSlot); }
        }

        public IEnumerable<ISlot> outputSlots
        {
            get { return m_Slots.Where(x => x.isOutputSlot); }
        }

        public IEnumerable<ISlot> slots
        {
            get { return m_Slots; }
        }

        public SerializableNode(IGraph theOwner)
        {
            owner = theOwner;
            m_Guid = Guid.NewGuid();
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
            m_Slots.RemoveAll(x => x.name == name);
        }

        public void RemoveSlotsNameNotMatching(string[] slotNames)
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
            var slot = FindSlot(name);
            if (slot == null)
                return null;
            return new SlotReference(guid, name);
        }

        public ISlot FindSlot(string name)
        {
            var slot = slots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Input Slot: {0} could be found on node {1}", name, this);
            return slot;
        }

        public ISlot FindInputSlot(string name)
        {
            var slot = inputSlots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Input Slot: {0} could be found on node {1}", name, this);
            return slot;
        }

        public ISlot FindOutputSlot(string name)
        {
            var slot = outputSlots.FirstOrDefault(x => x.name == name);
            if (slot == null)
                Debug.LogErrorFormat("Output Slot: {0} could be found on node {1}", name, this);
            return slot;
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

        public static bool DoSlotUI(SerializableNode node, ISlot slot)
        {
            GUILayout.BeginHorizontal( /*EditorStyles.inspectorBig*/);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("MaterialSlot " + slot.name, EditorStyles.largeLabel);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            //TODO: fix this
            return false;
            //return slot.OnGUI();
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
        }

        public virtual IEnumerable<ISlot> GetInputsWithNoConnection() 
        {
            return inputSlots.Where(x => !owner.GetEdges(GetSlotReference(x.name)).Any());
        }
    }
}
