namespace UnityEditor.Experimental.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;

        internal SerializedProperty size;
        internal SerializedProperty maxSubdivisionMultiplier;
        internal SerializedProperty minSubdivisionMultiplier;

        SerializedObject m_SerializedObject;

        internal SerializedProbeVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            probeVolumeParams = m_SerializedObject.FindProperty("parameters");

            size = probeVolumeParams.FindPropertyRelative("size");
            maxSubdivisionMultiplier = probeVolumeParams.FindPropertyRelative("maxSubdivisionMultiplier");
            minSubdivisionMultiplier = probeVolumeParams.FindPropertyRelative("minSubdivisionMultiplier");
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
