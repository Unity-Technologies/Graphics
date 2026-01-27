using UnityEngine;

namespace UnityEditor.Experimental.VFX.Utility
{
    partial class PointCacheBakeTool : EditorWindow
    {
        static readonly Vector2 kMinSize = new Vector2(310f, 210f);

        [MenuItem("Window/Visual Effects/Utilities/Point Cache Bake Tool", false, 3012)]
        static void OpenWindow()
        {
            var window = GetWindow<PointCacheBakeTool>();
            window.minSize = kMinSize;
            window.titleContent = Contents.title;
        }

        public enum BakeMode
        {
            Texture,
            Mesh
        }

        public BakeMode mode = BakeMode.Mesh;

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            mode = (BakeMode)EditorGUILayout.EnumPopup(Contents.mode, mode);
            GUILayout.EndHorizontal();

            switch (mode)
            {
                case BakeMode.Mesh: OnGUI_Mesh(); break;
                case BakeMode.Texture: OnGUI_Texture(); break;
            }
        }

        static partial class Contents
        {
            public static GUIContent title = new GUIContent("Point Cache Tool");
            public static GUIContent mode = new GUIContent("Bake Mode");
        }
    }
}
