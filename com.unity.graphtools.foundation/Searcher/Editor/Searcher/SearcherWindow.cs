using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Searcher
{
    /// <summary>
    /// Editor Window for the searcher.
    /// </summary>
    [PublicAPI]
    public class SearcherWindow : EditorWindow
    {
        [PublicAPI]
        public struct Alignment
        {
            [PublicAPI]
            public enum Horizontal { Left = 0, Center, Right }
            [PublicAPI]
            public enum Vertical { Top = 0, Center, Bottom }

            public readonly Vertical vertical;
            public readonly Horizontal horizontal;

            public Alignment(Vertical v, Horizontal h)
            {
                vertical = v;
                horizontal = h;
            }
        }

        const string k_DatabaseDirectory = "/../Library/Searcher";

        const string k_StylesheetName = "SearcherWindow.uss";
        const string k_WindowClassName = "unity-item-library-window";
        const string k_PopupWindowClassName = k_WindowClassName + "--popup";

        static Vector2 s_DefaultSize = new Vector2(300, 300);

        static IEnumerable<SearcherItem> s_Items;
        static Searcher s_Searcher;
        static Func<SearcherItem, bool> s_ItemSelectedDelegate;

        static readonly public Vector2 MinSize = new Vector2(120, 130);

        // TODO remove when access to internals
        enum ShowMode
        {
            NormalWindow, PopupMenu
        };

        Action<Searcher.AnalyticsEvent> m_AnalyticsDataDelegate;
        SearcherControl m_SearcherControl;
        Vector2 m_OriginalMousePos;
        Rect m_OriginalWindowPos;
        Rect m_NewWindowPos;
        bool m_IsMouseDownOnResizer;
        bool m_IsMouseDownOnTitle;
        Focusable m_FocusedBefore;
        Vector2 m_Size;

        static void UpdateDefaultSize(Searcher searcher)
        {
            var isPreviewPanelVisible = searcher != null && searcher.IsPreviewPanelVisible();
            var defaultWidth = SearcherControl.DefaultSearchPanelWidth;
            if (isPreviewPanelVisible)
                defaultWidth += SearcherControl.DefaultDetailsPanelWidth;
            s_DefaultSize = new Vector2(defaultWidth, SearcherControl.DefaultHeight);
        }

        public static void Show(
            EditorWindow host,
            IList<SearcherItem> items,
            string title,
            Func<SearcherItem, bool> itemSelectedDelegate,
            Vector2 displayPosition,
            Alignment align = default)
        {
            Show(host, items, title, Application.dataPath + k_DatabaseDirectory, itemSelectedDelegate, displayPosition, align);
        }

        public static void Show(
            EditorWindow host,
            IList<SearcherItem> items,
            ISearcherAdapter adapter,
            Func<SearcherItem, bool> itemSelectedDelegate,
            Vector2 displayPosition,
            Action<Searcher.AnalyticsEvent> analyticsDataDelegate,
            Alignment align = default)
        {
            Show(host, items, adapter, Application.dataPath + k_DatabaseDirectory, itemSelectedDelegate,
                displayPosition, analyticsDataDelegate, align);
        }

        public static void Show(
            EditorWindow host,
            IList<SearcherItem> items,
            string title,
            string directoryPath,
            Func<SearcherItem, bool> itemSelectedDelegate,
            Vector2 displayPosition,
            Alignment align = default)
        {
            s_Items = items;
            var databaseDir = directoryPath;
            var database = new SearcherDatabase(s_Items.ToList());
            database.SerializeToDirectory(databaseDir);
            var searcher = new Searcher(database, title);

            Show(host, searcher, itemSelectedDelegate, displayPosition, null, align);
        }

        public static void Show(
            EditorWindow host,
            IEnumerable<SearcherItem> items,
            ISearcherAdapter adapter,
            string directoryPath,
            Func<SearcherItem, bool> itemSelectedDelegate,
            Vector2 displayPosition,
            Action<Searcher.AnalyticsEvent> analyticsDataDelegate,
            Alignment align = default)
        {
            s_Items = items;
            var database = new SearcherDatabase(s_Items.ToList());
            database.SerializeToDirectory(directoryPath);
            var searcher = new Searcher(database, adapter);

            Show(host, searcher, itemSelectedDelegate, displayPosition, analyticsDataDelegate, align);
        }

        public static void Show(
            EditorWindow host,
            Searcher searcher,
            Func<SearcherItem, bool> itemSelectedDelegate,
            Vector2 displayPosition,
            Action<Searcher.AnalyticsEvent> analyticsDataDelegate,
            Alignment align = default)
        {
            UpdateDefaultSize(searcher);

            var rect = new Rect(displayPosition, s_DefaultSize);

            Show(host, searcher, itemSelectedDelegate, analyticsDataDelegate, rect, align);
        }

        public static void Show(
            EditorWindow host,
            Searcher searcher,
            Func<SearcherItem, bool> itemSelectedDelegate,
            Action<Searcher.AnalyticsEvent> analyticsDataDelegate,
            Rect rect,
            Alignment align = default)
        {
            s_ItemSelectedDelegate = itemSelectedDelegate;
#if UNITY_EDITOR_OSX
            // Workaround for bug https://jira.unity3d.com/browse/GTF-642 to be fixed by the uitoolkit team
            host.SendEvent(
                new Event
                {
                    type = EventType.MouseUp,
                    mousePosition = Vector2.zero,
                    clickCount = 1,
                    button = (int)MouseButton.RightMouse
                });
#endif


            var window = CreateInstance<SearcherWindow>();
            window.m_AnalyticsDataDelegate = analyticsDataDelegate;
            window.position = GetRectStartingInWindow(rect, host, align);
            window.minSize = MinSize;
            window.Initialize(searcher, ShowMode.PopupMenu);
            window.ShowPopup();
            window.Focus();
        }

        public static SearcherWindow ShowReusableWindow(
            Searcher searcher,
            Func<SearcherItem, bool> itemSelectedDelegate = null,
            Action<Searcher.AnalyticsEvent> analyticsDataDelegate = null,
            Rect rect = default
            )
        {
            return ShowReusableWindow<SearcherWindow>(searcher, itemSelectedDelegate, analyticsDataDelegate, rect);
        }

        public static T ShowReusableWindow<T>(
            Searcher searcher,
            Func<SearcherItem, bool> itemSelectedDelegate = null,
            Action<Searcher.AnalyticsEvent> analyticsDataDelegate = null,
            Rect rect = default) where T: SearcherWindow
        {
            s_ItemSelectedDelegate = itemSelectedDelegate;

            bool windowExists = HasOpenInstances<T>();
            var window = GetWindow<T>();
            if (!windowExists && rect != default)
                window.position = rect;

            window.m_AnalyticsDataDelegate = analyticsDataDelegate;
            window.Initialize(searcher, ShowMode.NormalWindow);
            return window;
        }

        static Rect GetRectStartingInWindow(Rect rect, EditorWindow host, Alignment align = default)
        {
            var pos = rect.position;
            pos.x = Mathf.Max(0, Mathf.Min(host.position.size.x, pos.x));
            pos.y = Mathf.Max(0, Mathf.Min(host.position.size.y, pos.y));

            switch (align.horizontal)
            {
                case Alignment.Horizontal.Center:
                    pos.x -= rect.size.x / 2;
                    break;

                case Alignment.Horizontal.Right:
                    pos.x -= rect.size.x;
                    break;
            }

            switch (align.vertical)
            {
                case Alignment.Vertical.Center:
                    pos.y -= rect.size.y / 2;
                    break;

                case Alignment.Vertical.Bottom:
                    pos.y -= rect.size.y;
                    break;
            }

            return new Rect(pos + host.position.position, rect.size);
        }

        void Initialize(Searcher searcher, ShowMode showMode)
        {
            rootVisualElement.AddStylesheet(k_StylesheetName);
            rootVisualElement.AddToClassList(k_WindowClassName);
            rootVisualElement.AddToClassList("unity-theme-env-variables");
            rootVisualElement.EnableInClassList(k_PopupWindowClassName, showMode == ShowMode.PopupMenu);

            SetupSearcher(searcher);
        }

        void SetupSearcher(Searcher searcher)
        {
            s_Searcher = searcher;
            UpdateDefaultSize(searcher);

            m_SearcherControl = new SearcherControl();
            m_SearcherControl.Setup(searcher, SelectionCallback, OnAnalyticsDataCallback, OnSearcherChangePreviewVisibility);

            m_SearcherControl.TitleContainer.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            m_SearcherControl.TitleContainer.RegisterCallback<MouseUpEvent>(OnTitleMouseUp);

            m_SearcherControl.Resizer.RegisterCallback<MouseDownEvent>(OnResizerMouseDown);
            m_SearcherControl.Resizer.RegisterCallback<MouseUpEvent>(OnResizerMouseUp);

            var root = rootVisualElement;
            root.style.flexGrow = 1;
            root.Add(m_SearcherControl);
        }

        void OnSearcherChangePreviewVisibility(float widthDelta)
        {
            m_Size = position.size + widthDelta * Vector2.right;
            position = new Rect(position.position, m_Size);
            Repaint();
        }

        void OnDisable()
        {
            if (m_SearcherControl != null)
            {
                m_SearcherControl.TitleContainer.UnregisterCallback<MouseDownEvent>(OnTitleMouseDown);
                m_SearcherControl.TitleContainer.UnregisterCallback<MouseUpEvent>(OnTitleMouseUp);

                m_SearcherControl.Resizer.UnregisterCallback<MouseDownEvent>(OnResizerMouseDown);
                m_SearcherControl.Resizer.UnregisterCallback<MouseUpEvent>(OnResizerMouseUp);
            }
        }

        void OnTitleMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            m_IsMouseDownOnTitle = true;

            m_NewWindowPos = position;
            m_OriginalWindowPos = position;
            m_OriginalMousePos = evt.mousePosition;

            m_FocusedBefore = rootVisualElement.panel.focusController.focusedElement;

            m_SearcherControl.TitleContainer.RegisterCallback<MouseMoveEvent>(OnTitleMouseMove);
            m_SearcherControl.TitleContainer.RegisterCallback<KeyDownEvent>(OnSearcherKeyDown);
            m_SearcherControl.TitleContainer.CaptureMouse();
        }

        void OnTitleMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (!m_SearcherControl.TitleContainer.HasMouseCapture())
                return;

            FinishMove();
        }

        void FinishMove()
        {
            m_SearcherControl.TitleContainer.UnregisterCallback<MouseMoveEvent>(OnTitleMouseMove);
            m_SearcherControl.TitleContainer.UnregisterCallback<KeyDownEvent>(OnSearcherKeyDown);
            m_SearcherControl.TitleContainer.ReleaseMouse();
            m_FocusedBefore?.Focus();
            m_IsMouseDownOnTitle = false;
        }

        void OnTitleMouseMove(MouseMoveEvent evt)
        {
            var delta = evt.mousePosition - m_OriginalMousePos;

            // TODO Temporary fix for Visual Scripting 1st drop. Find why position.position is 0,0 on MacOs in MouseMoveEvent
            // Bug occurs with Unity 2019.2.0a13
#if UNITY_EDITOR_OSX
            m_NewWindowPos = new Rect(m_NewWindowPos.position + delta, position.size);
#else
            m_NewWindowPos = new Rect(position.position + delta, position.size);
#endif
            Repaint();
        }

        void OnResizerMouseDown(MouseDownEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            m_IsMouseDownOnResizer = true;

            m_NewWindowPos = position;
            m_OriginalWindowPos = position;
            m_OriginalMousePos = evt.mousePosition;

            m_FocusedBefore = rootVisualElement.panel.focusController.focusedElement;

            m_SearcherControl.Resizer.RegisterCallback<MouseMoveEvent>(OnResizerMouseMove);
            m_SearcherControl.Resizer.RegisterCallback<KeyDownEvent>(OnSearcherKeyDown);
            m_SearcherControl.Resizer.CaptureMouse();
        }

        void OnResizerMouseUp(MouseUpEvent evt)
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (!m_SearcherControl.Resizer.HasMouseCapture())
                return;

            FinishResize();
        }

        void FinishResize()
        {
            m_SearcherControl.Resizer.UnregisterCallback<MouseMoveEvent>(OnResizerMouseMove);
            m_SearcherControl.Resizer.UnregisterCallback<KeyDownEvent>(OnSearcherKeyDown);
            m_SearcherControl.Resizer.ReleaseMouse();
            m_FocusedBefore?.Focus();
            m_IsMouseDownOnResizer = false;
        }

        void OnResizerMouseMove(MouseMoveEvent evt)
        {
            var delta = evt.mousePosition - m_OriginalMousePos;
            m_Size = m_OriginalWindowPos.size + delta;

            if (m_Size.x < minSize.x)
                m_Size.x = minSize.x;
            if (m_Size.y < minSize.y)
                m_Size.y = minSize.y;

            // TODO Temporary fix for Visual Scripting 1st drop. Find why position.position is 0,0 on MacOs in MouseMoveEvent
            // Bug occurs with Unity 2019.2.0a13
#if UNITY_EDITOR_OSX
            m_NewWindowPos = new Rect(m_NewWindowPos.position, m_Size);
#else
            m_NewWindowPos = new Rect(position.position, m_Size);
#endif
            Repaint();
        }

        void OnSearcherKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
            {
                if (m_IsMouseDownOnTitle)
                {
                    FinishMove();
                    position = m_OriginalWindowPos;
                }
                else if (m_IsMouseDownOnResizer)
                {
                    FinishResize();
                    position = m_OriginalWindowPos;
                }
            }
        }

        void OnGUI()
        {
            if ((m_IsMouseDownOnTitle || m_IsMouseDownOnResizer) && Event.current.type == EventType.Layout)
                position = m_NewWindowPos;
        }

        void SelectionCallback(SearcherItem item)
        {
            if (s_ItemSelectedDelegate == null || s_ItemSelectedDelegate(item))
                Close();
        }

        void OnAnalyticsDataCallback(Searcher.AnalyticsEvent item)
        {
            m_AnalyticsDataDelegate?.Invoke(item);
        }

        void OnLostFocus()
        {
            if (m_IsMouseDownOnTitle)
            {
                FinishMove();
            }
            else if (m_IsMouseDownOnResizer)
            {
                FinishResize();
            }

            // TODO: HACK - ListView's scroll view steals focus using the scheduler.
            EditorApplication.update += HackDueToCloseOnLostFocusCrashing;
        }

        // See: https://fogbugz.unity3d.com/f/cases/1004504/
        void HackDueToCloseOnLostFocusCrashing()
        {
            // Notify user that the searcher action was cancelled.
            s_ItemSelectedDelegate?.Invoke(null);

            Close();

            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= HackDueToCloseOnLostFocusCrashing;
        }
    }
}
