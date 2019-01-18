using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

using static UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal partial class CameraSettingsUI
    {
        public static void Draw(
            SerializedCameraSettings serialized, Editor owner,
            SerializedCameraSettingsOverride @override,
            CameraSettingsOverride displayedFields, CameraSettingsOverride overridableFields
        )
        {
            const CameraSettingsFields bufferFields = CameraSettingsFields.bufferClearBackgroundColorHDR
                | CameraSettingsFields.bufferClearClearDepth
                | CameraSettingsFields.bufferClearColorMode;
            const CameraSettingsFields volumesFields = CameraSettingsFields.volumesAnchorOverride
                | CameraSettingsFields.volumesLayerMask;
            const CameraSettingsFields cullingFields = CameraSettingsFields.cullingCullingMask
                | CameraSettingsFields.cullingInvertFaceCulling
                | CameraSettingsFields.cullingUseOcclusionCulling;
            const CameraSettingsFields frustumFields = CameraSettingsFields.frustumAspect
                | CameraSettingsFields.frustumFarClipPlane
                | CameraSettingsFields.frustumMode
                | CameraSettingsFields.frustumNearClipPlane
                | CameraSettingsFields.frustumProjectionMatrix
                | CameraSettingsFields.frustumFieldOfView;
            const CameraSettingsFields frustumFarOrNearPlane = CameraSettingsFields.frustumFarClipPlane
                | CameraSettingsFields.frustumNearClipPlane;

            if ((displayedFields.camera & bufferFields) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearColorMode, serialized.bufferClearColorMode, EditorGUIUtility.TrTextContent("Clear Mode"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearBackgroundColorHDR, serialized.bufferClearBackgroundColorHDR, EditorGUIUtility.TrTextContent("Background Color"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearClearDepth, serialized.bufferClearClearDepth, EditorGUIUtility.TrTextContent("Clear Depth"), @override.camera, displayedFields.camera, overridableFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & volumesFields) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.volumesLayerMask, serialized.volumesLayerMask, EditorGUIUtility.TrTextContent("Volume Layer Mask"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.volumesAnchorOverride, serialized.volumesAnchorOverride, EditorGUIUtility.TrTextContent("Volume Anchor Override"), @override.camera, displayedFields.camera, overridableFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & cullingFields) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingUseOcclusionCulling, serialized.cullingUseOcclusionCulling, EditorGUIUtility.TrTextContent("Use Occlusion Culling"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingCullingMask, serialized.cullingCullingMask, EditorGUIUtility.TrTextContent("Culling Mask"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingInvertFaceCulling, serialized.cullingCullingMask, EditorGUIUtility.TrTextContent("Invert Backface Culling"), @override.camera, displayedFields.camera, overridableFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & frustumFields) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumAspect, serialized.frustumAspect, EditorGUIUtility.TrTextContent("Aspect"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumFieldOfView, serialized.frustumFieldOfView, EditorGUIUtility.TrTextContent("Field Of View"), @override.camera, displayedFields.camera, overridableFields.camera);
                var areBothOverrideable = overridableFields.camera.HasFlag(frustumFarOrNearPlane);
                var areBothNotOverrideable = (overridableFields.camera & frustumFarOrNearPlane) == 0;
                var areBothDisplayed = displayedFields.camera.HasFlag(frustumFarOrNearPlane);
                if (areBothDisplayed && (areBothOverrideable || areBothNotOverrideable))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (areBothOverrideable)
                        GUI.enabled = FlagToggle(frustumFarOrNearPlane, @override.camera);
                    else
                        ReserveAndGetFlagToggleRect();
                    UnityEditor.Rendering.CoreEditorUtils.DrawMultipleFields(
                        "Clip Planes",
                        new[] { serialized.frustumNearClipPlane, serialized.frustumFarClipPlane },
                        new[] { EditorGUIUtility.TrTextContent("Near"), EditorGUIUtility.TrTextContent("Far") });
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumFarClipPlane, serialized.frustumFarClipPlane, EditorGUIUtility.TrTextContent("Far Clip Plane"), @override.camera, displayedFields.camera, overridableFields.camera);
                    PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumNearClipPlane, serialized.frustumNearClipPlane, EditorGUIUtility.TrTextContent("Near Clip Plane"), @override.camera, displayedFields.camera, overridableFields.camera);
                }
                EditorGUILayout.Space();
            }

            PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.flipYMode, serialized.flipYMode, EditorGUIUtility.TrTextContent("Flip Y"), @override.camera, displayedFields.camera, overridableFields.camera);
            PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.probeLayerMask, serialized.probeLayerMask, EditorGUIUtility.TrTextContent("Probe Layer Mask"), @override.camera, displayedFields.camera, overridableFields.camera);
            PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.customRenderingSettings, serialized.customRenderingSettings, EditorGUIUtility.TrTextContent("Custom Frame Settings"), @override.camera, displayedFields.camera, overridableFields.camera);

            if ((displayedFields.camera & CameraSettingsFields.frameSettings) != 0)
            {
                //Warning, fullscreenPassThrough have been removed from RenderingPath enum
                //and replaced with a toggle on the camera. If this script aim to be used
                //on camera too, add it here.
                
                if (serialized.customRenderingSettings.boolValue)
                {
                    --EditorGUI.indentLevel; //fix alignment issue for Planar Reflection and Reflection probe's FrameSettings
                    // TODO: place it in static cache
                    var drawer = FrameSettingsUI.Inspector(true);
                    drawer.Draw(serialized.frameSettings, owner);
                    ++EditorGUI.indentLevel;
                }
            }
        }
    }
}
