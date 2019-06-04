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

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardFieldKeywordView : VisualElement
    {
        readonly GraphData m_Graph;
        readonly ShaderKeyword m_Keyword;

        readonly BlackboardField m_BlackboardField;
        List<VisualElement> m_Rows;
        int m_UndoGroup = -1;
        
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
