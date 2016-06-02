using UnityEngine;

namespace UnityEditor.Experimental.Graph.Examples
{
    class IMGUIExampleWidget : CanvasElement
    {
        private int m_ControlInteger = 0;
        private bool m_GUIToggle = false;
        private float m_GUIFloatValue = 42.0f;
        private string m_SomeText = "<enter some text>";
        public Rect windowRect = new Rect(20, 20, 120, 50);

        public IMGUIExampleWidget(Vector2 position, float size)
        {
            translation = position;
            scale = new Vector2(size, size);
            AddManipulator(new Draggable());
            AddManipulator(new Resizable(new Vector2(200, 100.0f)));
            AddManipulator(new ImguiContainer());
        }

        public override void Render(Rect parentRect, Canvas2D canvas)
        {
            EventType t = Event.current.type;
            Color backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.7f);
            Color selectedColor = new Color(1.0f, 0.7f, 0.0f, 0.7f);
            EditorGUI.DrawRect(new Rect(0, 0, scale.x, scale.y), selected ? selectedColor : backgroundColor);

            GUILayout.BeginVertical();
            GUILayout.Label("Layout begins here");
            if (GUILayout.Button("Layout Buttton" + m_ControlInteger, GUILayout.Width(150)))
            {
                m_ControlInteger++;
                Debug.Log("Layout Button was pressed: " + m_ControlInteger);
            }

            m_GUIToggle = GUILayout.Toggle(m_GUIToggle, "GUI Toggle");

            m_GUIFloatValue = GUILayout.HorizontalSlider(m_GUIFloatValue, 0.0f, 100.0f, GUILayout.Width(150));

            m_SomeText = GUILayout.TextField(m_SomeText, GUILayout.Width(200));
            GUILayout.Label("Layout ends here");
            GUILayout.EndVertical();

            float y = 150.0f;
            GUI.Label(new Rect(0, y, 150, 30), "No-layout begins here");

            if (GUI.Button(new Rect(0, y + 30.0f, 120, 30), "GUI Button:" + m_ControlInteger))
            {
                m_ControlInteger++;
                Debug.Log("GUI Button was pressed: " + m_ControlInteger);
            }

            m_GUIToggle = GUI.Toggle(new Rect(120, y + 30.0f, 120, 30), m_GUIToggle, "GUI Toggle");

            m_GUIFloatValue = GUI.HorizontalSlider(new Rect(0, y + 60.0f, 120, 30), m_GUIFloatValue, 0.0f, 100.0f);

            GUI.Label(new Rect(0, y + 90.0f, 120, 30), "No-layout ends here");
        }
    }

}
