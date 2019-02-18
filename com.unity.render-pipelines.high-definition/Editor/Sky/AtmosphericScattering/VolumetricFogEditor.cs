using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEditor.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [VolumeComponentEditor(typeof(VolumetricFog))]
    public class VolumetricFogEditor : AtmosphericScatteringEditor
    {
        SerializedDataParameter m_Albedo;
        SerializedDataParameter m_MeanFreePath;
        SerializedDataParameter m_BaseHeight;
        SerializedDataParameter m_MeanHeight;
        SerializedDataParameter m_Anisotropy;
        SerializedDataParameter m_GlobalLightProbeDimmer;
        SerializedDataParameter m_EnableDistantFog;

        static GUIContent s_AlbedoLabel                 = new GUIContent("Single Scattering Albedo", "Specifies the color this fog scatteres light to.");
        static GUIContent s_MeanFreePathLabel           = new GUIContent("Base Fog Distance", "Sets the density at the base of the fog. Determines how far you can see through the fog in meters.");
        static GUIContent s_BaseHeightLabel             = new GUIContent("Base Height", "Sets the height of the boundary between the constant and exponential fog.");
        static GUIContent s_MeanHeightLabel             = new GUIContent("Mean Height", "Sets the rate of falloff for the height fog. Higher values stretch the fog vertically.");
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
            m_MeanHeight             = Unpack(o.Find(x => x.meanHeight));
            m_Anisotropy             = Unpack(o.Find(x => x.anisotropy));
            m_GlobalLightProbeDimmer = Unpack(o.Find(x => x.globalLightProbeDimmer));
            m_EnableDistantFog       = Unpack(o.Find(x => x.enableDistantFog));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Albedo,                 s_AlbedoLabel);
            PropertyField(m_MeanFreePath,           s_MeanFreePathLabel);
            PropertyField(m_BaseHeight,             s_BaseHeightLabel);
            PropertyField(m_MeanHeight,             s_MeanHeightLabel);
            PropertyField(m_Anisotropy,             s_AnisotropyLabel);
            PropertyField(m_GlobalLightProbeDimmer, s_GlobalLightProbeDimmerLabel);
            PropertyField(m_MaxFogDistance);
            PropertyField(m_EnableDistantFog,       s_EnableDistantFog);

            if (m_EnableDistantFog.value.boolValue)
            {
                EditorGUI.indentLevel++;
                base.OnInspectorGUI(); // Color
                EditorGUI.indentLevel--;
            }
        }
    }
}
