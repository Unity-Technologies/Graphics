namespace UnityEditor.Rendering
{
    static class EditorGUIBridge
    {
        public readonly struct CompositeStylingScope : System.IDisposable
        {
            readonly EditorGUI.CompositeStylingScope m_Scope;

            public CompositeStylingScope(bool enabled = true)
            {
                m_Scope = new EditorGUI.CompositeStylingScope(enabled);
            }

            public void Dispose() => m_Scope.Dispose();
        }
    }
}
