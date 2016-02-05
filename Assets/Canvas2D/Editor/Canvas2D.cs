using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditorInternal;
using UnityEditorInternal.Experimental;
using Object = UnityEngine.Object;

//#pragma warning disable 0414
//#pragma warning disable 0219

namespace UnityEditor.Experimental
{
    public delegate bool CanvasEvent(CanvasElement element, Event e, Canvas2D parent);
    public delegate void ModalWindowProc(Canvas2D parent);
    public delegate void CanvasAnimationCallback(CanvasElement element, CanvasAnimation owner, object userData);

    public class Canvas2D : CanvasElement
    {
        protected List<CanvasElement> m_Elements = new List<CanvasElement>();
        protected List<CanvasElement> m_Selection = new List<CanvasElement>();

        private QuadTree<CanvasElement> m_QuadTree = new QuadTree<CanvasElement>();
        private Rect m_CanvasRect;
        private Vector2 m_ViewOffset;
        private Vector2 m_ViewOffsetUnscaled;
        private bool m_ShowDebug;
        private string m_DebugEventName = "";
        private RenderTexture m_RenderTexture;
        private float m_ScreenHeightOffset = 12;
        public Rect debugRect = new Rect();
        public event CanvasEvent OnBackground;
        public event CanvasEvent OnOverlay;
        public event CanvasEvent OnLayout;
        private EditorWindow m_HostWindow;
        private ICanvasDataSource m_DataSource;
        private Rect m_ClientRectangle;
        private ModalWindowProc m_CurrentModalWindow;
        private Rect m_CurrentModalWindowRect;
        private List<CanvasAnimation> m_Animations = new List<CanvasAnimation>();

        public ICanvasDataSource dataSource
        {
            get { return m_DataSource; }
            set { m_DataSource = value; }
        }

        internal class CaptureSession
        {
            public CanvasElement callbacks;
            public List<CanvasElement> targets = new List<CanvasElement>();
            public IManipulate manipulator;
            public bool isRunning;
            public bool isEnding;
        }

        private CaptureSession m_CaptureSession;

        public Rect clientRect
        {
            get { return m_ClientRectangle; }
        }

        public Vector2 viewOffset
        {
            get { return m_ViewOffset; }
        }

        public Rect canvasRect
        {
            get { return m_CanvasRect; }
        }

        public List<CanvasElement> elements
        {
            get { return m_Children; }
        }

        public List<CanvasElement> selection
        {
            get { return m_Selection; }
        }

        public bool showQuadTree
        {
            get { return m_ShowDebug; }
            set { m_ShowDebug = value; }
        }

        public void ReleaseTextures()
        {
            if (m_RenderTexture)
            {
                m_RenderTexture.Release();
                m_RenderTexture = null;
            }
        }

        public void RecreateRenderTexture()
        {
            if (m_RenderTexture)
            {
                if (m_RenderTexture.IsCreated())
                    return;

                m_RenderTexture.Release();
                m_RenderTexture = null;
            }

            m_RenderTexture = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            m_RenderTexture.useMipMap = true;
            m_RenderTexture.generateMips = true;
            m_RenderTexture.antiAliasing = 1;
            m_RenderTexture.filterMode = FilterMode.Trilinear;
            m_RenderTexture.Create();
        }

        public void ReloadData()
        {
            if (m_DataSource == null)
                return;
            Clear();
            CanvasElement[] elems = m_DataSource.FetchElements();
            foreach (var e in elems)
                AddChild(e);
            ZSort();
        }

        public override void Clear()
        {
            if (m_Elements != null)
                m_Elements.Clear();

            if (m_Selection != null)
                m_Selection.Clear();

            if (m_QuadTree != null)
                m_QuadTree.Clear();

            m_Children.Clear();
            if (m_Children != null)
            {
                /*foreach (CanvasElement e in m_Children)
                    {
                        e.Clear();
                    }
                    m_Children.Clear();*/
            }
        }

        public override void DeepInvalidate()
        {
            foreach (CanvasElement e in m_Children)
            {
                e.DeepInvalidate();
            }
        }

        public Vector2 MouseToCanvas(Vector2 lhs)
        {
            return new Vector2((lhs.x - m_Translation.x) / m_Scale.x, (lhs.y - m_Translation.y) / m_Scale.y) + (m_ViewOffset / 2.0f);
        }

        public Vector2 CanvasToScreen(Vector2 lhs)
        {
            return new Vector2((lhs.x * m_Scale.x) + m_Translation.x, (lhs.y * m_Scale.y) + m_Translation.y);
        }

        public Rect CanvasToScreen(Rect r)
        {
            Vector3 t = m_Translation;
            t -= new Vector3(m_ViewOffsetUnscaled.x / 2.0f, m_ViewOffsetUnscaled.y / 2.0f, 0.0f);
            Matrix4x4 mm = Matrix4x4.TRS(t, Quaternion.identity, m_Scale);


            Vector3 offset = new Vector3(0.0f, 0.0f, 0.0f);

            Vector3[] points =
            {
                new Vector3(r.xMin, r.yMin, 0.0f),
                new Vector3(r.xMax, r.yMin, 0.0f),
                new Vector3(r.xMax, r.yMax, 0.0f),
                new Vector3(r.xMin, r.yMax, 0.0f)
            };

            for (int a = 0; a < 4; a++)
            {
                points[a] = mm.MultiplyPoint(points[a]);
                points[a] += offset;
            }

            return new Rect(points[0].x, points[0].y, points[2].x - points[0].x, points[2].y - points[0].y);
        }

        public Canvas2D(Object target, EditorWindow host, ICanvasDataSource dataSource)
        {
            MouseDown += NoOp;
            MouseUp += NoOp;
            DoubleClick += NoOp;
            ScrollWheel += NoOp;
            m_HostWindow = host;
            m_DataSource = dataSource;

            RecreateRenderTexture();
        }

        public void Repaint()
        {
            m_HostWindow.Repaint();
        }

        private bool NoOp(CanvasElement element, Event e, Canvas2D parent)
        {
            return true;
        }

        public void OnGUI(EditorWindow parent, Rect clientRectangle)
        {
            m_ClientRectangle = clientRectangle;

            Event evt = Event.current;

            /*Rect canvasGLArea = clientRectangle;
                canvasGLArea.height += canvasGLArea.y;
                GL.Viewport(canvasGLArea);*/

            if (evt.type == EventType.Layout)
            {
                for (int a = 0; a < m_Animations.Count; a++)
                    m_Animations[a].Tick();

                if (OnLayout != null)
                    OnLayout(this, Event.current, this);
            }

            if (evt.type == EventType.Repaint)
            {
                RecreateRenderTexture();

                if (OnBackground != null)
                    OnBackground(this, Event.current, this);

                OnRender(parent, clientRectangle);

                if (OnOverlay != null)
                    OnOverlay(this, Event.current, this);

                if (m_CurrentModalWindow != null)
                {
                    Handles.DrawSolidRectangleWithOutline(m_CurrentModalWindowRect, new Color(0.22f, 0.22f, 0.22f, 1.0f), new Color(0.22f, 0.22f, 0.22f, 1.0f));
                    GUI.BeginGroup(m_CurrentModalWindowRect);
                    m_CurrentModalWindow(this);
                    GUI.EndGroup();
                }

                //m_ShowDebug = GUI.Toggle(new Rect(10, 55, 200, 50), m_ShowDebug, "Debug Info");

                return;
            }

            if (evt.isMouse || evt.isKey)
            {
                if (!clientRectangle.Contains(evt.mousePosition))
                    return;
            }

            //m_ShowDebug = GUI.Toggle(new Rect(10, 55, 200, 50), m_ShowDebug, "Debug Info");

            // sync selection globally on MouseUp and KeyEvents
            bool syncSelection = evt.type == EventType.MouseUp || evt.isKey;

            OnEvent(evt);

            if (syncSelection)
            {
                SyncUnitySelection();
            }
        }

        private void SyncUnitySelection()
        {
            List<Object> targets = new List<Object>();

            foreach (CanvasElement se in m_Selection)
            {
                if (se.target != null)
                {
                    targets.Add(se.target);
                }
            }
            if (targets.Count() == 1)
            {
                Selection.activeObject = targets[0];
            }
            else if (targets.Count() > 1)
            {
                Selection.activeObject = null;
            }
            else if (targets.Count() == 0)
            {
                Selection.activeObject = target;
            }
        }

        private void OnRender(EditorWindow parent, Rect clientRectangle)
        {
            m_ScreenHeightOffset = clientRectangle.y - 42;
            // query quad tree for the list of elements visible
            Rect screenRect = new Rect();
            screenRect.min = MouseToCanvas(new Vector2(0.0f, 0.0f));
            screenRect.max = MouseToCanvas(new Vector2(Screen.width, Screen.height));

            List<CanvasElement> visibleElements = m_QuadTree.ContainedBy(screenRect).OrderBy(c => c.zIndex).ToList();

            // update render textures
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = m_RenderTexture;
            Rect localCanvasRect = boundingRect;
            foreach (CanvasElement e in visibleElements)
            {
                if (e.PrepareRender())
                {
                    GL.Clear(true, true, new Color(0, 0, 0, 0));
                    GUI.BeginClip(new Rect(0, 0, e.boundingRect.width, e.boundingRect.height), Vector2.zero, Vector2.zero, true);
                    e.Render(localCanvasRect, this);
                    GUI.EndGroup();

                    e.EndRender(m_RenderTexture.height);
                }
            }
            RenderTexture.active = prev;

            Rect extents = clientRectangle;
            Matrix4x4 m = GUI.matrix;
            //m_Scale = Vector3.one;
            GUI.matrix = Matrix4x4.TRS(m_Translation, Quaternion.identity, m_Scale);

            m_ViewOffset = new Vector2(0.0f, -(extents.yMin - m_ScreenHeightOffset) * (1.0f / m_Scale.y));
            m_ViewOffsetUnscaled = new Vector2(0.0f, -(extents.yMin - m_ScreenHeightOffset));
            GUI.BeginClip(extents, Vector2.zero, m_ViewOffset, true);

            if (m_ShowDebug)
            {
                m_QuadTree.DebugDraw();
            }

            OnRenderList(visibleElements, this);

            //m_ShowDebug = true;
            if (m_ShowDebug)
            {
                foreach (CanvasElement e in visibleElements)
                {
                    e.DebugDraw();
                }
            }

            GUI.EndClip();

            GUI.matrix = m;

            if (m_ShowDebug)
            {
                Color c = GUI.color;
                GUI.color = new Color(1.0f, 0.5f, 0.0f, 1.0f);
                GUI.Label(new Rect(10, 75, 200, 32), "elements rendered:" + visibleElements.Count, GUIStyle.none);
                GUI.Label(new Rect(10, 100, 200, 32), "last event:" + m_DebugEventName, GUIStyle.none);

                if (m_CaptureSession != null)
                {
                    string typesCaptured = "";
                    foreach (CanvasElement e in m_CaptureSession.targets)
                    {
                        typesCaptured += " [" + e.GetType() + "]";
                    }
                    GUI.Label(new Rect(10, 85, 200, 32),
                        m_CaptureSession.manipulator.GetType() + " captured elements: " + m_CaptureSession.targets.Count +
                        " " + typesCaptured, GUIStyle.none);
                }
                GUI.color = c;

                Handles.DrawSolidRectangleWithOutline(debugRect, new Color(1.0f, 0.5f, 0.0f, 1.0f), new Color(1.0f, 0.5f, 0.0f, 1.0f));
            }
        }

        public override void AddChild(CanvasElement e)
        {
            base.AddChild(e);
            RebuildQuadTree();
        }

        public void RebuildQuadTree()
        {
            if (m_Children.Count == 0)
                return;

            m_CanvasRect = m_Children[0].boundingRect;
            foreach (CanvasElement c in m_Children)
            {
                Rect childRect = c.boundingRect;
                childRect = RectUtils.Inflate(childRect, 1.1f);
                m_CanvasRect = RectUtils.Encompass(m_CanvasRect, childRect);
            }

            m_QuadTree.SetSize(m_CanvasRect);
            m_QuadTree.Insert(m_Children);
        }

        public bool OnEvent(Event evt)
        {
            bool logEvent = false;
            if ((evt.type != EventType.Repaint) && (evt.type != EventType.Layout))
            {
                logEvent = (!evt.isMouse) && m_ShowDebug;
                m_DebugEventName = evt.type.ToString();
            }

            // if user clicks outside the current modal window, dismiss it
            if (evt.type == EventType.MouseDown && m_CurrentModalWindow != null)
            {
                if (!m_CurrentModalWindowRect.Contains(evt.mousePosition))
                {
                    m_CurrentModalWindow = null;
                }
            }

            if (m_CurrentModalWindow != null)
            {
                if (logEvent)
                {
                    m_DebugEventName += " handled by modal window";
                }

                GUI.BeginGroup(m_CurrentModalWindowRect);
                m_CurrentModalWindow(this);
                GUI.EndGroup();
                return true;
            }
            // select elements that will receive the events
            // 1- Captured elements have precedence
            // 2- Then the selection
            // 3- If nothing is captured or selected, we raycast into the quadtree

            if (m_CaptureSession != null)
            {
                if (RunCaptureSession(evt))
                {
                    if (logEvent)
                    {
                        m_DebugEventName += " handled by capture session\n";
                    }
                    evt.Use();
                    return true;
                }
                return false;
            }

            List<CanvasElement> elems = m_Selection.ToArray().ToList();

            // special case for clicking outside of the selection
            if (evt.type == EventType.MouseDown)
            {
                bool collidesWithSelection = false;
                foreach (CanvasElement e in elems)
                {
                    if (e.Contains(MouseToCanvas(evt.mousePosition)))
                    {
                        collidesWithSelection = true;
                        break;
                    }
                }
                if (!collidesWithSelection)
                {
                    if (m_Selection.Count() > 0)
                    {
                        ClearSelection();
                    }
                    //EditorGUI.EndEditingActiveTextField();
                    elems.Clear();
                }
            }

            if (elems.Count == 0)
            {
                Vector2 canvasPosition = MouseToCanvas(evt.mousePosition);
                Rect mouseRect = new Rect(canvasPosition.x, canvasPosition.y, 10, 10);
                elems = m_QuadTree.ContainedBy(mouseRect);
            }

            EventType originalEvent = evt.type;
            bool usedByAtLeastOneChildren = false;
            foreach (CanvasElement e in elems)
            {
                if (e != this)
                {
                    e.DispatchEvents(evt, this);

                    // if the event was consumed by an element and we are not multiselecting, they we stop
                    // propagation, otherwise we continue propagating the original event
                    if (evt.type == EventType.Used && m_Selection.Count == 0)
                    {
                        if (logEvent)
                            m_DebugEventName += " propagation stopped";
                        return true;
                    }
                    if (evt.type == EventType.Used)
                    {
                        usedByAtLeastOneChildren = true;
                        evt.type = originalEvent;
                    }
                }
            }

            if (usedByAtLeastOneChildren)
            {
                if (logEvent)
                    m_DebugEventName += " used by children";
                evt.Use();
                return true;
            }

            if (logEvent)
                m_DebugEventName += " was ignored by all children, event falls back to main canvas";
            // event was not handled by any of our children, so we fallback to ourselves (the main canvas2D)
            FireEvents(evt, this, this);
            return false;
        }

        public CanvasAnimation Animate(CanvasElement elem)
        {
            var anim = new CanvasAnimation(elem);
            m_Animations.Add(anim);
            return anim;
        }

        public void EndAnimation(CanvasAnimation a)
        {
            m_Animations.Remove(a);
        }

        public void LogInfo(string info)
        {
            if (m_ShowDebug)
            {
                m_DebugEventName += info;
            }
        }

        private bool RunCaptureSession(Event evt)
        {
            m_CaptureSession.isRunning = true;
            EventType originalEvent = evt.type;
            bool wasUsed = false;
            foreach (CanvasElement e in m_CaptureSession.targets)
            {
                m_CaptureSession.callbacks.FireEvents(evt, this, e);
                if (evt.type == EventType.Used)
                {
                    wasUsed = true;
                }
                evt.type = originalEvent;
            }
            if (wasUsed)
            {
                evt.Use();
            }
            m_CaptureSession.isRunning = false;
            if (m_CaptureSession.isEnding)
            {
                EndCapture();
            }
            return wasUsed;
        }

        public void StartCapture(IManipulate manipulator, CanvasElement e)
        {
            m_CaptureSession = new CaptureSession();
            m_CaptureSession.manipulator = manipulator;
            m_CaptureSession.callbacks = new CanvasElement();
            manipulator.AttachTo(m_CaptureSession.callbacks);
            if (m_Selection.Count > 0 && manipulator.GetCaps(ManipulatorCapability.MultiSelection))
            {
                m_CaptureSession.targets.AddRange(m_Selection);
            }
            else
            {
                m_CaptureSession.targets.Add(e);
            }
        }

        public void EndCapture()
        {
            if (m_CaptureSession == null)
                return;
            if (m_CaptureSession.isRunning == false)
            {
                foreach (CanvasElement e in m_CaptureSession.targets)
                {
                    e.UpdateModel(UpdateType.Update);
                }

                m_CaptureSession = null;
                RebuildQuadTree();
                return;
            }

            m_CaptureSession.isEnding = true;
        }

        public bool IsCaptured(IManipulate manipulator)
        {
            return m_CaptureSession == null ? false : m_CaptureSession.manipulator == manipulator;
        }

        public void AddToSelection(CanvasElement e)
        {
            if (e is Canvas2D)
                return;

            e.selected = true;
            m_Selection.Add(e);
        }

        public void ClearSelection()
        {
            foreach (CanvasElement e in m_Selection)
            {
                e.selected = false;
            }

            m_Selection.Clear();
            RebuildQuadTree();
        }

        public CanvasElement[] Pick<T>(Rect area)
        {
            List<CanvasElement> elems = m_QuadTree.ContainedBy(area);
            List<CanvasElement> returnedElements = new List<CanvasElement>();

            foreach (CanvasElement e in elems)
            {
                CanvasElement[] allTs = e.FindChildren<T>();
                foreach (CanvasElement c in allTs)
                {
                    returnedElements.Add(c);
                }
            }

            return returnedElements.ToArray();
        }

        public CanvasElement PickSingle<T>(Vector2 position)
        {
            Vector2 canvasPosition = MouseToCanvas(position);
            Rect mouseRect = new Rect(canvasPosition.x, canvasPosition.y, 10, 10);
            List<CanvasElement> elems = m_QuadTree.ContainedBy(mouseRect);
            foreach (CanvasElement e in elems)
            {
                CanvasElement[] allTs = e.FindChildren<T>();
                foreach (CanvasElement c in allTs)
                {
                    if (c.Contains(canvasPosition))
                    {
                        return c;
                    }
                }
            }

            return null;
        }

        public void RunModal(Rect rect, ModalWindowProc mwp)
        {
            if (m_CurrentModalWindow != null)
                return;
            m_CurrentModalWindow = mwp;
            m_CurrentModalWindowRect = rect;
        }

        public void EndModal()
        {
            m_CurrentModalWindow = null;
            Repaint();
        }
    }
}
