namespace UnityEditor.Experimental.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;

        internal SerializedProperty size;
        internal SerializedProperty maxSubdivisionMultiplier;
        internal SerializedProperty minSubdivisionMultiplier;
        internal SerializedProperty objectLayerMask;

        internal SerializedObject serializedObject;

        internal SerializedProbeVolume(SerializedObject obj)
        {
            serializedObject = obj;

            size = m_SerializedObject.FindProperty("size");
            maxSubdivisionMultiplier = m_SerializedObject.FindProperty("maxSubdivisionMultiplier");
            minSubdivisionMultiplier = m_SerializedObject.FindProperty("minSubdivisionMultiplier");
            objectLayerMask = m_SerializedObject.FindProperty("objectLayerMask");
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
