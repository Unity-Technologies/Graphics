using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;
using ResizableElement = UnityEditor.ShaderGraph.Drawing.ResizableElement;

namespace Drawing.Views
{
    public class GraphSubWindow : GraphElement, ISelection
    {
        protected VisualElement m_MainContainer;
        protected VisualElement m_Root;
        protected Label m_TitleLabel;
        protected Label m_SubTitleLabel;
        protected ScrollView m_ScrollView;
        protected VisualElement m_ContentContainer;
        protected VisualElement m_HeaderItem;

        private bool m_Scrollable = false;

        private Dragger m_Dragger;
        protected GraphView m_GraphView;

        WindowDockingLayout m_Layout;
        WindowDockingLayout m_DefaultLayout = new WindowDockingLayout
        {
            dockingTop = true,
            dockingLeft = true,
            verticalOffset = 16,
            horizontalOffset = 16,
            size = new Vector2(200, 400),
        };

        private const string UxmlName = "GraphSubWindow";

        // Each sub-window will override these if they need to
        protected virtual string windowTitle => "SubWindow";
        protected virtual string layoutKey => "ShaderGraph.SubWindow";
        protected virtual string elementName => "GraphSubWindow";
        protected virtual string styleName => "GraphSubWindow";

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

        public Action<GraphSubWindow> addItemRequested { get; set; }
        public Action<GraphSubWindow, int, VisualElement> moveItemRequested { get; set; }
        public Action<GraphSubWindow, VisualElement, string> editTextRequested { get; set; }

        // ISelection implementation
        public List<ISelectable> selection
        {
            get
            {
                return graphView?.selection;
            }
        }

        public override string title
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
        }

        public string subTitle
        {
            get { return m_SubTitleLabel.text; }
            set { m_SubTitleLabel.text = value; }
        }

        bool m_Windowed;
        public bool windowed
        {
            get { return m_Windowed; }
            set
            {
                if (m_Windowed == value) return;

                if (value)
                {
                    capabilities &= ~Capabilities.Movable;
                    AddToClassList("windowed");
                    this.RemoveManipulator(m_Dragger);
                }
                else
                {
                    capabilities |= Capabilities.Movable;
                    RemoveFromClassList("windowed");
                    this.AddManipulator(m_Dragger);
                }
                m_Windowed = value;
            }
        }

        public override VisualElement contentContainer { get { return m_ContentContainer; } }

        public bool scrollable
        {
            get
            {
                return m_Scrollable;
            }
            set
            {
                if (m_Scrollable == value)
                    return;

                m_Scrollable = value;

                if (m_Scrollable)
                {
                    if (m_ScrollView == null)
                    {
                        m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
                    }

                    // Remove the sections container from the content item and add it to the scrollview
                    m_ContentContainer.RemoveFromHierarchy();
                    m_Root.Add(m_ScrollView);
                    m_ScrollView.Add(m_ContentContainer);

                    AddToClassList("scrollable");
                }
                else
                {
                    if (m_ScrollView != null)
                    {
                        // Remove the sections container from the scrollview and add it to the content item
                        m_ScrollView.RemoveFromHierarchy();
                        m_ContentContainer.RemoveFromHierarchy();
                        m_Root.Add(m_ContentContainer);
                    }
                    RemoveFromClassList("scrollable");
                }
            }
        }

        protected GraphSubWindow(GraphView associatedGraphView = null) : base()
        {
            m_GraphView = associatedGraphView;
            m_GraphView.Add(this);

            // Setup VisualElement from Stylesheet and UXML file
            styleSheets.Add(Resources.Load<StyleSheet>($"Styles/{styleName}"));
            var uxml = Resources.Load<VisualTreeAsset>($"UXML/{UxmlName}");
            m_MainContainer = uxml.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");

            m_Root = m_MainContainer.Q("content");
            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("subWindowHeader");

            m_TitleLabel = m_MainContainer.Q<Label>(name: "titleLabel");
            m_SubTitleLabel = m_MainContainer.Q<Label>(name: "subTitleLabel");
            m_ContentContainer = m_MainContainer.Q(name: "contentContainer");

            hierarchy.Add(m_MainContainer);

            capabilities |= Capabilities.Movable | Capabilities.Resizable;
            style.overflow = Overflow.Hidden;
            focusable = true;
            scrollable = true;
            name = elementName;
            title = windowTitle;

            ClearClassList();
            AddToClassList(name);

            BuildManipulators();

            /* Event interception to prevent GraphView manipulators from being triggered */
            RegisterCallback<DragUpdatedEvent>(e =>
            {
                e.StopPropagation();
            });

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

            DeserializeLayout();
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

        void BuildManipulators()
        {
            this.AddManipulator(m_Dragger);
            m_Dragger = new Dragger { clampToParentEdges = true };

            var resizeElement = this.Q<ResizableElement>();
            resizeElement.BindOnResizeCallback(OnWindowResize);
            hierarchy.Add(resizeElement);

            var windowDraggable = new WindowDraggable(null, m_GraphView);
            windowDraggable.OnDragFinished += SerializeLayout;
            this.AddManipulator(windowDraggable);
        }

        void OnWindowResize(MouseUpEvent upEvent)
        {
            SerializeLayout();
        }

        void SerializeLayout()
        {
            m_Layout.CalculateDockingCornerAndOffset(layout, m_GraphView.layout);
            m_Layout.ClampToParentWindow();

            var serializedLayout = JsonUtility.ToJson(m_Layout);
            EditorUserSettings.SetConfigValue(layoutKey, serializedLayout);
        }

        void DeserializeLayout()
        {
            var serializedLayout = EditorUserSettings.GetConfigValue(layoutKey);
            if (!string.IsNullOrEmpty(serializedLayout))
                m_Layout = JsonUtility.FromJson<WindowDockingLayout>(serializedLayout);
            else
                m_Layout = m_DefaultLayout;

            m_Layout.ApplyPosition(this);
            m_Layout.ApplySize(this);
        }
    }
}
