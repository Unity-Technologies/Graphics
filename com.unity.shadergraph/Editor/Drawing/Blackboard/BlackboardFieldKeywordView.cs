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
    class BlackboardFieldKeywordView : BlackboardFieldView
    {
        private ReorderableList m_ReorderableList;
        private IMGUIContainer m_Container;
        private int m_SelectedIndex;
        private ShaderKeyword m_Keyword;

        public BlackboardFieldKeywordView(BlackboardField blackboardField, GraphData graph, ShaderInput input)
            : base (blackboardField, graph, input)
        {
        }

        public override void BuildCustomFields(ShaderInput input)
        {
            m_Keyword = input as ShaderKeyword;
            if(m_Keyword == null)
                return;

            // KeywordDefinition
            var keywordDefinitionField = new EnumField((Enum)m_Keyword.keywordDefinition);
            keywordDefinitionField.RegisterValueChangedCallback(evt =>
            {
                graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                if (m_Keyword.keywordDefinition == (KeywordDefinition)evt.newValue)
                    return;
                m_Keyword.keywordDefinition = (KeywordDefinition)evt.newValue;
                Rebuild();
            });
            AddRow("Definition", keywordDefinitionField, !m_Keyword.isBuiltIn);

            // KeywordScope
            if(m_Keyword.keywordDefinition != KeywordDefinition.Predefined)
            {
                var keywordScopeField = new EnumField((Enum)m_Keyword.keywordScope);
                keywordScopeField.RegisterValueChangedCallback(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                    if (m_Keyword.keywordScope == (KeywordScope)evt.newValue)
                        return;
                    m_Keyword.keywordScope = (KeywordScope)evt.newValue;
                });
                AddRow("Scope", keywordScopeField, !m_Keyword.isBuiltIn);
            }

            switch(m_Keyword.keywordType)
            {
                case KeywordType.Boolean:
                    BuildBooleanKeywordField(m_Keyword);
                    break;
                case KeywordType.Enum:
                    BuildEnumKeywordField(m_Keyword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void BuildBooleanKeywordField(ShaderKeyword keyword)
        {
            // Default field
            var field = new Toggle() { value = keyword.value == 1 };
            field.OnToggleChanged(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change property value");
                    keyword.value = evt.newValue ? 1 : 0;
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow("Default", field);
        }

        void BuildEnumKeywordField(ShaderKeyword keyword)
        {
            // Clamp value between entry list
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);

            // Default field
            var field = new PopupField<string>(keyword.entries.Select(x => x.displayName).ToList(), value);
            field.RegisterValueChangedCallback(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change Keyword Value");
                    keyword.value = field.index;
                    DirtyNodes(ModificationScope.Graph);
                });
            AddRow("Default", field);

            // Entries
            m_Container = new IMGUIContainer(() => OnGUIHandler ()) { name = "ListContainer" };
            AddRow("Entries", m_Container, !keyword.isBuiltIn);
        }

        public ShaderKeyword keyword => m_Keyword;

        private void OnGUIHandler()
        {
            if(m_ReorderableList == null)
            {
                RecreateList();
                AddCallbacks();
            }

            m_ReorderableList.index = m_SelectedIndex;
            m_ReorderableList.DoLayoutList();
        }

        internal void RecreateList()
        {
            // Create reorderable list from entries
            m_ReorderableList = new ReorderableList(m_Keyword.entries, typeof(KeywordEntry), true, true, true, true);
        }

        private void AddCallbacks()
        {
            // Draw Header
            m_ReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(displayRect, "Display Name");
                var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(referenceRect, "Reference Suffix");
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                KeywordEntry entry = ((KeywordEntry)m_ReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();

                var displayName = EditorGUI.DelayedTextField( new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.displayName, EditorStyles.label);
                var referenceName = EditorGUI.DelayedTextField( new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.referenceName, EditorStyles.label);

                displayName = GetDuplicateSafeDisplayName(entry.id, displayName);
                referenceName = GetDuplicateSafeReferenceName(entry.id, referenceName.ToUpper());

                if (EditorGUI.EndChangeCheck())
                {
                    m_Keyword.entries[index] = new KeywordEntry(entry.id, displayName, referenceName);

                    DirtyNodes();
                    Rebuild();
                }
            };

            // Element height
            m_ReorderableList.elementHeightCallback = (int indexer) =>
            {
                return m_ReorderableList.elementHeight;
            };

            // Can add
            m_ReorderableList.onCanAddCallback = (ReorderableList list) =>
            {
                return list.count < KeywordNode.k_MaxEnumEntries;
            };

            // Can remove
            m_ReorderableList.onCanRemoveCallback = (ReorderableList list) =>
            {
                return list.count > KeywordNode.k_MinEnumEntries;
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
            graph.owner.RegisterCompleteObjectUndo("Add Keyword Entry");

            int index = GetFirstUnusedID();
            if (index <= 0)
                return; // Error has already occured, don't attempt to add this entry.

            var displayName = GetDuplicateSafeDisplayName(index, "New");
            var referenceName = GetDuplicateSafeReferenceName(index, "NEW");

            // Add new entry
            m_Keyword.entries.Add(new KeywordEntry(index, displayName, referenceName));

            // Update Blackboard & Nodes
            DirtyNodes();
            Rebuild();
            graph.OnKeywordChanged();
            m_SelectedIndex = list.list.Count - 1;
        }

        // Allowed indicies are 1-MAX_ENUM_ENTRIES
        private int GetFirstUnusedID()
        {
            List<int> ususedIDs = new List<int>();

            foreach (KeywordEntry keywordEntry in m_Keyword.entries)
            {
                ususedIDs.Add(keywordEntry.id);
            }

            for (int x = 1; x <= KeywordNode.k_MaxEnumEntries; x++)
            {
                if (!ususedIDs.Contains(x))
                    return x;
            }

            Debug.LogError("GetFirstUnusedID: Attempting to get unused ID when all IDs are used.");
            return -1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            graph.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (KeywordEntry)m_ReorderableList.list[list.index];
            m_Keyword.entries.Remove(selectedEntry);

            // Clamp value within new entry range
            int value = Mathf.Clamp(m_Keyword.value, 0, m_Keyword.entries.Count - 1);
            m_Keyword.value = value;

            // Update Blackboard & Nodes
            DirtyNodes();
            Rebuild();
            graph.OnKeywordChanged();
            m_SelectedIndex = m_SelectedIndex >= list.list.Count - 1 ? list.list.Count - 1 : m_SelectedIndex;
        }

        private void ReorderEntries(ReorderableList list)
        {
            DirtyNodes();
        }

        public string GetDuplicateSafeDisplayName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_ReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} ({1})", name);
        }

        public string GetDuplicateSafeReferenceName(int id, string name)
        {
            name = name.Trim();
            name = Regex.Replace(name, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");
            var entryList = m_ReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.referenceName), "{0}_{1}", name);
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
