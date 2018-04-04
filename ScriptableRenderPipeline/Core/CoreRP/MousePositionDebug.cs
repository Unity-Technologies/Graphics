using System;

namespace UnityEngine.Experimental.Rendering
{
    public class MousePositionDebug
    {
        // Singleton
        private static MousePositionDebug s_Instance = null;

        static public MousePositionDebug instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new MousePositionDebug();
                }

                return s_Instance;
            }
        }

        public int debugStep
        {
            get
            {
#if UNITY_EDITOR
                return m_DebugStep;
#else
                return 0;
#endif
            }
        }

#if UNITY_EDITOR
        private Vector2 m_mousePosition = Vector2.zero;
        Vector2 m_MouseClickPosition = Vector2.zero;
        int m_DebugStep = 0;

        private void OnSceneGUI(UnityEditor.SceneView sceneview)
        {
            m_mousePosition = Event.current.mousePosition;
            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    m_MouseClickPosition = m_mousePosition;
                    break;
                case EventType.KeyDown:
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.PageUp:
                            ++m_DebugStep;
                            break;
                        case KeyCode.PageDown:
                            m_DebugStep = Mathf.Max(0, m_DebugStep - 1);
                            break;
                        case KeyCode.End:
                            // Usefull we you don't want to change the scene viewport but still update the mouse click position
                            m_MouseClickPosition = m_mousePosition;
                            break;
                    }
                    break;
            }
        }
#endif

        public void Build()
        {
#if UNITY_EDITOR
            UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
            UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
#endif
        }

        public void Cleanup()
        {
#if UNITY_EDITOR
            UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
#endif
        }

        public Vector2 GetMousePosition(float ScreenHeight)
        {
            Vector2 mousePixelCoord = Input.mousePosition;
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                mousePixelCoord = m_mousePosition;
                mousePixelCoord.y = (ScreenHeight - 1.0f) - mousePixelCoord.y;
            }
#endif
            return mousePixelCoord;
        }

        public Vector2 GetMouseClickPosition(float ScreenHeight)
        {
#if UNITY_EDITOR
            Vector2 mousePixelCoord = m_MouseClickPosition;
            mousePixelCoord.y = (ScreenHeight - 1.0f) - mousePixelCoord.y;
            return mousePixelCoord;
#else
            return Vector2.zero;
#endif
        }
    }
}
