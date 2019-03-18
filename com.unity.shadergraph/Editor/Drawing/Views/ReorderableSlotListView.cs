using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal class ReorderableSlotListView : VisualElement
    {
        private AbstractMaterialNode m_Node;
        private SlotType m_SlotType;
        private ReorderableList m_ReorderableList;
        private IMGUIContainer m_Container;
        private GUIStyle m_LabelStyle;
        private int m_SelectedIndex = -1;
        int[] m_ValueTypeIndices;
        string[] m_ValueTypeNames;
        private string label => string.Format("{0}s", m_SlotType.ToString());
        public int labelWidth => 80;

        public GUIStyle labelStyle
        {
            get
            {
                if(m_LabelStyle == null)
                {
                    m_LabelStyle = new GUIStyle();
                    m_LabelStyle.normal.textColor = Color.white;
                }
                return m_LabelStyle;
            }
        }

        internal ReorderableSlotListView(AbstractMaterialNode node, SlotType slotType, ConcreteSlotValueType[] allowedValueTypes = null)
        {
            m_ValueTypeIndices = allowedValueTypes == null ? Enum.GetValues(typeof(ConcreteSlotValueType)).Cast<int>().ToArray() : allowedValueTypes.Cast<int>().ToArray();
            m_ValueTypeNames = m_ValueTypeIndices.Select(x => ((ConcreteSlotValueType)x).ToString()).ToArray();
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ReorderableSlotListView"));
            m_Node = node;
            m_SlotType = slotType;
            m_Container = new IMGUIContainer(() => OnGUIHandler ()) { name = "ListContainer" };
            Add(m_Container);
        }

        internal void RecreateList()
        {
            // Get slots based on type
            List<MaterialSlot> slots = new List<MaterialSlot>();
            if(m_SlotType == SlotType.Input)
                m_Node.GetInputSlots(slots);
            else
                m_Node.GetOutputSlots(slots);
            
            // Create reorderable list from IDs
            List<int> slotIDs = slots.Select(s => s.id).ToList();
            m_ReorderableList = new ReorderableList(slotIDs, typeof(int), true, true, true, true);
        }

        private void OnGUIHandler()
        {
            if(m_ReorderableList == null)
            {
                RecreateList();
                AddCallbacks();
            }

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                m_ReorderableList.index = m_SelectedIndex;
                m_ReorderableList.DoLayoutList();

                if (changeCheckScope.changed)
                    m_Node.Dirty(ModificationScope.Node);
            }
        }

        private void AddCallbacks() 
        {      
            m_ReorderableList.drawHeaderCallback = (Rect rect) => 
            {  
                var labelRect = new Rect(rect.x, rect.y, rect.width-10, rect.height);
                EditorGUI.LabelField(labelRect, label);
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
            {
                rect.y += 2;

                // Slot is guaranteed to exist in this UI state
                MaterialSlot oldSlot = m_Node.FindSlot<MaterialSlot>((int)m_ReorderableList.list[index]);

                EditorGUI.BeginChangeCheck();
                
                var displayName = EditorGUI.DelayedTextField( new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), oldSlot.RawDisplayName(), labelStyle); 
                var shaderOutputName = NodeUtils.GetHLSLSafeName(displayName);
                
                var concreteValueType = (ConcreteSlotValueType)EditorGUI.IntPopup( new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, EditorGUIUtility.singleLineHeight), (int)oldSlot.concreteValueType, m_ValueTypeNames, m_ValueTypeIndices);

                if(displayName != oldSlot.RawDisplayName())
                    displayName = NodeUtils.GetDuplicateSafeNameForSlot(m_Node, oldSlot.id, displayName);

                if(EditorGUI.EndChangeCheck())
                {
                    // Cant modify existing slots so need to create new and copy values
                    var newSlot = MaterialSlot.CreateMaterialSlot(concreteValueType.ToSlotValueType(), oldSlot.id, displayName, shaderOutputName, m_SlotType, Vector4.zero);
                    newSlot.CopyValuesFrom(oldSlot);
                    m_Node.AddSlot(newSlot);

                    // Need to get all current slots as everything after the edited slot in the list must be added again
                    List<MaterialSlot> slots = new List<MaterialSlot>();
                    if(m_SlotType == SlotType.Input)
                        m_Node.GetInputSlots<MaterialSlot>(slots);
                    else
                        m_Node.GetOutputSlots<MaterialSlot>(slots);

                    // Iterate all the slots
                    foreach (MaterialSlot slot in slots)
                    {
                        // Because the list doesnt match the slot IDs (reordering)
                        // Need to get the index in the list of every slot
                        int listIndex = 0;
                        for(int i = 0; i < m_ReorderableList.list.Count; i++)
                        {
                            if((int)m_ReorderableList.list[i] == slot.id)
                                listIndex = i;
                        }

                        // Then for everything after the edited slot
                        if(listIndex <= index)
                            continue;

                        // Remove and re-add
                        m_Node.AddSlot(slot);
                    }

                    RecreateList();
                    m_Node.ValidateNode();
                }   
            };

            // Element height
            m_ReorderableList.elementHeightCallback = (int indexer) => 
            {
                return m_ReorderableList.elementHeight;
            };

            // Add callback delegates
            m_ReorderableList.onSelectCallback += SelectEntry;
            m_ReorderableList.onAddCallback += AddEntry;
            m_ReorderableList.onRemoveCallback += RemoveEntry;
            m_ReorderableList.onReorderCallback += ReorderEntries;
        }

        private void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        private void AddEntry(ReorderableList list)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Add Port");

            // Need to get all current slots to get the next valid ID
            List<MaterialSlot> slots = new List<MaterialSlot>();
            m_Node.GetSlots(slots);
            int[] slotIDs = slots.Select(s => s.id).OrderByDescending(s => s).ToArray();
            int newSlotID = slotIDs.Length > 0 ? slotIDs[0] + 1 : 0;
            
            string name = NodeUtils.GetDuplicateSafeNameForSlot(m_Node, newSlotID, "New");

            // Create a new slot and add it
            var newSlot = MaterialSlot.CreateMaterialSlot(SlotValueType.Vector1, newSlotID, name, NodeUtils.GetHLSLSafeName(name), m_SlotType, Vector4.zero);
            m_Node.AddSlot(newSlot);

            // Select the new slot, then validate the node
            m_SelectedIndex = list.list.Count - 1;
            m_Node.ValidateNode();
        }

        private void RemoveEntry(ReorderableList list)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Remove Port");

            // Remove the slot from the node
            m_SelectedIndex = list.index;
            m_Node.RemoveSlot((int)m_ReorderableList.list[m_SelectedIndex]);

            // Then remove it from the list
            // Need to do this order to preserve the list indicies for previous step
            ReorderableList.defaultBehaviours.DoRemoveButton(list);

            // Validate
            m_Node.ValidateNode();
        }

        private void ReorderEntries(ReorderableList list)
        {
            m_Node.owner.owner.RegisterCompleteObjectUndo("Reorder Ports");
            
            // Get all the current slots
            List<MaterialSlot> slots = new List<MaterialSlot>();
            if(m_SlotType == SlotType.Input)
                m_Node.GetInputSlots<MaterialSlot>(slots);
            else
                m_Node.GetOutputSlots<MaterialSlot>(slots);

            // Get reorder slots so need to remove them all then re-add
            foreach (MaterialSlot slot in slots)
                m_Node.RemoveSlot(slot.id);

            // Order them by their slot ID
            slots = slots.OrderBy(s => s.id).ToList();
            
            // Now add the slots back based on the list order
            // For each list entry get the slot with that ID
            for(int i = 0; i < list.list.Count; i++)
            {
                var currentSlot = slots.Where(s => s.id == (int)list.list[i]).FirstOrDefault();
                m_Node.AddSlot(currentSlot);
            }

            RecreateList();
            m_Node.ValidateNode();
        }
    }
}
