using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(Fog))]
    class FogEditor : VolumeComponentEditor
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
        protected SerializedDataParameter m_Anisotropy;
        protected SerializedDataParameter m_GlobalLightProbeDimmer;
        protected SerializedDataParameter m_EnableVolumetricFog;
        protected SerializedDataParameter m_DepthExtent;
        protected SerializedDataParameter m_SliceDistributionUniformity;
        protected SerializedDataParameter m_Filter;

        static GUIContent s_Enabled = new GUIContent("Enable", "Check this to enable fog in your scene.");
        static GUIContent s_AlbedoLabel = new GUIContent("Albedo", "Specifies the color this fog scatters light to.");
        static GUIContent s_MeanFreePathLabel = new GUIContent("Fog Attenuation Distance", "Controls the density at the base level (per color channel). Distance at which fog reduces background light intensity by 63%. Units: m.");
        static GUIContent s_BaseHeightLabel = new GUIContent("Base Height", "Reference height (e.g. sea level). Sets the height of the boundary between the constant and exponential fog.");
        static GUIContent s_MaximumHeightLabel = new GUIContent("Maximum Height", "Max height of the fog layer. Controls the rate of height-based density falloff. Units: m.");
        static GUIContent s_AnisotropyLabel = new GUIContent("Anisotropy", "Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering.");
        static GUIContent s_GlobalLightProbeDimmerLabel = new GUIContent("Ambient Light Probe Dimmer", "Controls the intensity reduction of the global Light Probe that the sky generates.");
        static GUIContent s_EnableVolumetricFog = new GUIContent("Volumetric Fog", "When enabled, activates volumetric fog.");

        public override bool hasAdvancedMode => true;

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
            m_Filter = Unpack(o.Find(x => x.filter));
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
            EditorGUI.indentLevel++;
            if (!m_ColorMode.value.hasMultipleDifferentValues && (FogColorMode)m_ColorMode.value.intValue == FogColorMode.ConstantColor)
            {
                PropertyField(m_Color);
            }
            else
            {
                PropertyField(m_Tint);

                if (isInAdvancedMode)
                {
                    PropertyField(m_MipFogNear);
                    PropertyField(m_MipFogFar);
                    PropertyField(m_MipFogMaxMip);
                }
            }
            EditorGUI.indentLevel--;

            bool volumetricLightingAvailable = false;
            var hdpipe = HDRenderPipeline.currentAsset;
            if (hdpipe != null)
                volumetricLightingAvailable = hdpipe.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (volumetricLightingAvailable)
            {
                PropertyField(m_EnableVolumetricFog, s_EnableVolumetricFog);
                if (m_EnableVolumetricFog.value.boolValue)
                {
                    EditorGUI.indentLevel++;
                    PropertyField(m_Albedo, s_AlbedoLabel);
                    PropertyField(m_Anisotropy, s_AnisotropyLabel);
                    PropertyField(m_GlobalLightProbeDimmer, s_GlobalLightProbeDimmerLabel);

                    if (isInAdvancedMode)
                    {
                        PropertyField(m_DepthExtent);
                        PropertyField(m_SliceDistributionUniformity);
                        PropertyField(m_Filter);
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }
    }
}
