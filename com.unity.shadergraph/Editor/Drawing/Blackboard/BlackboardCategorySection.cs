using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardCateogrySection : BlackboardSection
    {
        InputCategory m_Category;
        GraphData m_Graph; // TODO: y are we sure about this?

        public BlackboardCateogrySection(InputCategory category, GraphData graph) : base()
        {
            m_Category = category;
            m_Graph = graph;
            title = m_Category.header;

            // TODO: y why is this necessary?
            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            RefreshSection();
        }

        public void RefreshSection()
        {
            Clear();
            foreach (ShaderInput input in m_Category.inputs)
            {
                AddDisplayedInputRow(input);
            }
        }

        public void AddDisplayedInputRow(ShaderInput input)
        {
            // TODO: z double check that things cannot be added twice
//            if (m_InputRows.ContainsKey(input.guid))
//                return;

            BlackboardField field = null;
            BlackboardRow row = null;

            switch(input)
            {
                case AbstractShaderProperty property:
                {
                    var icon = (m_Graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    field = new BlackboardField(icon, property.displayName, property.propertyType.ToString()) { userData = property };
                    var propertyView = new BlackboardFieldPropertyView(field, m_Graph, property);
                    row = new BlackboardRow(field, propertyView) { userData = input };

                    break;
                }
                case ShaderKeyword keyword:
                {
                    var icon = (m_Graph.isSubGraph || (keyword.isExposable && keyword.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    var typeText = KeywordUtil.IsBuiltinKeyword(keyword) ? "Built-in Keyword" : keyword.keywordType.ToString();
                    field = new BlackboardField(icon, keyword.displayName, typeText) { userData = keyword };
                    var keywordView = new BlackboardFieldKeywordView(field, m_Graph, keyword);
                    row = new BlackboardRow(field, keywordView);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            this.Add(row);

            // TODO: z
//            var pill = row.Q<Pill>();
//            pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
//            pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
//            pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
//
//            var expandButton = row.Q<Button>("expandButton");
//            expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, input), TrickleDown.TrickleDown);
//
//            m_InputRows[input.guid] = row;
//            m_InputRows[input.guid].expanded = SessionState.GetBool(input.guid.ToString(), true);
        }

        #region DropdownMenu
        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }
        #endregion
    }
}
