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
    class BlackboardFieldKeywordView : VisualElement
    {
        readonly GraphData m_Graph;
        ShaderKeyword m_Keyword;

        readonly BlackboardField m_BlackboardField;
        List<VisualElement> m_Rows;

        private ReorderableList m_ReorderableList;
        private IMGUIContainer m_Container;
        private int m_SelectedIndex = -1;
        
        public BlackboardFieldKeywordView(BlackboardField blackboardField, GraphData graph, ShaderKeyword keyword)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/ShaderGraphBlackboard"));
            
            m_Graph = graph;
            m_Keyword = keyword;
            m_BlackboardField = blackboardField;
            m_Rows = new List<VisualElement>();

            BuildFields(keyword);
            AddToClassList("sgblackboardFieldView");
        }

        private void BuildFields(ShaderKeyword keyword)
        {
            // KeywordType
            if(keyword.isEditable)
            {
                var keywordTypeField = new EnumField((Enum)keyword.keywordType);
                keywordTypeField.RegisterValueChangedCallback(evt =>
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                    if (keyword.keywordType == (ShaderKeywordType)evt.newValue)
                        return;
                    keyword.keywordType = (ShaderKeywordType)evt.newValue;
                    RemoveAllElements();
                    BuildFields(keyword);
                    this.MarkDirtyRepaint();
                });
                AddRow("Type", keywordTypeField);
            }
            
            // KeywordScope
            if(keyword.isEditable)
            {
                if(keyword.keywordType != ShaderKeywordType.Predefined)
                {
                    var keywordScopeField = new EnumField((Enum)keyword.keywordScope);
                    keywordScopeField.RegisterValueChangedCallback(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                        if (keyword.keywordScope == (ShaderKeywordScope)evt.newValue)
                            return;
                        keyword.keywordScope = (ShaderKeywordScope)evt.newValue;
                    });
                    AddRow("Scope", keywordScopeField);
                }
            }

            // Exposed
            if(!m_Graph.isSubGraph)
            { 
                if(keyword.isExposable)
                {
                    var exposedToogle = new Toggle();
                    exposedToogle.OnToggleChanged(evt =>
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Change Exposed Toggle");
                        keyword.generatePropertyBlock = evt.newValue;
                        if (keyword.generatePropertyBlock)
                        {
                            m_BlackboardField.icon = BlackboardProvider.exposedIcon;
                        }
                        else
                        {
                            m_BlackboardField.icon = null;
                        }
                        DirtyNodes(ModificationScope.Graph);
                    });
                    exposedToogle.value = keyword.generatePropertyBlock;
                    AddRow("Exposed", exposedToogle);
                }
            }

            // Entries
            if(keyword.isEditable)
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

            // Can remove
            m_ReorderableList.onCanRemoveCallback = (ReorderableList list) => 
            {  
                return list.count > 1;
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
            m_Graph.owner.RegisterCompleteObjectUndo("Add Keyword Entry");

            // Add new entry
            m_Keyword.entries.Add(new ShaderKeywordEntry("New", "_NEW"));

            // Update GUI
            DirtyNodes();
            m_SelectedIndex = list.list.Count - 1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (ShaderKeywordEntry)m_ReorderableList.list[list.index];
            m_Keyword.entries.Remove(selectedEntry);

            // Update GUI
            DirtyNodes();
            // ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        private void ReorderEntries(ReorderableList list)
        {
            // m_Graph.owner.RegisterCompleteObjectUndo("Reorder Keyword Entries");
            
            // // Update entry list
            // m_Keyword.entries = (List<ShaderKeywordEntry>)m_ReorderableList.list;

            // // Update GUI
            RecreateList();
            DirtyNodes();
        }

        VisualElement CreateRow(string labelText, VisualElement control)
        {
            VisualElement rowView = new VisualElement();
            Label label = new Label(labelText);

            rowView.Add(label);
            rowView.Add(control);

            rowView.AddToClassList("rowView");
            label.AddToClassList("rowViewLabel");
            control.AddToClassList("rowViewControl");

            return rowView;
        }

        VisualElement AddRow(string labelText, VisualElement control)
        {
            VisualElement rowView = CreateRow(labelText, control);
            Add(rowView);
            m_Rows.Add(rowView);
            return rowView;
        }

        void RemoveAllElements()
        {
            for (int i = 0; i < m_Rows.Count; i++)
            {
                if (m_Rows[i].parent == this)
                    Remove(m_Rows[i]);
            }
            Remove(m_Container);
        }

        void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in m_Graph.GetNodes<KeywordNode>())
                node.UpdateNode();
        }
    }
}
