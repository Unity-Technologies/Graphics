using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Experimental.Rendering.LightweightPipeline
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(LightweightRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class LightweightRenderPipelineCameraEditor : CameraEditor
    {
        internal class Styles
        {
            public readonly GUIContent renderingPathLabel = EditorGUIUtility.TrTextContent("Rendering Path", "The Lightweight Render Pipeline only supports Forward rendering path.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Enable this to make this camera render shadows.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera does not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture) null);

            public readonly GUIContent[] renderingPathOptions = { EditorGUIUtility.TrTextContent("Forward") };
            public readonly string hdrDisabledWarning = "HDR rendering is disabled in the Lightweight Render Pipeline asset.";
            public readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Lightweight Render Pipeline asset.";

            // This is for adding more data like Pipeline Asset option
            public static GUIContent[] displayedAdditionalDataOptions =
            {
                new GUIContent("Off"),
                new GUIContent("On"),
                new GUIContent("Use Pipeline Settings"),
            };

            public static GUIContent[] displayedDepthTextureOverride =
            {
                new GUIContent("On (Forced due to Post Processing)"),
            };

            public static int[] additionalDataOptions = Enum.GetValues(typeof(CameraOverrideOption)).Cast<int>().ToArray();

            // Using the pipeline Settings
            public static GUIContent[] displayedCameraOptions =
            {
                new GUIContent("Off"),
                new GUIContent("Use Pipeline Settings"),
            };
            public static int[] cameraOptions = { 0, 1 };
        };

        public Camera camera { get { return target as Camera; } }

        // Animation Properties
        public bool isSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        public bool isSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

        static readonly int[] s_RenderingPathValues = {0};
        static Styles s_Styles;
        LightweightRenderPipelineAsset m_LightweightRenderPipeline;
        LWRPAdditionalCameraData m_AdditionalCameraData;
        SerializedObject m_AdditionalCameraDataSO;

        readonly AnimBool m_ShowBGColorAnim = new AnimBool();
        readonly AnimBool m_ShowOrthoAnim = new AnimBool();
        readonly AnimBool m_ShowTargetEyeAnim = new AnimBool();

        SerializedProperty m_AdditionalCameraDataRenderShadowsProp;
        SerializedProperty m_AdditionalCameraDataRenderDepthProp;
        SerializedProperty m_AdditionalCameraDataRenderOpaqueProp;

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
            m_LightweightRenderPipeline = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;

            m_AdditionalCameraData = camera.gameObject.GetComponent<LWRPAdditionalCameraData>();
            settings.OnEnable();
            init(m_AdditionalCameraData);

            UpdateAnimationValues(true);
        }

        void init(LWRPAdditionalCameraData additionalCameraData)
        {
            if(additionalCameraData == null)
                return;

            m_AdditionalCameraDataSO = new SerializedObject(additionalCameraData);
            m_AdditionalCameraDataRenderShadowsProp = m_AdditionalCameraDataSO.FindProperty("m_RenderShadows");
            m_AdditionalCameraDataRenderDepthProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresDepthTextureOption");
            m_AdditionalCameraDataRenderOpaqueProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresOpaqueTextureOption");
        }

        public void OnDisable()
        {
            m_ShowBGColorAnim.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoAnim.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            m_LightweightRenderPipeline = null;
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
            DrawAdditionalData();
            settings.DrawVR();
            settings.DrawMultiDisplay();

            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowTargetEyeAnim.faded))
                if (group.visible) settings.DrawTargetEye();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            settings.ApplyModifiedProperties();
        }

        void DrawRenderingPath()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.IntPopup(s_Styles.renderingPathLabel, 0, s_Styles.renderingPathOptions, s_RenderingPathValues);
            }
        }

        void DrawHDR()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.allowHDR, settings.HDR);
            int selectedValue = !settings.HDR.boolValue ? 0 : 1;
            settings.HDR.boolValue = EditorGUI.IntPopup(controlRect, Styles.allowHDR, selectedValue, Styles.displayedCameraOptions, Styles.cameraOptions) == 1;
            EditorGUI.EndProperty();
        }

        void DrawMSAA()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.allowMSAA, settings.allowMSAA);
            int selectedValue = !settings.allowMSAA.boolValue ? 0 : 1;
            settings.allowMSAA.boolValue = EditorGUI.IntPopup(controlRect, Styles.allowMSAA, selectedValue, Styles.displayedCameraOptions, Styles.cameraOptions) == 1;
            EditorGUI.EndProperty();
        }

        void DrawTargetTexture()
        {
            EditorGUILayout.PropertyField(settings.targetTexture);

            if (!settings.targetTexture.hasMultipleDifferentValues)
            {
                var texture = settings.targetTexture.objectReferenceValue as RenderTexture;
                int pipelineSamplesCount = m_LightweightRenderPipeline.msaaSampleCount;

                if (texture && texture.antiAliasing > pipelineSamplesCount)
                {
                    string pipelineMSAACaps = (pipelineSamplesCount > 1)
                        ? String.Format("is set to support {0}x", pipelineSamplesCount)
                        : "has MSAA disabled";
                    EditorGUILayout.HelpBox(String.Format("Camera target texture requires {0}x MSAA. Lightweight pipeline {1}.", texture.antiAliasing, pipelineMSAACaps),
                        MessageType.Warning, true);
                }
            }
        }

        void DrawAdditionalData()
        {
            bool hasChanged = false;
            bool selectedValueShadows;
            CameraOverrideOption selectedDepthOption;
            CameraOverrideOption selectedOpaqueOption;

            if (m_AdditionalCameraDataSO == null)
            {
                selectedValueShadows = true;
                selectedDepthOption = CameraOverrideOption.UsePipelineSettings;
                selectedOpaqueOption = CameraOverrideOption.UsePipelineSettings;
            }
            else
            {
                m_AdditionalCameraDataSO.Update();
                selectedValueShadows = m_AdditionalCameraData.renderShadows;
                selectedDepthOption = (CameraOverrideOption)m_AdditionalCameraDataRenderDepthProp.intValue;
                selectedOpaqueOption =(CameraOverrideOption)m_AdditionalCameraDataRenderOpaqueProp.intValue;
            }

            // Depth Texture
            Rect controlRectDepth = EditorGUILayout.GetControlRect(true);
            // Need to check if post processing is added and active.
            // If it is we will set the int pop to be 1 which is ON and gray it out
            bool defaultDrawOfDepthTextureUI = true;
            PostProcessLayer ppl = camera.GetComponent<PostProcessLayer>();
            var propValue = (int)selectedDepthOption;
            if (ppl != null && ppl.isActiveAndEnabled)
            {
                if ((propValue == 2 && !m_LightweightRenderPipeline.supportsCameraDepthTexture) || propValue == 0)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUI.IntPopup(controlRectDepth, Styles.requireDepthTexture, 0, Styles.displayedDepthTextureOverride, Styles.additionalDataOptions);
                    EditorGUI.EndDisabledGroup();
                    defaultDrawOfDepthTextureUI = false;
                }
            }
            if(defaultDrawOfDepthTextureUI)
            {
                if(m_AdditionalCameraDataSO != null)
                    EditorGUI.BeginProperty(controlRectDepth, Styles.requireDepthTexture, m_AdditionalCameraDataRenderDepthProp);
                EditorGUI.BeginChangeCheck();

                selectedDepthOption = (CameraOverrideOption)EditorGUI.IntPopup(controlRectDepth, Styles.requireDepthTexture, (int)selectedDepthOption, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    hasChanged = true;
                }
                if(m_AdditionalCameraDataSO != null)
                    EditorGUI.EndProperty();
            }

            // Opaque Texture
            Rect controlRectColor = EditorGUILayout.GetControlRect(true);
            // Starting to check the property if we have the scriptable object
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectColor, Styles.requireOpaqueTexture, m_AdditionalCameraDataRenderOpaqueProp);
            EditorGUI.BeginChangeCheck();
            selectedOpaqueOption = (CameraOverrideOption)EditorGUI.IntPopup(controlRectColor, Styles.requireOpaqueTexture, (int)selectedOpaqueOption, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            // Ending to check the property if we have the scriptable object
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.EndProperty();

            // Shadows
            Rect controlRectShadows = EditorGUILayout.GetControlRect(true);
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRectShadows, Styles.renderingShadows, m_AdditionalCameraDataRenderShadowsProp);
            EditorGUI.BeginChangeCheck();

            selectedValueShadows = EditorGUI.Toggle(controlRectShadows, Styles.renderingShadows, selectedValueShadows);
            if (EditorGUI.EndChangeCheck())
            {
                hasChanged = true;
            }
            if(m_AdditionalCameraDataSO != null)
                EditorGUI.EndProperty();

            if (hasChanged)
            {
                if (m_AdditionalCameraDataSO == null)
                {
                    m_AdditionalCameraData = camera.gameObject.AddComponent<LWRPAdditionalCameraData>();
                    init(m_AdditionalCameraData);
                }
                m_AdditionalCameraDataRenderShadowsProp.boolValue = selectedValueShadows;
                m_AdditionalCameraDataRenderDepthProp.intValue = (int)selectedDepthOption;
                m_AdditionalCameraDataRenderOpaqueProp.intValue = (int)selectedOpaqueOption;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }


        }
    }
}
