using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal class ShaderValueDescriptorListView : VisualElement
    {
        private AbstractMaterialNode m_Node;
        private SlotType m_SlotType;
        private ReorderableList m_ReorderableList;
        private IMGUIContainer m_Container;
        private GUIStyle m_LabelStyle;
        private int m_SelectedIndex = -1;
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

        internal ShaderValueDescriptorListView(AbstractMaterialNode node, SlotType slotType)
        {
            //styleSheets.Add(Resources.Load<StyleSheet>("Styles/Views/ShaderValueDescriptorListView"));
            m_Node = node;
            m_SlotType = slotType;
            m_Container = new IMGUIContainer(() => OnGUIHandler ()) { name = "ListContainer" };
            Add(m_Container);
        }

        internal void RecreateList()
        {
            List<MaterialSlot> slots = new List<MaterialSlot>();
            if(m_SlotType == SlotType.Input)
                m_Node.GetInputSlots(slots);
            else
                m_Node.GetOutputSlots(slots);
                
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
                MaterialSlot slot = m_Node.FindSlot<MaterialSlot>((int)m_ReorderableList.list[index]);

                EditorGUI.BeginChangeCheck();
                
                var displayName = EditorGUI.DelayedTextField( new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), slot.RawDisplayName(), labelStyle); 
                var shaderOutputName = NodeUtils.GetHLSLSafeName(slot.RawDisplayName());
                var valueType = (SlotValueType)EditorGUI.EnumPopup( new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, EditorGUIUtility.singleLineHeight), slot.valueType);
                
                if(EditorGUI.EndChangeCheck())
                {
                    var newSlot = MaterialSlot.CreateMaterialSlot(valueType, slot.id, displayName, shaderOutputName, m_SlotType, Vector4.zero);
                    newSlot.CopyValuesFrom(slot);
                    m_Node.AddSlot(newSlot);
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
            List<MaterialSlot> slots = new List<MaterialSlot>();
            m_Node.GetSlots(slots);
            int[] slotIDs = slots.Select(s => s.id).OrderByDescending(s => s).ToArray();
            int newSlotID = slotIDs.Length > 0 ? slotIDs[0] + 1 : 0;

            var newSlot = MaterialSlot.CreateMaterialSlot(SlotValueType.Vector1, newSlotID, "New", "New", m_SlotType, Vector4.zero);
            m_Node.AddSlot(newSlot);
            RecreateList();

            m_SelectedIndex = list.list.Count - 1;
            m_Node.ValidateNode();
        }

        private void RemoveEntry(ReorderableList list)
        {
            list.index = m_SelectedIndex;
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
            m_SelectedIndex = list.index;
            m_Node.RemoveSlot((int)m_ReorderableList.list[list.index]);
            RecreateList();

            m_Node.ValidateNode();
        }

        private void ReorderEntries(ReorderableList list)
        {
            m_Node.ValidateNode();
        }
    }
}
