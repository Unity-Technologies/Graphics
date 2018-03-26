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

        void OnMouseDown(MouseDownEvent e)
        {
            if( e.button == 0 && e.clickCount == 1)
            {
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                e.StopPropagation();
                target.TakeMouseCapture();
                m_StartPosition = resizedElement.WorldToLocal(e.mousePosition);
                m_StartSize = new Vector2(resizedElement.style.width,resizedElement.style.height);
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            string targetName = target.name;

            Debug.Log(targetName);

            Vector2 mousePos = resizedElement.WorldToLocal(e.mousePosition);

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
                    if( resizedElement is IVFXMovable)
                    {
                        (resizedElement as IVFXMovable).OnMoved();
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

    class StickyNote : GraphElement
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

        public static readonly Vector2 defaultSize = new Vector2(100, 100);

        public StickyNote(Vector2 position) : this(UXMLHelper.GetUXMLPath("uxml/StickyNote.uxml"), position)
        {
            AddStyleSheetPath("Selectable");
            AddStyleSheetPath("Resizable");
            AddStyleSheetPath("StickyNote");
        }

        public StickyNote(string uiFile, Vector2 position)
        {
            var tpl = EditorGUIUtility.Load(uiFile) as VisualTreeAsset;

            tpl.CloneTree(this, new Dictionary<string, VisualElement>());

            capabilities = Capabilities.Movable | Capabilities.Deletable | Capabilities.Ascendable | Capabilities.Selectable;

            m_Title = this.Q<Label>(name: "title");
            m_Contents = this.Q<Label>(name: "contents");

            if (m_Title != null)
            {
                m_TitleField = m_Title.Q<TextField>(name: "title-field");
                if (m_TitleField != null)
                {
                    m_TitleField.visible = false;
                }
                m_Title.RegisterCallback<MouseDownEvent>(OnTitleMouseDown);
                m_TitleField.RegisterCallback<BlurEvent>(OnTitleBlur);
            }

            if (m_Contents != null)
            {
                m_ContentsField = m_Contents.Q<TextField>(name: "contents-field");
                if (m_ContentsField != null)
                {
                    m_ContentsField.visible = false;
                }
                m_Contents.RegisterCallback<MouseDownEvent>(OnContentsMouseDown);
                m_ContentsField.RegisterCallback<BlurEvent>(OnContentsBlur);
            }

            //SetPosition(new Rect(position, defaultSize));

            style.positionLeft = position.x;
            style.positionTop = position.y;
            style.width = defaultSize.x;
            style.height = defaultSize.y;

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
                evt.menu.AppendSeparator();
            }
        }

        public string contents
        {
            get{return m_Contents.text;}
            set{m_Contents.text = value;}
        }
        public string title
        {
            get{return m_Title.text;}
            set{
                m_Title.text = value;

                if (!string.IsNullOrEmpty(m_Title.text))
                {
                    m_Title.AddToClassList("not-empty");
                }
                else
                {
                    m_Title.RemoveFromClassList("not-empty");
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
        }

        void OnTitleBlur(BlurEvent e)
        {
            bool changed = m_Title.text != m_TitleField.value;
            title = m_TitleField.value;
            m_TitleField.visible = false;

            //Notify change
            if( changed)
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

        void OnTitleMouseDown(MouseDownEvent e)
        {
            if (e.clickCount == 2)
            {
                m_Title.AddToClassList("not-empty");
                m_TitleField.value = m_Title.text;
                m_TitleField.visible = true;

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
        TextField m_TitleField;
        Label m_Contents;
        TextField m_ContentsField;
    }
}
