using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental.Rendering;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.LWRP
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(LightweightRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class LightweightRenderPipelineCameraEditor : CameraEditor
    {
        internal enum BackgroundType
        {
            Skybox = 0,
            SolidColor,
            DontCare,
        }

        internal class Styles
        {
            // Groups
            public static GUIContent commonCameraSettingsText = EditorGUIUtility.TrTextContent("Projection", "These settings control how the camera views the world.");
            public static GUIContent environmentSettingsText = EditorGUIUtility.TrTextContent("Environment", "These settings control what the camera background looks like.");
            public static GUIContent outputSettingsText = EditorGUIUtility.TrTextContent("Output", "These settings control how the camera output is formatted.");
            public static GUIContent renderingSettingsText = EditorGUIUtility.TrTextContent("Rendering", "These settings coltrol for the specific rendering features for this camera.");
            public static GUIContent stackSettingsText = EditorGUIUtility.TrTextContent("Stack", "The list of overlay cameras assigned to this camera. ");

            public static GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.");
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Render Mode", "Controls which type of camera this is.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Makes this camera render shadows.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture) null);
            public static GUIContent priority = EditorGUIUtility.TrTextContent("Priority", "A camera with a higher priority is drawn on top of a camera with a lower priority [ -100, 100 ].");

            public static GUIContent rendererType = EditorGUIUtility.TrTextContent("Renderer Type", "Controls which renderer this camera uses.");
            public static GUIContent rendererData = EditorGUIUtility.TrTextContent("Renderer Data", "Required by a custom renderer. If none is assigned, this camera uses the one assigned in the Pipeline Settings.");

            public readonly GUIContent[] renderingPathOptions = { EditorGUIUtility.TrTextContent("Forward") };
            public readonly string hdrDisabledWarning = "HDR rendering is disabled in the Lightweight Render Pipeline asset.";
            public readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Lightweight Render Pipeline asset.";

            public static GUIContent[] displayedRendererTypeOverride =
            {
                new GUIContent("Custom"),
                new GUIContent("Use Pipeline Settings"),
            };

            public static int[] rendererTypeOptions = Enum.GetValues(typeof(RendererOverrideOption)) as int[];
            public static GUIContent[] cameraBackgroundType =
            {
                new GUIContent("Skybox"),
                new GUIContent("Solid Color"),
                new GUIContent("Uninitialized"),
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

            public static int[] additionalDataOptions = Enum.GetValues(typeof(CameraOverrideOption)) as int[];

            // Using the pipeline Settings
            public static GUIContent[] displayedCameraOptions =
            {
                new GUIContent("Off"),
                new GUIContent("Use Pipeline Settings"),
            };
            public static int[] cameraOptions = { 0, 1 };

            // Camera Types
            public static List<GUIContent> m_CameraTypeNames = null;
            public static readonly string[] cameraTypeNames = Enum.GetNames(typeof(LWRPCameraType));
            public static int[] additionalDataCameraTypeOptions = Enum.GetValues(typeof(LWRPCameraType)) as int[];
        };


        ReorderableList m_LayerList;

        public Camera camera { get { return target as Camera; } }

        static List<Camera> k_Cameras;

        List<Camera> validCameras = new List<Camera>();
        // This is the valid list of types, so if we need to add more types we just add it here.
        List<LWRPCameraType> validCameraTypes = new List<LWRPCameraType>{LWRPCameraType.Overlay};
        List<Camera> errorCameras = new List<Camera>();
        Texture2D m_ErrorIcon;

        // Temporary saved bools for foldout header
        SavedBool m_CommonCameraSettingsFoldout;
        SavedBool m_EnvironmentSettingsFoldout;
        SavedBool m_OutputSettingsFoldout;
        SavedBool m_RenderingSettingsFoldout;
        SavedBool m_StackSettingsFoldout;

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
        SerializedProperty m_AdditionalCameraDataRendererProp;
        SerializedProperty m_AdditionalCameraDataRendererDataProp;
        SerializedProperty m_AdditionalCameraDataCameraTypeProp;

        SerializedProperty m_AdditionalCameraDataCameras;

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
            SetAnimationTarget(m_ShowBGColorAnim, initialize, isSameClearFlags && (camera.clearFlags == CameraClearFlags.SolidColor));
            SetAnimationTarget(m_ShowOrthoAnim, initialize, isSameOrthographic && camera.orthographic);
            SetAnimationTarget(m_ShowTargetEyeAnim, initialize, settings.targetEye.intValue != (int)StereoTargetEyeMask.Both || PlayerSettings.virtualRealitySupported);
        }

        void UpdateCameraTypeIntPopupData()
        {
            if (Styles.m_CameraTypeNames == null)
            {
                Styles.m_CameraTypeNames = new List<GUIContent>();
                foreach (string typeName in Styles.cameraTypeNames)
                {
                    Styles.m_CameraTypeNames.Add(new GUIContent(typeName));
                }
            }
        }

        public new void OnEnable()
        {
            m_CommonCameraSettingsFoldout = new SavedBool($"{target.GetType()}.CommonCameraSettingsFoldout", false);
            m_EnvironmentSettingsFoldout = new SavedBool($"{target.GetType()}.EnvironmentSettingsFoldout", false);
            m_OutputSettingsFoldout = new SavedBool($"{target.GetType()}.OutputSettingsFoldout", false);
            m_RenderingSettingsFoldout = new SavedBool($"{target.GetType()}.RenderingSettingsFoldout", false);
            m_StackSettingsFoldout = new SavedBool($"{target.GetType()}.StackSettingsFoldout", false);

            m_ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            validCameras.Clear();
            errorCameras.Clear();
            m_LightweightRenderPipeline = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
            settings.OnEnable();

            // Additional Camera Data
            m_AdditionalCameraData = camera.gameObject.GetComponent<LWRPAdditionalCameraData>();
            if (m_AdditionalCameraData == null)
            {
                m_AdditionalCameraData = camera.gameObject.AddComponent<LWRPAdditionalCameraData>();
            }
            init(m_AdditionalCameraData);

            UpdateAnimationValues(true);
            UpdateCameraTypeIntPopupData();
            UpdateCameras();
        }

        void UpdateCameras()
        {
            var o = new PropertyFetcher<LWRPAdditionalCameraData>(m_AdditionalCameraDataSO);
            m_AdditionalCameraDataCameras = o.Find(x => x.cameras);

            var camType = (LWRPCameraType)m_AdditionalCameraDataCameraTypeProp.intValue;
            if (camType == LWRPCameraType.Base)
            {
                m_LayerList = new ReorderableList(m_AdditionalCameraDataSO, m_AdditionalCameraDataCameras, true, false, true, true);
                //m_LayerList.drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Stack"); };
                m_LayerList.drawElementCallback += DrawElementCallback;
                m_LayerList.onSelectCallback += SelectElement;
                m_LayerList.onRemoveCallback = list =>
                {
                    m_AdditionalCameraDataCameras.DeleteArrayElementAtIndex(list.index);
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    m_AdditionalCameraDataSO.ApplyModifiedProperties();
                };

                m_LayerList.onAddDropdownCallback = (rect, list) => AddCameraToCameraList(rect, list);
            }
        }

        void SelectElement(ReorderableList list)
        {
            var element = m_AdditionalCameraDataCameras.GetArrayElementAtIndex(list.index);
            var cam = element.objectReferenceValue as Camera;
            if (Event.current.clickCount == 2)
            {
                Selection.activeObject = cam;
            }

            EditorGUIUtility.PingObject(cam);
        }

        static GUIContent s_TextImage = new GUIContent();
        static GUIContent TempContent(string text, string tooltip, Texture i)
        {
            s_TextImage.image = i;
            s_TextImage.text = text;
            s_TextImage.tooltip = tooltip;
            return s_TextImage;
        }

        GUIContent m_NameContent = new GUIContent();

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 1;

            var element = m_AdditionalCameraDataCameras.GetArrayElementAtIndex(index);

            var cam = element.objectReferenceValue as Camera;
            if (cam != null)
            {
                bool warning = false;
                string warningInfo = "";
                var type = cam.gameObject.GetComponent<LWRPAdditionalCameraData>().cameraType;
                if (!validCameraTypes.Contains(type))
                {
                    warning = true;
                    warningInfo += "Not a supported type";
                    if (!errorCameras.Contains(cam))
                    {
                        errorCameras.Add(cam);
                    }
                }
                else if (errorCameras.Contains(cam))
                {
                    errorCameras.Remove(cam);
                }

                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= 20f;
                if (warning)
                {
                    GUIStyle errorStyle = new GUIStyle(EditorStyles.label) {padding = new RectOffset{left = -16} };
                    m_NameContent.text = cam.name;
                    EditorGUI.LabelField(rect, m_NameContent, TempContent(type.GetName(), warningInfo, m_ErrorIcon), errorStyle);
                }
                else
                {
                    EditorGUI.LabelField(rect, cam.name, type.ToString());
                }

                EditorGUIUtility.labelWidth = labelWidth;
            }
            else
            {
                // Automagicaly deletes the entry if a user has removed a camera from the scene
                m_AdditionalCameraDataCameras.DeleteArrayElementAtIndex(index);
                m_AdditionalCameraDataSO.ApplyModifiedProperties();

                // Need to clean out the errorCamera list here.
                errorCameras.Clear();
            }
        }

        void AddCameraToCameraList(Rect rect, ReorderableList list)
        {
            Camera[] allCameras = new Camera[Camera.allCamerasCount];
            Camera.GetAllCameras(allCameras);
            foreach (var camera in allCameras)
            {
                var component = camera.gameObject.GetComponent<LWRPAdditionalCameraData>();
                if (component != null)
                {
                    if (validCameraTypes.Contains(component.cameraType))
                    {
                        validCameras.Add(camera);
                    }
                }
            }

            var names = new GUIContent[validCameras.Count];

            for(int i = 0; i < validCameras.Count; ++i)
            {
                names[i] = new GUIContent( validCameras[i].name );
            }

            EditorUtility.DisplayCustomMenu(rect, names, -1, AddCameraToCameraListMenuSelected, null);
        }

        void AddCameraToCameraListMenuSelected(object userData, string[] options, int selected)
        {
            var length = m_AdditionalCameraDataCameras.arraySize;
            ++m_AdditionalCameraDataCameras.arraySize;
            m_AdditionalCameraDataCameras.serializedObject.ApplyModifiedProperties();
            m_AdditionalCameraDataCameras.GetArrayElementAtIndex(length).objectReferenceValue = validCameras[selected];
            m_AdditionalCameraDataCameras.serializedObject.ApplyModifiedProperties();
        }

        void init(LWRPAdditionalCameraData additionalCameraData)
        {
            m_AdditionalCameraDataSO = new SerializedObject(additionalCameraData);
            m_AdditionalCameraDataRenderShadowsProp = m_AdditionalCameraDataSO.FindProperty("m_RenderShadows");
            m_AdditionalCameraDataRenderDepthProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresDepthTextureOption");
            m_AdditionalCameraDataRenderOpaqueProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresOpaqueTextureOption");
            m_AdditionalCameraDataRendererProp = m_AdditionalCameraDataSO.FindProperty("m_RendererOverrideOption");
            m_AdditionalCameraDataRendererDataProp = m_AdditionalCameraDataSO.FindProperty("m_RendererData");
            m_AdditionalCameraDataCameraTypeProp = m_AdditionalCameraDataSO.FindProperty("m_CameraType");

            m_AdditionalCameraDataCameras = m_AdditionalCameraDataSO.FindProperty("m_Cameras");
        }

        public void OnDisable()
        {
            m_ShowBGColorAnim.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoAnim.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            m_LightweightRenderPipeline = null;
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

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();
            UpdateAnimationValues(false);

            DrawCameraType();
            EditorGUILayout.Space();

            // Get the type of Camera we are using
            var camType = (LWRPCameraType)m_AdditionalCameraDataCameraTypeProp.intValue;

            // Offscreen Camera
            if (camType == LWRPCameraType.Offscreen)
            {
                DrawCommonSettings();
                DrawRenderingSettings();
                DrawEnvironmentSettings();
                DrawOutputSettings();
            }

            // Game Camera
            if (camType == LWRPCameraType.Base)
            {
                DrawCommonSettings();
                DrawRenderingSettings();
                DrawEnvironmentSettings();
                DrawOutputSettings();
                DrawStackSettings();
                DrawVRSettings();
            }

            // Overlay Camera
            if (camType == LWRPCameraType.Overlay)
            {
                DrawCommonSettings();
                DrawRenderingSettings();
            }

            // UI Camera
            if (camType == LWRPCameraType.ScreenSpaceUI)
            {
                DrawCommonSettings();
            }

        settings.ApplyModifiedProperties();
        }

        void DrawCommonSettings()
        {
            m_CommonCameraSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_CommonCameraSettingsFoldout.value, Styles.commonCameraSettingsText);
            if (m_CommonCameraSettingsFoldout.value)
            {
                settings.DrawProjection();
                settings.DrawClippingPlanes();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawStackSettings()
        {
            m_StackSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_StackSettingsFoldout.value, Styles.stackSettingsText);
            if (m_StackSettingsFoldout.value)
            {
                m_LayerList.DoLayoutList();
                m_AdditionalCameraDataSO.ApplyModifiedProperties();

                if (errorCameras.Any())
                {
                    string errorString = "These cameras are not of a valid type:\n";
                    string validCameras = "";
                    foreach (var errorCamera in errorCameras)
                    {
                        errorString += errorCamera.name + "\n";
                    }

                    foreach (var validCameraType in validCameraTypes)
                    {
                        validCameras += validCameraType + "  ";
                    }
                    errorString += "Valid types are " + validCameras;
                    EditorGUILayout.HelpBox( errorString, MessageType.Warning);
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawEnvironmentSettings()
        {
            m_EnvironmentSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_EnvironmentSettingsFoldout.value, Styles.environmentSettingsText);
            if (m_EnvironmentSettingsFoldout.value)
            {
                DrawClearFlags();
                using (var group = new EditorGUILayout.FadeGroupScope(m_ShowBGColorAnim.faded))
                {
                    if (group.visible)
                    {
                        EditorGUI.indentLevel++;
                        settings.DrawBackgroundColor();
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawRenderingSettings()
        {
            m_RenderingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_RenderingSettingsFoldout.value, Styles.renderingSettingsText);
            if (m_RenderingSettingsFoldout.value)
            {
                var selectedCameraType = (LWRPCameraType)m_AdditionalCameraDataCameraTypeProp.intValue;

                if (selectedCameraType == LWRPCameraType.Overlay)
                {
                    settings.DrawCullingMask();
                    settings.DrawOcclusionCulling();
                }
                else
                {
                    settings.DrawCullingMask();
                    settings.DrawOcclusionCulling();

                    DrawRendererType();
                    DrawOpaqueTexture();
                    DrawDepthTexture();
                    DrawRenderShadows();
                    DrawPriority();
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawOutputSettings()
        {
            m_OutputSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_OutputSettingsFoldout.value, Styles.outputSettingsText);
            if (m_OutputSettingsFoldout.value)
            {
                var selectedCameraType = (LWRPCameraType)m_AdditionalCameraDataCameraTypeProp.intValue;

                if (selectedCameraType == LWRPCameraType.Base)
                {
                    DrawHDR();
                    DrawMSAA();
                    settings.DrawNormalizedViewPort();
                    settings.DrawDynamicResolution();
                    settings.DrawMultiDisplay();
                }
                else if (selectedCameraType == LWRPCameraType.Offscreen)
                {
                    DrawTargetTexture();
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawCameraType()
        {
            LWRPCameraType selectedCameraType;
            selectedCameraType = (LWRPCameraType)m_AdditionalCameraDataCameraTypeProp.intValue;

            EditorGUI.BeginChangeCheck();
            int selCameraType = EditorGUILayout.IntPopup(Styles.cameraType, (int)selectedCameraType, Styles.m_CameraTypeNames.ToArray(), Styles.additionalDataCameraTypeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalCameraDataCameraTypeProp.intValue = selCameraType;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
                UpdateCameras();
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

        void DrawPriority()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.PropertyField(controlRect, settings.depth, Styles.priority);
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

        void DrawRendererType()
        {
            RendererOverrideOption selectedRendererOption;
            m_AdditionalCameraDataSO.Update();
            selectedRendererOption = (RendererOverrideOption) m_AdditionalCameraDataRendererProp.intValue;

            Rect controlRectRendererType = EditorGUILayout.GetControlRect(true);

            EditorGUI.BeginProperty(controlRectRendererType, Styles.rendererType, m_AdditionalCameraDataRendererProp);
            EditorGUI.BeginChangeCheck();
            selectedRendererOption = (RendererOverrideOption)EditorGUI.IntPopup(controlRectRendererType, Styles.rendererType, (int)selectedRendererOption, Styles.displayedRendererTypeOverride, Styles.rendererTypeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalCameraDataRendererProp.intValue = (int)selectedRendererOption;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
            EditorGUI.EndProperty();
        }

        void DrawDepthTexture()
        {
            CameraOverrideOption selectedDepthOption;
            m_AdditionalCameraDataSO.Update();
            selectedDepthOption = (CameraOverrideOption)m_AdditionalCameraDataRenderDepthProp.intValue;
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
                EditorGUI.BeginProperty(controlRectDepth, Styles.requireDepthTexture, m_AdditionalCameraDataRenderDepthProp);
                EditorGUI.BeginChangeCheck();

                selectedDepthOption = (CameraOverrideOption)EditorGUI.IntPopup(controlRectDepth, Styles.requireDepthTexture, (int)selectedDepthOption, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    m_AdditionalCameraDataRenderDepthProp.intValue = (int)selectedDepthOption;
                    m_AdditionalCameraDataSO.ApplyModifiedProperties();
                }
                EditorGUI.EndProperty();
            }
        }

        void DrawOpaqueTexture()
        {
            CameraOverrideOption selectedOpaqueOption;
            m_AdditionalCameraDataSO.Update();
            selectedOpaqueOption =(CameraOverrideOption)m_AdditionalCameraDataRenderOpaqueProp.intValue;

            Rect controlRectColor = EditorGUILayout.GetControlRect(true);

            EditorGUI.BeginProperty(controlRectColor, Styles.requireOpaqueTexture, m_AdditionalCameraDataRenderOpaqueProp);
            EditorGUI.BeginChangeCheck();
            selectedOpaqueOption = (CameraOverrideOption)EditorGUI.IntPopup(controlRectColor, Styles.requireOpaqueTexture, (int)selectedOpaqueOption, Styles.displayedAdditionalDataOptions, Styles.additionalDataOptions);
            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalCameraDataRenderOpaqueProp.intValue = (int)selectedOpaqueOption;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
            EditorGUI.EndProperty();
        }

        void DrawRenderShadows()
        {
            bool selectedValueShadows;
            m_AdditionalCameraDataSO.Update();
            selectedValueShadows = m_AdditionalCameraData.renderShadows;

            Rect controlRectShadows = EditorGUILayout.GetControlRect(true);

            EditorGUI.BeginProperty(controlRectShadows, Styles.renderingShadows, m_AdditionalCameraDataRenderShadowsProp);
            EditorGUI.BeginChangeCheck();

            selectedValueShadows = EditorGUI.Toggle(controlRectShadows, Styles.renderingShadows, selectedValueShadows);
            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalCameraDataRenderShadowsProp.boolValue = selectedValueShadows;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
            EditorGUI.EndProperty();
        }

        void DrawVRSettings()
        {
            settings.DrawVR();
            using (var group = new EditorGUILayout.FadeGroupScope(m_ShowTargetEyeAnim.faded))
                if (group.visible)
                    settings.DrawTargetEye();
        }
    }
}
