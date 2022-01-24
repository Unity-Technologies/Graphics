using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public class RenderingDebuggerWindow : EditorWindow
    {
        [MenuItem("Window/Analysis/Rendering Debugger (UITK)", priority = 10006)]
        public static void ShowDefaultWindow()
        {
            var wnd = GetWindow<RenderingDebuggerWindow>();
            wnd.titleContent = new GUIContent("Rendering Debugger (UITK)");
        }

        public void OnEnable()
        {
            var windowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Editor/Debugging/RenderingDebugger/rendering-debugger.uxml");
            if (windowTemplate != null)
                windowTemplate.CloneTree(this.rootVisualElement);

            this.minSize = new Vector2(400, 250);
        }
    }
}
