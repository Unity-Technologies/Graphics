using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(IndirectLightingController))]
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
            PropertyField(m_IndirectDiffuseLightingMultiplier, L10n.TextContent("Indirect Diffuse Lighting Multiplier", "Sets the multiplier for indirect diffuse lighting.\nIt affect Ambient Probe, Light Probes, Lightmaps, Adaptive Probe Volumes, Screen Space Global Illumination, Raytrace Global Illumination.", null, null));
            using (new EditorGUI.DisabledScope(!HDUtils.hdrpSettings.supportLightLayers))
            {
                PropertyField(m_IndirectDiffuseLightingLayers, L10n.TextContent("Indirect Diffuse Rendering Layer Mask", "The Indirect Diffuse Lighting Multiplier only affects Renderers with a matching Rendering Layer Mask.\nThis is only available when Light Layers are enabled.", null, null));
            }

            PropertyField(m_ReflectionLightingMultiplier, L10n.TextContent("Reflection Lighting Multiplier", "Sets the multiplier for reflected specular lighting.\nIt affect Sky Reflection, Reflection Probes, Planar Probes, Screen Space Reflection, Raytrace Reflection.", null, null));
            using (new EditorGUI.DisabledScope(!HDUtils.hdrpSettings.supportLightLayers))
            {
                PropertyField(m_ReflectionLightingLayers, L10n.TextContent("Reflection Rendering Layer Mask", "Sets the Rendering Layer Mask for reflected specular lighting. Only Renderers with a matching Rendering Layer Mask will get affected by the multiplier.\nThis is only available when Light Layers are enabled.", null, null));
            }
            PropertyField(m_ReflectionProbeIntensityMultiplier, L10n.TextContent("Reflection/Planar Probe Intensity Multiplier", "Sets the intensity multiplier for Reflection/Planar Probes.", null, null));
        }
    }
}
