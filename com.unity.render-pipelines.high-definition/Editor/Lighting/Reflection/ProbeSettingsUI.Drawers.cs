using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using static UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils;

namespace UnityEditor.Experimental.Rendering.HDPipeline
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
                | ProbeSettingsFields.lightingWeight;
            const ProbeSettingsFields proxy = ProbeSettingsFields.proxyCapturePositionProxySpace
                | ProbeSettingsFields.proxyCaptureRotationProxySpace
                | ProbeSettingsFields.proxyMirrorPositionProxySpace
                | ProbeSettingsFields.proxyMirrorRotationProxySpace
                | ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume;
            const ProbeSettingsFields frustum = ProbeSettingsFields.frustumFieldOfViewMode
                | ProbeSettingsFields.frustumAutomaticScale
                | ProbeSettingsFields.frustumFixedValue;

            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hd))
                return;

            if ((displayedFields.probe & lighting) != 0)
            {

                GUI.enabled = hd.currentPlatformRenderPipelineSettings.supportLightLayers;
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingLightLayer, serialized.lightingLightLayer, EditorGUIUtility.TrTextContent("Light Layer", "Specifies the Light Layer the Reflection Probe uses to capture its view of the Scene. The Probe only uses Lights on the Light Layer you specify."), @override.probe, displayedFields.probe, overridableFields.probe,
                    (property, label) => LightLayerMaskPropertyDrawer(label, property)
                );

                GUI.enabled = true;
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingMultiplier, serialized.lightingMultiplier, EditorGUIUtility.TrTextContent("Multiplier", "Sets the multiplier value that reflective Materials apply to the results from the Reflection Probe."), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingWeight, serialized.lightingWeight, EditorGUIUtility.TrTextContent("Weight", "Sets the weight of this Reflection Probe. When multiple Probes both affect the same area of a reflective Material, the Material uses the Weight of each Probe to determine their contribution to the reflective effect."), @override.probe, displayedFields.probe, overridableFields.probe);
                EditorGUILayout.Space();
            }

            if ((displayedFields.probe & frustum) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.frustumFieldOfViewMode, serialized.frustumFieldOfViewMode, EditorGUIUtility.TrTextContent("Field Of View Mode"), @override.probe, displayedFields.probe, overridableFields.probe);
                switch ((ProbeSettings.Frustum.FOVMode)serialized.frustumFieldOfViewMode.enumValueIndex)
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
                EditorGUILayout.Space();
            }

            CameraSettingsUI.Draw(serialized.cameraSettings, owner, @override.camera, displayedFields.camera, overridableFields.camera);
        }
    }
}
