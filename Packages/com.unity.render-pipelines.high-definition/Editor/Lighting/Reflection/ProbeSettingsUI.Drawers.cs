using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using static UnityEditor.Rendering.HighDefinition.HDEditorUtils;

namespace UnityEditor.Rendering.HighDefinition
{
    internal partial class ProbeSettingsUI
    {
        public static void Draw(
            SerializedProbeSettings serialized, Editor owner,
            ProbeSettingsOverride displayedFields
        )
        {
            const ProbeSettingsFields lighting = ProbeSettingsFields.lightingLightLayer
                | ProbeSettingsFields.importance
                | ProbeSettingsFields.lightingMultiplier
                | ProbeSettingsFields.lightingWeight
                | ProbeSettingsFields.lightingFadeDistance;
            const ProbeSettingsFields proxy = ProbeSettingsFields.proxyCapturePositionProxySpace
                | ProbeSettingsFields.proxyCaptureRotationProxySpace
                | ProbeSettingsFields.proxyMirrorPositionProxySpace
                | ProbeSettingsFields.proxyMirrorRotationProxySpace
                | ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume
                | ProbeSettingsFields.lightingRangeCompression;
            const ProbeSettingsFields frustum = ProbeSettingsFields.frustumFieldOfViewMode
                | ProbeSettingsFields.frustumAutomaticScale
                | ProbeSettingsFields.frustumFixedValue
                | ProbeSettingsFields.resolution;

            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hd))
                return;

            if ((displayedFields.probe & lighting) != 0)
            {
                using (new EditorGUI.DisabledScope(!hd.currentPlatformRenderPipelineSettings.supportLightLayers))
                {
                    PropertyFieldWithoutToggle(ProbeSettingsFields.lightingLightLayer, serialized.lightingLightLayer, L10n.TextContent("Rendering Layer Mask", "This Reflection Probe only affects Renderers with a matching Rendering Layer Mask.\nThis is only available when Light Layers are enabled.", null, null), displayedFields.probe,
                        (property, label) => EditorGUILayout.PropertyField(property, label)
                    );
                }
                PropertyFieldWithoutToggle(ProbeSettingsFields.importance, serialized.importance, L10n.TextContent("Importance", "When reflection probes overlap, Unity uses Importance to determine which probe should take priority.", null, null), displayedFields.probe);
                if (serialized.importance.intValue < 0 || serialized.importance.intValue > 32767)
                    serialized.importance.intValue = Mathf.Clamp(serialized.importance.intValue, 0, 32767);
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingMultiplier, serialized.lightingMultiplier, L10n.TextContent("Multiplier", "Sets the multiplier value that reflective Materials apply to the results from the Reflection Probe.", null, null), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingWeight, serialized.lightingWeight, L10n.TextContent("Weight", "Sets the weight of this Reflection Probe. When multiple Probes both affect the same area of a reflective Material, the Material uses the Weight of each Probe to determine their contribution to the reflective effect.", null, null), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingFadeDistance, serialized.lightingFadeDistance, L10n.TextContent("Fade Distance", "Sets the distance from the camera at which reflections smoothly fadeout before HDRP cuts them completely.", null, null), displayedFields.probe);
            }

            if ((displayedFields.probe & frustum) != 0)
            {
                PropertyFieldWithoutToggle(ProbeSettingsFields.frustumFieldOfViewMode, serialized.frustumFieldOfViewMode, L10n.TextContent("Field Of View Mode", null, null, null), displayedFields.probe);
                switch (serialized.frustumFieldOfViewMode.GetEnumValue<ProbeSettings.Frustum.FOVMode>())
                {
                    case ProbeSettings.Frustum.FOVMode.Fixed:
                        PropertyFieldWithoutToggle(ProbeSettingsFields.frustumFixedValue, serialized.frustumFixedValue, L10n.TextContent("Value", null, null, null), displayedFields.probe, indent: 1);
                        break;
                    case ProbeSettings.Frustum.FOVMode.Viewer:
                        PropertyFieldWithoutToggle(ProbeSettingsFields.frustumViewerScale, serialized.frustumViewerScale, L10n.TextContent("Scale", null, null, null), displayedFields.probe, indent: 1);
                        break;
                    case ProbeSettings.Frustum.FOVMode.Automatic:
                        PropertyFieldWithoutToggle(ProbeSettingsFields.frustumAutomaticScale, serialized.frustumAutomaticScale, L10n.TextContent("Scale", null, null, null), displayedFields.probe, indent: 1);
                        break;
                }
                EditorGUILayout.Space();
            }

            if ((displayedFields.probe & proxy) != 0)
            {
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume, serialized.proxyUseInfluenceVolumeAsProxyVolume, L10n.TextContent("Use Influence Volume As Proxy Volume", "When enabled, this Reflection Probe uses the boundaries of the Influence Volume as its Proxy Volume.", null, null), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyCapturePositionProxySpace, serialized.proxyCapturePositionProxySpace, L10n.TextContent("Capture Position", "Sets the position, relative to the Proxy Volume Position, from which the Reflection Probe captures its surroundings.", null, null), displayedFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                    }
                );
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyCaptureRotationProxySpace, serialized.proxyCaptureRotationProxySpace, L10n.TextContent("Capture Rotation", "Sets the rotation of the capture point relative to the Transform Rotation.", null, null), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyMirrorPositionProxySpace, serialized.proxyMirrorPositionProxySpace, L10n.TextContent("Mirror Position", "Sets the position of the Planar Reflection Probe relative to the Transform Position.", null, null), displayedFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                    }
                );
            }

            CameraSettingsUI.Draw(serialized.cameraSettings, owner, displayedFields.camera);

            // Only display the field if it should
            if (((int)ProbeSettingsFields.resolution & (int)displayedFields.probe) != 0)
            {
                var scalableSetting = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.planarReflectionResolution;
                serialized.resolutionScalable.LevelAndEnumGUILayout<PlanarReflectionAtlasResolution>(
                    L10n.TextContent("Resolution", "Sets the resolution for the planar reflection probe camera.", null, null), scalableSetting, null
                );
            }

            if (((int)ProbeSettingsFields.cubeResolution & (int)displayedFields.probe) != 0)
            {
                var scalableSetting = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.cubeReflectionResolution;
                serialized.cubeResolution.LevelAndEnumGUILayout<CubeReflectionResolution>(
                    L10n.TextContent("Resolution", "Sets the resolution for the reflection probe camera.", null, null), scalableSetting, null
                );
            }

            PropertyFieldWithoutToggle(ProbeSettingsFields.roughReflections, serialized.roughReflections, L10n.TextContent("Rough Reflections", "When disabled the reflections evaluated using the planar reflection will be perfectly smooth. This save GPU time when the planar reflection is used as a pure mirror.", null, null), displayedFields.probe);

            if ((displayedFields.probe & proxy) != 0)
            {
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingRangeCompression, serialized.lightingRangeCompressionFactor, L10n.TextContent("Range Compression Factor", "The result of the rendering of the probe will be divided by this factor. When the probe is read, this factor is undone as the probe data is read. This is to simply avoid issues with values clamping due to precision of the storing format.", null, null), displayedFields.probe);
            }
        }
    }
}
