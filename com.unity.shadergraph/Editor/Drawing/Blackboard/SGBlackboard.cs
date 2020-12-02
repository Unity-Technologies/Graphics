using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Views
{
    class SGBlackboard : GraphSubWindow, ISelection
    {
        private Button m_AddButton;
        private Dragger m_Dragger;
        VisualElement m_ScrollBoundaryTop;
        VisualElement m_ScrollBoundaryBottom;

        bool m_scrollToTop = false;
        bool m_scrollToBottom = false;
        bool m_IsFieldBeingDragged = false;

        protected override string windowTitle => "Blackboard";
        protected override string elementName => "SGBlackboard";
        protected override string styleName => "Blackboard";
        protected override string UxmlName => "GraphView/Blackboard";
        protected override string layoutKey => "UnityEditor.ShaderGraph.Blackboard";

        public Action<SGBlackboard> addItemRequested { get; set; }
        public Action<SGBlackboard, int, VisualElement> moveItemRequested { get; set; }
        public Action<SGBlackboard, VisualElement, string> editTextRequested { get; set; }

        public SGBlackboard(GraphView associatedGraphView) : base(associatedGraphView)
        {
            windowDockingLayout.dockingLeft = true;

            m_AddButton = m_MainContainer.Q(name: "addButton") as Button;
            m_AddButton.clickable.clicked += () => {
                if (addItemRequested != null)
                {
                    addItemRequested(this);
                }
            };

            m_ScrollBoundaryTop = m_MainContainer.Q(name: "scrollBoundaryTop");
            m_ScrollBoundaryTop.RegisterCallback<MouseEnterEvent>(ScrollRegionTopEnter);
            m_ScrollBoundaryTop.RegisterCallback<MouseLeaveEvent>(ScrollRegionTopLeave);

            m_ScrollBoundaryBottom = m_MainContainer.Q(name: "scrollBoundaryBottom");
            m_ScrollBoundaryBottom.RegisterCallback<MouseEnterEvent>(ScrollRegionBottomEnter);
            m_ScrollBoundaryBottom.RegisterCallback<MouseLeaveEvent>(ScrollRegionBottomLeave);


            isWindowScrollable = true;
            isWindowResizable = true;

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            focusable = true;
        }

        void ScrollRegionTopEnter(MouseEnterEvent mouseEnterEvent)
        {
            Debug.Log("Top Enter!");
            if (m_IsFieldBeingDragged)
            {
                m_scrollToTop = true;
            }
        }

        void ScrollRegionTopLeave(MouseLeaveEvent mouseLeaveEvent)
        {
            Debug.Log("Top Exit!");
            if (m_IsFieldBeingDragged)
            {
                m_scrollToTop = false;
            }
        }

        void ScrollRegionBottomEnter(MouseEnterEvent mouseEnterEvent)
        {
            Debug.Log("Bottom Enter!");
            if (m_IsFieldBeingDragged)
            {
                m_scrollToBottom = true;
            }
        }

        void ScrollRegionBottomLeave(MouseLeaveEvent mouseLeaveEvent)
        {
            Debug.Log("Bottom Exit!");
            if (m_IsFieldBeingDragged)
            {
                m_scrollToBottom = false;
            }
        }

        public void OnFieldDragStart(DragEnterEvent dragStartEvent)
        {
            m_IsFieldBeingDragged = true;
        }

        public void OnFieldDragUpdate(DragUpdatedEvent dragUpdatedEvent)
        {
            if (m_scrollToTop)
            {
                m_ScrollView.scrollOffset = new Vector2(m_ScrollView.scrollOffset.x, Mathf.Clamp01(m_ScrollView.scrollOffset.y - 0.01f));
            }
            else if (m_scrollToBottom)
            {
                m_ScrollView.scrollOffset = new Vector2(m_ScrollView.scrollOffset.x, Mathf.Clamp01(m_ScrollView.scrollOffset.y + 0.01f));
            }
        }

        public void OnFieldDragEnd(DragExitedEvent dragEndEvent)
        {
            m_IsFieldBeingDragged = false;
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

        private void OnValidateCommand(ValidateCommandEvent evt)
        {
            //graphView?.OnValidateCommand(evt);
        }

        private void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            //graphView?.OnExecuteCommand(evt);
        }
    }
}
