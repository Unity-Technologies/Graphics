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
            public readonly GUIContent additionalCameraDataLabel = new GUIContent("Add Additional Camera Data");

            public readonly string mssaDisabledWarning = "Anti Aliasing is disabled in Lightweight Pipeline settings.";
        };

        public Camera camera { get { return target as Camera; } }

        // Animation Properties
        public bool isSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        public bool isSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

        static readonly int[] s_RenderingPathValues = {0};
        static Styles s_Styles;
        LightweightPipelineAsset m_LightweightPipeline;

        readonly AnimBool m_ShowBGColorAnim = new AnimBool();
        readonly AnimBool m_ShowOrthoAnim = new AnimBool();
        readonly AnimBool m_ShowTargetEyeAnim = new AnimBool();

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
            SetAnimationTarget(m_ShowBGColorAnim, initialize, isSameClearFlags && (camera.clearFlags == CameraClearFlags.SolidColor || camera.clearFlags == CameraClearFlags.Skybox));
            SetAnimationTarget(m_ShowOrthoAnim, initialize, isSameOrthographic && camera.orthographic);
            SetAnimationTarget(m_ShowTargetEyeAnim, initialize, settings.targetEye.intValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported);
        }

        public new void OnEnable()
        {
            m_LightweightPipeline = GraphicsSettings.renderPipelineAsset as LightweightPipelineAsset;

            settings.OnEnable();
            UpdateAnimationValues(true);
        }

        public void OnDisable()
        {
            m_ShowBGColorAnim.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoAnim.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            m_LightweightPipeline = null;
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();
            UpdateAnimationValues(false);

            settings.DrawClearFlags();

            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowBGColorAnim.faded))
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

            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowTargetEyeAnim.faded))
                if (group.visible) settings.DrawTargetEye();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            GameObject gameObject = camera.gameObject;
            if (gameObject.GetComponent<LightweightAdditionalCameraData>() == null)
            {
                if (GUILayout.Button(s_Styles.additionalCameraDataLabel))
                {
                    gameObject.AddComponent<LightweightAdditionalCameraData>();
                }
            }
            settings.ApplyModifiedProperties();
        }

        void DrawRenderingPath()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntPopup(s_Styles.renderingPathLabel, 0, s_Styles.renderingPathOptions, s_RenderingPathValues);
            }

            EditorGUILayout.HelpBox(s_Styles.renderingPathInfo.text, MessageType.Info);
        }

        void DrawHDR()
        {
            settings.DrawHDR();
            if (settings.HDR.boolValue && !m_LightweightPipeline.supportsHDR)
                EditorGUILayout.HelpBox("HDR rendering is disabled in Lightweight Pipeline asset.", MessageType.Warning);
        }

        void DrawTargetTexture()
        {
            EditorGUILayout.PropertyField(settings.targetTexture);

            if (!settings.targetTexture.hasMultipleDifferentValues)
            {
                var texture = settings.targetTexture.objectReferenceValue as RenderTexture;
                int pipelineSamplesCount = m_LightweightPipeline.msaaSampleCount;

                if (texture && texture.antiAliasing > pipelineSamplesCount)
                {
                    string pipelineMSAACaps = (pipelineSamplesCount > 1)
                        ? String.Format("is set to support {0}x", pipelineSamplesCount)
                        : "has MSAA disabled";
                    EditorGUILayout.HelpBox(String.Format("Camera target texture requires {0}x MSAA. Lightweight pipeline {1}.", texture.antiAliasing, pipelineMSAACaps),
                        MessageType.Warning, true);

                    if (GUILayout.Button(s_Styles.fixNow))
                        m_LightweightPipeline.msaaSampleCount = texture.antiAliasing;
                }
            }
        }

        void DrawMSAA()
        {
            EditorGUILayout.PropertyField(settings.allowMSAA);
            if (settings.allowMSAA.boolValue && m_LightweightPipeline.msaaSampleCount <= 1)
            {
                EditorGUILayout.HelpBox(s_Styles.mssaDisabledWarning, MessageType.Warning);
                if (GUILayout.Button(s_Styles.fixNow))
                    m_LightweightPipeline.msaaSampleCount = 4;
            }
        }
    }
}
