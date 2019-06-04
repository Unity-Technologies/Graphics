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
        readonly ShaderKeyword m_Keyword;

        readonly BlackboardField m_BlackboardField;
        List<VisualElement> m_Rows;
        int m_UndoGroup = -1;

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
            
            // KeywordScope
            if(keyword.keywordType != ShaderKeywordType.None)
            {
                var keywordScopeField = new EnumField((Enum)keyword.keywordScope);
                keywordScopeField.RegisterValueChangedCallback(evt =>
                {
                    m_Graph.owner.RegisterCompleteObjectUndo("Change Keyword Type");
                    if (keyword.keywordScope == (ShaderKeywordScope)evt.newValue)
                        return;
                    keyword.keywordScope = (ShaderKeywordScope)evt.newValue;
                    keywordTypeField.MarkDirtyRepaint();
                });
                AddRow("Scope", keywordScopeField);
            }

            // Entries
            m_Container = new IMGUIContainer(() => OnGUIHandler ()) { name = "ListContainer" };
            Add(m_Container);
        }

        private void OnGUIHandler()
        {
            // if(m_ReorderableList == null)
            // {
                RecreateList();
                AddCallbacks();
            // }

            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                m_ReorderableList.index = m_SelectedIndex;
                m_ReorderableList.DoLayoutList();

                if (changeCheckScope.changed)
                    this.MarkDirtyRepaint();
            }
        }

        internal void RecreateList()
        {           
            // Create reorderable list from entries
            m_ReorderableList = new ReorderableList(m_Keyword.entries, typeof(int), true, true, true, true);
            // this.MarkDirtyRepaint();
        }

        private void AddCallbacks() 
        {      
            m_ReorderableList.drawHeaderCallback = (Rect rect) => 
            {  
                var labelRect = new Rect(rect.x, rect.y, rect.width-10, rect.height);
                EditorGUI.LabelField(labelRect, "Entries");
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => 
            {
                KeyValuePair<string, string> entry = ((KeyValuePair<string, string>)m_ReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();
                
                var displayName = EditorGUI.DelayedTextField( new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.Key, EditorStyles.label);
                var referenceName = EditorGUI.DelayedTextField( new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.Value, EditorStyles.label);
                
                if(EditorGUI.EndChangeCheck())
                {
                    m_Keyword.entries[index] = new KeyValuePair<string, string>(displayName, referenceName);
                    RecreateList();
                    DirtyNodes();
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
            m_Graph.owner.RegisterCompleteObjectUndo("Add Keyword Entry");

            // Add new entry
            m_Keyword.entries.Add(new KeyValuePair<string, string>("New", "_NEW"));

            // Update GUI
            RecreateList();
            DirtyNodes();
            m_SelectedIndex = list.list.Count - 1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (KeyValuePair<string, string>)m_ReorderableList.list[m_SelectedIndex];
            m_Keyword.entries.Remove(selectedEntry);

            // Update GUI
            RecreateList();
            DirtyNodes();
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        private void ReorderEntries(ReorderableList list)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Reorder Keyword Entries");
            
            // Update entry list
            m_Keyword.entries = (List<KeyValuePair<string, string>>)m_ReorderableList.list;

            // Update GUI
            RecreateList();
            DirtyNodes();
            ReorderableList.defaultBehaviours.DoRemoveButton(list);
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
        }

        void DirtyNodes(ModificationScope modificationScope = ModificationScope.Node)
        {
            foreach (var node in m_Graph.GetNodes<KeywordNode>())
                node.Dirty(modificationScope);
        }
    }
}
