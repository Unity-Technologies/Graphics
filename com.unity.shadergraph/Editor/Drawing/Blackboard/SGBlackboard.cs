using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Views.Blackboard
{
    class SGBlackboard : GraphSubWindow, ISelection
    {
        VisualElement m_ScrollBoundaryTop;
        VisualElement m_ScrollBoundaryBottom;
        VisualElement m_BottomResizer;

        bool m_scrollToTop = false;
        bool m_scrollToBottom = false;
        bool m_IsFieldBeingDragged = false;

        const int k_DraggedPropertyScrollSpeed = 6;

        public override string windowTitle => "Blackboard";
        public override string elementName => "SGBlackboard";
        public override string styleName => "Blackboard";
        public override string UxmlName => "GraphView/Blackboard";
        public override string layoutKey => "UnityEditor.ShaderGraph.Blackboard";

        public Action<SGBlackboard> addItemRequested { get; set; }
        public Action<SGBlackboard, int, VisualElement> moveItemRequested { get; set; }

        public SGBlackboard(GraphView associatedGraphView) : base(associatedGraphView)
        {
            windowDockingLayout.dockingLeft = true;

            var addButton = m_MainContainer.Q(name: "addButton") as Button;
            addButton.clickable.clicked += () => {
                if (addItemRequested != null)
                {
                    addItemRequested(this);
                }
            };

            associatedGraphView.RegisterCallback<FocusOutEvent>(evt => HideScrollBoundaryRegions());

            // These callbacks make sure the scroll boundary regions don't show up user is not dragging/dropping properties
            this.RegisterCallback<MouseUpEvent>((evt => HideScrollBoundaryRegions()));
            this.RegisterCallback<DragExitedEvent>(evt => HideScrollBoundaryRegions());

            m_ScrollBoundaryTop = m_MainContainer.Q(name: "scrollBoundaryTop");
            m_ScrollBoundaryTop.RegisterCallback<MouseEnterEvent>(ScrollRegionTopEnter);
            m_ScrollBoundaryTop.RegisterCallback<DragUpdatedEvent>(OnFieldDragUpdate);
            m_ScrollBoundaryTop.RegisterCallback<MouseLeaveEvent>(ScrollRegionTopLeave);

            m_ScrollBoundaryBottom = m_MainContainer.Q(name: "scrollBoundaryBottom");
            m_ScrollBoundaryBottom.RegisterCallback<MouseEnterEvent>(ScrollRegionBottomEnter);
            m_ScrollBoundaryBottom.RegisterCallback<DragUpdatedEvent>(OnFieldDragUpdate);
            m_ScrollBoundaryBottom.RegisterCallback<MouseLeaveEvent>(ScrollRegionBottomLeave);

            m_BottomResizer = m_MainContainer.Q("bottom-resize");

            HideScrollBoundaryRegions();

            // Sets delegate association so scroll boundary regions are hidden when a blackboard property is dropped into graph
            if (associatedGraphView is MaterialGraphView materialGraphView)
                materialGraphView.blackboardFieldDropDelegate = HideScrollBoundaryRegions;

            isWindowScrollable = true;
            isWindowResizable = true;
            focusable = true;
        }

        public void ShowScrollBoundaryRegions()
        {
            if (!m_IsFieldBeingDragged && scrollableHeight > 0)
            {
                // Interferes with scrolling functionality of properties with the bottom scroll boundary
                m_BottomResizer.style.visibility = Visibility.Hidden;

                m_IsFieldBeingDragged = true;
                var contentElement = m_MainContainer.Q(name: "content");
                scrollViewIndex = contentElement.IndexOf(m_ScrollView);
                contentElement.Insert(scrollViewIndex, m_ScrollBoundaryTop);
                scrollViewIndex = contentElement.IndexOf(m_ScrollView);
                contentElement.Insert(scrollViewIndex + 1, m_ScrollBoundaryBottom);
            }
        }

        public void HideScrollBoundaryRegions()
        {
            m_BottomResizer.style.visibility = Visibility.Visible;
            m_IsFieldBeingDragged = false;
            m_ScrollBoundaryTop.RemoveFromHierarchy();
            m_ScrollBoundaryBottom.RemoveFromHierarchy();
        }

        int scrollViewIndex { get; set; }

        void ScrollRegionTopEnter(MouseEnterEvent mouseEnterEvent)
        {
            if (m_IsFieldBeingDragged)
            {
                m_scrollToTop = true;
                m_scrollToBottom = false;
            }
        }

        void ScrollRegionTopLeave(MouseLeaveEvent mouseLeaveEvent)
        {
            if (m_IsFieldBeingDragged)
                m_scrollToTop = false;
        }

        void ScrollRegionBottomEnter(MouseEnterEvent mouseEnterEvent)
        {
            if (m_IsFieldBeingDragged)
            {
                m_scrollToBottom = true;
                m_scrollToTop = false;
            }
        }

        void ScrollRegionBottomLeave(MouseLeaveEvent mouseLeaveEvent)
        {
            if (m_IsFieldBeingDragged)
                m_scrollToBottom = false;
        }

        void OnFieldDragUpdate(DragUpdatedEvent dragUpdatedEvent)
        {
            if (m_scrollToTop)
                m_ScrollView.scrollOffset = new Vector2(m_ScrollView.scrollOffset.x, Mathf.Clamp(m_ScrollView.scrollOffset.y - k_DraggedPropertyScrollSpeed, 0, scrollableHeight));
            else if (m_scrollToBottom)
                m_ScrollView.scrollOffset = new Vector2(m_ScrollView.scrollOffset.x, Mathf.Clamp(m_ScrollView.scrollOffset.y + k_DraggedPropertyScrollSpeed, 0, scrollableHeight));
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
    }
}
