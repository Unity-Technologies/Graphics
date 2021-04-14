using UnityEditor;

namespace UnityEngine.Rendering
{
    [CustomEditor(typeof(RenderPipelineResources), editorForChildClasses: true)]
    class RenderPipelineResourcesEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (EditorPrefs.GetBool("DeveloperMode")
                && GUILayout.Button("Reload All"))
            {
                foreach (RenderPipelineResources t in targets)
                {
                    if (string.IsNullOrEmpty(t.packagePath_Internal))
                    {
                        Debug.LogError($"packagePath is not set in {t.GetType().Name}. We will not be able to reload it. Skipping.");
                        continue;
                    }

                    foreach (var field in t.GetType().GetFields())
                        field.SetValue(t, null);

                    ResourceReloader.ReloadAllNullIn(t, t.packagePath_Internal);
                }
            }
        }
    }
}
