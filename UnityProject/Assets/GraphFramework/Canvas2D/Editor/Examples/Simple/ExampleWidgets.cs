using UnityEngine;
using UnityEditor.Experimental;

namespace UnityEditor.Experimental.Graph.Examples
{
    class SimpleBox : CanvasElement
    {
        protected string m_Title = "simpleBox";
        public SimpleBox(Vector2 position, float size)
        {
            translation = position;
            scale = new Vector2(size, size);
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
            Color selectedColor = new Color(1.0f, 0.7f, 0.0f, 0.7f);
            EditorGUI.DrawRect(new Rect(0, 0, scale.x, scale.y), selected ? selectedColor : backgroundColor);
            GUI.Label(new Rect(0, 0, scale.x, 26f), GUIContent.none, new GUIStyle("preToolbar"));
            GUI.Label(new Rect(10, 2, scale.x - 20.0f, 16.0f), m_Title, EditorStyles.toolbarTextField);
            base.Render(parentRect, canvas);
        }
    }

    class MoveableBox : SimpleBox
    {
        public MoveableBox(Vector2 position, float size)
            : base(position, size)
        {
            m_Title = "Drag me!";
            AddManipulator(new Draggable());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
        }
    }

    class FloatingBox : CanvasElement
    {
        public FloatingBox(Vector2 position, float size)
        {
            m_Translation = position;
            m_Scale = new Vector3(size, size, size);
            m_Caps |= Capabilities.Floating;
            AddManipulator(new Draggable());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
            Color selectedColor = new Color(1.0f, 0.7f, 0.0f, 0.7f);
            EditorGUI.DrawRect(new Rect(0, 0, scale.x, scale.y), selected ? selectedColor : backgroundColor);
            GUI.Label(new Rect(0, 0, m_Scale.x, 20.0f), "Floating Minimap");
            foreach (var child in canvas.Children())
            {
                if ((child.caps & Capabilities.Floating) != 0)
                    continue;
                var rect = child.canvasBoundingRect;
                rect.x /= canvas.clientRect.width;
                rect.width /= canvas.clientRect.width;
                rect.y /= canvas.clientRect.height;
                rect.height /= canvas.clientRect.height;

                rect.x *= m_Scale.x / 2.0f;
                rect.y *= m_Scale.y / 2.0f;
                rect.width *= m_Scale.x / 2.0f;
                rect.height *= m_Scale.y / 2.0f;
                rect.y += 20;
                EditorGUI.DrawRect(rect, Color.grey);
            }

            Invalidate();
            canvas.Repaint();
        }
    }

    class InvisibleBorderContainer : CanvasElement
    {
        private bool m_NormalizedDragRegion = false;
        private Draggable m_DragManipulator = null;
        public InvisibleBorderContainer(Vector2 position, float size, bool normalizedDragRegion)
        {
            translation = position;
            scale = new Vector2(size, size);
            m_NormalizedDragRegion = normalizedDragRegion;
            if (normalizedDragRegion)
            {
                m_DragManipulator = new Draggable(new Rect(0.1f, 0.1f, 0.9f, 0.9f), true);
                AddManipulator(m_DragManipulator);
            }
            else
            {
                float padding = size / 10.0f;
                m_DragManipulator = new Draggable(new Rect(padding, padding, size - (padding * 2), size - (padding * 2)), false);
                AddManipulator(m_DragManipulator);
            }
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            EditorGUI.DrawRect(new Rect(0, 0, m_Scale.x, m_Scale.y), m_Selected ? Color.blue : new Color(0.0f, 0.0f, 0.0f, 0.5f));
            Rect activeDragRect = m_DragManipulator.ComputeDragRegion(this, false);
            EditorGUI.DrawRect(new Rect(activeDragRect.x - boundingRect.x, activeDragRect.y - boundingRect.y, activeDragRect.width, activeDragRect.height), Color.green);

            GUI.Label(new Rect(0, (m_Scale.y * 0.5f) - 10.0f, 100, 20), "normalized:" + m_NormalizedDragRegion);

            base.Render(parentRect, canvas);
        }
    }

    class Circle : CanvasElement
    {
        public Circle(Vector2 position, float size)
        {
            translation = position;
            scale = new Vector2(size, size);
            AddManipulator(new Draggable());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
            Handles.DrawSolidDisc(new Vector3(scale.x / 2.0f, scale.x / 2.0f, 0.0f), new Vector3(0.0f, 0.0f, -1.0f), scale.x / 2.0f);
        }

        public override bool Contains(Vector2 point)
        {
            Rect canvasRect = canvasBoundingRect;
            return Vector2.Distance(canvasRect.center, point) <= (scale.x / 2.0f);
        }
    }

    class ResizableBox : SimpleBox
    {
        public ResizableBox(Vector2 position, float size)
            : base(position, size)
        {
            m_Title = "Resize me!";
            AddManipulator(new Resizable());
            AddManipulator(new Draggable());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);
        }
    }

    class WWWImageBox : SimpleBox
    {
        Texture2D m_WWWTexture = new Texture2D(4, 4, TextureFormat.DXT1, false);
        WWW www = null;
        private float timeToNextPicture = 0.0f;

        public WWWImageBox(Vector2 position, float size)
            : base(position, size)
        {
            m_Title = "I cause repaints every frame!";
            AddManipulator(new Draggable());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            if (www != null && www.isDone)
            {
                www.LoadImageIntoTexture(m_WWWTexture);
                www = null;
                timeToNextPicture = 3.0f;
            }

            timeToNextPicture -= Time.deltaTime;
            if (timeToNextPicture < 0.0f)
            {
                timeToNextPicture = 99999.0f;
                www = new WWW("http://lorempixel.com/200/200");
            }

            base.Render(parentRect, canvas);

            GUI.DrawTexture(new Rect(0, 20, 200, 200), m_WWWTexture);
            Invalidate();
            canvas.Repaint();
        }
    }

    class IMGUIControls : SimpleBox
    {
        private string m_Text1 = "this is a text field";
        private string m_Text2 = "this is a text field";
        private bool m_Toggle = true;
        private Texture2D m_aTexture = null;

        public IMGUIControls(Vector2 position, float size)
            : base(position, size)
        {
            m_Caps = Capabilities.Unselectable;

            m_Title = "modal";
            AddManipulator(new Draggable());
            AddManipulator(new Resizable());
            AddManipulator(new ImguiContainer());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            base.Render(parentRect, canvas);

            int currentY = 22;

            m_Text1 = GUI.TextField(new Rect(0, currentY, 80, 20), m_Text1);
            currentY += 22;

            m_Toggle = GUI.Toggle(new Rect(0, currentY, 10, 10), m_Toggle, GUIContent.none);
            currentY += 22;

            m_Text2 = GUI.TextField(new Rect(0, currentY, 80, 20), m_Text2);
            currentY += 22;

            m_aTexture = EditorGUI.ObjectField(new Rect(0, currentY, 80, 100), m_aTexture, typeof(Texture2D), false) as Texture2D;
        }
    }
}
