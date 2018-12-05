using System;
using System.Reflection;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    partial class HDCameraUI
    {
        enum Expandable
        {
            General = 1 << 0,
            Physical = 1 << 1,
            Output = 1 << 2,
            Orthographic = 1 << 3,
            RenderLoop = 1 << 4,
            XR = 1 << 5
        }

        readonly static ExpandedState<Expandable, Camera> k_ExpandedState = new ExpandedState<Expandable, Camera>(Expandable.General, "HDRP");

        static HDCameraUI()
        {
            Inspector = new[]
            {
                CED.space,
                SectionGeneralSettings,
                SectionFrameSettings,
                // Not used for now
                //SectionPhysicalSettings,
                SectionOutputSettings,
                SectionXRSettings
            };
        }

        public static readonly CED.IDrawer[] Inspector = null;

        public static readonly CED.IDrawer SectionGeneralSettings = CED.FoldoutGroup(
            generalSettingsHeaderContent,
            Expandable.General,
            k_ExpandedState,
            FoldoutOption.Indent | FoldoutOption.NoSpaceAtEnd, //no space as FrameSettings is drawn just under
            CED.Group(
                Drawer_FieldClear,
                Drawer_FieldCullingMask,
                Drawer_FieldVolumeLayerMask,
                Drawer_FieldVolumeAnchorOverride,
                Drawer_FieldOcclusionCulling
                ),
            CED.space,
            CED.Group(
                Drawer_Projection,
                Drawer_FieldClippingPlanes
                ),
            CED.space,
            CED.Group(
                Drawer_CameraWarnings,
                Drawer_FieldRenderingPath
                )
            );

        public static readonly CED.IDrawer SectionPhysicalSettings = CED.FoldoutGroup(
            physicalSettingsHeaderContent,
            Expandable.Physical,
            k_ExpandedState,
            CED.Group(
                Drawer_FieldAperture,
                Drawer_FieldShutterSpeed,
                Drawer_FieldIso
                )
            );

        public static readonly CED.IDrawer SectionOutputSettings = CED.FoldoutGroup(
            outputSettingsHeaderContent,
            Expandable.Output,
            k_ExpandedState,
            CED.Group(
#if ENABLE_MULTIPLE_DISPLAYS
                Drawer_SectionMultiDisplay,
#endif
                Drawer_FieldRenderTarget,
                Drawer_FieldDepth,
                Drawer_FieldNormalizedViewPort
                )
            );

        public static readonly CED.IDrawer SectionXRSettings = CED.Conditional(
            (serialized, owner) => PlayerSettings.virtualRealitySupported,
            CED.FoldoutGroup(
                xrSettingsHeaderContent,
                Expandable.XR,
                k_ExpandedState,
                CED.Group(
                    Drawer_FieldVR,
                    Drawer_FieldTargetEye
                    )
                )
            );

        public static readonly CED.IDrawer SectionFrameSettings = CED.Conditional(
            (serialized, owner) => k_ExpandedState[Expandable.General],
            CED.Group((serialized, owner) =>
            { 
                if ((HDAdditionalCameraData.RenderingPath)serialized.renderingPath.intValue == HDAdditionalCameraData.RenderingPath.Custom)
                    FrameSettingsUI.Inspector().Draw(serialized.frameSettings, owner);
                else
                    EditorGUILayout.Space();
            })
        );

        static void Drawer_FieldBackgroundColorHDR(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.backgroundColorHDR, backgroundColorContent);
        }

        static void Drawer_FieldVolumeLayerMask(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeLayerMask, volumeLayerMaskContent);
        }
        static void Drawer_FieldVolumeAnchorOverride(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeAnchorOverride, volumeAnchorOverrideContent);
        }

        static void Drawer_FieldCullingMask(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.cullingMask, cullingMaskContent);
        }

        static void Drawer_Projection(SerializedHDCamera p, Editor owner)
        {
            p.baseCameraSettings.DrawProjection();
        }

        static void Drawer_FieldClippingPlanes(SerializedHDCamera p, Editor owner)
        {
            CoreEditorUtils.DrawMultipleFields(
                clippingPlaneMultiFieldTitle,
                new[] { p.baseCameraSettings.nearClippingPlane, p.baseCameraSettings.farClippingPlane },
                new[] { nearPlaneContent, farPlaneContent });
        }

        static void Drawer_FieldAperture(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.aperture, apertureContent);
        }

        static void Drawer_FieldShutterSpeed(SerializedHDCamera p, Editor owner)
        {
            float shutterSpeed = 1f / p.shutterSpeed.floatValue;
            EditorGUI.BeginChangeCheck();
            shutterSpeed = EditorGUILayout.FloatField(shutterSpeedContent, shutterSpeed);
            if (EditorGUI.EndChangeCheck())
            {
                p.shutterSpeed.floatValue = 1f / shutterSpeed;
                p.Apply();
            }
        }

        static void Drawer_FieldIso(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.iso, isoContent);
        }

        static void Drawer_FieldNormalizedViewPort(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.normalizedViewPortRect, viewportContent);
        }

        static void Drawer_FieldDepth(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.depth, depthContent);
        }

        static void Drawer_FieldClear(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.clearColorMode, clearModeContent);
            //if(p.clearColorMode.enumValueIndex == (int)HDAdditionalCameraData.ClearColorMode.BackgroundColor) or no sky in scene
            EditorGUILayout.PropertyField(p.backgroundColorHDR, backgroundColorContent);
            EditorGUILayout.PropertyField(p.clearDepth, clearDepthContent);
        }

        static void Drawer_FieldRenderingPath(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderingPath, renderingPathContent);
        }

        static void Drawer_FieldRenderTarget(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.targetTexture);

            // show warning if we have deferred but manual MSAA set
            // only do this if the m_TargetTexture has the same values across all target cameras
            if (!p.baseCameraSettings.targetTexture.hasMultipleDifferentValues)
            {
                var targetTexture = p.baseCameraSettings.targetTexture.objectReferenceValue as RenderTexture;
                if (targetTexture
                    && targetTexture.antiAliasing > 1
                    && p.frameSettings.litShaderMode.enumValueIndex == (int)LitShaderMode.Deferred)
                {
                    EditorGUILayout.HelpBox(msaaWarningMessage, MessageType.Warning, true);
                }
            }
        }

        static void Drawer_FieldOcclusionCulling(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.occlusionCulling, occlusionCullingContent);
        }

        static void Drawer_CameraWarnings(SerializedHDCamera p, Editor owner)
        {
            foreach (Camera camera in p.serializedObject.targetObjects)
            {
                var warnings = GetCameraBufferWarnings(camera);
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Warning, true);
            }
        }

        static void Drawer_FieldVR(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.baseCameraSettings.stereoSeparation, stereoSeparationContent);
            EditorGUILayout.PropertyField(p.baseCameraSettings.stereoConvergence, stereoConvergenceContent);
        }

#if ENABLE_MULTIPLE_DISPLAYS
        static void Drawer_SectionMultiDisplay(SerializedHDCamera p, Editor owner)
        {
            if (ModuleManager_ShouldShowMultiDisplayOption())
            {
                var prevDisplay = p.baseCameraSettings.targetDisplay.intValue;
                EditorGUILayout.IntPopup(p.baseCameraSettings.targetDisplay, DisplayUtility_GetDisplayNames(), DisplayUtility_GetDisplayIndices(), targetDisplayContent);
                if (prevDisplay != p.baseCameraSettings.targetDisplay.intValue)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

#endif

        static readonly int[] k_TargetEyeValues = { (int)StereoTargetEyeMask.Both, (int)StereoTargetEyeMask.Left, (int)StereoTargetEyeMask.Right, (int)StereoTargetEyeMask.None };

        static void Drawer_FieldTargetEye(SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.IntPopup(p.baseCameraSettings.targetEye, k_TargetEyes, k_TargetEyeValues, targetEyeContent);
        }

        static MethodInfo k_DisplayUtility_GetDisplayIndices = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
            .GetMethod("GetDisplayIndices");
        static int[] DisplayUtility_GetDisplayIndices()
        {
            return (int[])k_DisplayUtility_GetDisplayIndices.Invoke(null, null);
        }

        static MethodInfo k_DisplayUtility_GetDisplayNames = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
            .GetMethod("GetDisplayNames");
        static GUIContent[] DisplayUtility_GetDisplayNames()
        {
            return (GUIContent[])k_DisplayUtility_GetDisplayNames.Invoke(null, null);
        }

        static MethodInfo k_ModuleManager_ShouldShowMultiDisplayOption = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor")
            .GetMethod("ShouldShowMultiDisplayOption", BindingFlags.Static | BindingFlags.NonPublic);
        static bool ModuleManager_ShouldShowMultiDisplayOption()
        {
            return (bool)k_ModuleManager_ShouldShowMultiDisplayOption.Invoke(null, null);
        }

        static readonly MethodInfo k_Camera_GetCameraBufferWarnings = typeof(Camera).GetMethod("GetCameraBufferWarnings", BindingFlags.Instance | BindingFlags.NonPublic);
        static string[] GetCameraBufferWarnings(Camera camera)
        {
            return (string[])k_Camera_GetCameraBufferWarnings.Invoke(camera, null);
        }
    }
}
