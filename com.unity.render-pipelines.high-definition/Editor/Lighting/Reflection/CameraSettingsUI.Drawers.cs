using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

using static UnityEditor.Experimental.Rendering.HDPipeline.HDEditorUtils;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = UnityEditor.Rendering.CoreEditorUtils;

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
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearColorMode, serialized.bufferClearColorMode, _.GetContent("Clear Mode"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearBackgroundColorHDR, serialized.bufferClearBackgroundColorHDR, _.GetContent("Background Color"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.bufferClearClearDepth, serialized.bufferClearClearDepth, _.GetContent("Clear Depth"), @override.camera, displayedFields.camera, overridableFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & volumesFields) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.volumesLayerMask, serialized.volumesLayerMask, _.GetContent("Volume Layer Mask"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.volumesAnchorOverride, serialized.volumesAnchorOverride, _.GetContent("Volume Anchor Override"), @override.camera, displayedFields.camera, overridableFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & cullingFields) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingUseOcclusionCulling, serialized.cullingUseOcclusionCulling, _.GetContent("Use Occlusion Culling"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingCullingMask, serialized.cullingCullingMask, _.GetContent("Culling Mask"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.cullingInvertFaceCulling, serialized.cullingCullingMask, _.GetContent("Invert Backface Culling"), @override.camera, displayedFields.camera, overridableFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & frustumFields) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumAspect, serialized.frustumAspect, _.GetContent("Aspect"), @override.camera, displayedFields.camera, overridableFields.camera);
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumFieldOfView, serialized.frustumFieldOfView, _.GetContent("Field Of View"), @override.camera, displayedFields.camera, overridableFields.camera);
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
                    _.DrawMultipleFields(
                        "Clip Planes",
                        new[] { serialized.frustumNearClipPlane, serialized.frustumFarClipPlane },
                        new[] { _.GetContent("Near"), _.GetContent("Far") });
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumFarClipPlane, serialized.frustumFarClipPlane, _.GetContent("Far Clip Plane"), @override.camera, displayedFields.camera, overridableFields.camera);
                    PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.frustumNearClipPlane, serialized.frustumNearClipPlane, _.GetContent("Near Clip Plane"), @override.camera, displayedFields.camera, overridableFields.camera);
                }
                EditorGUILayout.Space();
            }

            PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.flipYMode, serialized.flipYMode, _.GetContent("Flip Y"), @override.camera, displayedFields.camera, overridableFields.camera);
            PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.renderingPath, serialized.renderingPath, _.GetContent("Rendering Path"), @override.camera, displayedFields.camera, overridableFields.camera);

            if ((displayedFields.camera & CameraSettingsFields.frameSettings) != 0)
            {
                var renderingPath = (HDAdditionalCameraData.RenderingPath)serialized.renderingPath.intValue;
                if (renderingPath != HDAdditionalCameraData.RenderingPath.UseGraphicsSettings)
                {
                    // TODO: place it in static cache
                    var drawer = FrameSettingsUI.Inspector(true);
                    drawer.Draw(serialized.frameSettings, owner);
                }
            }
        }
    }
}
