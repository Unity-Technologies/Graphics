using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Fog))]
    class FogEditor : VolumeComponentWithQualityEditor
    {
        protected SerializedDataParameter m_Enabled;
        protected SerializedDataParameter m_MaxFogDistance;
        protected SerializedDataParameter m_ColorMode;
        protected SerializedDataParameter m_Color;
        protected SerializedDataParameter m_Tint;
        protected SerializedDataParameter m_MipFogNear;
        protected SerializedDataParameter m_MipFogFar;
        protected SerializedDataParameter m_MipFogMaxMip;
        protected SerializedDataParameter m_Albedo;
        protected SerializedDataParameter m_MeanFreePath;
        protected SerializedDataParameter m_BaseHeight;
        protected SerializedDataParameter m_MaximumHeight;

        protected SerializedDataParameter m_EnableVolumetricFog;
        protected SerializedDataParameter m_Anisotropy;
        protected SerializedDataParameter m_DepthExtent;
        protected SerializedDataParameter m_GlobalLightProbeDimmer;
        protected SerializedDataParameter m_SliceDistributionUniformity;
        protected SerializedDataParameter m_FogControlMode;
        protected SerializedDataParameter m_ScreenResolutionPercentage;
        protected SerializedDataParameter m_VolumeSliceCount;
        protected SerializedDataParameter m_VolumetricFogBudget;
        protected SerializedDataParameter m_ResolutionDepthRatio;
        protected SerializedDataParameter m_DirectionalLightsOnly;
        protected SerializedDataParameter m_DenoisingMode;

        static GUIContent s_Enabled = new GUIContent("State", "When set to Enabled, HDRP renders fog in your scene.");
        static GUIContent s_AlbedoLabel = new GUIContent("Albedo", "Specifies the color this fog scatters light to.");
        static GUIContent s_MeanFreePathLabel = new GUIContent("Fog Attenuation Distance", "Controls the density at the base level (per color channel). Distance at which fog reduces background light intensity by 63%. Units: m.");
        static GUIContent s_BaseHeightLabel = new GUIContent("Base Height", "Reference height (e.g. sea level). Sets the height of the boundary between the constant and exponential fog.");
        static GUIContent s_MaximumHeightLabel = new GUIContent("Maximum Height", "Max height of the fog layer. Controls the rate of height-based density falloff. Units: m.");
        static GUIContent s_AnisotropyLabel = new GUIContent("Anisotropy", "Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering.");
        static GUIContent s_GlobalLightProbeDimmerLabel = new GUIContent("GI Dimmer", "Controls the intensity reduction of the global illumination contribution to volumetric fog. This is either APV (if enabled and present) or the global light probe that the sky produces.");
        static GUIContent s_EnableVolumetricFog = new GUIContent("Volumetric Fog", "When enabled, activates volumetric fog.");
        static GUIContent s_DepthExtentLabel = new GUIContent("Volumetric Fog Distance", "Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer. The lower the distance is, the higher the fog quality is.");

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Fog>(serializedObject);

            m_Enabled = Unpack(o.Find(x => x.enabled));
            m_MaxFogDistance = Unpack(o.Find(x => x.maxFogDistance));

            // Fog Color
            m_ColorMode = Unpack(o.Find(x => x.colorMode));
            m_Color = Unpack(o.Find(x => x.color));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_MipFogNear = Unpack(o.Find(x => x.mipFogNear));
            m_MipFogFar = Unpack(o.Find(x => x.mipFogFar));
            m_MipFogMaxMip = Unpack(o.Find(x => x.mipFogMaxMip));
            m_Albedo = Unpack(o.Find(x => x.albedo));
            m_MeanFreePath = Unpack(o.Find(x => x.meanFreePath));
            m_BaseHeight = Unpack(o.Find(x => x.baseHeight));
            m_MaximumHeight = Unpack(o.Find(x => x.maximumHeight));
            m_Anisotropy = Unpack(o.Find(x => x.anisotropy));
            m_GlobalLightProbeDimmer = Unpack(o.Find(x => x.globalLightProbeDimmer));
            m_EnableVolumetricFog = Unpack(o.Find(x => x.enableVolumetricFog));
            m_DepthExtent = Unpack(o.Find(x => x.depthExtent));
            m_SliceDistributionUniformity = Unpack(o.Find(x => x.sliceDistributionUniformity));
            m_FogControlMode = Unpack(o.Find(x => x.fogControlMode));
            m_ScreenResolutionPercentage = Unpack(o.Find(x => x.screenResolutionPercentage));
            m_VolumeSliceCount = Unpack(o.Find(x => x.volumeSliceCount));
            m_VolumetricFogBudget = Unpack(o.Find(x => x.volumetricFogBudget));
            m_ResolutionDepthRatio = Unpack(o.Find(x => x.resolutionDepthRatio));
            m_DirectionalLightsOnly = Unpack(o.Find(x => x.directionalLightsOnly));
            m_DenoisingMode = Unpack(o.Find(x => x.denoisingMode));

            base.OnEnable();
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enabled, s_Enabled);

            PropertyField(m_MeanFreePath, s_MeanFreePathLabel);
            PropertyField(m_BaseHeight, s_BaseHeightLabel);
            PropertyField(m_MaximumHeight, s_MaximumHeightLabel);
            PropertyField(m_MaxFogDistance);

            if (m_MaximumHeight.value.floatValue < m_BaseHeight.value.floatValue)
            {
                m_MaximumHeight.value.floatValue = m_BaseHeight.value.floatValue;
                serializedObject.ApplyModifiedProperties();
            }

            PropertyField(m_ColorMode);

            using (new IndentLevelScope())
            {
                if (!m_ColorMode.value.hasMultipleDifferentValues &&
                    (FogColorMode)m_ColorMode.value.intValue == FogColorMode.ConstantColor)
                {
                    PropertyField(m_Color);
                }
                else
                {
                    PropertyField(m_Tint);
                    PropertyField(m_MipFogNear);
                    PropertyField(m_MipFogFar);
                    PropertyField(m_MipFogMaxMip);
                }
            }

            bool volumetricLightingAvailable = false;
            var hdpipe = HDRenderPipeline.currentAsset;
            if (hdpipe != null)
                volumetricLightingAvailable = hdpipe.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (volumetricLightingAvailable)
            {
                PropertyField(m_EnableVolumetricFog, s_EnableVolumetricFog);

                using (new IndentLevelScope())
                {
                    PropertyField(m_Albedo, s_AlbedoLabel);
                    PropertyField(m_GlobalLightProbeDimmer, s_GlobalLightProbeDimmerLabel);
                    PropertyField(m_DepthExtent, s_DepthExtentLabel);
                    PropertyField(m_DenoisingMode);

                    PropertyField(m_SliceDistributionUniformity);

                    base.OnInspectorGUI(); // Quality Setting

                    using (new IndentLevelScope())
                    using (new QualityScope(this))
                    {
                        if (PropertyField(m_FogControlMode))
                        {
                            using (new IndentLevelScope())
                            {
                                if ((FogControl)m_FogControlMode.value.intValue == FogControl.Balance)
                                {
                                    PropertyField(m_VolumetricFogBudget);
                                    PropertyField(m_ResolutionDepthRatio);
                                }
                                else
                                {
                                    PropertyField(m_ScreenResolutionPercentage);
                                    PropertyField(m_VolumeSliceCount);
                                }
                            }
                        }
                    }

                    PropertyField(m_DirectionalLightsOnly);
                    PropertyField(m_Anisotropy, s_AnisotropyLabel);
                    if (m_Anisotropy.value.floatValue != 0.0f)
                    {
                        if (BeginAdditionalPropertiesScope())
                        {
                            EditorGUILayout.Space();
                            EditorGUILayout.HelpBox(
                                "When the value is not 0, the anisotropy effect significantly increases the performance impact of volumetric fog.",
                                MessageType.Info, wide: true);
                        }
                        EndAdditionalPropertiesScope();
                    }
                }
            }
        }

        public override QualitySettingsBlob SaveCustomQualitySettingsAsObject(QualitySettingsBlob settings = null)
        {
            if (settings == null)
                settings = new QualitySettingsBlob();

            settings.Save<int>(m_FogControlMode);

            settings.Save<float>(m_VolumetricFogBudget);
            settings.Save<float>(m_ResolutionDepthRatio);

            return settings;
        }

        public override void LoadSettingsFromObject(QualitySettingsBlob settings)
        {
            settings.TryLoad<int>(ref m_FogControlMode);

            settings.TryLoad<float>(ref m_VolumetricFogBudget);
            settings.TryLoad<float>(ref m_ResolutionDepthRatio);
        }

        public override void LoadSettingsFromQualityPreset(RenderPipelineSettings settings, int level)
        {
            CopySetting(ref m_FogControlMode, settings.lightingQualitySettings.Fog_ControlMode[level]);

            CopySetting(ref m_VolumetricFogBudget, settings.lightingQualitySettings.Fog_Budget[level]);
            CopySetting(ref m_ResolutionDepthRatio, settings.lightingQualitySettings.Fog_DepthRatio[level]);
        }
    }
}
