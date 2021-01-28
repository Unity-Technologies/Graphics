namespace UnityEditor.Rendering
{
    internal class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;

        internal SerializedProperty size;

        SerializedObject m_SerializedObject;

        internal SerializedProbeVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            probeVolumeParams = m_SerializedObject.FindProperty("parameters");

            size = probeVolumeParams.FindPropertyRelative("size");
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
