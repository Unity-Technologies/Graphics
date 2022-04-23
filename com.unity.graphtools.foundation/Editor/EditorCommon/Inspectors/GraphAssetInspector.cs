namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [CustomEditor(typeof(GraphAsset), true)]
    class GraphAssetInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var graph = ((IGraphAsset)target)?.GraphModel;
            if (graph != null)
            {
                EditorGUILayout.LabelField("Stencil Properties");

                EditorGUI.indentLevel++;
                ((Stencil)graph.Stencil)?.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }

            base.OnInspectorGUI();
        }
    }
}
