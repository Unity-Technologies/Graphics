using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [VolumeComponentEditor(typeof(IndirectLightingController))]
    class IndirectLightingControllerEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_IndirectDiffuseLightingMultiplier;
        SerializedDataParameter m_IndirectDiffuseLightingLayers;

        SerializedDataParameter m_ReflectionLightingMultiplier;
        SerializedDataParameter m_ReflectionLightingLayers;

        SerializedDataParameter m_ReflectionProbeIntensityMultiplier;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<IndirectLightingController>(serializedObject);

            m_IndirectDiffuseLightingMultiplier = Unpack(o.Find(x => x.indirectDiffuseLightingMultiplier));
            m_IndirectDiffuseLightingLayers = Unpack(o.Find(x => x.indirectDiffuseLightingLayers));

            m_ReflectionLightingMultiplier = Unpack(o.Find(x => x.reflectionLightingMultiplier));
            m_ReflectionLightingLayers = Unpack(o.Find(x => x.reflectionLightingLayers));

            m_ReflectionProbeIntensityMultiplier = Unpack(o.Find(x => x.reflectionProbeIntensityMultiplier));
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_IndirectDiffuseLightingMultiplier, EditorGUIUtility.TrTextContent("Indirect Diffuse Lighting Multiplier", "Sets the multiplier for indirect diffuse lighting.\nIt affect Ambient Probe, Light Probes, Lightmaps, Light Probe Volumes, Screen Space Global Illumination, Raytrace Global Illumination."));
            using (new EditorGUI.DisabledScope(!HDUtils.hdrpSettings.supportLightLayers))
            {
                PropertyField(m_IndirectDiffuseLightingLayers, EditorGUIUtility.TrTextContent("Indirect Diffuse Lighting Layers", "Sets the light layer mask for indirect diffuse lighting. Only matching RenderingLayers on Mesh will get affected by the multiplier."));
            }

            PropertyField(m_ReflectionLightingMultiplier, EditorGUIUtility.TrTextContent("Reflection Lighting Multiplier", "Sets the multiplier for reflected specular lighting.\nIt affect Sky Reflection, Reflection Probes, Planar Probes, Screen Space Reflection, Raytrace Reflection."));
            using (new EditorGUI.DisabledScope(!HDUtils.hdrpSettings.supportLightLayers))
            {
                PropertyField(m_ReflectionLightingLayers, EditorGUIUtility.TrTextContent("Reflection Lighting Layers", "Sets the light layer mask for reflected specular lighting. Only matching RenderingLayers on Mesh will get affected by the multiplier."));
            }
            PropertyField(m_ReflectionProbeIntensityMultiplier, EditorGUIUtility.TrTextContent("Reflection/Planar Probe Intensity Multiplier", "Sets the intensity multiplier for Reflection/Planar Probes."));
        }
    }
}
