using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

using static UnityEditor.Rendering.HighDefinition.HDEditorUtils;

namespace UnityEditor.Rendering.HighDefinition
{
    internal partial class CameraSettingsUI
    {
        public static void Draw(
            SerializedCameraSettings serialized, Editor owner,
            CameraSettingsOverride displayedFields
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
            const CameraSettingsFields antiAliasing = CameraSettingsFields.antiAliasingMode
                | CameraSettingsFields.antiAliasingSMAAQuality
                | CameraSettingsFields.antiAliasingTAASharpenStrength;
            const CameraSettingsFields postProcesses = CameraSettingsFields.postProcessesDithering;

            if ((displayedFields.camera & bufferFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.bufferClearColorMode, serialized.bufferClearColorMode, EditorGUIUtility.TrTextContent("Clear Mode"), displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.bufferClearBackgroundColorHDR, serialized.bufferClearBackgroundColorHDR, EditorGUIUtility.TrTextContent("Background Color"), displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.bufferClearClearDepth, serialized.bufferClearClearDepth, EditorGUIUtility.TrTextContent("Clear Depth"), displayedFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & volumesFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.volumesLayerMask, serialized.volumesLayerMask, EditorGUIUtility.TrTextContent("Volume Layer Mask"), displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.volumesAnchorOverride, serialized.volumesAnchorOverride, EditorGUIUtility.TrTextContent("Volume Anchor Override"), displayedFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & cullingFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.cullingUseOcclusionCulling, serialized.cullingUseOcclusionCulling, EditorGUIUtility.TrTextContent("Use Occlusion Culling"), displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.cullingCullingMask, serialized.cullingCullingMask, EditorGUIUtility.TrTextContent("Culling Mask"), displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.cullingInvertFaceCulling, serialized.cullingCullingMask, EditorGUIUtility.TrTextContent("Invert Backface Culling"), displayedFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & frustumFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.frustumAspect, serialized.frustumAspect, EditorGUIUtility.TrTextContent("Aspect"), displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.frustumFieldOfView, serialized.frustumFieldOfView, EditorGUIUtility.TrTextContent("Field Of View"), displayedFields.camera);
                var areBothDisplayed = displayedFields.camera.HasFlag(frustumFarOrNearPlane);
                if (areBothDisplayed)
                {
                    CoreEditorUtils.DrawMultipleFields(
                        "Clipping Planes",
                        new[] { serialized.frustumNearClipPlane, serialized.frustumFarClipPlane },
                        new[] { EditorGUIUtility.TrTextContent("Near"), EditorGUIUtility.TrTextContent("Far") });
                }
                else
                {
                    PropertyFieldWithoutToggle(CameraSettingsFields.frustumFarClipPlane, serialized.frustumFarClipPlane, EditorGUIUtility.TrTextContent("Far Clip Plane"), displayedFields.camera);
                    PropertyFieldWithoutToggle(CameraSettingsFields.frustumNearClipPlane, serialized.frustumNearClipPlane, EditorGUIUtility.TrTextContent("Near Clip Plane"), displayedFields.camera);
                }
                EditorGUILayout.Space();
            }

            PropertyFieldWithoutToggle(CameraSettingsFields.flipYMode, serialized.flipYMode, EditorGUIUtility.TrTextContent("Flip Y"), @override.camera, displayedFields.camera, overridableFields.camera);
            PropertyFieldWithoutToggle(CameraSettingsFields.probeLayerMask, serialized.probeLayerMask, EditorGUIUtility.TrTextContent("Probe Layer Mask"), @override.camera, displayedFields.camera, overridableFields.camera);

            if ((displayedFields.camera & antiAliasing) != 0)
            {
                PropertyFieldWithFlagToggleIfDisplayed(CameraSettingsFields.antiAliasingMode, serialized.antiAliasingMode, EditorGUIUtility.TrTextContent("Anti Aliasing"), @override.camera, displayedFields.camera, overridableFields.camera);
                switch ((HDAdditionalCameraData.AntialiasingMode)serialized.antiAliasingMode.intValue)
                {
                    case HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing:
                        PropertyFieldWithoutToggle(CameraSettingsFields.antiAliasingTAASharpenStrength, serialized.antiAliasingTAASharpenStrength, EditorGUIUtility.TrTextContent("Sharpen Strength"), @override.camera, displayedFields.camera, overridableFields.camera, indent: 1);
                        break;
                    case HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing:
                        PropertyFieldWithoutToggle(CameraSettingsFields.antiAliasingSMAAQuality, serialized.antiAliasingSMAAQuality, EditorGUIUtility.TrTextContent("Quality Preset"), @override.camera, displayedFields.camera, overridableFields.camera, indent: 1);
                        break;
                }
            }

            if ((displayedFields.camera & postProcesses) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.postProcessesDithering, serialized.postProcessesDithering, EditorGUIUtility.TrTextContent("Dithering"), @override.camera, displayedFields.camera, overridableFields.camera);
            }

            PropertyFieldWithoutToggle(CameraSettingsFields.customRenderingSettings, serialized.customRenderingSettings, EditorGUIUtility.TrTextContent("Custom Frame Settings"), @override.camera, displayedFields.camera, overridableFields.camera);

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
