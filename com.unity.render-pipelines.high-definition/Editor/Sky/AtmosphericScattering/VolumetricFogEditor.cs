using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.Rendering;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [VolumeComponentEditor(typeof(VolumetricFog))]
    class VolumetricFogEditor : AtmosphericScatteringEditor
    {
        SerializedDataParameter m_Albedo;
        SerializedDataParameter m_MeanFreePath;
        SerializedDataParameter m_BaseHeight;
        SerializedDataParameter m_MaximumHeight;
        SerializedDataParameter m_Anisotropy;
        SerializedDataParameter m_GlobalLightProbeDimmer;
        SerializedDataParameter m_EnableDistantFog;

        static GUIContent s_AlbedoLabel                 = new GUIContent("Fog Albedo", "Specifies the color this fog scatters light to.");
        static GUIContent s_MeanFreePathLabel           = new GUIContent("Fog Attenuation Distance", "Controls the density at the base level (per color channel). Distance at which fog reduces background light intensity by 63%. Units: m.");
        static GUIContent s_BaseHeightLabel             = new GUIContent("Base Height", "Reference height (e.g. sea level). Sets the height of the boundary between the constant and exponential fog.");
        static GUIContent s_MaximumHeightLabel          = new GUIContent("Maximum Height", "Max height of the fog layer. Controls the rate of height-based density falloff. Units: m.");
        static GUIContent s_AnisotropyLabel             = new GUIContent("Global Anisotropy", "Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering.");
        static GUIContent s_GlobalLightProbeDimmerLabel = new GUIContent("Global Light Probe Dimmer", "Controls the intensity reduction of the global Light Probe that the sky generates.");
        static GUIContent s_EnableDistantFog            = new GUIContent("Distant Fog", "When enabled, activates fog with precomputed lighting behind the volumetric section of the Camera’s frustum.");

        public override void OnEnable()
        {
            base.OnEnable();
            var o = new PropertyFetcher<VolumetricFog>(serializedObject);

            m_Albedo                 = Unpack(o.Find(x => x.albedo));
            m_MeanFreePath           = Unpack(o.Find(x => x.meanFreePath));
            m_BaseHeight             = Unpack(o.Find(x => x.baseHeight));
            m_MaximumHeight          = Unpack(o.Find(x => x.maximumHeight));
            m_Anisotropy             = Unpack(o.Find(x => x.anisotropy));
            m_GlobalLightProbeDimmer = Unpack(o.Find(x => x.globalLightProbeDimmer));
            m_EnableDistantFog       = Unpack(o.Find(x => x.enableDistantFog));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Albedo,                 s_AlbedoLabel);
            PropertyField(m_MeanFreePath,           s_MeanFreePathLabel);
            PropertyField(m_BaseHeight,             s_BaseHeightLabel);
            PropertyField(m_MaximumHeight,          s_MaximumHeightLabel);
            PropertyField(m_Anisotropy,             s_AnisotropyLabel);
            PropertyField(m_GlobalLightProbeDimmer, s_GlobalLightProbeDimmerLabel);
            PropertyField(m_MaxFogDistance);
            PropertyField(m_EnableDistantFog,       s_EnableDistantFog);

            if (m_MaximumHeight.value.floatValue < m_BaseHeight.value.floatValue)
            {
                m_MaximumHeight.value.floatValue = m_BaseHeight.value.floatValue;
                serializedObject.ApplyModifiedProperties();
            }

            if (m_EnableDistantFog.value.boolValue)
            {
                EditorGUI.indentLevel++;
                base.OnInspectorGUI(); // Color
                EditorGUI.indentLevel--;
            }

            if (!(GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset)
                ?.currentPlatformRenderPipelineSettings.supportVolumetrics ?? false)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("The current HDRP Asset does not support Volumetrics.", MessageType.Error, wide: true);
            }
        }
    }
}
