using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldKeywordView : BlackboardFieldView
    {
        private ReorderableList m_ReorderableList;
        private IMGUIContainer m_Container;
        private int m_SelectedIndex;

        private ShaderKeyword m_Keyword;

        bool reorderableListInHierarchy => m_Keyword.keywordType == ShaderKeywordType.Enum && m_Keyword.isEditable;

        public BlackboardFieldKeywordView(BlackboardField blackboardField, GraphData graph, ShaderInput input)
            : base (blackboardField, graph, input)
        {
        }

        public override void BuildCustomFields(ShaderInput input)
        {
            m_Keyword = input as ShaderKeyword;
            if(m_Keyword == null)
                return;
            
            if(m_Keyword.isEditable)
            {
                // KeywordDefinition
                var keywordDefinitionField = new EnumField((Enum)m_Keyword.keywordDefinition);
                keywordDefinitionField.RegisterValueChangedCallback(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                    if (m_Keyword.keywordDefinition == (ShaderKeywordDefinition)evt.newValue)
                        return;
                    m_Keyword.keywordDefinition = (ShaderKeywordDefinition)evt.newValue;

                    if(reorderableListInHierarchy)
                        Remove(m_Container);

                    Rebuild();
                });
                AddRow("Definition", keywordDefinitionField);

                // KeywordScope
                if(m_Keyword.keywordDefinition != ShaderKeywordDefinition.Predefined)
                {
                    var keywordScopeField = new EnumField((Enum)m_Keyword.keywordScope);
                    keywordScopeField.RegisterValueChangedCallback(evt =>
                    {
                        graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                        if (m_Keyword.keywordScope == (ShaderKeywordScope)evt.newValue)
                            return;
                        m_Keyword.keywordScope = (ShaderKeywordScope)evt.newValue;
                    });
                    AddRow("Scope", keywordScopeField);
                }
            }

            switch(m_Keyword.keywordType)
            {
                case ShaderKeywordType.Boolean:
                    BuildBooleanKeywordField(m_Keyword);
                    break;
                case ShaderKeywordType.Enum:
                    BuildEnumKeywordField(m_Keyword);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void BuildBooleanKeywordField(ShaderKeyword keyword)
        {
            var field = new Toggle() { value = keyword.value == 1 };
            field.OnToggleChanged(evt =>
                {
                    graph.owner.RegisterCompleteObjectUndo("Change property value");
                    keyword.value = evt.newValue ? 1 : 0;
                    DirtyNodes();
                });
            AddRow("Default", field);
        }

        void BuildEnumKeywordField(ShaderKeyword keyword)
        {
            var field = new IntegerField { value = keyword.value };
            field.RegisterValueChangedCallback(evt =>
            {
                keyword.value = evt.newValue;
                this.MarkDirtyRepaint();
            });
            field.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                graph.owner.RegisterCompleteObjectUndo("Change Keyword Value");
                int clampedValue = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);
                field.value = clampedValue;
                DirtyNodes();
            });
            AddRow("Default", field);

            // Entries
            if(reorderableListInHierarchy)
            {
                m_Container = new IMGUIContainer(() => OnGUIHandler ()) { name = "ListContainer" };
                Add(m_Container);
            }
        }

        private void OnGUIHandler()
        {
            // TODO: Add this back?
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
            m_ReorderableList = new ReorderableList(m_Keyword.entries, typeof(ShaderKeywordEntry), true, true, true, true);
        }

        private void AddCallbacks() 
        {
            // Draw Header      
            m_ReorderableList.drawHeaderCallback = (Rect rect) => 
            {  
                var labelRect = new Rect(rect.x, rect.y, rect.width-10, rect.height);
                EditorGUI.LabelField(labelRect, "Entries");
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
            {
                ShaderKeywordEntry entry = ((ShaderKeywordEntry)m_ReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();
                
                var displayName = EditorGUI.DelayedTextField( new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.displayName, EditorStyles.label);
                var referenceName = EditorGUI.DelayedTextField( new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.referenceName, EditorStyles.label);
                
                if(EditorGUI.EndChangeCheck())
                {
                    m_Keyword.entries[index] = new ShaderKeywordEntry(displayName, referenceName);
                    DirtyNodes();
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
                return list.count < 9;
            };

            // Can remove
            m_ReorderableList.onCanRemoveCallback = (ReorderableList list) => 
            {  
                return list.count > 2;
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

            // Add new entry
            m_Keyword.entries.Add(new ShaderKeywordEntry("New", "_NEW"));

            // Update GUI
            DirtyNodes();
            m_SelectedIndex = list.list.Count - 1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            graph.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (ShaderKeywordEntry)m_ReorderableList.list[list.index];
            m_Keyword.entries.Remove(selectedEntry);

            // Update GUI
            DirtyNodes();
        }

        private void ReorderEntries(ReorderableList list)
        {
            DirtyNodes();
        }

        public override void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in graph.GetNodes<KeywordNode>())
                node.UpdateNode();
        }
    }
}
