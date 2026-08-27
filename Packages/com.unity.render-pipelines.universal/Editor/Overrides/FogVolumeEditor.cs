#if VOLUMETRIC_FOG

using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(FogVolumeComponent))]
    sealed class FogVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_AnalyticFogEnabled;
        SerializedDataParameter m_VolumetricFogEnabled;
        SerializedDataParameter m_AnalyticFogDensityMode;
        SerializedDataParameter m_AnalyticFogDensity;
        SerializedDataParameter m_AnalyticFogBaseHeight;
        SerializedDataParameter m_AnalyticFogMaximumHeight;
        SerializedDataParameter m_VolumetricFogDensityMode;
        SerializedDataParameter m_VolumetricFogDensity;
        SerializedDataParameter m_VolumetricFogDensityTexture;
        SerializedDataParameter m_VolumetricFogDensityTextureTiling;
        SerializedDataParameter m_VolumetricFogBaseHeight;
        SerializedDataParameter m_VolumetricFogMaximumHeight;
        SerializedDataParameter m_AnalyticFogColor;
        SerializedDataParameter m_VolumetricFogAlbedo;
        SerializedDataParameter m_MaxFogDistance;
        SerializedDataParameter m_Anisotropy;
        SerializedDataParameter m_CutoffDistance;
        SerializedDataParameter m_ScreenResolutionPercentage;
        SerializedDataParameter m_VolumeSliceCount;
        SerializedDataParameter m_SliceDistributionUniformity;
        SerializedDataParameter m_DenoisingMode;
        SerializedDataParameter m_VolumetricLightingDensityCutoff;
        SerializedDataParameter m_EnableLightCookies;
        SerializedDataParameter m_LightFilter;
        SerializedDataParameter m_MultipleScatteringIntensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FogVolumeComponent>(serializedObject);

            m_AnalyticFogEnabled = Unpack(o.Find(x => x.analyticFogEnabled));
            m_VolumetricFogEnabled = Unpack(o.Find(x => x.volumetricFogEnabled));
            m_AnalyticFogDensityMode = Unpack(o.Find(x => x.analyticFogDensityMode));
            m_AnalyticFogDensity = Unpack(o.Find(x => x.analyticFogDensity));
            m_AnalyticFogBaseHeight = Unpack(o.Find(x => x.analyticFogBaseHeight));
            m_AnalyticFogMaximumHeight = Unpack(o.Find(x => x.analyticFogMaximumHeight));
            m_VolumetricFogDensityMode = Unpack(o.Find(x => x.volumetricFogDensityMode));
            m_VolumetricFogDensity = Unpack(o.Find(x => x.volumetricFogDensity));
            m_VolumetricFogDensityTexture = Unpack(o.Find(x => x.volumetricFogDensityTexture));
            m_VolumetricFogDensityTextureTiling = Unpack(o.Find(x => x.volumetricFogDensityTextureTiling));
            m_VolumetricFogBaseHeight = Unpack(o.Find(x => x.volumetricFogBaseHeight));
            m_VolumetricFogMaximumHeight = Unpack(o.Find(x => x.volumetricFogMaximumHeight));
            m_AnalyticFogColor = Unpack(o.Find(x => x.analyticFogColor));
            m_VolumetricFogAlbedo = Unpack(o.Find(x => x.volumetricFogAlbedo));
            m_MaxFogDistance = Unpack(o.Find(x => x.maxFogDistance));
            m_Anisotropy = Unpack(o.Find(x => x.anisotropy));
            m_CutoffDistance = Unpack(o.Find(x => x.cutoffDistance));
            m_ScreenResolutionPercentage = Unpack(o.Find(x => x.screenResolutionPercentage));
            m_VolumeSliceCount = Unpack(o.Find(x => x.volumeSliceCount));
            m_SliceDistributionUniformity = Unpack(o.Find(x => x.sliceDistributionUniformity));
            m_DenoisingMode = Unpack(o.Find(x => x.denoisingMode));
            m_VolumetricLightingDensityCutoff = Unpack(o.Find(x => x.volumetricLightingDensityCutoff));
            m_EnableLightCookies = Unpack(o.Find(x => x.enableLightCookies));
            m_LightFilter = Unpack(o.Find(x => x.lightFilter));
            m_MultipleScatteringIntensity = Unpack(o.Find(x => x.multipleScatteringIntensity));
        }

        static UnityEngine.GUIContent Label(SerializedDataParameter property, string text)
            => EditorGUIUtility.TrTextContent(text, property.GetAttribute<UnityEngine.TooltipAttribute>()?.tooltip);

        public override void OnInspectorGUI()
        {
            using (new BoldLabelScope())
                PropertyField(m_AnalyticFogEnabled, Label(m_AnalyticFogEnabled, "Analytic Fog"));

            using (new IndentLevelScope())
            {
                PropertyField(m_AnalyticFogDensityMode, Label(m_AnalyticFogDensityMode, "Density Mode"));
                using (new IndentLevelScope())
                {
                    PropertyField(m_AnalyticFogDensity, Label(m_AnalyticFogDensity, "Density"));
                    if ((AnalyticFogDensityMode)m_AnalyticFogDensityMode.value.intValue == AnalyticFogDensityMode.Height)
                    {
                        PropertyField(m_AnalyticFogBaseHeight, Label(m_AnalyticFogBaseHeight, "Base Height"));
                        PropertyField(m_AnalyticFogMaximumHeight, Label(m_AnalyticFogMaximumHeight, "Max Height"));
                    }
                }
                PropertyField(m_AnalyticFogColor, Label(m_AnalyticFogColor, "Color"));
                PropertyField(m_MaxFogDistance, Label(m_MaxFogDistance, "Max Distance"));
            }
            EditorGUILayout.Space();

            using (new BoldLabelScope())
                PropertyField(m_VolumetricFogEnabled, Label(m_VolumetricFogEnabled, "Volumetric Fog"));

            using (new IndentLevelScope())
            {
                PropertyField(m_VolumetricFogDensityMode, Label(m_VolumetricFogDensityMode, "Density Mode"));
                var volumetricFogDensityMode = (VolumetricFogDensityMode)m_VolumetricFogDensityMode.value.intValue;
                using (new IndentLevelScope())
                {
                    PropertyField(m_VolumetricFogDensity, Label(m_VolumetricFogDensity, "Density"));
                    if (volumetricFogDensityMode == VolumetricFogDensityMode.Height || volumetricFogDensityMode == VolumetricFogDensityMode.Texture)
                    {
                        PropertyField(m_VolumetricFogBaseHeight, Label(m_VolumetricFogBaseHeight, "Base Height"));
                        PropertyField(m_VolumetricFogMaximumHeight, Label(m_VolumetricFogMaximumHeight, "Max Height"));
                    }
                    if (volumetricFogDensityMode == VolumetricFogDensityMode.Texture)
                    {
                        PropertyField(m_VolumetricFogDensityTexture, Label(m_VolumetricFogDensityTexture, "Density Texture"));
                        PropertyField(m_VolumetricFogDensityTextureTiling, Label(m_VolumetricFogDensityTextureTiling, "Tiling"));
                    }
                }
                PropertyField(m_VolumetricFogAlbedo, Label(m_VolumetricFogAlbedo, "Albedo"));
                PropertyField(m_Anisotropy);
                PropertyField(m_MultipleScatteringIntensity);
                PropertyField(m_CutoffDistance, Label(m_CutoffDistance, "Cutoff Distance"));
            }

            EditorGUILayout.Space();
            using (new IndentLevelScope())
            {
                var lightingRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                lightingRect.xMin += 10f;
                EditorGUI.LabelField(lightingRect, "Lighting", EditorStyles.boldLabel);
                PropertyField(m_LightFilter);
                PropertyField(m_EnableLightCookies);
            }

            EditorGUILayout.Space();
            using (new IndentLevelScope())
            {
                var qualityRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                qualityRect.xMin += 10f;
                EditorGUI.LabelField(qualityRect, "Quality", EditorStyles.boldLabel);
                PropertyField(m_DenoisingMode);
                PropertyField(m_SliceDistributionUniformity);
                PropertyField(m_VolumeSliceCount);
                PropertyField(m_ScreenResolutionPercentage);
                PropertyField(m_VolumetricLightingDensityCutoff);
            }

        }

        sealed class BoldLabelScope : UnityEngine.GUI.Scope
        {
            readonly UnityEngine.FontStyle m_PrevFontStyle;

            public BoldLabelScope()
            {
                m_PrevFontStyle = EditorStyles.label.fontStyle;
                EditorStyles.label.fontStyle = UnityEngine.FontStyle.Bold;
            }

            protected override void CloseScope()
            {
                EditorStyles.label.fontStyle = m_PrevFontStyle;
            }
        }
    }
}

#endif
