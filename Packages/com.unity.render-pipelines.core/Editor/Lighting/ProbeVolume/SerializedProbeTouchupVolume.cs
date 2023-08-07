namespace UnityEditor.Rendering
{
    internal class SerializedProbeTouchupVolume
    {
        internal SerializedProperty shape;
        internal SerializedProperty size;
        internal SerializedProperty radius;

        internal SerializedProperty mode;
        internal SerializedProperty intensityScale;
        internal SerializedProperty overriddenDilationThreshold;
        internal SerializedProperty virtualOffsetRotation;
        internal SerializedProperty virtualOffsetDistance;
        internal SerializedProperty geometryBias;
        internal SerializedProperty rayOriginBias;

        internal SerializedObject serializedObject;

        internal SerializedProbeTouchupVolume(SerializedObject obj)
        {
            serializedObject = obj;

            shape = serializedObject.FindProperty("shape");
            size = serializedObject.FindProperty("size");
            radius = serializedObject.FindProperty("radius");

            mode = serializedObject.FindProperty("mode");
            intensityScale = serializedObject.FindProperty("intensityScale");
            overriddenDilationThreshold = serializedObject.FindProperty("overriddenDilationThreshold");
            virtualOffsetRotation = serializedObject.FindProperty("virtualOffsetRotation");
            virtualOffsetDistance = serializedObject.FindProperty("virtualOffsetDistance");
            geometryBias = serializedObject.FindProperty("geometryBias");
            rayOriginBias = serializedObject.FindProperty("rayOriginBias");
        }
    }
}
