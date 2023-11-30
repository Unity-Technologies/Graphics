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
                    PropertyFieldWithoutToggle(ProbeSettingsFields.lightingLightLayer, serialized.lightingLightLayer, EditorGUIUtility.TrTextContent("Rendering Layer Mask", "This Reflection Probe only affects Renderers with a matching Rendering Layer Mask.\nThis is only available when Light Layers are enabled."), displayedFields.probe,
                        (property, label) => EditorGUILayout.PropertyField(property, label)
                    );
                }
                PropertyFieldWithoutToggle(ProbeSettingsFields.importance, serialized.importance, EditorGUIUtility.TrTextContent("Importance", "When reflection probes overlap, Unity uses Importance to determine which probe should take priority."), displayedFields.probe);
                if (serialized.importance.intValue < 0 || serialized.importance.intValue > 32767)
                    serialized.importance.intValue = Mathf.Clamp(serialized.importance.intValue, 0, 32767);
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingMultiplier, serialized.lightingMultiplier, EditorGUIUtility.TrTextContent("Multiplier", "Sets the multiplier value that reflective Materials apply to the results from the Reflection Probe."), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingWeight, serialized.lightingWeight, EditorGUIUtility.TrTextContent("Weight", "Sets the weight of this Reflection Probe. When multiple Probes both affect the same area of a reflective Material, the Material uses the Weight of each Probe to determine their contribution to the reflective effect."), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingFadeDistance, serialized.lightingFadeDistance, EditorGUIUtility.TrTextContent("Fade Distance", "Sets the distance from the camera at which reflections smoothly fadeout before HDRP cuts them completely."), displayedFields.probe);
            }

            if ((displayedFields.probe & frustum) != 0)
            {
                PropertyFieldWithoutToggle(ProbeSettingsFields.frustumFieldOfViewMode, serialized.frustumFieldOfViewMode, EditorGUIUtility.TrTextContent("Field Of View Mode"), displayedFields.probe);
                switch (serialized.frustumFieldOfViewMode.GetEnumValue<ProbeSettings.Frustum.FOVMode>())
                {
                    case ProbeSettings.Frustum.FOVMode.Fixed:
                        PropertyFieldWithoutToggle(ProbeSettingsFields.frustumFixedValue, serialized.frustumFixedValue, EditorGUIUtility.TrTextContent("Value"), displayedFields.probe, indent: 1);
                        break;
                    case ProbeSettings.Frustum.FOVMode.Viewer:
                        PropertyFieldWithoutToggle(ProbeSettingsFields.frustumViewerScale, serialized.frustumViewerScale, EditorGUIUtility.TrTextContent("Scale"), displayedFields.probe, indent: 1);
                        break;
                    case ProbeSettings.Frustum.FOVMode.Automatic:
                        PropertyFieldWithoutToggle(ProbeSettingsFields.frustumAutomaticScale, serialized.frustumAutomaticScale, EditorGUIUtility.TrTextContent("Scale"), displayedFields.probe, indent: 1);
                        break;
                }
                EditorGUILayout.Space();
            }

            if ((displayedFields.probe & proxy) != 0)
            {
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume, serialized.proxyUseInfluenceVolumeAsProxyVolume, EditorGUIUtility.TrTextContent("Use Influence Volume As Proxy Volume", "When enabled, this Reflection Probe uses the boundaries of the Influence Volume as its Proxy Volume."), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyCapturePositionProxySpace, serialized.proxyCapturePositionProxySpace, EditorGUIUtility.TrTextContent("Capture Position", "Sets the position, relative to the Proxy Volume Position, from which the Reflection Probe captures its surroundings."), displayedFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                        HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.CapturePosition, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
                    }
                );
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyCaptureRotationProxySpace, serialized.proxyCaptureRotationProxySpace, EditorGUIUtility.TrTextContent("Capture Rotation", "Sets the rotation of the capture point relative to the Transform Rotation."), displayedFields.probe);
                PropertyFieldWithoutToggle(ProbeSettingsFields.proxyMirrorPositionProxySpace, serialized.proxyMirrorPositionProxySpace, EditorGUIUtility.TrTextContent("Mirror Position", "Sets the position of the Planar Reflection Probe relative to the Transform Position."), displayedFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                        HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.MirrorPosition, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
                    }
                );
            }

            CameraSettingsUI.Draw(serialized.cameraSettings, owner, displayedFields.camera);

            // Only display the field if it should
            if (((int)ProbeSettingsFields.resolution & (int)displayedFields.probe) != 0)
            {
                var scalableSetting = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.planarReflectionResolution;
                serialized.resolutionScalable.LevelAndEnumGUILayout<PlanarReflectionAtlasResolution>(
                    EditorGUIUtility.TrTextContent("Resolution", "Sets the resolution for the planar reflection probe camera."), scalableSetting, null
                );
            }

            if (((int)ProbeSettingsFields.cubeResolution & (int)displayedFields.probe) != 0)
            {
                var scalableSetting = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.cubeReflectionResolution;
                serialized.cubeResolution.LevelAndEnumGUILayout<CubeReflectionResolution>(
                    EditorGUIUtility.TrTextContent("Resolution", "Sets the resolution for the reflection probe camera."), scalableSetting, null
                );
            }

            PropertyFieldWithoutToggle(ProbeSettingsFields.roughReflections, serialized.roughReflections, EditorGUIUtility.TrTextContent("Rough Reflections", "When disabled the reflections evaluated using the planar reflection will be perfectly smooth. This save GPU time when the planar reflection is used as a pure mirror."), displayedFields.probe);

            if ((displayedFields.probe & proxy) != 0)
            {
                PropertyFieldWithoutToggle(ProbeSettingsFields.lightingRangeCompression, serialized.lightingRangeCompressionFactor, EditorGUIUtility.TrTextContent("Range Compression Factor", "The result of the rendering of the probe will be divided by this factor. When the probe is read, this factor is undone as the probe data is read. This is to simply avoid issues with values clamping due to precision of the storing format."), displayedFields.probe);
            }
        }
    }
}
