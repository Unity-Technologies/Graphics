namespace UnityEditor.Rendering
{
    internal class SerializedProbeTouchupVolume
    {
        internal SerializedProperty size;
        internal SerializedProperty intensityScale;
        internal SerializedProperty invalidateProbes;
        internal SerializedProperty overrideDilationThreshold;
        internal SerializedProperty overriddenDilationThreshold;

        internal SerializedObject serializedObject;

        internal SerializedProbeTouchupVolume(SerializedObject obj)
        {
            serializedObject = obj;

            size = serializedObject.FindProperty("size");
            intensityScale = serializedObject.FindProperty("intensityScale");
            invalidateProbes = serializedObject.FindProperty("invalidateProbes");
            overrideDilationThreshold = serializedObject.FindProperty("overrideDilationThreshold");
            overriddenDilationThreshold = serializedObject.FindProperty("overriddenDilationThreshold");
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
