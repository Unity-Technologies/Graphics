using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// An utility window used to list and filter a set of elements, as seen in the inspector when
    /// clicking on the "Add Component" button.
    /// </summary>
    [InitializeOnLoad]
    public class FilterWindow : EditorWindow
    {
        /// <summary>
        /// The interface to implement to populate the list or tree and traverse its elements.
        /// </summary>
        public interface IProvider
        {
            /// <summary>
            /// The position of the window on screen.
            /// </summary>
            Vector2 position { get; set; }

            /// <summary>
            /// Implement this method to populate the list or tree of elements.
            /// </summary>
            /// <param name="tree">The list to populate.</param>
            void CreateComponentTree(List<Element> tree);

            /// <summary>
            /// Implement this method to define the behavior when an item is selected.
            /// </summary>
            /// <param name="element">The selected element.</param>
            /// <param name="addIfComponent"></param>
            /// <returns><c>true</c> if the window should close, <c>false</c> otherwise.</returns>
            bool GoToChild(Element element, bool addIfComponent);
        }

        /// <summary>
        /// The default width for the window.
        /// </summary>
        public static readonly float DefaultWidth = 250f;

        /// <summary>
        /// The default height for the window.
        /// </summary>
        public static readonly float DefaultHeight = 300f;

        #region BaseElements

        /// <summary>
        /// An element from the filtered list or tree.
        /// </summary>
        /// <seealso cref="GroupElement"/>
        public class Element : IComparable
        {
            /// <summary>
            /// The current hierarchical level in the tree.
            /// </summary>
            public int level;

            /// <summary>
            /// The displayed content for the element.
            /// </summary>
            public GUIContent content;

            /// <summary>
            /// The name of the element as displayed in the UI.
            /// </summary>
            public string name
            {
                get { return content.text; }
            }

            /// <summary>
            /// Compares this element to another object.
            /// </summary>
            /// <param name="o">The object to compare to.</param>
            /// <returns><c>true</c> if both objects are the same, <c>false</c> otherwise.</returns>
            public int CompareTo(object o)
            {
                return name.CompareTo((o as Element).name);
            }
        }

        /// <summary>
        /// A meta element used to group several elements in the list or tree.
        /// </summary>
        /// <seealso cref="Element"/>
        [Serializable]
        public class GroupElement : Element
        {
            /// <summary>
            /// The current scroll position in the UI.
            /// </summary>
            public Vector2 scroll;

            /// <summary>
            /// The current selected index in the group.
            /// </summary>
            public int selectedIndex;

            /// <summary>
            /// Requests focus for the element.
            /// </summary>
            public bool WantsFocus { get; protected set; }

            /// <summary>
            /// Returns <c>true</c> if this group and its content should appear disabled in the UI.
            /// </summary>
            public virtual bool ShouldDisable
            {
                get { return false; }
            }

            /// <summary>
            /// Creates a new <see cref="GroupElement"/>
            /// </summary>
            /// <param name="level">The group level.</param>
            /// <param name="name">The display name for the group.</param>
            public GroupElement(int level, string name)
            {
                this.level = level;
                content = new GUIContent(name);
            }

            /// <summary>
            /// Handles custom keyboard events on this group.
            /// </summary>
            /// <param name="evt">The event.</param>
            /// <param name="window">A reference to the parent <see cref="FilterWindow"/>.</param>
            /// <param name="goToParent">The action to execute if a "back" action is triggered in the UI.</param>
            /// <returns><c>true</c> if the builtin events should execute for this group, <c>false</c> otherwise.</returns>
            public virtual bool HandleKeyboard(Event evt, FilterWindow window, Action goToParent)
            {
                return false;
            }

            /// <summary>
            /// A custom drawing method for this group.
            /// </summary>
            /// <param name="sFilterWindow">A reference to the parent <see cref="FilterWindow"/>.</param>
            /// <returns><c>true</c> if the builtin drawing function should execute for this group,
            /// <c>false</c> otherwise.</returns>
            public virtual bool OnGUI(FilterWindow sFilterWindow)
            {
                return false;
            }
        }

        #endregion

        // Styles

        class Styles
        {
            public GUIStyle header = (GUIStyle)typeof(EditorStyles).GetProperty("inspectorBig", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null, null);
            public GUIStyle componentButton = new GUIStyle("PR Label");
            public GUIStyle groupButton;
            public GUIStyle background = "grey_border";
            public GUIStyle rightArrow = "AC RightArrow";
            public GUIStyle leftArrow = "AC LeftArrow";

            public Styles()
            {
                header.font = EditorStyles.boldLabel.font;

                componentButton.alignment = TextAnchor.MiddleLeft;
                componentButton.fixedHeight = 20;
                componentButton.imagePosition = ImagePosition.ImageAbove;

                groupButton = new GUIStyle(componentButton);
            }
        }

        const int k_HeaderHeight = 30;
        const int k_WindowHeight = 400 - 80;
        const int k_HelpHeight = 80 * 0;
        const string k_ComponentSearch = "NodeSearchString";

        static Styles s_Styles;
        static FilterWindow s_FilterWindow;
        static long s_LastClosedTime;
        static bool s_DirtyList;

        IProvider m_Provider;
        Element[] m_Tree;
        Element[] m_SearchResultTree;
        List<GroupElement> m_Stack = new List<GroupElement>();

        float m_Anim = 1;
        int m_AnimTarget = 1;
        long m_LastTime;
        bool m_ScrollToSelected;
        string m_DelayedSearch;
        string m_Search = "";

        bool m_HasSearch { get { return !string.IsNullOrEmpty(m_Search); } }
        GroupElement m_ActiveParent { get { return m_Stack[m_Stack.Count - 2 + m_AnimTarget]; } }
        Element[] m_ActiveTree { get { return m_HasSearch ? m_SearchResultTree : m_Tree; } }
        Element m_ActiveElement
        {
            get
            {
                if (m_ActiveTree == null)
                    return null;

                var children = GetChildren(m_ActiveTree, m_ActiveParent);
                return children.Count == 0
                    ? null
                    : children[m_ActiveParent.selectedIndex];
            }
        }
        bool m_IsAnimating { get { return !Mathf.Approximately(m_Anim, m_AnimTarget); } }

        static FilterWindow()
        {
            s_DirtyList = true;
        }

        void OnEnable()
        {
            s_FilterWindow = this;
            m_Search = "";
        }

        void OnDisable()
        {
            s_LastClosedTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            s_FilterWindow = null;
        }

        void OnLostFocus() => Close();

        internal static bool ValidateAddComponentMenuItem()
        {
            return true;
        }

        /// <summary>
        /// Shows the filter window using the given provider.
        /// </summary>
        /// <param name="position">The position to show the filter window at.</param>
        /// <param name="provider">The provider of items for the filter window.</param>
        /// <returns>Returns true if the window is shown, false otherwise.</returns>
        public static bool Show(Vector2 position, IProvider provider)
        {
            // If the window is already open, close it instead.
            var wins = Resources.FindObjectsOfTypeAll(typeof(FilterWindow));
            if (wins.Length > 0)
            {
                try
                {
                    ((EditorWindow)wins[0]).Close();
                    return false;
                }
                catch (Exception)
                {
                    s_FilterWindow = null;
                }
            }

            // We could not use realtimeSinceStartUp since it is set to 0 when entering/exitting
            // playmode, we assume an increasing time when comparing time.
            long nowMilliSeconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            bool justClosed = nowMilliSeconds < s_LastClosedTime + 50;

            if (!justClosed)
            {
                Event.current.Use();

                if (s_FilterWindow == null)
                {
                    s_FilterWindow = CreateInstance<FilterWindow>();
                    s_FilterWindow.hideFlags = HideFlags.HideAndDontSave;
                }

                s_FilterWindow.Init(position, provider);
                return true;
            }
            return false;
        }

        static object Invoke(Type t, object inst, string method, params object[] args)
        {
            var flags = (inst == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic;
            var mi = t.GetMethod(method, flags);
            return mi.Invoke(inst, args);
        }

        void Init(Vector2 position, IProvider provider)
        {
            m_Provider = provider;
            // Has to be done before calling Show / ShowWithMode
            m_Provider.position = position;
            position = GUIUtility.GUIToScreenPoint(position);
            var buttonRect = new Rect(position.x - DefaultWidth / 2, position.y - 16, DefaultWidth, 1);

            CreateComponentTree();

            ShowAsDropDown(buttonRect, new Vector2(buttonRect.width, k_WindowHeight));

            Focus();

            wantsMouseMove = true;
        }

        void CreateComponentTree()
        {
            var tree = new List<Element>();
            m_Provider.CreateComponentTree(tree);

            m_Tree = tree.ToArray();

            // Rebuild stack
            if (m_Stack.Count == 0)
            {
                m_Stack.Add(m_Tree[0] as GroupElement);
            }
            else
            {
                // The root is always the match for level 0
                var match = m_Tree[0] as GroupElement;
                int level = 0;
                while (true)
                {
                    // Assign the match for the current level
                    var oldElement = m_Stack[level];
                    m_Stack[level] = match;
                    m_Stack[level].selectedIndex = oldElement.selectedIndex;
                    m_Stack[level].scroll = oldElement.scroll;

                    // See if we reached last element of stack
                    level++;
                    if (level == m_Stack.Count)
                        break;

                    // Try to find a child of the same name as we had before
                    var children = GetChildren(m_ActiveTree, match);
                    var childMatch = children.FirstOrDefault(c => c.name == m_Stack[level].name);

                    if (childMatch is GroupElement)
                    {
                        match = childMatch as GroupElement;
                    }
                    else
                    {
                        // If we couldn't find the child, remove all further elements from the stack
                        while (m_Stack.Count > level)
                            m_Stack.RemoveAt(level);
                    }
                }
            }

            s_DirtyList = false;
            RebuildSearch();
        }

        internal void OnGUI()
        {
            // Avoids errors in the console if a domain reload is triggered while the filter window
            // is opened
            if (m_Provider == null)
                return;

            if (s_Styles == null)
                s_Styles = new Styles();

            GUI.Label(new Rect(0, 0, position.width, position.height), GUIContent.none, s_Styles.background);

            if (s_DirtyList)
                CreateComponentTree();

            // Keyboard
            HandleKeyboard();

            GUILayout.Space(7);

            // Search
            if (!m_ActiveParent.WantsFocus)
            {
                EditorGUI.FocusTextInControl("ComponentSearch");
            }

            var searchRect = GUILayoutUtility.GetRect(10, 20);
            searchRect.x += 8;
            searchRect.width -= 16;

            GUI.SetNextControlName("ComponentSearch");

            using (new EditorGUI.DisabledScope(m_ActiveParent.ShouldDisable))
            {
                string newSearch = (string)Invoke(typeof(EditorGUI), null, "SearchField", searchRect, m_DelayedSearch ?? m_Search);

                if (newSearch != m_Search || m_DelayedSearch != null)
                {
                    if (!m_IsAnimating)
                    {
                        m_Search = m_DelayedSearch ?? newSearch;
                        EditorPrefs.SetString(k_ComponentSearch, m_Search);
                        RebuildSearch();
                        m_DelayedSearch = null;
                    }
                    else
                    {
                        m_DelayedSearch = newSearch;
                    }
                }
            }

            // Show lists
            ListGUI(m_ActiveTree, m_Anim, GetElementRelative(0), GetElementRelative(-1));
            if (m_Anim < 1)
                ListGUI(m_ActiveTree, m_Anim + 1, GetElementRelative(-1), GetElementRelative(-2));

            // Animate
            if (m_IsAnimating && Event.current.type == EventType.Repaint)
            {
                long now = DateTime.Now.Ticks;
                float deltaTime = (now - m_LastTime) / (float)TimeSpan.TicksPerSecond;
                m_LastTime = now;
                m_Anim = Mathf.MoveTowards(m_Anim, m_AnimTarget, deltaTime * 4);

                if (m_AnimTarget == 0 && Mathf.Approximately(m_Anim, 0))
                {
                    m_Anim = 1;
                    m_AnimTarget = 1;
                    m_Stack.RemoveAt(m_Stack.Count - 1);
                }

                Repaint();
            }
        }

        void HandleKeyboard()
        {
            var evt = Event.current;

            if (evt.type == EventType.KeyDown)
            {
                // Special handling when in new script panel
                if (!m_ActiveParent.HandleKeyboard(evt, s_FilterWindow, GoToParent))
                {
                    // Always do these
                    if (evt.keyCode == KeyCode.DownArrow)
                    {
                        m_ActiveParent.selectedIndex++;
                        m_ActiveParent.selectedIndex = Mathf.Min(m_ActiveParent.selectedIndex, GetChildren(m_ActiveTree, m_ActiveParent).Count - 1);
                        m_ScrollToSelected = true;
                        evt.Use();
                    }

                    if (evt.keyCode == KeyCode.UpArrow)
                    {
                        m_ActiveParent.selectedIndex--;
                        m_ActiveParent.selectedIndex = Mathf.Max(m_ActiveParent.selectedIndex, 0);
                        m_ScrollToSelected = true;
                        evt.Use();
                    }

                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        if (m_ActiveElement != null)
                        {
                            GoToChild(m_ActiveElement, true);
                            evt.Use();
                        }
                    }

                    // Do these if we're not in search mode
                    if (!m_HasSearch)
                    {
                        if (evt.keyCode == KeyCode.LeftArrow || evt.keyCode == KeyCode.Backspace)
                        {
                            GoToParent();
                            evt.Use();
                        }

                        if (evt.keyCode == KeyCode.RightArrow)
                        {
                            GoToChild(m_ActiveElement, false);
                            evt.Use();
                        }

                        if (evt.keyCode == KeyCode.Escape)
                        {
                            Close();
                            evt.Use();
                        }
                    }
                }
            }
        }

        const string k_SearchHeader = "Search";

        void RebuildSearch()
        {
            if (!m_HasSearch)
            {
                m_SearchResultTree = null;

                if (m_Stack[m_Stack.Count - 1].name == k_SearchHeader)
                {
                    m_Stack.Clear();
                    m_Stack.Add(m_Tree[0] as GroupElement);
                }

                m_AnimTarget = 1;
                m_LastTime = DateTime.Now.Ticks;
                return;
            }

            // Support multiple search words separated by spaces.
            var searchWords = m_Search.ToLower().Split(' ');

            // We keep two lists. Matches that matches the start of an item always get first priority.
            var matchesStart = new List<Element>();
            var matchesWithin = new List<Element>();

            foreach (var e in m_Tree)
            {
                if (e is GroupElement)
                    continue;

                string name = e.name.ToLower().Replace(" ", "");
                bool didMatchAll = true;
                bool didMatchStart = false;

                // See if we match ALL the seaarch words.
                for (int w = 0; w < searchWords.Length; w++)
                {
                    string search = searchWords[w];

                    if (name.Contains(search))
                    {
                        // If the start of the item matches the first search word, make a note of that.
                        if (w == 0 && name.StartsWith(search))
                            didMatchStart = true;
                    }
                    else
                    {
                        // As soon as any word is not matched, we disregard this item.
                        didMatchAll = false;
                        break;
                    }
                }

                // We always need to match all search words.
                // If we ALSO matched the start, this item gets priority.
                if (didMatchAll)
                {
                    if (didMatchStart)
                        matchesStart.Add(e);
                    else
                        matchesWithin.Add(e);
                }
            }

            matchesStart.Sort();
            matchesWithin.Sort();

            // Create search tree
            var tree = new List<Element>();

            // Add parent
            tree.Add(new GroupElement(0, k_SearchHeader));

            // Add search results
            tree.AddRange(matchesStart);
            tree.AddRange(matchesWithin);

            // Create search result tree
            m_SearchResultTree = tree.ToArray();
            m_Stack.Clear();
            m_Stack.Add(m_SearchResultTree[0] as GroupElement);

            // Always select the first search result when search is changed (e.g. a character was typed in or deleted),
            // because it's usually the best match.
            if (GetChildren(m_ActiveTree, m_ActiveParent).Count >= 1)
                m_ActiveParent.selectedIndex = 0;
            else
                m_ActiveParent.selectedIndex = -1;
        }

        GroupElement GetElementRelative(int rel)
        {
            int i = m_Stack.Count + rel - 1;

            if (i < 0)
                return null;

            return m_Stack[i];
        }

        void GoToParent()
        {
            if (m_Stack.Count > 1)
            {
                m_AnimTarget = 0;
                m_LastTime = DateTime.Now.Ticks;
            }
        }

        void ListGUI(Element[] tree, float anim, GroupElement parent, GroupElement grandParent)
        {
            // Smooth the fractional part of the anim value
            anim = Mathf.Floor(anim) + Mathf.SmoothStep(0, 1, Mathf.Repeat(anim, 1));

            // Calculate rect for animated area
            var animRect = position;
            animRect.x = position.width * (1 - anim) + 1;
            animRect.y = k_HeaderHeight;
            animRect.height -= k_HeaderHeight + k_HelpHeight;
            animRect.width -= 2;

            // Start of animated area (the part that moves left and right)
            GUILayout.BeginArea(animRect);

            // Header
            var headerRect = GUILayoutUtility.GetRect(10, 25);
            string name = parent.name;
            GUI.Label(headerRect, name, s_Styles.header);

            // Back button
            if (grandParent != null)
            {
                var arrowRect = new Rect(headerRect.x + 4, headerRect.y + 7, 13, 13);
                var e = Event.current;

                if (e.type == EventType.Repaint)
                    s_Styles.leftArrow.Draw(arrowRect, false, false, false, false);

                if (e.type == EventType.MouseDown && headerRect.Contains(e.mousePosition))
                {
                    GoToParent();
                    e.Use();
                }
            }

            if (!parent.OnGUI(s_FilterWindow))
                ListGUI(tree, parent);

            GUILayout.EndArea();
        }

        void GoToChild(Element e, bool addIfComponent)
        {
            if (m_Provider.GoToChild(e, addIfComponent))
            {
                Close();
            }
            else if (!m_HasSearch)
            {
                m_LastTime = DateTime.Now.Ticks;

                if (m_AnimTarget == 0)
                {
                    m_AnimTarget = 1;
                }
                else if (Mathf.Approximately(m_Anim, 1f))
                {
                    m_Anim = 0;
                    m_Stack.Add(e as GroupElement);
                }
            }
        }

        void ListGUI(Element[] tree, GroupElement parent)
        {
            // Start of scroll view list
            parent.scroll = GUILayout.BeginScrollView(parent.scroll);

            EditorGUIUtility.SetIconSize(new Vector2(16, 16));

            var children = GetChildren(tree, parent);
            var selectedRect = new Rect();
            var evt = Event.current;

            // Iterate through the children
            for (int i = 0; i < children.Count; i++)
            {
                var e = children[i];
                var r = GUILayoutUtility.GetRect(16, 20, GUILayout.ExpandWidth(true));

                // Select the element the mouse cursor is over.
                // Only do it on mouse move - keyboard controls are allowed to overwrite this until the next time the mouse moves.
                if (evt.type == EventType.MouseMove || evt.type == EventType.MouseDown)
                {
                    if (parent.selectedIndex != i && r.Contains(evt.mousePosition))
                    {
                        parent.selectedIndex = i;
                        Repaint();
                    }
                }

                bool selected = false;

                // Handle selected item
                if (i == parent.selectedIndex)
                {
                    selected = true;
                    selectedRect = r;
                }

                // Draw element
                if (evt.type == EventType.Repaint)
                {
                    var labelStyle = (e is GroupElement) ? s_Styles.groupButton : s_Styles.componentButton;
                    labelStyle.Draw(r, e.content, false, false, selected, selected);

                    if (e is GroupElement)
                    {
                        var arrowRect = new Rect(r.x + r.width - 13, r.y + 4, 13, 13);
                        s_Styles.rightArrow.Draw(arrowRect, false, false, false, false);
                    }
                }

                if (evt.type == EventType.MouseDown && r.Contains(evt.mousePosition))
                {
                    evt.Use();
                    parent.selectedIndex = i;
                    GoToChild(e, true);
                }
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);

            GUILayout.EndScrollView();

            // Scroll to show selected
            if (m_ScrollToSelected && evt.type == EventType.Repaint)
            {
                m_ScrollToSelected = false;
                var scrollRect = GUILayoutUtility.GetLastRect();

                if (selectedRect.yMax - scrollRect.height > parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.yMax - scrollRect.height;
                    Repaint();
                }

                if (selectedRect.y < parent.scroll.y)
                {
                    parent.scroll.y = selectedRect.y;
                    Repaint();
                }
            }
        }

        List<Element> GetChildren(Element[] tree, Element parent)
        {
            var children = new List<Element>();
            int level = -1;
            int i;

            for (i = 0; i < tree.Length; i++)
            {
                if (tree[i] == parent)
                {
                    level = parent.level + 1;
                    i++;
                    break;
                }
            }

            if (level == -1)
                return children;

            for (; i < tree.Length; i++)
            {
                var e = tree[i];

                if (e.level < level)
                    break;

                if (e.level > level && !m_HasSearch)
                    continue;

                children.Add(e);
            }

            return children;
        }
    }
}
