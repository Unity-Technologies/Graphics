namespace UnityEditor.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;

        internal SerializedProperty size;
        internal SerializedProperty maxSubdivisionMultiplier;
        internal SerializedProperty minSubdivisionMultiplier;
        internal SerializedProperty objectLayerMask;

        SerializedObject m_SerializedObject;

        internal SerializedProbeVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            size = m_SerializedObject.FindProperty("size");
            maxSubdivisionMultiplier = m_SerializedObject.FindProperty("maxSubdivisionMultiplier");
            minSubdivisionMultiplier = m_SerializedObject.FindProperty("minSubdivisionMultiplier");
            objectLayerMask = m_SerializedObject.FindProperty("objectLayerMask");
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
