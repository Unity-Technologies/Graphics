namespace UnityEditor.Experimental.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;

        internal SerializedProperty size;
        internal SerializedProperty maxSubdivisionMultiplier;
        internal SerializedProperty minSubdivisionMultiplier;

        internal SerializedObject serializedObject;

        internal SerializedProbeVolume(SerializedObject obj)
        {
            serializedObject = obj;

            probeVolumeParams = serializedObject.FindProperty("parameters");

            size = probeVolumeParams.FindPropertyRelative("size");
            maxSubdivisionMultiplier = probeVolumeParams.FindPropertyRelative("maxSubdivisionMultiplier");
            minSubdivisionMultiplier = probeVolumeParams.FindPropertyRelative("minSubdivisionMultiplier");
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
