using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VFX.Utils
{
    public partial class PointCacheBakeTool : EditorWindow
    {
        [MenuItem("Window/Visual Effects/Utilities/Point Cache Bake Tool",false,3012)]
        static void OpenWindow()
        {
            GetWindow<PointCacheBakeTool>();
        }

        public enum BakeMode
        {
            Texture,
            MeshSurface,
            MeshVolume
        }

        public BakeMode mode = BakeMode.MeshSurface;

        private void OnGUI()
        {
            titleContent = Contents.title;
            mode = (BakeMode)EditorGUILayout.EnumPopup(Contents.mode, mode);
            switch (mode)
            {
                case BakeMode.MeshSurface: OnGUI_MeshSurface(); break;
                case BakeMode.MeshVolume: OnGUI_MeshVolume(); break;
                case BakeMode.Texture: OnGUI_Texture(); break;
            }
        }

        static partial class Contents
        {
            public static GUIContent title = new GUIContent("pCache Tool");
            public static GUIContent mode = new GUIContent("Bake Mode");
        }
    }
}
