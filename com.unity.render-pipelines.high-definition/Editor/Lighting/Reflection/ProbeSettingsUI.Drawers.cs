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
            SerializedProbeSettingsOverride @override,
            ProbeSettingsOverride displayedFields, ProbeSettingsOverride overridableFields
        )
        {
            const ProbeSettingsFields lighting = ProbeSettingsFields.lightingLightLayer
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
                | ProbeSettingsFields.frustumFixedValue;

            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hd))
                return;

            if ((displayedFields.probe & lighting) != 0)
            {

                GUI.enabled = hd.currentPlatformRenderPipelineSettings.supportLightLayers;
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingLightLayer, serialized.lightingLightLayer, EditorGUIUtility.TrTextContent("Light Layer", "Specifies the Light Layer the Reflection Probe uses to capture its view of the Scene. The Probe only uses Lights on the Light Layer you specify."), @override.probe, displayedFields.probe, overridableFields.probe,
                    (property, label) => EditorGUILayout.PropertyField(property, label)
                );

                GUI.enabled = true;
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingMultiplier, serialized.lightingMultiplier, EditorGUIUtility.TrTextContent("Multiplier", "Sets the multiplier value that reflective Materials apply to the results from the Reflection Probe."), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingWeight, serialized.lightingWeight, EditorGUIUtility.TrTextContent("Weight", "Sets the weight of this Reflection Probe. When multiple Probes both affect the same area of a reflective Material, the Material uses the Weight of each Probe to determine their contribution to the reflective effect."), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingFadeDistance, serialized.lightingFadeDistance, EditorGUIUtility.TrTextContent("Fade Distance", "Specifies the distance at which reflections smoothly fadeout before HDRP cuts them completely."), @override.probe, displayedFields.probe, overridableFields.probe);
            }

            if ((displayedFields.probe & frustum) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.frustumFieldOfViewMode, serialized.frustumFieldOfViewMode, EditorGUIUtility.TrTextContent("Field Of View Mode"), @override.probe, displayedFields.probe, overridableFields.probe);
                switch (serialized.frustumFieldOfViewMode.GetEnumValue<ProbeSettings.Frustum.FOVMode>())
                {
                    case ProbeSettings.Frustum.FOVMode.Fixed:
                        PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.frustumFixedValue, serialized.frustumFixedValue, EditorGUIUtility.TrTextContent("Value"), @override.probe, displayedFields.probe, overridableFields.probe, indent: 1);
                        break;
                    case ProbeSettings.Frustum.FOVMode.Viewer:
                        PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.frustumViewerScale, serialized.frustumViewerScale, EditorGUIUtility.TrTextContent("Scale"), @override.probe, displayedFields.probe, overridableFields.probe, indent: 1);
                        break;
                    case ProbeSettings.Frustum.FOVMode.Automatic:
                        PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.frustumAutomaticScale, serialized.frustumAutomaticScale, EditorGUIUtility.TrTextContent("Scale"), @override.probe, displayedFields.probe, overridableFields.probe, indent: 1);
                        break;
                }
                EditorGUILayout.Space();
            }

            if ((displayedFields.probe & proxy) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume, serialized.proxyUseInfluenceVolumeAsProxyVolume, EditorGUIUtility.TrTextContent("Use Influence Volume As Proxy Volume", "When enabled, this Reflection Probe uses the boundaries of the Influence Volume as its Proxy Volume."), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyCapturePositionProxySpace, serialized.proxyCapturePositionProxySpace, EditorGUIUtility.TrTextContent("Capture Position", "Sets the position, relative to the Transform Position, from which the Reflection Probe captures its surroundings."), @override.probe, displayedFields.probe, overridableFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                        HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.CapturePosition, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
                    }
                );
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyCaptureRotationProxySpace, serialized.proxyCaptureRotationProxySpace, EditorGUIUtility.TrTextContent("Capture Rotation", "Sets the rotation of the capture point relative to the Transform Rotation."), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyMirrorPositionProxySpace, serialized.proxyMirrorPositionProxySpace, EditorGUIUtility.TrTextContent("Mirror Position", "Sets the position of the Planar Reflection Probe relative to the Transform Position."), @override.probe, displayedFields.probe, overridableFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                        HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.MirrorPosition, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
                    }
                );
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyMirrorRotationProxySpace, serialized.proxyMirrorRotationProxySpace, EditorGUIUtility.TrTextContent("Mirror Rotation", "Sets the rotation of the Planar Reflection Probe relative to the Transform Rotation."), @override.probe, displayedFields.probe, overridableFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                        HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.MirrorRotation, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
                    }
                );
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingRangeCompression, serialized.lightingRangeCompressionFactor, EditorGUIUtility.TrTextContent("Range Compression Factor", "The result of the rendering of the probe will be divided by this factor. When the probe is read, this factor is undone as the probe data is read. This is to simply avoid issues with values clamping due to precision of the storing format."), @override.probe, displayedFields.probe, overridableFields.probe);

                EditorGUILayout.Space();
            }

            CameraSettingsUI.Draw(serialized.cameraSettings, owner, @override.camera, displayedFields.camera, overridableFields.camera);
        }
    }
}
