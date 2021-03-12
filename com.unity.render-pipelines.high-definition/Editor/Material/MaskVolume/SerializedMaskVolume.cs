namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedMaskVolume
    {
        internal SerializedProperty maskVolumeParams;
        internal SerializedProperty maskVolumeAsset;
        internal SerializedProperty debugColor;
        internal SerializedProperty drawGizmos;

        internal SerializedProperty maskSpacingMode;

        internal SerializedProperty resolutionX;
        internal SerializedProperty resolutionY;
        internal SerializedProperty resolutionZ;

        internal SerializedProperty densityX;
        internal SerializedProperty densityY;
        internal SerializedProperty densityZ;

        internal SerializedProperty blendMode;
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

        internal SerializedMaskVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            maskVolumeParams = m_SerializedObject.FindProperty("parameters");
            maskVolumeAsset = m_SerializedObject.FindProperty("maskVolumeAsset");

            debugColor = maskVolumeParams.FindPropertyRelative("debugColor");
            drawGizmos = maskVolumeParams.FindPropertyRelative("drawGizmos");

            maskSpacingMode = maskVolumeParams.FindPropertyRelative("maskSpacingMode");

            resolutionX = maskVolumeParams.FindPropertyRelative("resolutionX");
            resolutionY = maskVolumeParams.FindPropertyRelative("resolutionY");
            resolutionZ = maskVolumeParams.FindPropertyRelative("resolutionZ");

            densityX = maskVolumeParams.FindPropertyRelative("densityX");
            densityY = maskVolumeParams.FindPropertyRelative("densityY");
            densityZ = maskVolumeParams.FindPropertyRelative("densityZ");

            blendMode = maskVolumeParams.FindPropertyRelative("blendMode");
            weight = maskVolumeParams.FindPropertyRelative("weight");
            normalBiasWS = maskVolumeParams.FindPropertyRelative("normalBiasWS");

            size = maskVolumeParams.FindPropertyRelative("size");

            positiveFade = maskVolumeParams.FindPropertyRelative("m_PositiveFade");
            negativeFade = maskVolumeParams.FindPropertyRelative("m_NegativeFade");

            uniformFade = maskVolumeParams.FindPropertyRelative("m_UniformFade");
            advancedFade = maskVolumeParams.FindPropertyRelative("advancedFade");

            distanceFadeStart = maskVolumeParams.FindPropertyRelative("distanceFadeStart");
            distanceFadeEnd   = maskVolumeParams.FindPropertyRelative("distanceFadeEnd");

            backfaceTolerance = maskVolumeParams.FindPropertyRelative("backfaceTolerance");
            dilationIterations = maskVolumeParams.FindPropertyRelative("dilationIterations");

            lightLayers = maskVolumeParams.FindPropertyRelative("lightLayers");
        }

        internal void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
