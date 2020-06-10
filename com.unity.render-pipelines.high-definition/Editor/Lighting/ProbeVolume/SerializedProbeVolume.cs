namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedProbeVolume
    {
        internal SerializedProperty probeVolumeParams;
        internal SerializedProperty probeVolumeAsset;
        internal SerializedProperty debugColor;
        internal SerializedProperty drawProbes;

        internal SerializedProperty probeSpacingMode;

        internal SerializedProperty resolutionX;
        internal SerializedProperty resolutionY;
        internal SerializedProperty resolutionZ;

        internal SerializedProperty densityX;
        internal SerializedProperty densityY;
        internal SerializedProperty densityZ;

        internal SerializedProperty volumeBlendMode;
        internal SerializedProperty weight;
        internal SerializedProperty normalBiasWS;

        internal SerializedProperty size;

        internal SerializedProperty positiveFade;
        internal SerializedProperty negativeFade;
        internal SerializedProperty uniformFade;
        internal SerializedProperty advancedFade;

        internal SerializedProperty distanceFadeStart;
        internal SerializedProperty distanceFadeEnd;

        internal SerializedProperty backfaceTolerance;
        internal SerializedProperty dilationIterations;

        internal SerializedProperty lightLayers;

        SerializedObject m_SerializedObject;

        internal SerializedProbeVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            probeVolumeParams = m_SerializedObject.FindProperty("parameters");
            probeVolumeAsset = m_SerializedObject.FindProperty("probeVolumeAsset");

            debugColor = probeVolumeParams.FindPropertyRelative("debugColor");
            drawProbes = probeVolumeParams.FindPropertyRelative("drawProbes");

            probeSpacingMode = probeVolumeParams.FindPropertyRelative("probeSpacingMode");

            resolutionX = probeVolumeParams.FindPropertyRelative("resolutionX");
            resolutionY = probeVolumeParams.FindPropertyRelative("resolutionY");
            resolutionZ = probeVolumeParams.FindPropertyRelative("resolutionZ");

            densityX = probeVolumeParams.FindPropertyRelative("densityX");
            densityY = probeVolumeParams.FindPropertyRelative("densityY");
            densityZ = probeVolumeParams.FindPropertyRelative("densityZ");

            volumeBlendMode = probeVolumeParams.FindPropertyRelative("volumeBlendMode");
            weight = probeVolumeParams.FindPropertyRelative("weight");
            normalBiasWS = probeVolumeParams.FindPropertyRelative("normalBiasWS");

            size = probeVolumeParams.FindPropertyRelative("size");

            positiveFade = probeVolumeParams.FindPropertyRelative("m_PositiveFade");
            negativeFade = probeVolumeParams.FindPropertyRelative("m_NegativeFade");

            uniformFade = probeVolumeParams.FindPropertyRelative("m_UniformFade");
            advancedFade = probeVolumeParams.FindPropertyRelative("advancedFade");

            distanceFadeStart = probeVolumeParams.FindPropertyRelative("distanceFadeStart");
            distanceFadeEnd   = probeVolumeParams.FindPropertyRelative("distanceFadeEnd");

            backfaceTolerance = probeVolumeParams.FindPropertyRelative("backfaceTolerance");
            dilationIterations = probeVolumeParams.FindPropertyRelative("dilationIterations");

            lightLayers = probeVolumeParams.FindPropertyRelative("lightLayers");
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
