using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldSubgraphDelegateView : BlackboardFieldView
    {
        private ReorderableList m_ReorderableList_Input;
        private ReorderableList m_ReorderableList_Output;
        private IMGUIContainer m_Container;
        private int m_SelectedIndex;
        private ShaderSubgraphDelegate m_Delegate;

        public BlackboardFieldSubgraphDelegateView(BlackboardField blackboardField, GraphData graph, ShaderInput input)
            : base (blackboardField, graph, input)
        {
        }

        public override void BuildCustomFields(ShaderInput input)
        {
            m_Delegate = input as ShaderSubgraphDelegate;
            if(m_Delegate == null)
                return;

            // Default field
            var field1 = new PopupField<string>(m_Delegate.input_Entries.Select(x => x.propertyType.ToString()).ToList(), 0);
            AddRow("Inputs", field1);
            var field2 = new PopupField<string>(m_Delegate.output_Entries.Select(x => x.propertyType.ToString()).ToList(), 0);
            AddRow("Outputs", field2);

            // Entries
            m_Container = new IMGUIContainer(() => OnGUIHandler()) { name = "ListContainer" };
            AddRow("Entries", m_Container, m_Delegate.isEditable);
        }


        private void OnGUIHandler()
        {
            if(m_ReorderableList_Input == null || m_ReorderableList_Output == null)
            {
                RecreateList();
                AddCallbacks(m_ReorderableList_Input, m_Delegate.input_Entries);
                //AddCallbacks(m_ReorderableList_Output, m_Delegate.output_Entries);
            }

            m_ReorderableList_Input.index = m_SelectedIndex;
            m_ReorderableList_Input.DoLayoutList();
            //m_ReorderableList_Output.index = m_SelectedIndex;
            //m_ReorderableList_Output.DoLayoutList();
        }

        internal void RecreateList()
        {
            // Create reorderable list from entries
            m_ReorderableList_Input = new ReorderableList(m_Delegate.input_Entries, typeof(SubgraphDelegateEntry), true, true, true, true);
            m_ReorderableList_Output = new ReorderableList(m_Delegate.input_Entries, typeof(SubgraphDelegateEntry), true, true, true, true);
        }

        private void AddCallbacks(ReorderableList reorderableList, List<SubgraphDelegateEntry> entries) 
        {
            // Draw Header      
            reorderableList.drawHeaderCallback = (Rect rect) => 
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(displayRect, "Display Name");
                var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(referenceRect, "Reference Suffix");
            };

            // Draw Element
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
            {
                SubgraphDelegateEntry entry = ((SubgraphDelegateEntry)reorderableList.list[index]);
                EditorGUI.BeginChangeCheck();
                
                PropertyType enumName = (PropertyType)EditorGUI.EnumPopup( new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.propertyType, EditorStyles.label);
                
                if(EditorGUI.EndChangeCheck())
                {
                    entries[index] = new SubgraphDelegateEntry(index + 1, enumName);
                    DirtyNodes();
                    Rebuild();
                }   
            };

            // Element height
            reorderableList.elementHeightCallback = (int indexer) => 
            {
                return reorderableList.elementHeight;
            };

            // Can add
            reorderableList.onCanAddCallback = (ReorderableList list) => 
            {  
                return list.count < 8;
            };

            // Can remove
            reorderableList.onCanRemoveCallback = (ReorderableList list) => 
            {  
                return list.count > 2;
            };

            // Add callback delegates
            reorderableList.onSelectCallback += SelectEntry;
            reorderableList.onAddCallback += AddEntry;
            reorderableList.onRemoveCallback += RemoveEntry;
            reorderableList.onReorderCallback += ReorderEntries;
        }

        private void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        private void AddEntry(ReorderableList list)
        {
            graph.owner.RegisterCompleteObjectUndo("Add Subgraph Delegate Entry");

            var index = list.list.Count + 1;

            // Add new entry
            m_Delegate.input_Entries.Add(new SubgraphDelegateEntry(index, PropertyType.Vector1));

            // Update GUI
            Rebuild();
            //graph.OnKeywordChanged();
            m_SelectedIndex = list.list.Count - 1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            graph.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (SubgraphDelegateEntry)m_ReorderableList_Input.list[list.index];
            m_Delegate.input_Entries.Remove(selectedEntry);

            Rebuild();
            //graph.OnKeywordChanged();
        }

        private void ReorderEntries(ReorderableList list)
        {
            DirtyNodes();
        }

        public override void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in graph.GetNodes<KeywordNode>())
            {
                node.UpdateNode();
                node.Dirty(modificationScope);
            }

            // Cant determine if Sub Graphs contain the keyword so just update them
            foreach (var node in graph.GetNodes<SubGraphNode>())
            {
                node.Dirty(modificationScope);
            }
        }
    }
}
