using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

using static UnityEditor.Rendering.HighDefinition.HDEditorUtils;

namespace UnityEditor.Rendering.HighDefinition
{
    //Currently for Reflection Probes
    class CameraSettingsUI
    {
        static class Styles
        {
            public static readonly GUIContent bufferClearColorMode = L10n.TextContent("Clear Mode", null, null, null);
            public static readonly GUIContent bufferClearBackgroundColorHDR = L10n.TextContent("Background Color", null, null, null);
            public static readonly GUIContent bufferClearClearDepth = L10n.TextContent("Clear Depth", null, null, null);
            public static readonly GUIContent volumesLayerMask = L10n.TextContent("Volume Layer Mask", null, null, null);
            public static readonly GUIContent volumesAnchorOverride = L10n.TextContent("Volume Anchor Override", null, null, null);
            public static readonly GUIContent cullingUseOcclusionCulling = L10n.TextContent("Occlusion Culling", null, null, null);
            public static readonly GUIContent cullingCullingMask = L10n.TextContent("Culling Mask", null, null, null);
            public static readonly GUIContent cullingInvertFaceCulling = L10n.TextContent("Invert Backface Culling", null, null, null);
            public static readonly GUIContent frustumAspect = L10n.TextContent("Aspect", null, null, null);
            public static readonly GUIContent frustumFieldOfView = L10n.TextContent("Field Of View", null, null, null);
            public static readonly GUIContent clippingPlanesLabel = L10n.TextContent("Clipping Planes", null, null, null);
            public static readonly GUIContent[] clippingPlanes = new[]
            {
                L10n.TextContent("Near", null, null, null),
                L10n.TextContent("Far", null, null, null),
            };
            public static readonly GUIContent frustumFarClipPlane = L10n.TextContent("Far Clip Plane", null, null, null); //alone version
            public static readonly GUIContent frustumNearClipPlane = L10n.TextContent("Near Clip Plane", null, null, null); //alone version
            public static readonly GUIContent flipYMode = L10n.TextContent("Flip Y", null, null, null);
            public static readonly GUIContent probeLayerMask = L10n.TextContent("Probe Layer Mask", null, null, null);
            public static readonly GUIContent customRenderingSettings = L10n.TextContent("Custom Frame Settings", null, null, null);
        }

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

            if ((displayedFields.camera & bufferFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.bufferClearColorMode, serialized.bufferClearColorMode, Styles.bufferClearColorMode, displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.bufferClearBackgroundColorHDR, serialized.bufferClearBackgroundColorHDR, Styles.bufferClearBackgroundColorHDR, displayedFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & volumesFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.volumesLayerMask, serialized.volumesLayerMask, Styles.volumesLayerMask, displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.volumesAnchorOverride, serialized.volumesAnchorOverride, Styles.volumesAnchorOverride, displayedFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & cullingFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.cullingUseOcclusionCulling, serialized.cullingUseOcclusionCulling, Styles.cullingUseOcclusionCulling, displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.cullingCullingMask, serialized.cullingCullingMask, Styles.cullingCullingMask, displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.cullingInvertFaceCulling, serialized.cullingInvertFaceCulling, Styles.cullingInvertFaceCulling, displayedFields.camera);
                EditorGUILayout.Space();
            }

            if ((displayedFields.camera & frustumFields) != 0)
            {
                PropertyFieldWithoutToggle(CameraSettingsFields.frustumAspect, serialized.frustumAspect, Styles.frustumAspect, displayedFields.camera);
                PropertyFieldWithoutToggle(CameraSettingsFields.frustumFieldOfView, serialized.frustumFieldOfView, Styles.frustumFieldOfView, displayedFields.camera);
                var areBothDisplayed = displayedFields.camera.HasFlag(frustumFarOrNearPlane);
                if (areBothDisplayed)
                {
                    CoreEditorUtils.DrawMultipleFields(
                        Styles.clippingPlanesLabel,
                        new[] { serialized.frustumNearClipPlane, serialized.frustumFarClipPlane },
                        Styles.clippingPlanes);
                }
                else
                {
                    PropertyFieldWithoutToggle(CameraSettingsFields.frustumFarClipPlane, serialized.frustumFarClipPlane, Styles.frustumFarClipPlane, displayedFields.camera);
                    PropertyFieldWithoutToggle(CameraSettingsFields.frustumNearClipPlane, serialized.frustumNearClipPlane, Styles.frustumNearClipPlane, displayedFields.camera);
                }

                // Enforce valid value range
                serialized.frustumNearClipPlane.floatValue = Mathf.Max(CameraSettings.Frustum.MinNearClipPlane, serialized.frustumNearClipPlane.floatValue);
                serialized.frustumFarClipPlane.floatValue = Mathf.Max(serialized.frustumNearClipPlane.floatValue + CameraSettings.Frustum.MinFarClipPlane, serialized.frustumFarClipPlane.floatValue);
                EditorGUILayout.Space();
            }

            PropertyFieldWithoutToggle(CameraSettingsFields.flipYMode, serialized.flipYMode, Styles.flipYMode, displayedFields.camera);
            PropertyFieldWithoutToggle(CameraSettingsFields.probeLayerMask, serialized.probeLayerMask, Styles.probeLayerMask, displayedFields.camera);
            PropertyFieldWithoutToggle(CameraSettingsFields.customRenderingSettings, serialized.customRenderingSettings, Styles.customRenderingSettings, displayedFields.camera);

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
