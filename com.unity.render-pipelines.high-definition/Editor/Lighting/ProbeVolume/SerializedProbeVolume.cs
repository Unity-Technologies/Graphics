namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;
        internal SerializedProperty probeVolumeAsset;
        internal SerializedProperty debugColor;
        internal SerializedProperty drawProbes;

        internal SerializedProperty size;

        internal SerializedProperty positiveFade;
        internal SerializedProperty negativeFade;
        internal SerializedProperty uniformFade;
        internal SerializedProperty advancedFade;

        internal SerializedProperty lightLayers;

        SerializedObject m_SerializedObject;

        internal SerializedProbeVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            probeVolumeParams = m_SerializedObject.FindProperty("parameters");
            probeVolumeAsset = m_SerializedObject.FindProperty("probeVolumeAsset");

            debugColor = probeVolumeParams.FindPropertyRelative("debugColor");
            drawProbes = probeVolumeParams.FindPropertyRelative("drawProbes");
            
            size = probeVolumeParams.FindPropertyRelative("size");

            positiveFade = probeVolumeParams.FindPropertyRelative("m_PositiveFade");
            negativeFade = probeVolumeParams.FindPropertyRelative("m_NegativeFade");

            uniformFade = probeVolumeParams.FindPropertyRelative("m_UniformFade");
            advancedFade = probeVolumeParams.FindPropertyRelative("advancedFade");

            lightLayers = probeVolumeParams.FindPropertyRelative("lightLayers");
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
