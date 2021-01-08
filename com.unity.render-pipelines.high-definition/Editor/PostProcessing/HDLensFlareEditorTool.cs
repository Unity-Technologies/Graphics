using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [InitializeOnLoad]
    class HDLensFlareEditorWindow : EditorWindow
    {
        [MenuItem("Window/Rendering/HD Lens Flare Editor", priority = 10000)]
        public static void OpenWindow()
        {
            var window = GetWindow<HDLensFlareEditorWindow>("HD Lens Flare Editor");
            Camera currentCamera = SceneView.lastActiveSceneView.camera;
            window.minSize = new Vector2(512f, 512f/currentCamera.aspect);
        }
    }
}
