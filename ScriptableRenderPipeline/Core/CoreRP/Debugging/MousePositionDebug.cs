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

#if UNITY_EDITOR
        private Vector2 m_mousePosition = Vector2.zero;

        private void OnSceneGUI(UnityEditor.SceneView sceneview)
        {
            m_mousePosition = Event.current.mousePosition;
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
    }
}
