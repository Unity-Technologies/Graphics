using System;
using System.Reflection;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<HDCameraUI, SerializedHDCamera>;

    class HDCameraUI : BaseUI<SerializedHDCamera>
    {
        static HDCameraUI()
        {
            Inspector = new []
            {
                SectionPrimarySettings,
                SectionPhysicalSettings,
                SectionCaptureSettings,
                SectionOutputSettings,
                SectionXRSettings,
                SectionRenderLoopSettings
            };
        }

        public static readonly CED.IDrawer[] Inspector = null;

        public static readonly CED.IDrawer SectionPrimarySettings = CED.Group(
            CED.Action(Drawer_FieldClearColorMode),
            CED.Action(Drawer_FieldBackgroundColorHDR),
            CED.Action(Drawer_FieldClearDepth),
            CED.Action(Drawer_FieldCullingMask),
            CED.Action(Drawer_FieldVolumeLayerMask),
            CED.space,
            CED.Action(Drawer_Projection),
            CED.Action(Drawer_FieldClippingPlanes),
            CED.space,
            CED.Action(Drawer_CameraWarnings),
            CED.Action(Drawer_FieldRenderingPath),
            CED.space
        );

        public static readonly CED.IDrawer SectionPhysicalSettings = CED.FoldoutGroup(
            "Physical Settings",
            (s, p, o) => s.isSectionExpandedPhysicalSettings,
            FoldoutOption.Indent,
            CED.Action(Drawer_FieldAperture),
            CED.Action(Drawer_FieldShutterSpeed),
            CED.Action(Drawer_FieldIso));

        public static readonly CED.IDrawer SectionCaptureSettings = CED.FoldoutGroup(
            "Capture Settings",
            (s, p, o) => s.isSectionExpandedCaptureSettings,
            FoldoutOption.Indent,
            CED.Action(Drawer_FieldOcclusionCulling),
            CED.Action(Drawer_FieldNormalizedViewPort));

        public static readonly CED.IDrawer SectionOutputSettings = CED.FoldoutGroup(
            "Output Settings",
            (s, p, o) => s.isSectionExpandedOutputSettings,
            FoldoutOption.Indent,
#if ENABLE_MULTIPLE_DISPLAYS
            CED.Action(Drawer_SectionMultiDisplay),
#endif
            CED.Action(Drawer_FieldDepth),
            CED.Action(Drawer_FieldRenderTarget));

        public static readonly CED.IDrawer SectionXRSettings = CED.FadeGroup(
            (s, d, o, i) => s.isSectionAvailableXRSettings,
            FadeOption.None,
            CED.FoldoutGroup(
                "XR Settings",
                (s, p, o) => s.isSectionExpandedXRSettings,
                FoldoutOption.Indent,
                CED.Action(Drawer_FieldVR),
                CED.Action(Drawer_FieldTargetEye)));

        public static readonly CED.IDrawer SectionRenderLoopSettings = CED.FadeGroup(
            (s, d, o, i) => s.isSectionAvailableRenderLoopSettings,
            FadeOption.None,
            CED.Select(
                (s, d, o) => s.frameSettingsUI,
                (s, d, o) => d.frameSettings,
                FrameSettingsUI.SectionRenderingPasses,
                FrameSettingsUI.SectionRenderingSettings,
                FrameSettingsUI.SectionLightingSettings),
            CED.Select(
                (s, d, o) => s.frameSettingsUI.lightLoopSettings,
                (s, d, o) => d.frameSettings.lightLoopSettings,
                LightLoopSettingsUI.SectionLightLoopSettings));

        enum ProjectionType { Perspective, Orthographic };

        SerializedHDCamera m_SerializedHdCamera;

        public AnimBool isSectionExpandedOrthoOptions { get { return m_AnimBools[0]; } }
        public AnimBool isSectionExpandedPhysicalSettings { get { return m_AnimBools[1]; } }
        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[2]; } }
        public AnimBool isSectionExpandedOutputSettings { get { return m_AnimBools[3]; } }
        public AnimBool isSectionAvailableRenderLoopSettings { get { return m_AnimBools[4]; } }
        public AnimBool isSectionExpandedXRSettings { get { return m_AnimBools[5]; } }
        public AnimBool isSectionAvailableXRSettings { get { return m_AnimBools[6]; } }

        public bool canOverrideRenderLoopSettings { get; set; }

        public FrameSettingsUI frameSettingsUI = new FrameSettingsUI();

        public HDCameraUI()
            : base(7)
        {
            canOverrideRenderLoopSettings = false;
        }

        public override void Reset(SerializedHDCamera data, UnityAction repaint)
        {
            m_SerializedHdCamera = data;
            frameSettingsUI.Reset(data.frameSettings, repaint);

            for (var i = 0; i < m_AnimBools.Length; ++i)
            {
                m_AnimBools[i].valueChanged.RemoveAllListeners();
                m_AnimBools[i].valueChanged.AddListener(repaint);
            }

            Update();
        }

        public override void Update()
        {
            base.Update();

            var renderingPath = (HDAdditionalCameraData.RenderingPath)m_SerializedHdCamera.renderingPath.intValue;
            canOverrideRenderLoopSettings = renderingPath == HDAdditionalCameraData.RenderingPath.Custom;

            isSectionExpandedOrthoOptions.target = !m_SerializedHdCamera.orthographic.hasMultipleDifferentValues && m_SerializedHdCamera.orthographic.boolValue;
            isSectionAvailableXRSettings.target = PlayerSettings.virtualRealitySupported;
            // SRP settings are available only if the rendering path is not the Default one (configured by the SRP asset)
            isSectionAvailableRenderLoopSettings.target = canOverrideRenderLoopSettings;

            frameSettingsUI.Update();
        }

        static void Drawer_FieldBackgroundColorHDR(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.backgroundColorHDR, _.GetContent("Background Color|The BackgroundColor used to clear the screen when selecting BackgrounColor before rendering."));
        }

        static void Drawer_FieldVolumeLayerMask(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.volumeLayerMask, _.GetContent("Volume Layer Mask"));
        }

        static void Drawer_FieldCullingMask(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.cullingMask, _.GetContent("Culling Mask"));
        }

        static void Drawer_Projection(HDCameraUI s, SerializedHDCamera p, Editor owner)
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

        static void Drawer_FieldClippingPlanes(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            _.DrawMultipleFields(
                "Clipping Planes",
                new[] { p.nearClippingPlane, p.farClippingPlane },
                new[] { _.GetContent("Near|The closest point relative to the camera that drawing will occur."), _.GetContent("Far|The furthest point relative to the camera that drawing will occur.\n") });
        }

        static void Drawer_FieldAperture(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.aperture, _.GetContent("Aperture"));
        }

        static void Drawer_FieldShutterSpeed(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            p.shutterSpeed.floatValue = 1f / p.shutterSpeed.floatValue;
            EditorGUILayout.PropertyField(p.shutterSpeed, _.GetContent("Shutter Speed (1 / x)"));
            p.shutterSpeed.floatValue = 1f / p.shutterSpeed.floatValue;
        }

        static void Drawer_FieldIso(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.iso, _.GetContent("ISO"));
        }

        static void Drawer_FieldNormalizedViewPort(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.normalizedViewPortRect, _.GetContent("Viewport Rect|Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0–1)."));
        }

        static void Drawer_FieldDepth(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.depth, _.GetContent("Depth"));
        }

        static void Drawer_FieldClearColorMode(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.clearColorMode, _.GetContent("Clear Mode|The Camera clears the screen to selected mode."));
        }

        static void Drawer_FieldRenderingPath(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.renderingPath, _.GetContent("Rendering Path"));
        }

        static void Drawer_FieldClearDepth(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.clearDepth, _.GetContent("ClearDepth|The Camera clears the depth buffer before rendering."));
        }

        static void Drawer_FieldRenderTarget(HDCameraUI s, SerializedHDCamera p, Editor owner)
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

        static void Drawer_FieldOcclusionCulling(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.PropertyField(p.occlusionCulling, _.GetContent("Occlusion Culling"));
        }

        static void Drawer_CameraWarnings(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            foreach (Camera camera in p.serializedObject.targetObjects)
            {
                var warnings = GetCameraBufferWarnings(camera);
                if (warnings.Length > 0)
                    EditorGUILayout.HelpBox(string.Join("\n\n", warnings), MessageType.Warning, true);
            }
        }

        static void Drawer_FieldVR(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            if (s.canOverrideRenderLoopSettings)
                EditorGUILayout.PropertyField(p.frameSettings.enableStereo, _.GetContent("Enable Stereo"));
            else
            {
                var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
                Assert.IsNotNull(hdrp, "This Editor is valid only for HDRP");
                var enableStereo = hdrp.GetFrameSettings().enableStereo;
                GUI.enabled = false;
                EditorGUILayout.Toggle(_.GetContent("Enable Stereo (Set by HDRP)"), enableStereo);
                GUI.enabled = true;
            }
            EditorGUILayout.PropertyField(p.stereoSeparation, _.GetContent("Stereo Separation"));
            EditorGUILayout.PropertyField(p.stereoConvergence, _.GetContent("Stereo Convergence"));
        }

#if ENABLE_MULTIPLE_DISPLAYS
        static void Drawer_SectionMultiDisplay(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            if (ModuleManager_ShouldShowMultiDisplayOption())
            {
                var prevDisplay = p.targetDisplay.intValue;
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
        static void Drawer_FieldTargetEye(HDCameraUI s, SerializedHDCamera p, Editor owner)
        {
            EditorGUILayout.IntPopup(p.targetEye, k_TargetEyes, k_TargetEyeValues, _.GetContent("Target Eye"));
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
