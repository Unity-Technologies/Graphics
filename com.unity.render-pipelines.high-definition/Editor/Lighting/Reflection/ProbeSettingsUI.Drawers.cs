using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using static UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = UnityEditor.Rendering.CoreEditorUtils;

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

            if (!(RenderPipelineManager.currentPipeline is HDRenderPipeline hd))
                return;

            if ((displayedFields.probe & lighting) != 0)
            {
                GUI.enabled = hd.renderPipelineSettings.supportLightLayers;
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingLightLayer, serialized.lightingLightLayer, _.GetContent("Light Layer"), @override.probe, displayedFields.probe, overridableFields.probe);
                GUI.enabled = true;
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingMultiplier, serialized.lightingMultiplier, _.GetContent("Multiplier"), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.lightingWeight, serialized.lightingWeight, _.GetContent("Weight"), @override.probe, displayedFields.probe, overridableFields.probe);
                EditorGUILayout.Space();
            }

            if ((displayedFields.probe & proxy) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyUseInfluenceVolumeAsProxyVolume, serialized.proxyUseInfluenceVolumeAsProxyVolume, _.GetContent("Use Influence Volume As Proxy Volume"), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyCapturePositionProxySpace, serialized.proxyCapturePositionProxySpace, _.GetContent("Capture Position|Capture Position in Proxy Space"), @override.probe, displayedFields.probe, overridableFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                        HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.CapturePosition, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
                    }
                );
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyCaptureRotationProxySpace, serialized.proxyCaptureRotationProxySpace, _.GetContent("Capture Rotation|Capture Rotation in Proxy Space"), @override.probe, displayedFields.probe, overridableFields.probe);
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyMirrorPositionProxySpace, serialized.proxyMirrorPositionProxySpace, _.GetContent("Mirror Position|Mirror Position in Proxy Space"), @override.probe, displayedFields.probe, overridableFields.probe,
                    (p, l) =>
                    {
                        EditorGUILayout.PropertyField(p, l);
                        HDProbeUI.Drawer_ToolBarButton(HDProbeUI.ToolBar.MirrorPosition, owner, GUILayout.Width(28f), GUILayout.MinHeight(22f));
                    }
                );
                PropertyFieldWithFlagToggleIfDisplayed(ProbeSettingsFields.proxyMirrorRotationProxySpace, serialized.proxyMirrorRotationProxySpace, _.GetContent("Mirror Rotation|Mirror Rotation in Proxy Space"), @override.probe, displayedFields.probe, overridableFields.probe,
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
