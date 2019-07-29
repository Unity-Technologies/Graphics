using System;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(UniversalRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class UniversalRenderPipelineCameraEditor : CameraEditor
    {
        internal enum BackgroundType
        {
            Skybox = 0,
            SolidColor,
            DontCare,
        }

        internal class Styles
        {
            public static GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nDon't care have undefined values for camera background. Use this only if you are rendering all pixels in the Camera's view.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Enable this to make this camera render shadows.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera does not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture) null);

            public static GUIContent rendererType = EditorGUIUtility.TrTextContent("Renderer Type", "Controls which renderer this camera uses.");
            public static GUIContent rendererData = EditorGUIUtility.TrTextContent("Renderer Data", "Required by a custom Renderer. If none is assigned this camera uses the one assigned in the Pipeline Settings.");

            public static GUIContent volumeLayerMask = EditorGUIUtility.TrTextContent("Volume Mask", "This camera will only be affected by volumes in the selected scene-layers.");
            public static GUIContent volumeTrigger = EditorGUIUtility.TrTextContent("Volume Trigger", "A transform that will act as a trigger for volume blending. If none is set, the camera itself will act as a trigger.");

            public static GUIContent renderPostProcessing = EditorGUIUtility.TrTextContent("Render Post-processing", "Enable this to make this camera render post-processing effects.");
            public static GUIContent antialiasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "The anti-aliasing method to use.");
            public static GUIContent antialiasingQuality = EditorGUIUtility.TrTextContent("Quality", "The quality level to use for the selected anti-aliasing method.");
            public static GUIContent stopNaN = EditorGUIUtility.TrTextContent("Stop NaN", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will affect performances and should only be used if you experience NaN issues that you can't fix. Has no effect on GLES2 platforms.");
            public static GUIContent dithering = EditorGUIUtility.TrTextContent("Dithering", "Applies 8-bit dithering to the final render to reduce color banding.");

            public readonly GUIContent[] renderingPathOptions = { EditorGUIUtility.TrTextContent("Forward") };
            public readonly string hdrDisabledWarning = "HDR rendering is disabled in the Universal Render Pipeline asset.";
            public readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Universal Render Pipeline asset.";

            public static GUIContent[] displayedRendererTypeOverride =
            {
                new GUIContent("Custom"),
                new GUIContent("Use Pipeline Settings"),
            };

            public static int[] rendererTypeOptions = Enum.GetValues(typeof(RendererOverrideOption)).Cast<int>().ToArray();
            public static GUIContent[] cameraBackgroundType =
            {
                new GUIContent("Skybox"),
                new GUIContent("Solid Color"),
                new GUIContent("Don't Care"),
            };

            public static int[] cameraBackgroundValues = { 0, 1, 2};

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

            // Beautified anti-aliasing options
            public static GUIContent[] antialiasingOptions =
            {
                new GUIContent("None"),
                new GUIContent("Fast Approximate Anti-aliasing (FXAA)"),
                new GUIContent("Subpixel Morphological Anti-aliasing (SMAA)"),
                //new GUIContent("Temporal Anti-aliasing (TAA)")
            };
            public static int[] antialiasingValues = { 0, 1, 2/*, 3*/ };

            // Beautified anti-aliasing quality names
            public static GUIContent[] antialiasingQualityOptions =
            {
                new GUIContent("Low"),
                new GUIContent("Medium"),
                new GUIContent("High")
            };
            public static int[] antialiasingQualityValues = { 0, 1, 2 };
        };

        public Camera camera { get { return target as Camera; } }

        // Animation Properties
        public bool isSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        public bool isSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

        static readonly int[] s_RenderingPathValues = {0};
        static Styles s_Styles;
        UniversalRenderPipelineAsset m_UniversalRenderPipeline;
        UniversalAdditionalCameraData m_AdditionalCameraData;
        SerializedObject m_AdditionalCameraDataSO;

        readonly AnimBool m_ShowBGColorAnim = new AnimBool();
        readonly AnimBool m_ShowOrthoAnim = new AnimBool();
        readonly AnimBool m_ShowTargetEyeAnim = new AnimBool();

        SerializedProperty m_AdditionalCameraDataRenderShadowsProp;
        SerializedProperty m_AdditionalCameraDataRenderDepthProp;
        SerializedProperty m_AdditionalCameraDataRenderOpaqueProp;
        SerializedProperty m_AdditionalCameraDataRendererProp;
        SerializedProperty m_AdditionalCameraDataRendererDataProp;
        SerializedProperty m_AdditionalCameraDataVolumeLayerMask;
        SerializedProperty m_AdditionalCameraDataVolumeTrigger;
        SerializedProperty m_AdditionalCameraDataRenderPostProcessing;
        SerializedProperty m_AdditionalCameraDataAntialiasing;
        SerializedProperty m_AdditionalCameraDataAntialiasingQuality;
        SerializedProperty m_AdditionalCameraDataStopNaN;
        SerializedProperty m_AdditionalCameraDataDithering;

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
            m_UniversalRenderPipeline = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;

            m_AdditionalCameraData = camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
            settings.OnEnable();
            init(m_AdditionalCameraData);

            UpdateAnimationValues(true);
        }

        void init(UniversalAdditionalCameraData additionalCameraData)
        {
            if(additionalCameraData == null)
                return;

            m_AdditionalCameraDataSO = new SerializedObject(additionalCameraData);
            m_AdditionalCameraDataRenderShadowsProp = m_AdditionalCameraDataSO.FindProperty("m_RenderShadows");
            m_AdditionalCameraDataRenderDepthProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresDepthTextureOption");
            m_AdditionalCameraDataRenderOpaqueProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresOpaqueTextureOption");
            m_AdditionalCameraDataRendererProp = m_AdditionalCameraDataSO.FindProperty("m_RendererOverrideOption");
            m_AdditionalCameraDataRendererDataProp = m_AdditionalCameraDataSO.FindProperty("m_RendererData");
            m_AdditionalCameraDataVolumeLayerMask = m_AdditionalCameraDataSO.FindProperty("m_VolumeLayerMask");
            m_AdditionalCameraDataVolumeTrigger = m_AdditionalCameraDataSO.FindProperty("m_VolumeTrigger");
            m_AdditionalCameraDataRenderPostProcessing = m_AdditionalCameraDataSO.FindProperty("m_RenderPostProcessing");
            m_AdditionalCameraDataAntialiasing = m_AdditionalCameraDataSO.FindProperty("m_Antialiasing");
            m_AdditionalCameraDataAntialiasingQuality = m_AdditionalCameraDataSO.FindProperty("m_AntialiasingQuality");
            m_AdditionalCameraDataStopNaN = m_AdditionalCameraDataSO.FindProperty("m_StopNaN");
            m_AdditionalCameraDataDithering = m_AdditionalCameraDataSO.FindProperty("m_Dithering");
        }

        public void OnDisable()
        {
            m_ShowBGColorAnim.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoAnim.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            m_UniversalRenderPipeline = null;
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();
            UpdateAnimationValues(false);

            DrawClearFlags();
            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowBGColorAnim.faded))
                if (group.visible) settings.DrawBackgroundColor();

            settings.DrawCullingMask();

            EditorGUILayout.Space();

            settings.DrawProjection();
            settings.DrawClippingPlanes();
            settings.DrawNormalizedViewPort();

            EditorGUILayout.Space();
            settings.DrawDepth();
            DrawTargetTexture();
            settings.DrawOcclusionCulling();
            DrawHDR();
            DrawMSAA();
            settings.DrawDynamicResolution();
            DrawAdditionalData();
            settings.DrawVR();
            settings.DrawMultiDisplay();

            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowTargetEyeAnim.faded))
                if (group.visible) settings.DrawTargetEye();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            settings.ApplyModifiedProperties();
        }

        BackgroundType GetBackgroundType(CameraClearFlags clearFlags)
        {
            switch (clearFlags)
            {
                case CameraClearFlags.Skybox:
                    return BackgroundType.Skybox;
                case CameraClearFlags.Nothing:
                    return BackgroundType.DontCare;

                // DepthOnly is not supported by design in LWRP. We upgrade it to SolidColor
                default:
                    return BackgroundType.SolidColor;
            }
        }

        void DrawClearFlags()
        {
            // Converts between ClearFlags and Background Type.
            BackgroundType backgroundType = GetBackgroundType((CameraClearFlags) settings.clearFlags.intValue);

            EditorGUI.BeginChangeCheck();
            BackgroundType selectedType = (BackgroundType)EditorGUILayout.IntPopup(Styles.backgroundType, (int)backgroundType,
                Styles.cameraBackgroundType, Styles.cameraBackgroundValues);

            if (EditorGUI.EndChangeCheck())
            {
                CameraClearFlags selectedClearFlags;
                switch (selectedType)
                {
                    case BackgroundType.Skybox:
                        selectedClearFlags = CameraClearFlags.Skybox;
                        break;

                    case BackgroundType.DontCare:
                        selectedClearFlags = CameraClearFlags.Nothing;
                        break;

                    default:
                        selectedClearFlags = CameraClearFlags.SolidColor;
                        break;
                }

                settings.clearFlags.intValue = (int) selectedClearFlags;
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
                int pipelineSamplesCount = m_UniversalRenderPipeline.msaaSampleCount;

                if (texture && texture.antiAliasing > pipelineSamplesCount)
                {
                    string pipelineMSAACaps = (pipelineSamplesCount > 1)
                        ? String.Format("is set to support {0}x", pipelineSamplesCount)
                        : "has MSAA disabled";
                    EditorGUILayout.HelpBox(String.Format("Camera target texture requires {0}x MSAA. Universal pipeline {1}.", texture.antiAliasing, pipelineMSAACaps),
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
            RendererOverrideOption selectedRendererOption;
            LayerMask selectedVolumeLayerMask;
            Transform selectedVolumeTrigger;
            bool selectedRenderPostProcessing;
            AntialiasingMode selectedAntialiasing;
            AntialiasingQuality selectedAntialiasingQuality;
            bool selectedStopNaN;
            bool selectedDithering;

            if (m_AdditionalCameraDataSO == null)
            {
                selectedValueShadows = true;
                selectedDepthOption = CameraOverrideOption.UsePipelineSettings;
                selectedOpaqueOption = CameraOverrideOption.UsePipelineSettings;
                selectedRendererOption = RendererOverrideOption.UsePipelineSettings;
                selectedVolumeLayerMask = 1; // "Default"
                selectedVolumeTrigger = null;
                selectedRenderPostProcessing = false;
                selectedAntialiasing = AntialiasingMode.None;
                selectedAntialiasingQuality = AntialiasingQuality.High;
                selectedStopNaN = false;
                selectedDithering = false;
            }
            else
            {
                m_AdditionalCameraDataSO.Update();
                selectedValueShadows = m_AdditionalCameraData.renderShadows;
                selectedDepthOption = (CameraOverrideOption)m_AdditionalCameraDataRenderDepthProp.intValue;
                selectedOpaqueOption =(CameraOverrideOption)m_AdditionalCameraDataRenderOpaqueProp.intValue;
                selectedRendererOption = (RendererOverrideOption) m_AdditionalCameraDataRendererProp.intValue;
                selectedVolumeLayerMask = m_AdditionalCameraDataVolumeLayerMask.intValue;
                selectedVolumeTrigger = (Transform)m_AdditionalCameraDataVolumeTrigger.objectReferenceValue;
                selectedRenderPostProcessing = m_AdditionalCameraDataRenderPostProcessing.boolValue;
                selectedAntialiasing = (AntialiasingMode)m_AdditionalCameraDataAntialiasing.intValue;
                selectedAntialiasingQuality = (AntialiasingQuality)m_AdditionalCameraDataAntialiasingQuality.intValue;
                selectedStopNaN = m_AdditionalCameraDataStopNaN.boolValue;
                selectedDithering = m_AdditionalCameraDataDithering.boolValue;
            }

            hasChanged |= DrawLayerMask(m_AdditionalCameraDataVolumeLayerMask, ref selectedVolumeLayerMask, Styles.volumeLayerMask);
            hasChanged |= DrawObjectField(m_AdditionalCameraDataVolumeTrigger, ref selectedVolumeTrigger, Styles.volumeTrigger);
            hasChanged |= DrawIntPopup(m_AdditionalCameraDataRendererProp, ref selectedRendererOption, Styles.rendererType, Styles.displayedRendererTypeOverride, Styles.rendererTypeOptions);

            if (selectedRendererOption == RendererOverrideOption.Custom && m_AdditionalCameraDataSO != null)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_AdditionalCameraDataRendererDataProp, Styles.rendererData);
                if (EditorGUI.EndChangeCheck())
                    hasChanged = true;
                EditorGUI.indentLevel--;
            }

            // TODO: Fix this for lw/postfx
            bool defaultDrawOfDepthTextureUI = true;
            if (defaultDrawOfDepthTextureUI)
                hasChanged |= DrawIntPopup(m_AdditionalCameraDataRenderDepthProp, ref selectedDepthOption, Styles.requireDepthTexture, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);

            hasChanged |= DrawIntPopup(m_AdditionalCameraDataRenderOpaqueProp, ref selectedOpaqueOption, Styles.requireOpaqueTexture, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);
            hasChanged |= DrawToggle(m_AdditionalCameraDataRenderShadowsProp, ref selectedValueShadows, Styles.renderingShadows);
            hasChanged |= DrawToggle(m_AdditionalCameraDataRenderPostProcessing, ref selectedRenderPostProcessing, Styles.renderPostProcessing);

            if (selectedRenderPostProcessing)
            {
                EditorGUI.indentLevel++;
                hasChanged |= DrawIntPopup(m_AdditionalCameraDataAntialiasing, ref selectedAntialiasing, Styles.antialiasing, Styles.antialiasingOptions, Styles.antialiasingValues);

                if (selectedAntialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                {
                    EditorGUI.indentLevel++;
                    hasChanged |= DrawIntPopup(m_AdditionalCameraDataAntialiasingQuality, ref selectedAntialiasingQuality, Styles.antialiasingQuality, Styles.antialiasingQualityOptions, Styles.antialiasingQualityValues);
                    if (CoreEditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2))
                        EditorGUILayout.HelpBox("Sub-pixel Morphological Anti-Aliasing isn't supported on GLES2 platforms.", MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
                //else if (selectedAntialiasing == AntialiasingMode.TemporalAntialiasing)
                //{
                //    EditorGUILayout.HelpBox("Temporal anti-aliasing is not supported on the Universal Render Pipeline yet.", MessageType.Info);
                //}

                hasChanged |= DrawToggle(m_AdditionalCameraDataStopNaN, ref selectedStopNaN, Styles.stopNaN);
                hasChanged |= DrawToggle(m_AdditionalCameraDataDithering, ref selectedDithering, Styles.dithering);
                EditorGUI.indentLevel--;
            }

            if (hasChanged)
            {
                if (m_AdditionalCameraDataSO == null)
                {
                    m_AdditionalCameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    init(m_AdditionalCameraData);
                }

                m_AdditionalCameraDataRenderShadowsProp.boolValue = selectedValueShadows;
                m_AdditionalCameraDataRenderDepthProp.intValue = (int)selectedDepthOption;
                m_AdditionalCameraDataRenderOpaqueProp.intValue = (int)selectedOpaqueOption;
                m_AdditionalCameraDataRendererProp.intValue = (int)selectedRendererOption;
                m_AdditionalCameraDataVolumeLayerMask.intValue = selectedVolumeLayerMask;
                m_AdditionalCameraDataVolumeTrigger.objectReferenceValue = selectedVolumeTrigger;
                m_AdditionalCameraDataRenderPostProcessing.boolValue = selectedRenderPostProcessing;
                m_AdditionalCameraDataAntialiasing.intValue = (int)selectedAntialiasing;
                m_AdditionalCameraDataAntialiasingQuality.intValue = (int)selectedAntialiasingQuality;
                m_AdditionalCameraDataStopNaN.boolValue = selectedStopNaN;
                m_AdditionalCameraDataDithering.boolValue = selectedDithering;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
        }

        bool DrawLayerMask(SerializedProperty prop, ref LayerMask mask, GUIContent style)
        {
            var layers = InternalEditorUtility.layers;
            bool hasChanged = false;
            var controlRect = BeginProperty(prop, style);

            EditorGUI.BeginChangeCheck();

            // LayerMask needs to be converted to be used in a MaskField...
            int field = 0;
            for (int c = 0; c < layers.Length; c++)
                if ((mask & (1 << LayerMask.NameToLayer(layers[c]))) != 0)
                    field |= 1 << c;

            field = EditorGUI.MaskField(controlRect, style, field, InternalEditorUtility.layers);
            if (EditorGUI.EndChangeCheck())
                hasChanged = true;

            // ...and converted back.
            mask = 0;
            for (int c = 0; c < layers.Length; c++)
                if ((field & (1 << c)) != 0)
                    mask |= 1 << LayerMask.NameToLayer(layers[c]);

            EndProperty();
            return hasChanged;
        }

        bool DrawObjectField<T>(SerializedProperty prop, ref T value, GUIContent style)
            where T : UnityEngine.Object
        {
            bool hasChanged = false;
            var controlRect = BeginProperty(prop, style);

            EditorGUI.BeginChangeCheck();
            value = (T)EditorGUI.ObjectField(controlRect, style, value, typeof(T), true);
            if (EditorGUI.EndChangeCheck())
                hasChanged = true;

            EndProperty();
            return hasChanged;
        }

        bool DrawIntPopup<T>(SerializedProperty prop, ref T value, GUIContent style, GUIContent[] optionNames, int[] optionValues)
            where T : Enum
        {
            bool hasChanged = false;
            var controlRect = BeginProperty(prop, style);

            EditorGUI.BeginChangeCheck();
            value = (T)(object)EditorGUI.IntPopup(controlRect, style, (int)(object)value, optionNames, optionValues);
            if (EditorGUI.EndChangeCheck())
                hasChanged = true;

            EndProperty();
            return hasChanged;
        }

        bool DrawToggle(SerializedProperty prop, ref bool value, GUIContent style)
        {
            bool hasChanged = false;
            var controlRect = BeginProperty(prop, style);

            EditorGUI.BeginChangeCheck();
            value = EditorGUI.Toggle(controlRect, style, value);
            if (EditorGUI.EndChangeCheck())
                hasChanged = true;

            EndProperty();
            return hasChanged;
        }

        Rect BeginProperty(SerializedProperty prop, GUIContent style)
        {
            var controlRect = EditorGUILayout.GetControlRect(true);
            if (m_AdditionalCameraDataSO != null)
                EditorGUI.BeginProperty(controlRect, style, prop);
            return controlRect;
        }

        void EndProperty()
        {
            if (m_AdditionalCameraDataSO != null)
                EditorGUI.EndProperty();
        }
    }
}
