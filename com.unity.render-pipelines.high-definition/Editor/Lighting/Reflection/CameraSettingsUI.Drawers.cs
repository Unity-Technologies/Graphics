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
            public static readonly GUIContent bufferClearColorMode = EditorGUIUtility.TrTextContent("Clear Mode");
            public static readonly GUIContent bufferClearBackgroundColorHDR = EditorGUIUtility.TrTextContent("Background Color");
            public static readonly GUIContent bufferClearClearDepth = EditorGUIUtility.TrTextContent("Clear Depth");
            public static readonly GUIContent volumesLayerMask = EditorGUIUtility.TrTextContent("Volume Layer Mask");
            public static readonly GUIContent volumesAnchorOverride = EditorGUIUtility.TrTextContent("Volume Anchor Override");
            public static readonly GUIContent cullingUseOcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling");
            public static readonly GUIContent cullingCullingMask = EditorGUIUtility.TrTextContent("Culling Mask");
            public static readonly GUIContent cullingInvertFaceCulling = EditorGUIUtility.TrTextContent("Invert Backface Culling");
            public static readonly GUIContent frustumAspect = EditorGUIUtility.TrTextContent("Aspect");
            public static readonly GUIContent frustumFieldOfView = EditorGUIUtility.TrTextContent("Field Of View");
            public static readonly GUIContent clippingPlanesLabel = EditorGUIUtility.TrTextContent("Clipping Planes");
            public static readonly GUIContent[] clippingPlanes = new[]
            {
                EditorGUIUtility.TrTextContent("Near"),
                EditorGUIUtility.TrTextContent("Far"),
            };
            public static readonly GUIContent frustumFarClipPlane = EditorGUIUtility.TrTextContent("Far Clip Plane"); //alone version
            public static readonly GUIContent frustumNearClipPlane = EditorGUIUtility.TrTextContent("Near Clip Plane"); //alone version
            public static readonly GUIContent flipYMode = EditorGUIUtility.TrTextContent("Flip Y");
            public static readonly GUIContent probeLayerMask = EditorGUIUtility.TrTextContent("Probe Layer Mask");
            public static readonly GUIContent customRenderingSettings = EditorGUIUtility.TrTextContent("Custom Frame Settings");
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
