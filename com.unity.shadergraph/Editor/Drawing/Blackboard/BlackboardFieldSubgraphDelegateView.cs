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
        private IMGUIContainer m_Container_Input;
        private IMGUIContainer m_Container_Output;
        private int m_SelectedIndex_Input;
        private int m_SelectedIndex_Output;
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

            /*// Default field
            var field1 = new PopupField<string>(m_Delegate.input_Entries.Select(x => x.propertyType.ToString()).ToList(), 0);
            AddRow("Inputs", field1);
            var field2 = new PopupField<string>(m_Delegate.output_Entries.Select(x => x.propertyType.ToString()).ToList(), 0);
            AddRow("Outputs", field2);*/

            // Entries
            m_Container_Input = new IMGUIContainer(() => OnGUIHandlerInputs()) { name = "ListContainer" };
            AddRow("Inputs", m_Container_Input, m_Delegate.isEditable);

            m_Container_Output = new IMGUIContainer(() => OnGUIHandlerOutputs()) { name = "ListContainer" };
            AddRow("Outputs", m_Container_Output, m_Delegate.isEditable);
        }


        private void OnGUIHandlerInputs()
        {
            if(m_ReorderableList_Input == null)
            {
                RecreateInputList();
                AddCallbacks(m_ReorderableList_Input, m_Delegate.input_Entries);
            }

            m_ReorderableList_Input.index = m_SelectedIndex_Input;
            m_ReorderableList_Input.DoLayoutList();
        }
        private void OnGUIHandlerOutputs()
        {
            if (m_ReorderableList_Output == null)
            {
                RecreateOutputList();
                AddCallbacks(m_ReorderableList_Output, m_Delegate.output_Entries);
            }

            m_ReorderableList_Output.index = m_SelectedIndex_Output;
            m_ReorderableList_Output.DoLayoutList();
        }

        internal void RecreateInputList()
        {
            // Create reorderable list from entries
            m_ReorderableList_Input = new ReorderableList(m_Delegate.input_Entries, typeof(SubgraphDelegateEntry), true, true, true, true);
        }
        internal void RecreateOutputList()
        {
            // Create reorderable list from entries
            m_ReorderableList_Output = new ReorderableList(m_Delegate.output_Entries, typeof(SubgraphDelegateEntry), true, true, true, true);
        }

        private void AddCallbacks(ReorderableList reorderableList, List<SubgraphDelegateEntry> entries) 
        {
            // Draw Header      
            reorderableList.drawHeaderCallback = (Rect rect) => 
            {
                int indent = 14;
                var propertyRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 3, rect.height);
                EditorGUI.LabelField(propertyRect, "PropertyType");
                var displayRect = new Rect((rect.x + indent) + (rect.width - indent) / 3, rect.y, (rect.width - indent) / 3, rect.height);
                EditorGUI.LabelField(displayRect, "Display Name");
                var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 3 * 2, rect.y, (rect.width - indent) / 3, rect.height);
                EditorGUI.LabelField(referenceRect, "Reference Suffix");
            };

            // Draw Element
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
            {
                SubgraphDelegateEntry entry = ((SubgraphDelegateEntry)reorderableList.list[index]);
                EditorGUI.BeginChangeCheck();
                
                PropertyType enumName = (PropertyType)EditorGUI.EnumPopup( new Rect(rect.x, rect.y, rect.width / 3, EditorGUIUtility.singleLineHeight), entry.propertyType, EditorStyles.label);
                var displayName = EditorGUI.DelayedTextField(new Rect((rect.x) + (rect.width) / 3, rect.y, rect.width / 3, EditorGUIUtility.singleLineHeight), entry.displayName, EditorStyles.label);
                var referenceName = EditorGUI.DelayedTextField(new Rect((rect.x) + (rect.width) / 3 * 2, rect.y, rect.width / 3, EditorGUIUtility.singleLineHeight), entry.referenceName, EditorStyles.label);

                displayName = GetDuplicateSafeDisplayName(entry.id, displayName, reorderableList);
                referenceName = GetDuplicateSafeReferenceName(entry.id, referenceName.ToUpper(), reorderableList);

                if (EditorGUI.EndChangeCheck())
                {
                    entries[index] = new SubgraphDelegateEntry(index + 1, enumName, displayName, referenceName);
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
                return list.count < 20;
            };

            // Can remove
            reorderableList.onCanRemoveCallback = (ReorderableList list) => 
            {  
                return list.count > 1;
            };

            // Add callback delegates
            reorderableList.onSelectCallback += SelectEntry;
            reorderableList.onAddCallback += AddEntry;
            reorderableList.onRemoveCallback += RemoveEntry;
            reorderableList.onReorderCallback += ReorderEntries;
        }

        private void SelectEntry(ReorderableList list)
        {
            if (list == m_ReorderableList_Input)
                m_SelectedIndex_Input = list.index;
            else
                m_SelectedIndex_Output = list.index;
        }

        private void AddEntry(ReorderableList list)
        {
            graph.owner.RegisterCompleteObjectUndo("Add Subgraph Delegate Entry");

            var index = list.list.Count + 1;
            var displayName = GetDuplicateSafeDisplayName(index, "New", list);
            var referenceName = GetDuplicateSafeReferenceName(index, "NEW", list);

            // Add new entry
            if (list == m_ReorderableList_Input)
                m_Delegate.input_Entries.Add(new SubgraphDelegateEntry(index, PropertyType.Vector1, displayName, referenceName));
            else
                m_Delegate.output_Entries.Add(new SubgraphDelegateEntry(index, PropertyType.Vector1, displayName, referenceName));

            // Update GUI
            DirtyNodes();
            Rebuild();
            graph.OnSubgraphDelegateChanged();
            if (list == m_ReorderableList_Input)
                m_SelectedIndex_Input = list.list.Count - 1;
            else
                m_SelectedIndex_Output = list.list.Count - 1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            graph.owner.RegisterCompleteObjectUndo("Remove Subgraph Delegate Entry");

            // Remove entry
            if (list == m_ReorderableList_Input)
                m_SelectedIndex_Input = list.index;
            else
                m_SelectedIndex_Output = list.index;
            var selectedEntry = (SubgraphDelegateEntry)list.list[list.index];

            if (list == m_ReorderableList_Input)
                m_Delegate.input_Entries.Remove(selectedEntry);
            else
                m_Delegate.output_Entries.Remove(selectedEntry);

            DirtyNodes();
            Rebuild();
            graph.OnSubgraphDelegateChanged();
        }

        private void ReorderEntries(ReorderableList list)
        {
            DirtyNodes();
        }

        public string GetDuplicateSafeDisplayName(int id, string name, ReorderableList list)
        {
            name = name.Trim();
            var entryListIn = m_ReorderableList_Input.list as List<SubgraphDelegateEntry>;
            var entryListOut = m_ReorderableList_Output.list as List<SubgraphDelegateEntry>;
            var matchingIn = entryListIn.Where(p => p.id != id || list != m_ReorderableList_Input);
            var matchingOut = entryListOut.Where(p => p.id != id || list != m_ReorderableList_Output);
            var matchingString = matchingIn.Concat<SubgraphDelegateEntry>(matchingOut).Select(p => p.displayName);
            return GraphUtil.SanitizeName(matchingString, "{0} ({1})", name);
        }

        public string GetDuplicateSafeReferenceName(int id, string name, ReorderableList list)
        {
            name = name.Trim();
            name = Regex.Replace(name, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");
            var entryListIn = m_ReorderableList_Input.list as List<SubgraphDelegateEntry>;
            var entryListOut = m_ReorderableList_Output.list as List<SubgraphDelegateEntry>;
            var matchingIn = entryListIn.Where(p => p.id != id || list != m_ReorderableList_Input);
            var matchingOut = entryListOut.Where(p => p.id != id || list != m_ReorderableList_Output);
            var matchingString = matchingIn.Concat<SubgraphDelegateEntry>(matchingOut).Select(p => p.referenceName);
            return GraphUtil.SanitizeName(matchingString, "{0}_{1}", name);
        }

        public override void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in graph.GetNodes<SubgraphDelegateNode>())
            {
                node.UpdateNode();
                node.Dirty(modificationScope);
            }

            // Cant determine if Sub Graphs contain the subgraph delegates so just update them
            foreach (var node in graph.GetNodes<SubGraphNode>())
            {
                node.Dirty(modificationScope);
            }
        }
    }
}
