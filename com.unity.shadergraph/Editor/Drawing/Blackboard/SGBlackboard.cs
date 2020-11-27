using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Views
{
    class SGBlackboard : GraphSubWindow, ISelection
    {
        private Button m_AddButton;
        private bool m_Scrollable = true;

        private Dragger m_Dragger;
        private GraphView m_GraphView;
        public GraphView graphView
        {
            get
            {
                if (!windowed && m_GraphView == null)
                    m_GraphView = GetFirstAncestorOfType<GraphView>();
                return m_GraphView;
            }

            set
            {
                if (!windowed)
                    return;
                m_GraphView = value;
            }
        }

        internal static readonly string StyleSheetPath = "StyleSheets/GraphView/Blackboard.uss";

        public Action<SGBlackboard> addItemRequested { get; set; }
        public Action<SGBlackboard, int, VisualElement> moveItemRequested { get; set; }
        public Action<SGBlackboard, VisualElement, string> editTextRequested { get; set; }

        // ISelection implementation
        public List<ISelectable> selection
        {
            get
            {
                return graphView?.selection;
            }
        }

        public SGBlackboard(GraphView associatedGraphView = null)
        {
            var tpl = EditorGUIUtility.Load("UXML/GraphView/Blackboard.uxml") as VisualTreeAsset;
            this.AddStyleSheetFromPath(StyleSheetPath);

            m_MainContainer = tpl.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");

            m_Root = m_MainContainer.Q("content");

            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("blackboardHeader");

            m_AddButton = m_MainContainer.Q(name: "addButton") as Button;
            m_AddButton.clickable.clicked += () => {
                if (addItemRequested != null)
                {
                    addItemRequested(this);
                }
            };

            m_TitleLabel = m_MainContainer.Q<Label>(name: "titleLabel");
            m_SubTitleLabel = m_MainContainer.Q<Label>(name: "subTitleLabel");
            m_ContentContainer = m_MainContainer.Q(name: "contentContainer");

            hierarchy.Add(m_MainContainer);

            capabilities |= Capabilities.Movable | Capabilities.Resizable;
            style.overflow = Overflow.Hidden;

            ClearClassList();
            AddToClassList("blackboard");

            m_Dragger = new Dragger { clampToParentEdges = true };
            this.AddManipulator(m_Dragger);

            m_Scrollable = true;

            hierarchy.Add(new Resizer());

            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });

            // event interception to prevent GraphView manipulators from being triggered
            // when working with the blackboard

            // prevent Zoomer manipulator
            RegisterCallback<WheelEvent>(e =>
            {
                e.StopPropagation();
            });

            RegisterCallback<MouseDownEvent>(e =>
            {
                if (e.button == (int)MouseButton.LeftMouse)
                    ClearSelection();
                // prevent ContentDragger manipulator
                e.StopPropagation();
            });

            //RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            //RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            focusable = true;
        }

        public virtual void AddToSelection(ISelectable selectable)
        {
            graphView?.AddToSelection(selectable);
        }

        public virtual void RemoveFromSelection(ISelectable selectable)
        {
            graphView?.RemoveFromSelection(selectable);
        }

        public virtual void ClearSelection()
        {
            graphView?.ClearSelection();
        }

        /*private void OnValidateCommand(ValidateCommandEvent evt)
        {
            graphView?.OnValidateCommand(evt);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            graphView?.OnExecuteCommand(evt);
        }*/
    }
}
