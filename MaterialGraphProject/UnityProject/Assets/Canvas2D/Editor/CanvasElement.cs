using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEditorInternal.Experimental;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    public class CanvasElement : UnityEditorInternal.Experimental.IBounds
    {
        [Flags]
        public enum Capabilities
        {
            Normal,
            Unselectable,
            DoesNotCollapse,
            Floating
        }

        protected int m_ZIndex;
        protected List<CanvasElement> m_Children = new List<CanvasElement>();
        protected List<CanvasElement> m_Dependencies = new List<CanvasElement>();
        protected Vector3 m_Translation = Vector3.zero;
        protected Vector3 m_Scale = Vector3.one;
        protected bool m_Selected;
        protected bool m_Collapsed;
        protected RenderTexture m_Texture;
        protected CanvasElement m_Parent;
        protected Object m_Target;
        protected bool m_SupportsRenderToTexture = true;
        private bool m_Dirty = true;
        protected Capabilities m_Caps = Capabilities.Normal;

        public event CanvasEvent MouseDown;
        public event CanvasEvent MouseDrag;
        public event CanvasEvent MouseUp;
        public event CanvasEvent DoubleClick;
        public event CanvasEvent ScrollWheel;
        public event CanvasEvent KeyDown;
        public event CanvasEvent OnWidget;
        public event CanvasEvent ContextClick;
        public event CanvasEvent DragPerform;
        public event CanvasEvent DragExited;
        public event CanvasEvent DragUpdated;
        public event CanvasEvent AllEvents;

        private struct AnimationData
        {
            public Vector2 scaleFrom;
            public Vector2 scaleTo;
            public float scaleTime;
        }

        private AnimationData m_Animationdata;

        public Capabilities caps
        {
            get { return m_Caps; }
            set { m_Caps = value; }
        }

        public CanvasElement parent
        {
            get { return m_Parent; }
            set { m_Parent = value; }
        }

        public Object target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        public bool selected
        {
            get { return m_Selected; }
            set
            {
                if ((caps & Capabilities.Unselectable) == Capabilities.Unselectable)
                    value = false;
                m_Selected = value;
                foreach (CanvasElement e in m_Children)
                {
                    e.selected = value;
                }
                Invalidate();
            }
        }

        public bool collapsed
        {
            get { return m_Collapsed; }
            set
            {
                m_Collapsed = value;
                foreach (CanvasElement e in m_Children)
                {
                    e.collapsed = value;
                }
                UpdateModel(UpdateType.Update);
            }
        }

        public T FindParent<T>() where T : class
        {
            if (m_Parent == null)
                return default(T);

            T casted = m_Parent as T;

            if (casted != null)
            {
                return casted;
            }

            return m_Parent.FindParent<T>();
        }

        public Canvas2D ParentCanvas()
        {
            if (this is Canvas2D)
                return this as Canvas2D;

            CanvasElement e = FindTopMostParent();
            if (e is Canvas2D)
                return e as Canvas2D;

            return e.parent as Canvas2D;
        }

        public CanvasElement FindTopMostParent()
        {
            if (m_Parent == null)
                return null;

            if (m_Parent is Canvas2D)
                return this;

            return m_Parent.FindTopMostParent();
        }

        public void ZSort()
        {
            m_Children = m_Children.OrderBy(c => c.zIndex).ToList();
        }

        public bool IsCollapsed()
        {
            if ((caps & Capabilities.DoesNotCollapse) == Capabilities.DoesNotCollapse)
                return false;

            return collapsed;
        }

        public Rect elementRect
        {
            get
            {
                Rect rect = new Rect
                {
                    xMin = m_Translation.x,
                    yMin = m_Translation.y
                };
                rect.xMax = rect.xMin + m_Scale.x;
                rect.yMax = rect.yMin + m_Scale.y;
                return rect;
            }
        }

        public virtual Rect boundingRect
        {
            get
            {
                Rect rect = new Rect
                {
                    xMin = m_Translation.x,
                    yMin = m_Translation.y
                };
                rect.xMax = rect.xMin + m_Scale.x;
                rect.yMax = rect.yMin + m_Scale.y;

                for (int i = 0; i < m_Children.Count; i++)
                {
                    CanvasElement e = m_Children[i];
                    Rect childRect = e.boundingRect;
                    childRect.x += rect.x;
                    childRect.y += rect.y;
                    rect = UnityEditorInternal.Experimental.RectUtils.Encompass(rect, childRect);
                }

                if ((m_Caps & Capabilities.Floating) != 0)
                {
                    var canvas2d = ParentCanvas();
                    var matrix = Matrix4x4.TRS(canvas2d.translation, Quaternion.identity, canvas2d.scale).inverse;
                    Vector3 topCorner = new Vector3(rect.x, rect.y, 0.0f);
                    topCorner = matrix.MultiplyPoint(topCorner);

                    Vector3 bottomCorner = new Vector3(rect.xMax, rect.yMax, 0.0f);
                    bottomCorner = matrix.MultiplyPoint(bottomCorner);

                    rect.x = topCorner.x;
                    rect.y = topCorner.y;
                    rect.width = bottomCorner.x - topCorner.x;
                    rect.height = bottomCorner.y - topCorner.y;
                }
                return rect;
            }
        }

        public Rect canvasBoundingRect
        {
            get
            {
                Rect currentRect = boundingRect;
                CanvasElement p = parent;
                while (p != null)
                {
                    if (p is Canvas2D)
                        break;

                    currentRect.x += p.boundingRect.x;
                    currentRect.y += p.boundingRect.y;
                    p = p.parent;
                }
                return currentRect;
            }
        }

        public int zIndex
        {
            get { return m_ZIndex; }
            set { m_ZIndex = value; }
        }

        public Texture texture
        {
            get { return m_Texture; }
        }

        public Vector3 translation
        {
            get { return m_Translation; }
            set { m_Translation = value; }
        }

        public Vector3 scale
        {
            get { return m_Scale; }
            set { m_Scale = value; }
        }

        public virtual bool Contains(Vector2 point)
        {
            return canvasBoundingRect.Contains(point);
        }

        public virtual bool Intersects(Rect rect)
        {
            if (UnityEditorInternal.Experimental.RectUtils.Contains(rect, canvasBoundingRect))
            {
                return true;
            }
            else if (canvasBoundingRect.Overlaps(rect))
            {
                return true;
            }
            return false;
        }

        public virtual void DeepInvalidate()
        {
            m_Dirty = true;
            foreach (CanvasElement e in m_Children)
            {
                e.DeepInvalidate();
            }

            foreach (CanvasElement d in m_Dependencies)
            {
                d.DeepInvalidate();
            }
        }

        public void Invalidate()
        {
            m_Dirty = true;

            foreach (CanvasElement d in m_Dependencies)
            {
                d.Invalidate();
            }

            CanvasElement theParent = parent;

            if (theParent == null || (theParent is Canvas2D))
                return;

            while (theParent.parent != null && !(theParent.parent is Canvas2D))
            {
                theParent = theParent.parent;
            }

            theParent.DeepInvalidate();
        }

        public void AddManipulator(IManipulate m)
        {
            m.AttachTo(this);
        }

        private void CreateTexture()
        {
            Rect textureRect = boundingRect;
            textureRect.width = Mathf.Max(textureRect.width, 1.0f);
            textureRect.height = Mathf.Max(textureRect.height, 1.0f);
            m_Texture = new RenderTexture((int)textureRect.width, (int)textureRect.height, 0, RenderTextureFormat.ARGB32);
        }

        private RenderTexture m_OldActive;

        public bool PrepareRender()
        {
            if (Event.current.type != EventType.Repaint)
                return false;

            if (!m_Dirty || !m_SupportsRenderToTexture)
                return false;

            var bounds = boundingRect;

            // if null create
            // if size is differnt destroy / create
            if (m_Texture == null)
                CreateTexture();
            else if ((int)bounds.width != m_Texture.width || (int)bounds.height != m_Texture.height)
            {
                Object.DestroyImmediate(m_Texture);
                CreateTexture();
            }

            m_OldActive = RenderTexture.active;
            RenderTexture.active = m_Texture;

            Layout();
            m_Dirty = false;
            return true;
        }

        public void EndRender(float renderTextureHeight)
        {
            RenderTexture.active = m_OldActive;
        }

        bool Collide(Vector2 p)
        {
            return boundingRect.Contains(p);
        }

        public CanvasElement[] Children()
        {
            return m_Children.ToArray();
        }

        public bool HasDependency<T>()
        {
            foreach (CanvasElement d in m_Dependencies)
            {
                if (d is T)
                    return true;
            }
            return false;
        }

        public void AddDependency(CanvasElement e)
        {
            m_Dependencies.Add(e);
        }

        public CanvasElement[] FindChildren<T>()
        {
            List<CanvasElement> filtered = new List<CanvasElement>();
            foreach (CanvasElement e in m_Children)
            {
                CanvasElement[] inner = e.FindChildren<T>();
                filtered.AddRange(inner);

                if (e is T)
                {
                    filtered.Add(e);
                }
            }

            return filtered.ToArray();
        }

        public virtual void DebugDraw()
        {
            Handles.DrawSolidRectangleWithOutline(canvasBoundingRect, new Color(1.0f, 0.0f, 0.0f, 0.2f), new Color(1.0f, 0.0f, 0.0f, 0.4f));
            foreach (CanvasElement e in m_Children)
            {
                e.DebugDraw();
            }
        }

        public virtual void Clear()
        {
            m_Texture = null;
            m_Children.Clear();
        }

        public virtual void UpdateModel(UpdateType t)
        {
            foreach (CanvasElement c in m_Children)
            {
                c.UpdateModel(t);
            }
            foreach (CanvasElement e in m_Dependencies)
            {
                e.UpdateModel(t);
            }
        }

        public virtual void AddChild(CanvasElement e)
        {
            e.parent = this;
            if (!(e.parent is Canvas2D))
            {
                e.collapsed = collapsed;
            }
            m_Children.Add(e);
        }

        public virtual void Layout()
        {
            foreach (CanvasElement e in m_Children)
            {
                e.Layout();
            }
        }

        public virtual void OnRenderList(List<CanvasElement> visibleList, Canvas2D parent, bool renderWidgets)
        {
            Rect screenRect = new Rect
            {
                min = parent.MouseToCanvas(parent.clientRect.min),
                max = parent.MouseToCanvas(new Vector2(parent.clientRect.width, parent.clientRect.height))
            };
            Rect thisRect = boundingRect;
            for (int i = 0; i < visibleList.Count; i++)
            {
                CanvasElement e = visibleList[i];
                if (e.texture != null)
                {
                    float ratioY = 1.0f;
                    float ratioX = 1.0f;
                    Rect r = new Rect(e.translation.x, e.translation.y, e.texture.width, e.texture.height);
                    if (r.y < screenRect.y)
                    {
                        float overlap = (screenRect.y - r.y);
                        r.y = screenRect.y;
                        r.height -= overlap;
                        if (r.height < 0.0f)
                            r.height = 0.0f;
                        ratioY = r.height / e.texture.height;
                    }

                    if (r.xMax > screenRect.xMax)
                    {
                        float overlap = r.xMax - screenRect.xMax;
                        r.width -= overlap;
                        if (r.width < 0.0f)
                            r.width = 0.0f;
                        ratioX = r.width / e.texture.width;
                    }

                    Graphics.DrawTexture(r, e.texture, new Rect(0, 0, ratioX, ratioY), 0, 0, 0, 0);
                }
                else
                    e.Render(thisRect, parent);

                e.RenderWidgets(parent);
            }

            if (OnWidget != null && renderWidgets)
                OnWidget(this, Event.current, parent);
        }

        private void RenderWidgets(Canvas2D parent)
        {
            if (OnWidget != null)
            {
                OnWidget(this, Event.current, parent);
            }

            foreach (CanvasElement e in m_Children)
            {
                e.RenderWidgets(parent);
            }
        }

        public virtual bool DispatchEvents(Event evt, Canvas2D parent)
        {
            foreach (CanvasElement e in m_Children)
            {
                if (e.DispatchEvents(evt, parent))
                    return true;
            }

            return FireEvents(evt, parent, this);
        }

        public bool FireEvents(Event evt, Canvas2D parent, CanvasElement target)
        {
            if (parent != this && (evt.type == EventType.MouseDown))
            {
                if (!Contains(parent.MouseToCanvas(evt.mousePosition)))
                {
                    return false;
                }
            }

            if (target == null)
            {
                target = this;
            }

            bool handled = false;
            if (AllEvents != null)
            {
                bool wasNotUsed = evt.type != EventType.Used;

                AllEvents(target, evt, parent);
                if (wasNotUsed && evt.type == EventType.Used)
                {
                    parent.LogInfo("AllEvent handler on " + target);
                }
            }

            switch (evt.type)
            {
                case EventType.MouseUp:
                    handled = MouseUp == null ? false : MouseUp(target, evt, parent);
                    break;
                case EventType.MouseDown:
                {
                    if (evt.clickCount < 2)
                    {
                        handled = MouseDown == null ? false : MouseDown(target, evt, parent);
                        break;
                    }
                    else
                    {
                        handled = DoubleClick == null ? false : DoubleClick(target, evt, parent);
                        break;
                    }
                }
                case EventType.MouseDrag:
                    handled = MouseDrag == null ? false : MouseDrag(target, evt, parent);
                    break;
                case EventType.DragPerform:
                    handled = DragPerform == null ? false : DragPerform(target, evt, parent);
                    break;
                case EventType.DragExited:
                    handled = DragExited == null ? false : DragExited(target, evt, parent);
                    break;
                case EventType.DragUpdated:
                    handled = DragUpdated == null ? false : DragUpdated(target, evt, parent);
                    break;
                case EventType.ScrollWheel:
                    handled = ScrollWheel == null ? false : ScrollWheel(target, evt, parent);
                    break;
                case EventType.KeyDown:
                    handled = KeyDown == null ? false : KeyDown(target, evt, parent);
                    break;
                case EventType.ContextClick:
                    handled = ContextClick == null ? false : ContextClick(target, evt, parent);
                    break;
            }

            return handled;
        }

        public virtual void Render(Rect parentRect, Canvas2D canvas)
        {
            foreach (CanvasElement e in m_Children)
            {
                e.Render(parentRect, canvas);
            }

            if (OnWidget != null)
                OnWidget(this, Event.current, canvas);
        }

        public void AnimateScale(Vector2 from, Vector2 to)
        {
            var canvas = ParentCanvas();
            m_Animationdata.scaleFrom = from;
            m_Animationdata.scaleTo = to;
            m_Animationdata.scaleTime = 0.0f;
            canvas.OnLayout += OnAnimateScaleDelegate;
            canvas.Invalidate();
        }

        private bool OnAnimateScaleDelegate(CanvasElement element, Event e, Canvas2D parent)
        {
            parent.Invalidate();
            Invalidate();
            scale = Vector2.Lerp(m_Animationdata.scaleFrom, m_Animationdata.scaleTo, m_Animationdata.scaleTime);
            m_Animationdata.scaleTime += 0.01f;
            if (m_Animationdata.scaleTime > 1.0f)
            {
                parent.OnLayout -= OnAnimateScaleDelegate;
            }
            return false;
        }
    }
}
