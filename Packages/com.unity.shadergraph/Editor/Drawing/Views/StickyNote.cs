using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Interfaces;

namespace UnityEditor.ShaderGraph.Drawing
{
    class StickyNodeChangeEvent : EventBase<StickyNodeChangeEvent>
    {
        public static StickyNodeChangeEvent GetPooled(StickyNote target, Change change)
        {
            var evt = GetPooled();
            evt.target = target;
            evt.change = change;
            return evt;
        }

        public enum Change
        {
            title,
            contents,
            theme,
            textSize,
        }

        public Change change { get; protected set; }
    }

    class StickyNote : GraphElement, ISGResizable
    {
        GraphData m_Graph;
        public new StickyNoteData userData
        {
            get => (StickyNoteData)base.userData;
            set => base.userData = value;
        }

        public enum Theme
        {
            Classic,
            Black,
            Orange,
            Green,
            Blue,
            Red,
            Purple,
            Teal
        }

        Theme m_Theme = Theme.Classic;
        public Theme theme
        {
            get
            {
                return m_Theme;
            }
            set
            {
                if (m_Theme != value)
                {
                    m_Theme = value;
                    UpdateThemeClasses();
                }
            }
        }

        public enum TextSize
        {
            Small,
            Medium,
            Large,
            Huge
        }

        TextSize m_TextSize = TextSize.Medium;

        public TextSize textSize
        {
            get { return m_TextSize; }
            set
            {
                if (m_TextSize != value)
                {
                    m_TextSize = value;
                    UpdateSizeClasses();
                }
            }
        }

        public virtual void OnStartResize()
        {
        }

        public virtual void OnResized()
        {
            userData.position = new Rect(resolvedStyle.left, resolvedStyle.top, style.width.value.value, style.height.value.value);
        }

        public bool CanResizePastParentBounds()
        {
            return true;
        }

        Vector2 AllExtraSpace(VisualElement element)
        {
            return new Vector2(
                element.resolvedStyle.marginLeft + element.resolvedStyle.marginRight + element.resolvedStyle.paddingLeft + element.resolvedStyle.paddingRight + element.resolvedStyle.borderRightWidth + element.resolvedStyle.borderLeftWidth,
                element.resolvedStyle.marginTop + element.resolvedStyle.marginBottom + element.resolvedStyle.paddingTop + element.resolvedStyle.paddingBottom + element.resolvedStyle.borderBottomWidth + element.resolvedStyle.borderTopWidth
            );
        }

        void OnFitToText(DropdownMenuAction a)
        {
            FitText(false);
        }

        public void FitText(bool onlyIfSmaller)
        {
            Vector2 preferredTitleSize = Vector2.zero;
            if (!string.IsNullOrEmpty(m_Title.text))
                preferredTitleSize = m_Title.MeasureTextSize(m_Title.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined); // This is the size of the string with the current title font and such

            preferredTitleSize += AllExtraSpace(m_Title);
            preferredTitleSize.x += m_Title.ChangeCoordinatesTo(this, Vector2.zero).x + resolvedStyle.width - m_Title.ChangeCoordinatesTo(this, new Vector2(m_Title.layout.width, 0)).x;

            Vector2 preferredContentsSizeOneLine = m_Contents.MeasureTextSize(m_Contents.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);

            Vector2 contentExtraSpace = AllExtraSpace(m_Contents);
            preferredContentsSizeOneLine += contentExtraSpace;

            Vector2 extraSpace = new Vector2(resolvedStyle.width, resolvedStyle.height) - m_Contents.ChangeCoordinatesTo(this, new Vector2(m_Contents.layout.width, m_Contents.layout.height));
            extraSpace += m_Title.ChangeCoordinatesTo(this, Vector2.zero);
            preferredContentsSizeOneLine += extraSpace;

            float width = 0;
            float height = 0;
            // The content in one line is smaller than the current width.
            // Set the width to fit both title and content.
            // Set the height to have only one line in the content
            if (preferredContentsSizeOneLine.x < Mathf.Max(preferredTitleSize.x, resolvedStyle.width))
            {
                width = Mathf.Max(preferredContentsSizeOneLine.x, preferredTitleSize.x);
                height = preferredContentsSizeOneLine.y + preferredTitleSize.y;
            }
            else // The width is not enough for the content: keep the width or use the title width if bigger.
            {
                width = Mathf.Max(preferredTitleSize.x + extraSpace.x, resolvedStyle.width);
                float contextWidth = width - extraSpace.x - contentExtraSpace.x;
                Vector2 preferredContentsSize = m_Contents.MeasureTextSize(m_Contents.text, contextWidth, MeasureMode.Exactly, 0, MeasureMode.Undefined);

                preferredContentsSize += contentExtraSpace;

                height = preferredTitleSize.y + preferredContentsSize.y + extraSpace.y;
            }
            if (!onlyIfSmaller || resolvedStyle.width < width)
                style.width = width;
            if (!onlyIfSmaller || resolvedStyle.height < height)
                style.height = height;
            OnResized();
        }

        void UpdateThemeClasses()
        {
            foreach (Theme value in System.Enum.GetValues(typeof(Theme)))
            {
                if (m_Theme != value)
                {
                    RemoveFromClassList("theme-" + value.ToString().ToLower());
                }
                else
                {
                    AddToClassList("theme-" + value.ToString().ToLower());
                }
            }
        }

        void UpdateSizeClasses()
        {
            foreach (TextSize value in System.Enum.GetValues(typeof(TextSize)))
            {
                if (m_TextSize != value)
                {
                    RemoveFromClassList("size-" + value.ToString().ToLower());
                }
                else
                {
                    AddToClassList("size-" + value.ToString().ToLower());
                }
            }
        }

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public StickyNote(Rect position, GraphData graph) : this("UXML/StickyNote", position, graph)
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Selectable"));
            styleSheets.Add(Resources.Load<StyleSheet>("StickyNote"));
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public string displayName => $"{m_Title.text} (Sticky Note)";

        public StickyNote(string uiFile, Rect position, GraphData graph)
        {
            m_Graph = graph;
            var tpl = Resources.Load<VisualTreeAsset>(uiFile);

            tpl.CloneTree(this);

            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Selectable | Capabilities.Copiable | Capabilities.Groupable;

            m_Title = this.Q<Label>(name: "title");
            if (m_Title != null)
            {
                m_Title.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            }

            m_TitleField = this.Q<TextField>(name: "title-field");
            if (m_TitleField != null)
            {
                m_TitleField.style.display = DisplayStyle.None;
                m_TitleField.Q("unity-text-input").RegisterCallback<BlurEvent>(OnTitleBlur, TrickleDown.TrickleDown);
                m_TitleField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            }

            m_Contents = this.Q<Label>(name: "contents");
            if (m_Contents != null)
            {
                m_ContentsField = m_Contents.Q<TextField>(name: "contents-field");
                if (m_ContentsField != null)
                {
                    m_ContentsField.style.display = DisplayStyle.None;
                    m_ContentsField.multiline = true;
                    m_ContentsField.Q("unity-text-input").RegisterCallback<BlurEvent>(OnContentsBlur, TrickleDown.TrickleDown);
                }
                m_Contents.RegisterCallback<MouseDownEvent>(OnContentsMouseDown);
            }

            SetPosition(new Rect(position.x, position.y, defaultSize.x, defaultSize.y));

            AddToClassList("sticky-note");
            AddToClassList("selectable");
            UpdateThemeClasses();
            UpdateSizeClasses();
            // Manually set the layer of the sticky note so it's always on top. This used to be in the uss
            // but that causes issues with re-laying out at times that can do weird things to selection.
            this.layer = -100;

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is StickyNote)
            {
                /*foreach (Theme value in System.Enum.GetValues(typeof(Theme)))
                {
                    evt.menu.AppendAction("Theme/" + value.ToString(), OnChangeTheme, e => DropdownMenu.MenuAction.StatusFlags.Normal, value);
                }*/
                if (theme == Theme.Black)
                    evt.menu.AppendAction("Light Theme", OnChangeTheme, e => DropdownMenuAction.Status.Normal, Theme.Classic);
                else
                    evt.menu.AppendAction("Dark Theme", OnChangeTheme, e => DropdownMenuAction.Status.Normal, Theme.Black);
                evt.menu.AppendSeparator();
                foreach (TextSize value in System.Enum.GetValues(typeof(TextSize)))
                {
                    evt.menu.AppendAction(value.ToString() + " Text Size", OnChangeSize, e => DropdownMenuAction.Status.Normal, value);
                }
                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Fit To Text", OnFitToText, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();

                evt.menu.AppendAction("Delete", OnDelete, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnDelete(DropdownMenuAction menuAction)
        {
            m_Graph.owner.RegisterCompleteObjectUndo("Delete Sticky Note");
            m_Graph.RemoveStickyNote(userData);
        }

        void OnTitleChange(EventBase e)
        {
            //m_Graph.owner.RegisterCompleteObjectUndo("Title Changed");
            //title = m_TitleField.value;
            //userData.title = title;
        }

        public PropertySheet GetInspectorContent()
        {
            var sheet = new PropertySheet();
            return sheet;
        }

        const string fitTextClass = "fit-text";

        public override void SetPosition(Rect rect)
        {
            style.left = rect.x;
            style.top = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }

        public override Rect GetPosition()
        {
            return new Rect(resolvedStyle.left, resolvedStyle.top, resolvedStyle.width, resolvedStyle.height);
        }

        public string contents
        {
            get { return m_Contents.text; }
            set
            {
                if (m_Contents != null)
                {
                    m_Contents.text = value;
                }
            }
        }
        public new string title
        {
            get { return m_Title.text; }
            set
            {
                if (m_Title != null)
                {
                    m_Title.text = value;

                    if (!string.IsNullOrEmpty(m_Title.text))
                    {
                        m_Title.RemoveFromClassList("empty");
                    }
                    else
                    {
                        m_Title.AddToClassList("empty");
                    }
                    //UpdateTitleHeight();
                }
            }
        }

        void OnChangeTheme(DropdownMenuAction action)
        {
            theme = (Theme)action.userData;
            NotifyChange(StickyNodeChangeEvent.Change.theme);
        }

        void OnChangeSize(DropdownMenuAction action)
        {
            textSize = (TextSize)action.userData;
            NotifyChange(StickyNodeChangeEvent.Change.textSize);
            //panel.InternalValidateLayout();

            FitText(true);
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            //UpdateTitleHeight();
        }

        void OnTitleBlur(BlurEvent e)
        {
            //bool changed = m_Title.text != m_TitleField.value;
            title = m_TitleField.value;
            m_TitleField.style.display = DisplayStyle.None;

            m_Title.UnregisterCallback<GeometryChangedEvent>(OnTitleRelayout);

            //Notify change
            //if( changed)
            {
                NotifyChange(StickyNodeChangeEvent.Change.title);
            }
        }

        void OnContentsBlur(BlurEvent e)
        {
            bool changed = m_Contents.text != m_ContentsField.value;
            m_Contents.text = m_ContentsField.value;
            m_ContentsField.style.display = DisplayStyle.None;

            //Notify change
            if (changed)
            {
                NotifyChange(StickyNodeChangeEvent.Change.contents);
            }
        }

        void OnTitleRelayout(GeometryChangedEvent e)
        {
            UpdateTitleFieldRect();
        }

        void UpdateTitleFieldRect()
        {
            Rect rect = m_Title.layout;
            m_Title.parent.ChangeCoordinatesTo(m_TitleField.parent, rect);

            m_TitleField.style.left = rect.xMin - 1;
            m_TitleField.style.right = rect.yMin + m_Title.resolvedStyle.marginTop;
            m_TitleField.style.width = rect.width - m_Title.resolvedStyle.marginLeft - m_Title.resolvedStyle.marginRight;
            m_TitleField.style.height = rect.height - m_Title.resolvedStyle.marginTop - m_Title.resolvedStyle.marginBottom;
        }

        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                m_TitleField.RemoveFromClassList("empty");
                m_TitleField.value = m_Title.text;
                m_TitleField.style.display = DisplayStyle.Flex;
                UpdateTitleFieldRect();
                m_Title.RegisterCallback<GeometryChangedEvent>(OnTitleRelayout);

                m_TitleField.Q("unity-text-input").Focus();
                m_TitleField.SelectAll();

                e.StopPropagation();
                focusController.IgnoreEvent(e);
            }
        }

        void NotifyChange(StickyNodeChangeEvent.Change change)
        {
            m_Graph.owner.RegisterCompleteObjectUndo($"Change Sticky Note {change.ToString()}");
            if (change == StickyNodeChangeEvent.Change.title)
            {
                userData.title = title;
            }
            else if (change == StickyNodeChangeEvent.Change.contents)
            {
                userData.content = contents;
            }
            else if (change == StickyNodeChangeEvent.Change.textSize)
            {
                userData.textSize = (int)textSize;
            }
            else if (change == StickyNodeChangeEvent.Change.theme)
            {
                userData.theme = (int)theme;
            }
        }

        public System.Action<StickyNodeChangeEvent.Change> OnChange;

        void OnContentsMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                m_ContentsField.value = m_Contents.text;
                m_ContentsField.style.display = DisplayStyle.Flex;
                m_ContentsField.Q("unity-text-input").Focus();
                e.StopPropagation();
                focusController.IgnoreEvent(e);
            }
        }

        Label m_Title;
        protected TextField m_TitleField;
        Label m_Contents;
        protected TextField m_ContentsField;
    }
}
