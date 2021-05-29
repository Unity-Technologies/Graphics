namespace UnityEditor.Experimental.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;

        internal SerializedProperty size;
        internal SerializedProperty maxSubdivisionMultiplier;
        internal SerializedProperty minSubdivisionMultiplier;
        internal SerializedProperty objectLayerMask;
        internal SerializedProperty geometryDistanceOffset;

        internal SerializedObject serializedObject;

        internal SerializedProbeVolume(SerializedObject obj)
        {
            serializedObject = obj;

            size = serializedObject.FindProperty("size");
            maxSubdivisionMultiplier = serializedObject.FindProperty("maxSubdivisionMultiplier");
            minSubdivisionMultiplier = serializedObject.FindProperty("minSubdivisionMultiplier");
            objectLayerMask = serializedObject.FindProperty("objectLayerMask");
            geometryDistanceOffset = serializedObject.FindProperty("geometryDistanceOffset");
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
