using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Reflection;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class Resizable : Manipulator
    {
        
        public enum Direction
        {
            Horizontal = 1<<0,
            Vertical = 1<<1,
            Both = Vertical | Horizontal
        }

        public readonly Direction direction;

        public readonly VisualElement resizedElement;

        public Resizable(VisualElement resizedElement, Direction direction)
        {
            this.direction = direction;
            this.resizedElement = resizedElement;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        Vector2 m_StartPosition;
        Vector2 m_StartSize;

        bool m_DragStarted = false;

        void OnMouseDown(MouseDownEvent e)
        {
            if( e.button == 0 && e.clickCount == 1)
            {
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                e.StopPropagation();
                target.TakeMouseCapture();
                m_StartPosition = resizedElement.WorldToLocal(e.mousePosition);
                m_StartSize = new Vector2(resizedElement.style.width,resizedElement.style.height);
                m_DragStarted = false;
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            Vector2 mousePos = resizedElement.WorldToLocal(e.mousePosition);
            if( !m_DragStarted)
            {
                if( resizedElement is IVFXResizable)
                {
                    (resizedElement as IVFXResizable).OnStartResize();
                }
                m_DragStarted = true;
            }

            if( (direction & Direction.Horizontal) != 0 )
            {
                resizedElement.style.width = m_StartSize.x + mousePos.x - m_StartPosition.x;
            }
            if( (direction & Direction.Vertical) != 0 )
            {
                resizedElement.style.height = m_StartSize.y + mousePos.y - m_StartPosition.y;
            }
            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if( e.button == 0 )
            {
                if( resizedElement.style.width != m_StartSize.x || resizedElement.style.height != m_StartSize.y )
                {
                    if( resizedElement is IVFXResizable)
                    {
                        (resizedElement as IVFXResizable).OnResized();
                    }
                }
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
                target.ReleaseMouseCapture();
                e.StopPropagation();
            }
        }
    }


    class StickyNodeChangeEvent :  EventBase<StickyNodeChangeEvent>, IPropagatableEvent
    {
        public static StickyNodeChangeEvent GetPooled(StickyNote target,Change change)
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
            textSize
        }

        public Change change{get; protected set;}
    }

    class StickyNote : GraphElement, IVFXResizable
    {
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
                if( m_Theme != value)
                {
                    m_Theme = value;
                    UpdateThemeClasses();
                }
            }
        }

        public enum TextSize
        {
            small,
            medium,
            large,
            huge
        }

        TextSize m_TextSize;

        public TextSize textSize
        {
            get{return m_TextSize;}
            set
            {
                if( m_TextSize != value)
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
        }

        Vector2 AllExtraSpace(VisualElement element)
        {
            return new Vector2(
                element.style.marginLeft + element.style.marginRight + element.style.paddingLeft + element.style.paddingRight + element.style.borderRightWidth + element.style.borderLeftWidth,
                element.style.marginTop + element.style.marginBottom + element.style.paddingTop + element.style.paddingBottom + element.style.borderBottomWidth + element.style.borderTopWidth
            );
        }

        void OnFitToText(ContextualMenu.MenuAction a)
        {
            FitText();
        }

        public void FitText()
        {
            Vector2 preferredTitleSize = Vector2.zero;
            if( ! string.IsNullOrEmpty(m_Title.text))
                preferredTitleSize = m_Title.DoMeasure(0,MeasureMode.Undefined,0,MeasureMode.Undefined); // This is the size of the string with the current title font and such

            preferredTitleSize += AllExtraSpace(m_Title);
            preferredTitleSize.x += m_Title.ChangeCoordinatesTo(this,Vector2.zero).x + style.width - m_Title.ChangeCoordinatesTo(this,new Vector2(m_Title.layout.width,0)).x ;

            Vector2 preferredContentsSizeOneLine = m_Contents.DoMeasure(0,MeasureMode.Undefined,0,MeasureMode.Undefined);

            Vector2 contentExtraSpace = AllExtraSpace(m_Contents);
            preferredContentsSizeOneLine += contentExtraSpace;

            Vector2 extraSpace = new Vector2(style.width,style.height) - m_Contents.ChangeCoordinatesTo(this,new Vector2(m_Contents.layout.width,m_Contents.layout.height));
            extraSpace += m_Title.ChangeCoordinatesTo(this,Vector2.zero);
            preferredContentsSizeOneLine += extraSpace;

            float width = 0;
            float height = 0;
            // The content in one line is smaller than the current width. 
            // Set the width to fit both title and content.
            // Set the height to have only one line in the content
            if( preferredContentsSizeOneLine.x < Mathf.Max(preferredTitleSize.x,style.width))
            {
                width = Mathf.Max(preferredContentsSizeOneLine.x,preferredTitleSize.x);
                height = preferredContentsSizeOneLine.y + preferredTitleSize.y;
            }
            else // The width is not enough for the content: keep the width or use the title width if bigger.
            {

                width = Mathf.Max(preferredTitleSize.x + extraSpace.x,style.width); 
                float contextWidth = width - extraSpace.x - contentExtraSpace.x;
                Vector2 preferredContentsSize = m_Contents.DoMeasure(contextWidth,MeasureMode.Exactly,0,MeasureMode.Undefined);

                preferredContentsSize += contentExtraSpace;

                height = preferredTitleSize.y + preferredContentsSize.y + extraSpace.y;
            }

            style.width = width;
            style.height = height;
            OnResized();
        }

        void UpdateThemeClasses()
        {
            foreach(Theme value in System.Enum.GetValues(typeof(Theme)))
            {
                if( m_Theme != value)
                {
                    RemoveFromClassList("theme-"+value.ToString().ToLower());
                }
                else
                {
                    AddToClassList("theme-"+value.ToString().ToLower());
                }
            }
        }

        void UpdateSizeClasses()
        {
            foreach(TextSize value in System.Enum.GetValues(typeof(TextSize)))
            {
                if( m_TextSize != value)
                {
                    RemoveFromClassList("size-"+value.ToString().ToLower());
                }
                else
                {
                    AddToClassList("size-"+value.ToString().ToLower());
                }
            }
        }

        public static readonly Vector2 defaultSize = new Vector2(200, 160);

        public StickyNote(Vector2 position) : this(UXMLHelper.GetUXMLPath("uxml/StickyNote.uxml"), position)
        {
            AddStyleSheetPath("Selectable");
            AddStyleSheetPath("Resizable");
            AddStyleSheetPath("StickyNote");
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        }

        public StickyNote(string uiFile, Vector2 position)
        {
            var tpl = EditorGUIUtility.Load(uiFile) as VisualTreeAsset;

            tpl.CloneTree(this, new Dictionary<string, VisualElement>());

            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Selectable;

            m_Title = this.Q<Label>(name: "title");
            if (m_Title != null)
            {
                
                m_Title.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
            }
            
            m_TitleField = this.Q<TextField>(name: "title-field");
            if (m_TitleField != null)
            {
                m_TitleField.visible = false;
                m_TitleField.RegisterCallback<BlurEvent>(OnTitleBlur);
                m_TitleField.RegisterCallback<ChangeEvent<string>>(OnTitleChange);
            }


            m_Contents = this.Q<Label>(name: "contents");
            if (m_Contents != null)
            {
                m_ContentsField = m_Contents.Q<TextField>(name: "contents-field");
                if (m_ContentsField != null)
                {
                    m_ContentsField.visible = false;
                    m_ContentsField.multiline = true;
                    m_ContentsField.RegisterCallback<BlurEvent>(OnContentsBlur);
                }
                m_Contents.RegisterCallback<MouseDownEvent>(OnContentsMouseDown);
            }

            SetPosition(new Rect(position, defaultSize));

            AddToClassList("sticky-note");
            AddToClassList("selectable");
            UpdateThemeClasses();
            UpdateSizeClasses();

            var horizontalResizer = this.Q("horizontal-resize");
            if( horizontalResizer != null)
                horizontalResizer.AddManipulator(new Resizable(this,Resizable.Direction.Horizontal));
            var verticalResizer = this.Q("vertical-resize");
            if( verticalResizer != null)
                verticalResizer.AddManipulator(new Resizable(this,Resizable.Direction.Vertical));
            var cornerResizer = this.Q("corner-resize");
            if( cornerResizer != null)
                cornerResizer.AddManipulator(new Resizable(this,Resizable.Direction.Both));

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is StickyNote)
            {
                foreach(Theme value in System.Enum.GetValues(typeof(Theme)))
                {
                    evt.menu.AppendAction("Theme/" + value.ToString(), OnChangeTheme, e => ContextualMenu.MenuAction.StatusFlags.Normal,value);
                }

                foreach(TextSize value in System.Enum.GetValues(typeof(TextSize)))
                {
                    evt.menu.AppendAction("Text Size/" + value.ToString(), OnChangeSize, e => ContextualMenu.MenuAction.StatusFlags.Normal,value);
                }

                evt.menu.AppendAction("Fit To Text",OnFitToText, e => ContextualMenu.MenuAction.StatusFlags.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnTitleChange(EventBase e)
        {
            title = m_TitleField.value;
        }


        const string fitTextClass = "fit-text";

        public override void SetPosition(Rect rect)
        {
            style.positionLeft = rect.x;
            style.positionTop = rect.y;
            style.width = rect.width;
            style.height = rect.height;
        }

        public override Rect GetPosition()
        {
            return new Rect(style.positionLeft,style.positionTop,style.width,style.height);
        }

        public string contents
        {
            get{return m_Contents.text;}
            set{
                if(m_Contents != null)
                {
                    m_Contents.text = value;
                }
            }
        }
        public string title
        {
            get{return m_Title.text;}
            set{
                if( m_Title != null)
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

        void OnChangeTheme(ContextualMenu.MenuAction action)
        {
            theme = (Theme)action.userData;
            NotifyChange(StickyNodeChangeEvent.Change.theme);
        }

        void OnChangeSize(ContextualMenu.MenuAction action)
        {
            textSize = (TextSize)action.userData;
            NotifyChange(StickyNodeChangeEvent.Change.textSize);
            elementPanel.ValidateLayout();
                
            FitText();
        }

        void OnAttachToPanel(AttachToPanelEvent e)
        {
            //UpdateTitleHeight();
        }

        void OnTitleBlur(BlurEvent e)
        {
            bool changed = m_Title.text != m_TitleField.value;
            title = m_TitleField.value;
            m_TitleField.visible = false;

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
            m_ContentsField.visible = false;

            //Notify change
            if( changed)
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
            //if( m_Title != m_TitleField.parent)
                m_Title.parent.ChangeCoordinatesTo(m_TitleField.parent,rect);

            m_TitleField.style.positionLeft = rect.xMin/* + m_Title.style.marginLeft*/;
            m_TitleField.style.positionRight = rect.yMin + m_Title.style.marginTop;
            m_TitleField.style.width = rect.width - m_Title.style.marginLeft - m_Title.style.marginRight;
            m_TitleField.style.height = rect.height - m_Title.style.marginTop - m_Title.style.marginBottom;
        }

        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                m_TitleField.RemoveFromClassList("empty");
                m_TitleField.value = m_Title.text;
                m_TitleField.visible = true;
                UpdateTitleFieldRect();
                m_Title.RegisterCallback<GeometryChangedEvent>(OnTitleRelayout);

                m_TitleField.Focus();
                m_TitleField.SelectAll();

                e.StopPropagation();
                e.PreventDefault();

            }
        }

        void NotifyChange(StickyNodeChangeEvent.Change change)
        {
            using (StickyNodeChangeEvent evt = StickyNodeChangeEvent.GetPooled(this,change))
            {
                panel.dispatcher.DispatchEvent(evt,panel);
            }
        }

        void OnContentsMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                m_ContentsField.value = m_Contents.text;
                m_ContentsField.visible = true;
                m_ContentsField.Focus();
                e.StopPropagation();
                e.PreventDefault();
            }
        }

        Label m_Title;
        protected TextField m_TitleField;
        Label m_Contents;
        protected TextField m_ContentsField;
    }
}
