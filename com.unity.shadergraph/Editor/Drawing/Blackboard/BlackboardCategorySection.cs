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

        readonly Dictionary<Guid, BlackboardRow> m_InputRows;
        List<Node> m_SelectedNodes = new List<Node>();
        Dictionary<ShaderInput, bool> m_ExpandedInputs = new Dictionary<ShaderInput, bool>();

        // TODO: x how to get blackboard?
        internal BlackboardCateogrySection(InputCategory category) : base()
        {
            m_Category = category;
            title = m_Category.header;

            m_InputRows = new Dictionary<Guid, BlackboardRow>();

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        internal int GetIndexWithinBlackboard()
        {
            return parent.IndexOf(this);
        }

        internal void RebuildSection()
        {
            Clear();
            m_InputRows.Clear();

//            if (!m_Category.expanded)
//                return;

            foreach (ShaderInput input in m_Category.inputs)
            {
                AddDisplayedInputRow(input);
            }
        }

        internal void AddDisplayedInputRow(ShaderInput input)
        {
            Debug.Log("AddDisplayedInputRow() " + m_Category.header + " for " + input.displayName + " \n\n" + Environment.StackTrace.ToString());

            if (m_InputRows.ContainsKey(input.guid))
            {
                Debug.Log("BlackboardCategorySection: AddDisplayedInputRow() m_InputRows.ContainsKey(input.guid)");
                return;
            }

            Blackboard blackboard = GetFirstAncestorOfType<Blackboard>();
            if (blackboard == null)
            {
                Debug.Log("null Blackboard" + (GetFirstAncestorOfType<Blackboard>() == null ? " also null blackboard" : ""));
                return;
            }
            MaterialGraphView graphView = blackboard.GetFirstAncestorOfType<MaterialGraphView>();
            if (graphView == null)
            {
                Debug.Log("null MaterialGraphView" + (GetFirstAncestorOfType<Blackboard>() == null ? " also null blackboard" : ""));
                return;
            }
            GraphData graph = graphView.graph;
            if (graph == null)
            {
                Debug.Log("null GraphData");
                return;
            }

            BlackboardField field = null;
            BlackboardRow row = null;

            switch(input)
            {
                case AbstractShaderProperty property:
                {
                    var icon = (graph.isSubGraph || (property.isExposable && property.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    field = new BlackboardField(null, property.displayName, property.propertyType.ToString()) { userData = property };
                    var propertyView = new BlackboardFieldPropertyView(field, graph, property);
                    row = new BlackboardRow(field, propertyView) { userData = input };

                    break;
                }
                case ShaderKeyword keyword:
                {
                    var icon = (graph.isSubGraph || (keyword.isExposable && keyword.generatePropertyBlock)) ? BlackboardProvider.exposedIcon : null;
                    var typeText = KeywordUtil.IsBuiltinKeyword(keyword) ? "Built-in Keyword" : keyword.keywordType.ToString();
                    field = new BlackboardField(null, keyword.displayName, typeText) { userData = keyword };
                    var keywordView = new BlackboardFieldKeywordView(field, graph, keyword);
                    row = new BlackboardRow(field, keywordView);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Add(row);

            var pill = row.Q<Pill>();
            pill.RegisterCallback<MouseEnterEvent>(evt => OnMouseHover(evt, input));
            pill.RegisterCallback<MouseLeaveEvent>(evt => OnMouseHover(evt, input));
            pill.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);

            var expandButton = row.Q<Button>("expandButton");
            expandButton.RegisterCallback<MouseDownEvent>(evt => OnExpanded(evt, input), TrickleDown.TrickleDown);

            m_InputRows[input.guid] = row;
            m_InputRows[input.guid].expanded = SessionState.GetBool(input.guid.ToString(), true);
            
            Debug.Log("I currenlty have ... " + m_InputRows.Count());
        }

        void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            if (m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }

        public BlackboardRow GetBlackboardRow(Guid guid)
        {
            return m_InputRows[guid];
        }

        void OnMouseHover(EventBase evt, ShaderInput input)
        {
            var graphView = GetFirstAncestorOfType<MaterialGraphView>();
            if (evt.eventTypeId == MouseEnterEvent.TypeId())
            {
                foreach (var node in graphView.nodes.ToList())
                {
                    if(input is AbstractShaderProperty property)
                    {
                        if (node.userData is PropertyNode propertyNode)
                        {
                            if (propertyNode.propertyGuid == input.guid)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                    else if(input is ShaderKeyword keyword)
                    {
                        if (node.userData is KeywordNode keywordNode)
                        {
                            if (keywordNode.keywordGuid == input.guid)
                            {
                                m_SelectedNodes.Add(node);
                                node.AddToClassList("hovered");
                            }
                        }
                    }
                }
            }
            else if (evt.eventTypeId == MouseLeaveEvent.TypeId() && m_SelectedNodes.Any())
            {
                foreach (var node in m_SelectedNodes)
                {
                    node.RemoveFromClassList("hovered");
                }
                m_SelectedNodes.Clear();
            }
        }

        void OnExpanded(MouseDownEvent evt, ShaderInput input)
        {
            m_ExpandedInputs[input] = !m_InputRows[input.guid].expanded;
        }

        // TODO: Troll test method to test if things are working, since we cannot allow custom Edit Text field to work with GV changes or custom Graph Elements
        private readonly static string[] randomNames =
        {
            null,
            "Textures",
            "Lovely",
            "Vectors",
            "Random",
            "Stuff",
            "Whatever",
            "Dog",
            "Taco"
        };
        internal static string GetRandomTrollName()
        {
            string finalName = "";
            int l = randomNames.Length;
            finalName += randomNames[(int)UnityEngine.Random.Range(1.0f, (float)l)];
            string s2 = randomNames[(int)UnityEngine.Random.Range(0.0f, (float)l)];
            if (s2 != null)
                finalName += " " + s2;
            finalName += " " + UnityEngine.Random.Range(0.0f, 100.0f).ToString("0.0");

            Debug.Log("Graph View doesn't allow Blackboard Section's text to be altered, may need a new Graph Element. Generating random name :)   " + finalName);
            return finalName;
        }

        #region DropdownMenu
        void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }
        #endregion
    }
}
