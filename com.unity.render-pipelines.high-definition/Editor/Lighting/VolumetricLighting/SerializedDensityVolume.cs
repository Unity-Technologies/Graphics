namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedDensityVolume
    {
        public SerializedProperty densityParams;
        public SerializedProperty albedo;
        public SerializedProperty meanFreePath;

        public SerializedProperty volumeTexture;
        public SerializedProperty textureScroll;
        public SerializedProperty textureTile;

        public SerializedProperty size;

        public SerializedProperty positiveFade;
        public SerializedProperty negativeFade;
        public SerializedProperty uniformFade;
        public SerializedProperty advancedFade;
        public SerializedProperty invertFade;

        public SerializedProperty distanceFadeStart;
        public SerializedProperty distanceFadeEnd;

        SerializedObject m_SerializedObject;

        public SerializedDensityVolume(SerializedObject serializedObject)
        {
            m_SerializedObject = serializedObject;

            densityParams = m_SerializedObject.FindProperty("parameters");

            albedo = densityParams.FindPropertyRelative("albedo");
            meanFreePath = densityParams.FindPropertyRelative("meanFreePath");

            volumeTexture = densityParams.FindPropertyRelative("volumeMask");
            textureScroll = densityParams.FindPropertyRelative("textureScrollingSpeed");
            textureTile = densityParams.FindPropertyRelative("textureTiling");

            size = densityParams.FindPropertyRelative("size");

            positiveFade = densityParams.FindPropertyRelative("m_PositiveFade");
            negativeFade = densityParams.FindPropertyRelative("m_NegativeFade");
            uniformFade = densityParams.FindPropertyRelative("m_UniformFade");
            advancedFade = densityParams.FindPropertyRelative("advancedFade");
            invertFade = densityParams.FindPropertyRelative("invertFade");

            distanceFadeStart = densityParams.FindPropertyRelative("distanceFadeStart");
            distanceFadeEnd   = densityParams.FindPropertyRelative("distanceFadeEnd");
        }

        public void Apply()
        {
            m_SerializedObject.ApplyModifiedProperties();
        }
    }
}
