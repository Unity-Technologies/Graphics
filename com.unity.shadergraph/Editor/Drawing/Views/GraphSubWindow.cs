using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Interfaces;

namespace UnityEditor.ShaderGraph.Drawing.Views
{
    class GraphSubWindow : GraphElement, ISGResizable
    {
        Dragger m_Dragger;

        // This needs to be something that each subclass defines on its own
        // if they all use the same they'll be stacked on top of each other at SG window creation
        WindowDockingLayout m_DefaultLayout = new WindowDockingLayout
        {
            dockingTop = true,
            dockingLeft = false,
            verticalOffset = 8,
            horizontalOffset = 8,
        };

        // Used to cache the window docking layout between resizing operations as it interferes with window resizing operations
        private IStyle cachedWindowDockingStyle;

        WindowDockingLayout windowDockingLayout { get; set; }

        protected VisualElement m_MainContainer;
        protected VisualElement m_Root;
        protected Label m_TitleLabel;
        protected Label m_SubTitleLabel;
        protected ScrollView m_ScrollView;
        protected VisualElement m_ContentContainer;
        protected VisualElement m_HeaderItem;
        protected GraphView m_GraphView;

        // These are used as default values for styling and layout purposes
        // They can be overriden if a child class wants to roll its own style and layout behavior
        protected virtual string layoutKey => "UnityEditor.ShaderGraph.SubWindow";
        protected virtual string styleName => "GraphSubWindow";
        protected virtual string UxmlName => "GraphSubWindow";

        // Each sub-window will override these if they need to
        protected virtual string elementName => "";
        protected virtual string windowTitle => "";

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

        public List<ISelectable> selection => graphView?.selection;

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

        // Intended for future handling of docking to sides of the shader graph window
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

        public override VisualElement contentContainer => m_ContentContainer;

        private bool m_IsScrollable = false;

        // Can be set by child classes as needed
        protected bool isWindowScrollable
        {
            get => m_IsScrollable;
            set
            {
                if (m_IsScrollable != value)
                {
                    m_ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
                    m_IsScrollable = value;
                    HandleScrollingBehavior(m_IsScrollable);
                }
            }
        }

        void HandleScrollingBehavior(bool scrollable)
        {
            if (scrollable)
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

        protected GraphSubWindow(GraphView associatedGraphView = null) : base()
        {
            m_GraphView = associatedGraphView;
            m_GraphView.Add(this);

            var styleSheet = Resources.Load<StyleSheet>($"Styles/{styleName}");
            // Setup VisualElement from Stylesheet and UXML file
            styleSheets.Add(styleSheet);
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
            focusable = false;

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
                // prevent ContentDragger manipulator
                e.StopPropagation();
            });
        }

        protected void ShowWindow()
        {
            this.style.visibility = Visibility.Visible;
            contentContainer.MarkDirtyRepaint();
        }

        protected void HideWindow()
        {
            this.style.visibility = Visibility.Hidden;
            #if UNITY_2021_1_OR_NEWER
            this.m_ScrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            this.m_ScrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            #else
            this.m_ScrollView.showVertical = false;
            this.m_ScrollView.showHorizontal = false;
            #endif

            contentContainer.Clear();
            contentContainer.MarkDirtyRepaint();
        }

        void BuildManipulators()
        {
            m_Dragger = new Dragger { clampToParentEdges = true };
            RegisterCallback<MouseUpEvent>(OnMoveEnd);
            this.AddManipulator(m_Dragger);

            var resizeElement = this.Q<ResizableElement>();
            resizeElement.BindOnResizeCallback(OnWindowResize);
            hierarchy.Add(resizeElement);
        }

        #region Layout
        public void ClampToParentLayout(Rect parentLayout)
        {
            windowDockingLayout.CalculateDockingCornerAndOffset(layout, parentLayout);
            windowDockingLayout.ClampToParentWindow();
            windowDockingLayout.ApplyPosition(this);
            SerializeLayout();
        }

        public void OnStartResize()
        {
            cachedWindowDockingStyle = this.style;
        }

        public void OnResized()
        {
            this.style.left = cachedWindowDockingStyle.left;
            this.style.right = cachedWindowDockingStyle.right;
            this.style.bottom = cachedWindowDockingStyle.bottom;
            this.style.top = cachedWindowDockingStyle.top;

            windowDockingLayout.size = layout.size;
            SerializeLayout();
        }

        public void DeserializeLayout()
        {
            var serializedLayout = EditorUserSettings.GetConfigValue(layoutKey);
            if (!string.IsNullOrEmpty(serializedLayout))
                windowDockingLayout = JsonUtility.FromJson<WindowDockingLayout>(serializedLayout);
            else
            {
                windowDockingLayout = m_DefaultLayout;
                // The window size needs to come from the stylesheet or UXML as opposed to being defined in code
                windowDockingLayout.size = layout.size;
            }

            windowDockingLayout.ApplySize(this);
            windowDockingLayout.ApplyPosition(this);
        }

        void SerializeLayout()
        {
            windowDockingLayout.size = layout.size;
            var serializedLayout = JsonUtility.ToJson(windowDockingLayout);
            EditorUserSettings.SetConfigValue(layoutKey, serializedLayout);
        }

        void OnMoveEnd(MouseUpEvent upEvent)
        {
            windowDockingLayout.CalculateDockingCornerAndOffset(layout, graphView.layout);
            windowDockingLayout.ClampToParentWindow();

            // TODO: Dragger does some weird stuff when moving a graph sub window thats larger than screen bounds as it clamps it by offsetting the layout,
            // but because the windows larger than the graph window, the offsetting behavior pushes it out of the screen and basically moves the window out of bounds
            // Need to either prevent windows from being resized past window bounds, or adjust for the Grabber behavior somehow

            SerializeLayout();
        }

        public bool CanResizePastParentBounds()
        {
            return false;
        }

        void OnWindowResize(MouseUpEvent upEvent)
        {


        }
    }
    #endregion
}
