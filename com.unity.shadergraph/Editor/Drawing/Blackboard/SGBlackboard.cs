using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing
{
    class BlackboardGroupInfo
    {
        [SerializeField]
        SerializableGuid m_Guid = new SerializableGuid();

        internal Guid guid => m_Guid.guid;

        [SerializeField]
        string m_GroupName;

        internal string GroupName
        {
            get => m_GroupName;
            set => m_GroupName = value;
        }

        BlackboardGroupInfo()
        {

        }
    }

    class SGBlackboard : GraphSubWindow, ISGControlledElement<BlackboardController>
    {
        VisualElement m_ScrollBoundaryTop;
        VisualElement m_ScrollBoundaryBottom;
        VisualElement m_BottomResizer;


        // --- Begin ISGControlledElement implementation
        public void OnControllerChanged(ref SGControllerChangedEvent e)
        {

        }

        public void OnControllerEvent(SGControllerEvent e)
        {

        }

        public BlackboardController controller
        {
            get => m_Controller;
            set
            {
                if (m_Controller != value)
                {
                    if (m_Controller != null)
                    {
                        m_Controller.UnregisterHandler(this);
                    }
                    Clear();
                    m_Controller = value;

                    if (m_Controller != null)
                    {
                        m_Controller.RegisterHandler(this);
                    }
                }
            }
        }
        // --- ISGControlledElement implementation

        BlackboardController m_Controller;

        BlackboardViewModel m_ViewModel;

        BlackboardViewModel ViewModel
        {
            get => m_ViewModel;
            set => m_ViewModel = value;
        }

        readonly SGBlackboardSection m_DefaultPropertySection;
        readonly SGBlackboardSection m_DefaultKeywordSection;

        // List of user-made blackboard sections
        IList<SGBlackboardSection> m_BlackboardSections = new List<SGBlackboardSection>();

        bool m_scrollToTop = false;
        bool m_scrollToBottom = false;
        bool m_IsFieldBeingDragged = false;

        const int k_DraggedPropertyScrollSpeed = 6;

        public override string windowTitle => "Blackboard";
        public override string elementName => "SGBlackboard";
        public override string styleName => "Blackboard";
        public override string UxmlName => "GraphView/Blackboard";
        public override string layoutKey => "UnityEditor.ShaderGraph.Blackboard";

        public Action addItemRequested { get; set; }
        public Action<int, VisualElement> moveItemRequested { get; set; }

        GenericMenu m_AddPropertyMenu;

        public SGBlackboard(BlackboardViewModel viewModel) : base(viewModel)
        {
            ViewModel = viewModel;

            InitializeAddPropertyMenu();

            // By default dock blackboard to left of graph window
            windowDockingLayout.dockingLeft = true;

            if (m_MainContainer.Q(name: "addButton") is Button addButton)
                addButton.clickable.clicked += () =>
                {
                    addItemRequested?.Invoke();
                    ShowAddPropertyMenu();
                };

            ParentView.RegisterCallback<FocusOutEvent>(evt => HideScrollBoundaryRegions());

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
            if (ParentView is MaterialGraphView materialGraphView)
                materialGraphView.blackboardFieldDropDelegate = HideScrollBoundaryRegions;

            isWindowScrollable = true;
            isWindowResizable = true;
            focusable = true;

            // Want to retain properties and keywords UI, but need to iterate through the GroupInfos, and create sections for each of those
            // Then for each section, add the corresponding properties and keywords based on their GUIDs
            m_DefaultPropertySection =  this.Q<SGBlackboardSection>("propertySection");
            m_DefaultKeywordSection = this.Q<SGBlackboardSection>("keywordSection");

            // TODO: Need to create a PropertyViewModel that is used to drive a BlackboardRow/FieldView
            // Also, given how similar the NodeView and FieldView are, would be awesome if we could just unify the two and get rid of FieldView
            // Then would theoretically also get the ability to connect properties from blackboard to node inputs directly
            // (could handle in controller to create a new PropertyNodeView and connect that instead)
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

        public void HideAllDragIndicators()
        {

        }

        void InitializeAddPropertyMenu()
        {
            m_AddPropertyMenu = new GenericMenu();

            if (ViewModel == null)
            {
                Debug.Log("ERROR: SGBlackboard: View Model is null.");
                return;
            }

            foreach (var nameToAddActionTuple in ViewModel.PropertyNameToAddActionMap)
            {
                string propertyName = nameToAddActionTuple.Key;
                IGraphDataAction addAction = nameToAddActionTuple.Value;
                m_AddPropertyMenu.AddItem(new GUIContent(propertyName), false, ()=> addAction.ModifyGraphDataAction(ViewModel.Model));
            }
            m_AddPropertyMenu.AddSeparator($"/");

            foreach (var nameToAddActionTuple in ViewModel.DefaultKeywordNameToAddActionMap)
            {
                string defaultKeywordName = nameToAddActionTuple.Key;
                IGraphDataAction addAction = nameToAddActionTuple.Value;
                m_AddPropertyMenu.AddItem(new GUIContent($"Keyword/{defaultKeywordName}"), false, ()=> addAction.ModifyGraphDataAction(ViewModel.Model));
            }
            m_AddPropertyMenu.AddSeparator($"Keyword/");

            foreach (var nameToAddActionTuple in ViewModel.BuiltInKeywordNameToAddActionMap)
            {
                string builtInKeywordName = nameToAddActionTuple.Key;
                IGraphDataAction addAction = nameToAddActionTuple.Value;
                m_AddPropertyMenu.AddItem(new GUIContent($"Keyword/{builtInKeywordName}"), false, ()=> addAction.ModifyGraphDataAction(ViewModel.Model));
            }

            foreach (string disabledKeywordName in ViewModel.DisabledKeywordNameList)
            {
                m_AddPropertyMenu.AddDisabledItem(new GUIContent(disabledKeywordName));
            }
        }

        void ShowAddPropertyMenu()
        {
            m_AddPropertyMenu.ShowAsContext();
        }
    }
}
