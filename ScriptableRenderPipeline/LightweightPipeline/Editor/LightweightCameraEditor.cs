using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using UnityEditor.Modules;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(LightweightPipelineAsset))]
    [CanEditMultipleObjects]
    public class LightweightameraEditor : Editor
    {
        public class Styles
        {
            public readonly GUIContent renderingPathLabel = new GUIContent("Rendering Path");
            public readonly GUIContent[] renderingPathOptions = { new GUIContent("Forward") };
            public readonly GUIContent renderingPathInfo = new GUIContent("Lightweight Pipeline only supports Forward rendering path.");
            public readonly GUIContent clipingPlanesLabel = new GUIContent("Clipping Planes", "Distances from the camera to start and stop rendering.");
            public readonly GUIContent nearPlaneLabel = new GUIContent("Near", "The closest point relative to the camera that drawing will occur.");
            public readonly GUIContent farPlaneLabel = new GUIContent("Far", "The furthest point relative to the camera that drawing will occur.");
            public readonly GUIContent fixNow = new GUIContent("Fix now");
        };

        private static readonly int[] kRenderingPathValues = {0};
        private static Styles s_Styles;
        private LightweightPipelineAsset lightweightPipeline;

        public SerializedProperty clearFlags { get; private set; }
        public SerializedProperty backgroundColor { get; private set; }
        public SerializedProperty normalizedViewPortRect { get; private set; }
        public SerializedProperty fieldOfView { get; private set; }
        public SerializedProperty orthographic { get; private set; }
        public SerializedProperty orthographicSize { get; private set; }
        public SerializedProperty depth { get; private set; }
        public SerializedProperty cullingMask { get; private set; }
        public SerializedProperty renderingPath { get; private set; }
        public SerializedProperty occlusionCulling { get; private set; }
        public SerializedProperty targetTexture { get; private set; }
        public SerializedProperty HDR { get; private set; }
        public SerializedProperty allowMSAA { get; private set; }
        public SerializedProperty allowDynamicResolution { get; private set; }
        public SerializedProperty stereoConvergence { get; private set; }
        public SerializedProperty stereoSeparation { get; private set; }
        public SerializedProperty nearClippingPlane { get; private set; }
        public SerializedProperty farClippingPlane { get; private set; }


#if ENABLE_MULTIPLE_DISPLAYS
        public SerializedProperty targetDisplay { get; private set; }
#endif

        public SerializedProperty targetEye { get; private set; }

        private Camera camera { get { return target as Camera; } }

        // Animation Properties
        private bool IsSameClearFlags { get { return !clearFlags.hasMultipleDifferentValues; } }
        private bool IsSameOrthographic { get { return !orthographic.hasMultipleDifferentValues; } }

        readonly AnimBool showBGColorAnim = new AnimBool();
        readonly AnimBool showOrthoAnim = new AnimBool();
        readonly AnimBool showTargetEyeAnim = new AnimBool();

        void SetAnimationTarget(AnimBool anim, bool initialize, bool targetValue)
        {
            if (initialize)
            {
                anim.value = targetValue;
                anim.valueChanged.AddListener(Repaint);
            }
            else
            {
                anim.target = targetValue;
            }
        }

        void UpdateAnimationValues(bool initialize)
        {
            SetAnimationTarget(showBGColorAnim, initialize, IsSameClearFlags && (camera.clearFlags == CameraClearFlags.SolidColor || camera.clearFlags == CameraClearFlags.Skybox));
            SetAnimationTarget(showOrthoAnim, initialize, IsSameOrthographic && camera.orthographic);
            SetAnimationTarget(showTargetEyeAnim, initialize, targetEye.intValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported);
        }

        private static readonly GUIContent[] kTargetEyes =
        {
            new GUIContent("Both"),
            new GUIContent("Left"),
            new GUIContent("Right"),
            new GUIContent("None (Main Display)"),
        };

        private static readonly int[] kTargetEyeValues =
        {
            (int) StereoTargetEyeMask.Both, (int) StereoTargetEyeMask.Left,
            (int) StereoTargetEyeMask.Right, (int) StereoTargetEyeMask.None
        };

        void OnEnable()
        {
            lightweightPipeline = GraphicsSettings.renderPipelineAsset as LightweightPipelineAsset;

            clearFlags = serializedObject.FindProperty("m_ClearFlags");
            backgroundColor = serializedObject.FindProperty("m_BackGroundColor");
            normalizedViewPortRect = serializedObject.FindProperty("m_NormalizedViewPortRect");
            nearClippingPlane = serializedObject.FindProperty("near clip plane");
            farClippingPlane = serializedObject.FindProperty("far clip plane");
            fieldOfView = serializedObject.FindProperty("field of view");
            orthographic = serializedObject.FindProperty("orthographic");
            orthographicSize = serializedObject.FindProperty("orthographic size");
            depth = serializedObject.FindProperty("m_Depth");
            cullingMask = serializedObject.FindProperty("m_CullingMask");
            occlusionCulling = serializedObject.FindProperty("m_OcclusionCulling");
            targetTexture = serializedObject.FindProperty("m_TargetTexture");
            HDR = serializedObject.FindProperty("m_HDR");
            allowMSAA = serializedObject.FindProperty("m_AllowMSAA");
            allowDynamicResolution = serializedObject.FindProperty("m_AllowDynamicResolution");

            stereoConvergence = serializedObject.FindProperty("m_StereoConvergence");
            stereoSeparation = serializedObject.FindProperty("m_StereoSeparation");

#if ENABLE_MULTIPLE_DISPLAYS
            targetDisplay = serializedObject.FindProperty("m_TargetDisplay");
#endif

            targetEye = serializedObject.FindProperty("m_TargetEye");

            UpdateAnimationValues(true);
        }

        void OnDisable()
        {
            showBGColorAnim.valueChanged.RemoveListener(Repaint);
            showOrthoAnim.valueChanged.RemoveListener(Repaint);
            showTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            lightweightPipeline = null;
        }

        public void DrawClearFlags()
        {
            EditorGUILayout.PropertyField(clearFlags,
                new GUIContent(
                    "Clear Flags","What to display in empty areas of this Camera's view.\n\nChoose Skybox to display a skybox in empty areas, defaulting to a background color if no skybox is found.\n\nChoose Solid Color to display a background color in empty areas.\n\nChoose Depth Only to display nothing in empty areas.\n\nChoose Don't Clear to display whatever was displayed in the previous frame in empty areas."));
        }

        public void DrawBackgroundColor()
        {
            EditorGUILayout.PropertyField(backgroundColor,
                new GUIContent("Background", "The Camera clears the screen to this color before rendering."));
        }

        public void DrawCullingMask()
        {
            EditorGUILayout.PropertyField(cullingMask);
        }

        public void DrawProjection()
        {
            ProjectionType projectionType = orthographic.boolValue
                ? ProjectionType.Orthographic
                : ProjectionType.Perspective;
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = orthographic.hasMultipleDifferentValues;
            projectionType =
                (ProjectionType)
                EditorGUILayout.EnumPopup(
                    new GUIContent(
                        "Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective."),
                    projectionType);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                orthographic.boolValue = (projectionType == ProjectionType.Orthographic);

            if (!orthographic.hasMultipleDifferentValues)
            {
                if (projectionType == ProjectionType.Orthographic)
                    EditorGUILayout.PropertyField(orthographicSize, new GUIContent("Size"));
                else
                    EditorGUILayout.Slider(fieldOfView, 1f, 179f,
                        new GUIContent(
                            "Field of View", "The width of the Camera’s view angle, measured in degrees along the local Y axis."));
            }
        }

        public void DrawClippingPlanes()
        {
            EditorGUILayout.LabelField(s_Styles.clipingPlanesLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(nearClippingPlane, s_Styles.nearPlaneLabel);
            EditorGUILayout.PropertyField(farClippingPlane, s_Styles.farPlaneLabel);
            EditorGUI.indentLevel--;
        }

        public void DrawNormalizedViewPorts()
        {
            EditorGUILayout.PropertyField(normalizedViewPortRect,
                new GUIContent("Viewport Rect", "Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0–1)."));
        }

        public void DrawDepth()
        {
            EditorGUILayout.PropertyField(depth);
        }

        public void DrawRenderingPath()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntPopup(s_Styles.renderingPathLabel, 0, s_Styles.renderingPathOptions, kRenderingPathValues);
            }

            EditorGUILayout.HelpBox(s_Styles.renderingPathInfo.text, MessageType.Info);
        }

        public void DrawTargetTexture()
        {
            EditorGUILayout.PropertyField(targetTexture);

            // show warning if we have deferred but manual MSAA set
            // only do this if the m_TargetTexture has the same values across all target cameras

            // TODO: Add fix to change target texture msaa
            if (!targetTexture.hasMultipleDifferentValues)
            {
                var texture = targetTexture.objectReferenceValue as RenderTexture;

                int pipelineSamplesCount = lightweightPipeline.MSAASampleCount;

                if (texture && texture.antiAliasing > pipelineSamplesCount)
                {
                    string pipelineMSAACaps = (pipelineSamplesCount > 1)
                        ? String.Format("is set to support {0}x", pipelineSamplesCount)
                        : "has MSAA disabled";
                        pipelineMSAACaps += " due to Soft Particles being enabled in the pipeline asset";
                    EditorGUILayout.HelpBox(String.Format("Camera target texture requires {0}x MSAA. Lightweight pipeline {1}.", texture.antiAliasing, pipelineMSAACaps),
                        MessageType.Warning, true);
                }
            }
        }

        public void DrawOcclusionCulling()
        {
            EditorGUILayout.PropertyField(occlusionCulling);
        }

        public void DrawHDR()
        {
            EditorGUILayout.PropertyField(HDR, new GUIContent("Allow HDR"));
        }

        public void DrawMSAA()
        {
            EditorGUILayout.PropertyField(allowMSAA);
        }

        public void DrawDynamicResolution()
        {
            EditorGUILayout.PropertyField(allowDynamicResolution);
        }

        public void DrawVR()
        {
            if (PlayerSettings.virtualRealitySupported)
            {
                EditorGUILayout.PropertyField(stereoSeparation);
                EditorGUILayout.PropertyField(stereoConvergence);
            }
        }

        // Not supported ATM
//        public void DrawMultiDisplay()
//        {
//#if ENABLE_MULTIPLE_DISPLAYS
//            if (ModuleManager.ShouldShowMultiDisplayOption())
//            {
//                int prevDisplay = targetDisplay.intValue;
//                EditorGUILayout.Space();
//                EditorGUILayout.IntPopup(targetDisplay, DisplayUtility.GetDisplayNames(),
//                    DisplayUtility.GetDisplayIndices(), EditorGUIUtility.TempContent("Target Display"));
//                if (prevDisplay != targetDisplay.intValue)
//                    GameView.RepaintAll();
//            }
//#endif
//        }

        public void DrawTargetEye()
        {
            EditorGUILayout.IntPopup(targetEye, kTargetEyes, kTargetEyeValues, new GUIContent("Target Eye"));
        }

        enum ProjectionType
        {
            Perspective,
            Orthographic
        };

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();
            UpdateAnimationValues(false);

            DrawClearFlags();

            using (var group = new EditorGUILayout.FadeGroupScope(showBGColorAnim.faded))
                if (group.visible) DrawBackgroundColor();

            DrawCullingMask();

            EditorGUILayout.Space();

            DrawProjection();

            DrawClippingPlanes();

            DrawNormalizedViewPorts();

            EditorGUILayout.Space();
            DrawDepth();
            DrawRenderingPath();
            DrawTargetTexture();
            DrawOcclusionCulling();
            DrawHDR();
            DrawVR();

            using (var group = new EditorGUILayout.FadeGroupScope(showTargetEyeAnim.faded))
                if (group.visible) DrawTargetEye();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
