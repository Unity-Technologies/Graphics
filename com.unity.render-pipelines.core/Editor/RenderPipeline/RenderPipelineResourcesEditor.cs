namespace UnityEngine.Rendering
{
    [UnityEditor.CustomEditor(typeof(RenderPipelineResources), isFallback = true)]
    class RenderPipelineResourcesEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            // Add a "Reload All" button in inspector when we are in developer's mode
            if (UnityEditor.EditorPrefs.GetBool("DeveloperMode")
                && GUILayout.Button("Reload All"))
            {
                foreach (RenderPipelineResources t in targets)
                {
                    foreach (var field in t.GetType().GetFields())
                        field.SetValue(t, null);

                    ResourceReloader.ReloadAllNullIn(target, t.packagePath_Internal);
                }
            }
        }
    }
}
