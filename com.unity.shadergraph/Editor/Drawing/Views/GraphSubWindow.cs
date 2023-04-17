using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Views
{
    interface ISelectionProvider
    {
        List<ISelectable> GetSelection { get; }
    }

    class GraphSubWindow : GraphElement, ISGResizable
    {
        ISGViewModel m_ViewModel;

        ISGViewModel ViewModel
        {
            get => m_ViewModel;
            set => m_ViewModel = value;
        }

        Dragger m_Dragger;

        // This needs to be something that each subclass defines for itself at creation time
        // if they all use the same they'll be stacked on top of each other at SG window creation
        protected WindowDockingLayout windowDockingLayout { get; private set; } = new WindowDockingLayout
        {
            dockingTop = true,
            dockingLeft = false,
            verticalOffset = 8,
            horizontalOffset = 8,
        };

        // Used to cache the window docking layout between resizing operations as it interferes with window resizing operations
        private IStyle cachedWindowDockingStyle;

        protected VisualElement m_MainContainer;
        protected VisualElement m_Root;
        protected Label m_TitleLabel;
        protected Label m_SubTitleLabel;
        protected ScrollView m_ScrollView;
        protected VisualElement m_ContentContainer;
        protected VisualElement m_HeaderItem;
        protected VisualElement m_ParentView;

        // Added for test assembly access
        internal ScrollView scrollView => m_ScrollView;

        // These are used as default values for styling and layout purposes
        // They can be overriden if a child class wants to roll its own style and layout behavior
        public virtual string layoutKey => "UnityEditor.ShaderGraph.SubWindow";
        public virtual string styleName => "GraphSubWindow";
        public virtual string UxmlName => "GraphSubWindow";

        // Each sub-window will override these if they need to
        public virtual string elementName => "";
        public virtual string windowTitle => "";

        public VisualElement ParentView
        {
            get
            {
                if (!isWindowed && m_ParentView == null)
                    m_ParentView = GetFirstAncestorOfType<GraphView>();
                return m_ParentView;
            }

            set
            {
                if (!isWindowed)
                    return;
                m_ParentView = value;
            }
        }

        public List<ISelectable> selection
        {
            get
            {
                if (ParentView is ISelectionProvider selectionProvider)
                    return selectionProvider.GetSelection;

                AssertHelpers.Fail("GraphSubWindow was unable to find a selection provider. Please check if parent view of: " + name + " implements ISelectionProvider::GetSelection");
                return new List<ISelectable>();
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

        // Intended for future handling of docking to sides of the shader graph window
        bool m_IsWindowed;
        public bool isWindowed
        {
            get { return m_IsWindowed; }
            set
            {
                if (m_IsWindowed == value) return;

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
                m_IsWindowed = value;
            }
        }

        public override VisualElement contentContainer => m_ContentContainer;

        private bool m_IsResizable = false;

        // Can be set by child classes as needed
        protected bool isWindowResizable
        {
            get => m_IsResizable;
            set
            {
                if (m_IsResizable != value)
                {
                    m_IsResizable = value;
                    HandleResizingBehavior(m_IsResizable);
                }
            }
        }

        void HandleResizingBehavior(bool isResizable)
        {
            if (isResizable)
            {
                var resizeElement = this.Q<ResizableElement>();
                resizeElement.BindOnResizeCallback(OnWindowResize);
                hierarchy.Add(resizeElement);
            }
            else
            {
                var resizeElement = this.Q<ResizableElement>();
                resizeElement.SetResizeRules(ResizableElement.Resizer.None);
                hierarchy.Remove(resizeElement);
            }
        }

        protected void SetResizingRules(ResizableElement.Resizer resizeDirections)
        {
            var resizeElement = this.Q<ResizableElement>();
            resizeElement.SetResizeRules(resizeDirections);
        }

        private bool m_IsScrollable = false;

        // Can be set by child classes as needed
        protected bool isWindowScrollable
        {
            get => m_IsScrollable;
            set
            {
                if (m_IsScrollable != value)
                {
                    m_IsScrollable = value;
                    HandleScrollingBehavior(m_IsScrollable);
                }
            }
        }

        protected float scrollableWidth
        {
            get { return m_ScrollView.contentContainer.layout.width - m_ScrollView.contentViewport.layout.width; }
        }

        protected float scrollableHeight
        {
            get { return contentContainer.layout.height - m_ScrollView.contentViewport.layout.height; }
        }

        void HandleScrollingBehavior(bool scrollable)
        {
            if (scrollable)
            {
                // Remove the categories container from the content item and add it to the scrollview
                m_ContentContainer.RemoveFromHierarchy();
                m_ScrollView.Add(m_ContentContainer);
                AddToClassList("scrollable");
            }
            else
            {
                // Remove the categories container from the scrollview and add it to the content item
                m_ContentContainer.RemoveFromHierarchy();
                m_Root.Add(m_ContentContainer);

                RemoveFromClassList("scrollable");
            }
        }

        protected GraphSubWindow(ISGViewModel viewModel)
        {
            ViewModel = viewModel;
            m_ParentView = ViewModel.parentView;
            ParentView.Add(this);

            var styleSheet = Resources.Load<StyleSheet>($"Styles/{styleName}");
            // Setup VisualElement from Stylesheet and UXML file
            styleSheets.Add(styleSheet);
            var uxml = Resources.Load<VisualTreeAsset>($"UXML/{UxmlName}");
            m_MainContainer = uxml.Instantiate();
            m_MainContainer.AddToClassList("mainContainer");

            m_Root = m_MainContainer.Q("content");
            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("subWindowHeader");
            m_ScrollView = m_MainContainer.Q<ScrollView>("scrollView");
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
            //RegisterCallback<DragUpdatedEvent>(e =>
            //{
            //    e.StopPropagation();
            //});

            // prevent Zoomer manipulator
            RegisterCallback<WheelEvent>(e =>
            {
                e.StopPropagation();
            });

            //RegisterCallback<MouseDownEvent>(e =>
            //{
            //    // prevent ContentDragger manipulator
            //    e.StopPropagation();
            //});
        }

        public void ShowWindow()
        {
            this.style.visibility = Visibility.Visible;
            this.m_ScrollView.style.display = DisplayStyle.Flex;
            this.MarkDirtyRepaint();
        }

        public void HideWindow()
        {
            this.style.visibility = Visibility.Hidden;
            this.m_ScrollView.style.display = DisplayStyle.None;
            this.MarkDirtyRepaint();
        }

        void BuildManipulators()
        {
            m_Dragger = new Dragger { clampToParentEdges = true };
            RegisterCallback<MouseUpEvent>(OnMoveEnd);
            this.AddManipulator(m_Dragger);
        }

        #region Layout
        public void ClampToParentLayout(Rect parentLayout)
        {
            windowDockingLayout.CalculateDockingCornerAndOffset(layout, parentLayout);
            windowDockingLayout.ClampToParentWindow();

            // If the parent shader graph window is being resized smaller than this window on either axis
            if (parentLayout.width < this.layout.width || parentLayout.height < this.layout.height)
            {
                // Don't adjust the sub window in this case as it causes flickering errors and looks broken
            }
            else
            {
                windowDockingLayout.ApplyPosition(this);
            }

            SerializeLayout();
        }

        public void OnStartResize()
        {
            cachedWindowDockingStyle = this.style;
        }

        public void OnResized()
        {
            if (cachedWindowDockingStyle != null)
            {
                this.style.left = cachedWindowDockingStyle.left;
                this.style.right = cachedWindowDockingStyle.right;
                this.style.bottom = cachedWindowDockingStyle.bottom;
                this.style.top = cachedWindowDockingStyle.top;
            }
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
                // The window size needs to come from the stylesheet or UXML as opposed to being defined in code
                windowDockingLayout.size = layout.size;
            }

            windowDockingLayout.ApplySize(this);
            windowDockingLayout.ApplyPosition(this);
        }

        protected void AddStyleSheetFromPath(string styleSheetPath)
        {
            StyleSheet sheetAsset = Resources.Load<StyleSheet>(styleSheetPath); ;

            if (sheetAsset == null)
            {
                Debug.LogWarning(string.Format("Style sheet not found for path \"{0}\"", styleSheetPath));
                return;
            }
            styleSheets.Add(sheetAsset);
        }

        void SerializeLayout()
        {
            windowDockingLayout.size = layout.size;
            var serializedLayout = JsonUtility.ToJson(windowDockingLayout);
            EditorUserSettings.SetConfigValue(layoutKey, serializedLayout);
        }

        void OnMoveEnd(MouseUpEvent upEvent)
        {
            windowDockingLayout.CalculateDockingCornerAndOffset(layout, ParentView.layout);
            windowDockingLayout.ClampToParentWindow();

            SerializeLayout();
        }

        public bool CanResizePastParentBounds()
        {
            return false;
        }

        void OnWindowResize(MouseUpEvent upEvent)
        {
        }

        public virtual void Dispose()
        {
            m_MainContainer = null;
            m_Root = null;
            m_TitleLabel = null;
            m_SubTitleLabel = null;
            m_ScrollView = null;
            m_ContentContainer = null;
            m_HeaderItem = null;
            m_ParentView = null;
            cachedWindowDockingStyle = null;
            styleSheets.Clear();
        }
    }
    #endregion
}
