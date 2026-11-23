namespace UnityEditor.Rendering
{
    static class EditorWindowBridge
    {
        public static void RepaintImmediately(EditorWindow window)
            => window.RepaintImmediately();
    }
}
