using System;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDCameraEditor.UIState, SerializedHDCamera>;

    partial class HDCameraEditor
    {
        static readonly CED.IDrawer[] k_PrimarySection =
        {
            CED.Action(Drawer_FieldBackgroundColor),
            CED.Action(Drawer_FieldCullingMask),
            CED.Action(Drawer_FieldVolumeLayerMask),
            CED.space,
            CED.Action(Drawer_Projection),
            CED.Action(Drawer_FieldClippingPlanes),
            CED.Action(Drawer_FieldNormalizedViewPort),
            CED.space,
            CED.Action(Drawer_FieldDepth),
            CED.Action(Drawer_DeferredOrthographicWarning),
            CED.Action(Drawer_FieldRenderTarget),
            CED.Action(Drawer_FieldOcclusionCulling),
            CED.Action(Drawer_CameraWarnings),
            CED.Action(Drawer_FieldVR),
#if ENABLE_MULTIPLE_DISPLAYS
            CED.Action(Drawer_SectionMultiDisplay),
#endif
            CED.Action(Drawer_FieldRenderingPath),
            CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedTargetEyeOptions.faded,
                true,
                CED.Action(Drawer_FieldTargetEye)),
            CED.FadeGroup(
                (s, d, o, i) => s.isSectionExpandedRenderLoopSettings.faded,
                false,
                CED.FoldoutGroup(
                    "Shader Features",
                    (s, p, o) => s.isSectionExpandedShaderFeature,
                    true,
                    CED.Action(Drawer_SectionShaderFeature)),
                CED.FoldoutGroup(
                    "Screen Space",
                    (s, p, o) => s.isSectionExpandedScreenSpace,
                    true,
                    CED.Action(Drawer_SectionScreenSpace)),
                CED.FoldoutGroup(
                    "Miscellaneous",
                    (s, p, o) => s.isSectionExpandedMiscellaneous,
                    true,
                    CED.Action(Drawer_SectionMiscellaneous)),
                CED.FoldoutGroup(
                    "Light Loop",
                    (s, p, o) => s.isSectionExpandedLightLoop,
                    true,
                    CED.Action(Drawer_SectionLightLoop)))
        };

        static void Drawer_FieldBackgroundColor(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.backgroundColor, _.GetContent("Background Color|The Camera clears the screen to this color before rendering."));
        }

        static void Drawer_FieldVolumeLayerMask(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeLayerMask, _.GetContent("Volume Layer Mask"));
        }

        static void Drawer_FieldCullingMask(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.cullingMask, _.GetContent("Culling Mask"));
        }

        static void Drawer_Projection(UIState s, SerializedHDCamera p, Editor owner)
        {
            ProjectionType projectionType = p.orthographic.boolValue ? ProjectionType.Orthographic : ProjectionType.Perspective;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = p.orthographic.hasMultipleDifferentValues;
            projectionType = (ProjectionType)EditorGUILayout.EnumPopup(_.GetContent("Projection|How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective."), projectionType);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                p.orthographic.boolValue = (projectionType == ProjectionType.Orthographic);

            if (!p.orthographic.hasMultipleDifferentValues)
            {
                if (projectionType == ProjectionType.Orthographic)
                    EditorGUILayout.PropertyField(p.orthographicSize, _.GetContent("Size"));
                else
                    EditorGUILayout.Slider(p.fieldOfView, 1f, 179f, _.GetContent("Field of View|The width of the Camera’s view angle, measured in degrees along the local Y axis."));
            }
        }

        static void Drawer_FieldClippingPlanes(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.nearClippingPlane, _.GetContent("Near|The closest point relative to the camera that drawing will occur."));
            EditorGUILayout.PropertyField(p.farClippingPlane, _.GetContent("Far|The furthest point relative to the camera that drawing will occur.\n"));
        }

        static void Drawer_FieldNormalizedViewPort(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.normalizedViewPortRect, _.GetContent("Viewport Rect|Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0–1)."));
        }

        static void Drawer_FieldDepth(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.depth, _.GetContent("Depth"));
        }

        static void Drawer_FieldRenderingPath(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderingPath, _.GetContent("Rendering Path"));
        }

        static void Drawer_DeferredOrthographicWarning(UIState s, SerializedHDCamera p, Editor owner)
        {
            if (p.orthographic.boolValue && !p.frameSettings.enableForwardRenderingOnly.boolValue)
                EditorGUILayout.HelpBox("Deferred rendering does not work with Orthographic camera, will use Forward.",
                    MessageType.Warning, true);
        }

        static void Drawer_FieldRenderTarget(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.targetTexture);

            // show warning if we have deferred but manual MSAA set
            // only do this if the m_TargetTexture has the same values across all target cameras
            if (!p.targetTexture.hasMultipleDifferentValues)
            {
                var targetTexture = p.targetTexture.objectReferenceValue as RenderTexture;
                if (targetTexture
                    && targetTexture.antiAliasing > 1
                    && !p.frameSettings.enableForwardRenderingOnly.boolValue)
                {
                    EditorGUILayout.HelpBox("Manual MSAA target set with deferred rendering. This will lead to undefined behavior.", MessageType.Warning, true);
                }
            }
        }

        static void Drawer_FieldOcclusionCulling(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.occlusionCulling, _.GetContent("Occlusion Culling"));
        }

        static void Drawer_CameraWarnings(UIState s, SerializedHDCamera p, Editor owner)
        {
            foreach (Camera camera in p.serializedObject.targetObjects)
            {
                var warnings = GetCameraBufferWarnings(camera);
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Warning, true);
            }
        }

        static void Drawer_FieldVR(UIState s, SerializedHDCamera p, Editor owner)
        {
            if (PlayerSettings.virtualRealitySupported)
            {
                EditorGUILayout.PropertyField(p.frameSettings.enableStereo, _.GetContent("Enable Stereo"));
                EditorGUILayout.PropertyField(p.stereoSeparation, _.GetContent("Stereo Separation"));
                EditorGUILayout.PropertyField(p.stereoConvergence, _.GetContent("Stereo Convergence"));
            }
        }

#if ENABLE_MULTIPLE_DISPLAYS
        static void Drawer_SectionMultiDisplay(UIState s, SerializedHDCamera p, Editor owner)
        {
            if (ModuleManager_ShouldShowMultiDisplayOption())
            {
                var prevDisplay = p.targetDisplay.intValue;
                EditorGUILayout.Space();
                EditorGUILayout.IntPopup(p.targetDisplay, DisplayUtility_GetDisplayNames(), DisplayUtility_GetDisplayIndices(), _.GetContent("Target Display"));
                if (prevDisplay != p.targetDisplay.intValue)
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }
#endif

        static readonly int[] k_TargetEyeValues = { (int)StereoTargetEyeMask.Both, (int)StereoTargetEyeMask.Left, (int)StereoTargetEyeMask.Right, (int)StereoTargetEyeMask.None };
        static readonly GUIContent[] k_TargetEyes =
        {
            new GUIContent("Both"),
            new GUIContent("Left"),
            new GUIContent("Right"),
            new GUIContent("None (Main Display)"),
        };
        static void Drawer_FieldTargetEye(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.IntPopup(p.targetEye, k_TargetEyes, k_TargetEyeValues, _.GetContent("Target Eye"));
        }

        static void Drawer_SectionShaderFeature(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.frameSettings.enableTransparentPrepass, _.GetContent("Enable Transparent Prepass"));
            EditorGUILayout.PropertyField(p.frameSettings.enableTransparentPostpass, _.GetContent("Enable Transparent Postpass"));
            EditorGUILayout.PropertyField(p.frameSettings.enableMotionVectors, _.GetContent("Enable Motion Vectors"));
            EditorGUILayout.PropertyField(p.frameSettings.enableObjectMotionVectors, _.GetContent("Enable Object Motion Vectors"));
            EditorGUILayout.PropertyField(p.frameSettings.enableDBuffer, _.GetContent("Enable DBuffer"));
            EditorGUILayout.PropertyField(p.frameSettings.enableAtmosphericScattering, _.GetContent("Enable Atmospheric Scattering"));
            EditorGUILayout.PropertyField(p.frameSettings.enableRoughRefraction, _.GetContent("Enable Rough Refraction"));
            EditorGUILayout.PropertyField(p.frameSettings.enableDistortion, _.GetContent("Enable Distortion"));
            EditorGUILayout.PropertyField(p.frameSettings.enablePostprocess, _.GetContent("Enable Postprocess"));
        }

        static void Drawer_SectionScreenSpace(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.frameSettings.enableSSR, _.GetContent("Enable SSR"));
            EditorGUILayout.PropertyField(p.frameSettings.enableSSAO, _.GetContent("Enable SSAO"));
            EditorGUILayout.PropertyField(p.frameSettings.enableSSSAndTransmission, _.GetContent("Enable SSS And Transmission"));
        }

        static void Drawer_SectionMiscellaneous(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.frameSettings.enableShadow, _.GetContent("Enable Shadow"));
            EditorGUILayout.PropertyField(p.frameSettings.enableShadowMask, _.GetContent("Enable Shadow Masks"));

            EditorGUILayout.PropertyField(p.frameSettings.enableForwardRenderingOnly, _.GetContent("Enable Forward Rendering Only"));
            EditorGUILayout.PropertyField(p.frameSettings.enableDepthPrepassWithDeferredRendering, _.GetContent("Enable Depth Prepass With Deferred Rendering"));
            EditorGUILayout.PropertyField(p.frameSettings.enableAlphaTestOnlyInDeferredPrepass, _.GetContent("Enable Alpha Test Only In Deferred Prepass"));

            EditorGUILayout.PropertyField(p.frameSettings.enableAsyncCompute, _.GetContent("Enable Async Compute"));

            EditorGUILayout.PropertyField(p.frameSettings.enableOpaqueObjects, _.GetContent("Enable Opaque Objects"));
            EditorGUILayout.PropertyField(p.frameSettings.enableTransparentObjects, _.GetContent("Enable Transparent Objects"));

            EditorGUILayout.PropertyField(p.frameSettings.enableMSAA, _.GetContent("Enable MSAA"));
        }

        static void Drawer_SectionLightLoop(UIState s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.frameSettings.lightLoopSettings.enableTileAndCluster, _.GetContent("Enable Tile And Cluster"));
            EditorGUILayout.PropertyField(p.frameSettings.lightLoopSettings.enableComputeLightEvaluation, _.GetContent("Enable Compute Light Evaluation"));
            EditorGUILayout.PropertyField(p.frameSettings.lightLoopSettings.enableComputeLightVariants, _.GetContent("Enable Compute Light Variants"));
            EditorGUILayout.PropertyField(p.frameSettings.lightLoopSettings.enableComputeMaterialVariants, _.GetContent("Enable Compute Material Variants"));

            EditorGUILayout.PropertyField(p.frameSettings.lightLoopSettings.isFptlEnabled, _.GetContent("Enable FPTL"));
            EditorGUILayout.PropertyField(p.frameSettings.lightLoopSettings.enableFptlForForwardOpaque, _.GetContent("Enable FPTL For Forward Opaque"));

            EditorGUILayout.PropertyField(p.frameSettings.lightLoopSettings.enableBigTilePrepass, _.GetContent("Enable Big Tile Prepass"));
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
