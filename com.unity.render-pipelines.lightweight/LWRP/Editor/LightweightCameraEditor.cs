using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;

namespace UnityEditor
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(LightweightPipelineAsset))]
    [CanEditMultipleObjects]
    public class LightweightameraEditor : CameraEditor
    {
        public class Styles
        {
            public readonly GUIContent renderingPathLabel = new GUIContent("Rendering Path");
            public readonly GUIContent[] renderingPathOptions = { new GUIContent("Forward") };
            public readonly GUIContent renderingPathInfo = new GUIContent("Lightweight Pipeline only supports Forward rendering path.");
            public readonly GUIContent fixNow = new GUIContent("Fix now");

            public readonly string mssaDisabledWarning = "Anti Aliasing is disabled in Lightweight Pipeline settings.";
        };

        private static readonly int[] kRenderingPathValues = {0};
        private static Styles s_Styles;
        private LightweightPipelineAsset lightweightPipeline;

        private Camera camera { get { return target as Camera; } }

        // Animation Properties
        private bool IsSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        private bool IsSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

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
            SetAnimationTarget(showTargetEyeAnim, initialize, settings.targetEye.intValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported);
        }

        public new void OnEnable()
        {
            lightweightPipeline = GraphicsSettings.renderPipelineAsset as LightweightPipelineAsset;

            settings.OnEnable();
            UpdateAnimationValues(true);
        }

        public void OnDisable()
        {
            showBGColorAnim.valueChanged.RemoveListener(Repaint);
            showOrthoAnim.valueChanged.RemoveListener(Repaint);
            showTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            lightweightPipeline = null;
        }

        private void DrawRenderingPath()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntPopup(s_Styles.renderingPathLabel, 0, s_Styles.renderingPathOptions, kRenderingPathValues);
            }

            EditorGUILayout.HelpBox(s_Styles.renderingPathInfo.text, MessageType.Info);
        }

        private void DrawHDR()
        {
            settings.DrawHDR();
            if (settings.HDR.boolValue && !lightweightPipeline.SupportsHDR)
                EditorGUILayout.HelpBox("HDR rendering is disabled in Lightweight Pipeline asset.", MessageType.Warning);
        }

        private void DrawTargetTexture()
        {
            EditorGUILayout.PropertyField(settings.targetTexture);

            if (!settings.targetTexture.hasMultipleDifferentValues)
            {
                var texture = settings.targetTexture.objectReferenceValue as RenderTexture;
                int pipelineSamplesCount = lightweightPipeline.MSAASampleCount;

                if (texture && texture.antiAliasing > pipelineSamplesCount)
                {
                    string pipelineMSAACaps = (pipelineSamplesCount > 1)
                        ? String.Format("is set to support {0}x", pipelineSamplesCount)
                        : "has MSAA disabled";
                    EditorGUILayout.HelpBox(String.Format("Camera target texture requires {0}x MSAA. Lightweight pipeline {1}.", texture.antiAliasing, pipelineMSAACaps),
                        MessageType.Warning, true);

                    if (GUILayout.Button(s_Styles.fixNow))
                        lightweightPipeline.MSAASampleCount = texture.antiAliasing;
                }
            }
        }

        private void DrawMSAA()
        {
            EditorGUILayout.PropertyField(settings.allowMSAA);
            if (settings.allowMSAA.boolValue && lightweightPipeline.MSAASampleCount <= 1)
            {
                EditorGUILayout.HelpBox(s_Styles.mssaDisabledWarning, MessageType.Warning);
                if (GUILayout.Button(s_Styles.fixNow))
                    lightweightPipeline.MSAASampleCount = 4;
            }
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();
            UpdateAnimationValues(false);

            settings.DrawClearFlags();

            using (var group = new EditorGUILayout.FadeGroupScope(showBGColorAnim.faded))
                if (group.visible) settings.DrawBackgroundColor();

            settings.DrawCullingMask();

            EditorGUILayout.Space();

            settings.DrawProjection();
            settings.DrawClippingPlanes();
            settings.DrawNormalizedViewPort();

            EditorGUILayout.Space();
            settings.DrawDepth();
            DrawRenderingPath();
            DrawTargetTexture();
            settings.DrawOcclusionCulling();
            DrawHDR();
            DrawMSAA();
            settings.DrawVR();
            settings.DrawMultiDisplay();

            using (var group = new EditorGUILayout.FadeGroupScope(showTargetEyeAnim.faded))
                if (group.visible) settings.DrawTargetEye();

            settings.ApplyModifiedProperties();
        }
    }
}
