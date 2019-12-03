using System;
using System.Collections.Generic;
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
            // Groups
            public static GUIContent commonCameraSettingsText = EditorGUIUtility.TrTextContent("Projection", "These settings control how the camera views the world.");
            public static GUIContent environmentSettingsText = EditorGUIUtility.TrTextContent("Environment", "These settings control what the camera background looks like.");
            public static GUIContent outputSettingsText = EditorGUIUtility.TrTextContent("Output", "These settings control how the camera output is formatted.");
            public static GUIContent renderingSettingsText = EditorGUIUtility.TrTextContent("Rendering", "These settings control for the specific rendering features for this camera.");
            public static GUIContent stackSettingsText = EditorGUIUtility.TrTextContent("Stack", "The list of overlay cameras assigned to this camera.");
            public static GUIContent volumeSettingsText = EditorGUIUtility.TrTextContent("Environment", "These settings control the Environment.");

            public static GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.");
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Render Mode", "Controls which type of camera this is.");
            public static GUIContent cameraOutput = EditorGUIUtility.TrTextContent("Output Target", "Controls where we are rendering the output to.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Makes this camera render shadows.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture) null);
            public static GUIContent priority = EditorGUIUtility.TrTextContent("Priority", "A camera with a higher priority is drawn on top of a camera with a lower priority [ -100, 100 ].");

            public static GUIContent rendererType = EditorGUIUtility.TrTextContent("Renderer", "Controls which renderer this camera uses.");

            public static GUIContent volumeLayerMask = EditorGUIUtility.TrTextContent("Volume Mask", "This camera will only be affected by volumes in the selected scene-layers.");
            public static GUIContent volumeTrigger = EditorGUIUtility.TrTextContent("Volume Trigger", "A transform that will act as a trigger for volume blending. If none is set, the camera itself will act as a trigger.");

            public static GUIContent renderPostProcessing = EditorGUIUtility.TrTextContent("Post Processing", "Enable this to make this camera render post-processing effects.");
            public static GUIContent antialiasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "The anti-aliasing method to use.");
            public static GUIContent antialiasingQuality = EditorGUIUtility.TrTextContent("Quality", "The quality level to use for the selected anti-aliasing method.");
            public static GUIContent stopNaN = EditorGUIUtility.TrTextContent("Stop NaN", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will affect performances and should only be used if you experience NaN issues that you can't fix. Has no effect on GLES2 platforms.");
            public static GUIContent dithering = EditorGUIUtility.TrTextContent("Dithering", "Applies 8-bit dithering to the final render to reduce color banding.");

            public static readonly GUIContent targetTextureLabel = EditorGUIUtility.TrTextContent("Texture", "The texture to render this camera into.");

            public readonly string hdrDisabledWarning = "HDR rendering is disabled in the Universal Render Pipeline asset.";
            public readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Universal Render Pipeline asset.";

            public static readonly string missingRendererWarning = "The currently selected Renderer is missing form the Universal Render Pipeline asset.";
            public static readonly string noRendererError = "There are no valid Renderers available on the Universal Render Pipeline asset.";

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

            public static int[] additionalDataOptions = Enum.GetValues(typeof(CameraOverrideOption)).Cast<int>().ToArray();

            // Using the pipeline Settings
            public static GUIContent[] displayedCameraOptions =
            {
                new GUIContent("Off"),
                new GUIContent("Use Pipeline Settings"),
            };

            public static int[] cameraOptions = { 0, 1 };

            // Camera Types
            public static List<GUIContent> m_CameraTypeNames = null;
            public static readonly string[] cameraTypeNames = Enum.GetNames(typeof(CameraRenderType));
            public static int[] additionalDataCameraTypeOptions = Enum.GetValues(typeof(CameraRenderType)) as int[];

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

            // Camera Output
            public static List<GUIContent> m_CameraOutputTargets = null;
            public static readonly string[] cameraOutputTargets = Enum.GetNames(typeof(CameraOutput));
            public static int[] additionalDataCameraOutputOptions = Enum.GetValues(typeof(CameraOutput)) as int[];
        };

        ReorderableList m_LayerList;

        public Camera camera { get { return target as Camera; } }

        static List<Camera> k_Cameras;

        List<Camera> validCameras = new List<Camera>();
        // This is the valid list of types, so if we need to add more types we just add it here.
        // MTT: Commented due to not implemented yet
        //List<CameraRenderType> validCameraTypes = new List<CameraRenderType>{CameraRenderType.Overlay};
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
        SerializedProperty m_AdditionalCameraDataCameraTypeProp;
        SerializedProperty m_AdditionalCameraDataCameraOutputProp;
		SerializedProperty m_AdditionalCameraDataCameras;
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
            SetAnimationTarget(m_ShowTargetEyeAnim, initialize, settings.targetEye.intValue != (int)StereoTargetEyeMask.Both || XRGraphics.tryEnable);
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

        void UpdateCameraOutputIntPopupData()
        {
            if (Styles.m_CameraOutputTargets == null)
            {
                Styles.m_CameraOutputTargets = new List<GUIContent>();
                foreach (string outputTarget in Styles.cameraOutputTargets)
                {
                    Styles.m_CameraOutputTargets.Add(new GUIContent(outputTarget));
                }
            }
        }

        public new void OnEnable()
        {
            m_UniversalRenderPipeline = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;

            m_CommonCameraSettingsFoldout = new SavedBool($"{target.GetType()}.CommonCameraSettingsFoldout", false);
            m_EnvironmentSettingsFoldout = new SavedBool($"{target.GetType()}.EnvironmentSettingsFoldout", false);
            m_OutputSettingsFoldout = new SavedBool($"{target.GetType()}.OutputSettingsFoldout", false);
            m_RenderingSettingsFoldout = new SavedBool($"{target.GetType()}.RenderingSettingsFoldout", false);
            m_StackSettingsFoldout = new SavedBool($"{target.GetType()}.StackSettingsFoldout", false);
            m_AdditionalCameraData = camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
            m_ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            validCameras.Clear();
            errorCameras.Clear();
            settings.OnEnable();

            // Additional Camera Data
            if (m_AdditionalCameraData == null)
            {
                m_AdditionalCameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            init(m_AdditionalCameraData);

            UpdateAnimationValues(true);
            UpdateCameraTypeIntPopupData();
            UpdateCameraOutputIntPopupData();

            // MTT: Commented due to not implemented yet
            //UpdateCameras();
        }

        // MTT: Commented due to not implemented yet
//        void UpdateCameras()
//        {
//            var o = new PropertyFetcher<UniversalAdditionalCameraData>(m_AdditionalCameraDataSO);
//            m_AdditionalCameraDataCameras = o.Find(x => x.cameras);
//
//            var camType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;
//            if (camType == CameraRenderType.Base)
//            {
//                m_LayerList = new ReorderableList(m_AdditionalCameraDataSO, m_AdditionalCameraDataCameras, true, false, true, true);
//
//                m_LayerList.drawElementCallback += DrawElementCallback;
//                m_LayerList.onSelectCallback += SelectElement;
//                m_LayerList.onRemoveCallback = list =>
//                {
//                    m_AdditionalCameraDataCameras.DeleteArrayElementAtIndex(list.index);
//                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
//                    m_AdditionalCameraDataSO.ApplyModifiedProperties();
//                };
//
//                m_LayerList.onAddDropdownCallback = (rect, list) => AddCameraToCameraList(rect, list);
//            }
//        }

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

// MTT: Commented due to not implemented yet
//        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
//        {
//            rect.height = EditorGUIUtility.singleLineHeight;
//            rect.y += 1;
//
//            var element = m_AdditionalCameraDataCameras.GetArrayElementAtIndex(index);
//
//            var cam = element.objectReferenceValue as Camera;
//            if (cam != null)
//            {
//                bool warning = false;
//                string warningInfo = "";
//                var type = cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderType;
//                if (!validCameraTypes.Contains(type))
//                {
//                    warning = true;
//                    warningInfo += "Not a supported type";
//                    if (!errorCameras.Contains(cam))
//                    {
//                        errorCameras.Add(cam);
//                    }
//                }
//                else if (errorCameras.Contains(cam))
//                {
//                    errorCameras.Remove(cam);
//                }
//
//                var labelWidth = EditorGUIUtility.labelWidth;
//                EditorGUIUtility.labelWidth -= 20f;
//                if (warning)
//                {
//                    GUIStyle errorStyle = new GUIStyle(EditorStyles.label) {padding = new RectOffset{left = -16} };
//                    m_NameContent.text = cam.name;
//                    EditorGUI.LabelField(rect, m_NameContent, TempContent(type.GetName(), warningInfo, m_ErrorIcon), errorStyle);
//                }
//                else
//                {
//                    EditorGUI.LabelField(rect, cam.name, type.ToString());
//                }
//
//                EditorGUIUtility.labelWidth = labelWidth;
//            }
//            else
//            {
//                // Automagicaly deletes the entry if a user has removed a camera from the scene
//                m_AdditionalCameraDataCameras.DeleteArrayElementAtIndex(index);
//                m_AdditionalCameraDataSO.ApplyModifiedProperties();
//
//                // Need to clean out the errorCamera list here.
//                errorCameras.Clear();
//            }
//        }

        // MTT: Commented due to not implemented yet
//        void AddCameraToCameraList(Rect rect, ReorderableList list)
//        {
//            Camera[] allCameras = new Camera[Camera.allCamerasCount];
//            Camera.GetAllCameras(allCameras);
//            foreach (var camera in allCameras)
//            {
//                var component = camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
//                if (component != null)
//                {
//                    if (validCameraTypes.Contains(component.renderType))
//                    {
//                        validCameras.Add(camera);
//                    }
//                }
//            }
//
//            var names = new GUIContent[validCameras.Count];
//
//            for(int i = 0; i < validCameras.Count; ++i)
//            {
//                names[i] = new GUIContent( validCameras[i].name );
//            }
//
//            if (!validCameras.Any())
//            {
//                names = new GUIContent[1];
//                names[0] = new GUIContent("No Overlay Cameras exists");
//            }
//            EditorUtility.DisplayCustomMenu(rect, names, -1, AddCameraToCameraListMenuSelected, null);
//        }

        void AddCameraToCameraListMenuSelected(object userData, string[] options, int selected)
        {
            if(!validCameras.Any())
                return;

            var length = m_AdditionalCameraDataCameras.arraySize;
            ++m_AdditionalCameraDataCameras.arraySize;
            m_AdditionalCameraDataCameras.serializedObject.ApplyModifiedProperties();
            m_AdditionalCameraDataCameras.GetArrayElementAtIndex(length).objectReferenceValue = validCameras[selected];
            m_AdditionalCameraDataCameras.serializedObject.ApplyModifiedProperties();
        }

        void init(UniversalAdditionalCameraData additionalCameraData)
        {
            if(additionalCameraData == null)
                return;

            m_AdditionalCameraDataSO = new SerializedObject(additionalCameraData);
            m_AdditionalCameraDataRenderShadowsProp = m_AdditionalCameraDataSO.FindProperty("m_RenderShadows");
            m_AdditionalCameraDataRenderDepthProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresDepthTextureOption");
            m_AdditionalCameraDataRenderOpaqueProp = m_AdditionalCameraDataSO.FindProperty("m_RequiresOpaqueTextureOption");
            m_AdditionalCameraDataRendererProp = m_AdditionalCameraDataSO.FindProperty("m_RendererIndex");
            m_AdditionalCameraDataVolumeLayerMask = m_AdditionalCameraDataSO.FindProperty("m_VolumeLayerMask");
            m_AdditionalCameraDataVolumeTrigger = m_AdditionalCameraDataSO.FindProperty("m_VolumeTrigger");
            m_AdditionalCameraDataRenderPostProcessing = m_AdditionalCameraDataSO.FindProperty("m_RenderPostProcessing");
            m_AdditionalCameraDataAntialiasing = m_AdditionalCameraDataSO.FindProperty("m_Antialiasing");
            m_AdditionalCameraDataAntialiasingQuality = m_AdditionalCameraDataSO.FindProperty("m_AntialiasingQuality");
            m_AdditionalCameraDataStopNaN = m_AdditionalCameraDataSO.FindProperty("m_StopNaN");
            m_AdditionalCameraDataDithering = m_AdditionalCameraDataSO.FindProperty("m_Dithering");
            m_AdditionalCameraDataCameraTypeProp = m_AdditionalCameraDataSO.FindProperty("m_CameraType");
            m_AdditionalCameraDataCameraOutputProp = m_AdditionalCameraDataSO.FindProperty("m_CameraOutput");

            m_AdditionalCameraDataCameras = m_AdditionalCameraDataSO.FindProperty("m_Cameras");
        }

        public void OnDisable()
        {
            m_ShowBGColorAnim.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoAnim.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeAnim.valueChanged.RemoveListener(Repaint);

            m_UniversalRenderPipeline = null;
        }

        BackgroundType GetBackgroundType(CameraClearFlags clearFlags)
        {
            switch (clearFlags)
            {
                case CameraClearFlags.Skybox:
                    return BackgroundType.Skybox;
                case CameraClearFlags.Nothing:
                    return BackgroundType.DontCare;

                // DepthOnly is not supported by design in UniversalRP. We upgrade it to SolidColor
                default:
                    return BackgroundType.SolidColor;
            }
        }

        public override void OnInspectorGUI()
        {
            if(m_UniversalRenderPipeline == null)
			{
				EditorGUILayout.HelpBox("Universal RP asset not assigned, assign one in the Graphics Settings.", MessageType.Error);
                return;
			}

            if (s_Styles == null)
                s_Styles = new Styles();

            settings.Update();
            UpdateAnimationValues(false);

            DrawCameraType();
            EditorGUILayout.Space();

            EditorGUI.indentLevel++;
            // Get the type of Camera we are using
            var camType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;

            // Game Camera
            if (camType == CameraRenderType.Base)
            {
                DrawCommonSettings();
                DrawRenderingSettings();
                DrawEnvironmentSettings();
                DrawOutputSettings();
                //DrawStackSettings();
                DrawVRSettings();
            }

            // MTT: Commented due to not implemented yet
            // Overlay Camera
//            if (camType == CameraRenderType.Overlay)
//            {
//                DrawCommonSettings();
//                DrawRenderingSettings();
//            }
//
//            // UI Camera
//            if (camType == CameraRenderType.ScreenSpaceUI)
//            {
//                DrawCommonSettings();
//            }

            EditorGUI.indentLevel--;
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

        // MTT: Commented due to not implemented yet
//        void DrawStackSettings()
//        {
//            m_StackSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_StackSettingsFoldout.value, Styles.stackSettingsText);
//            if (m_StackSettingsFoldout.value)
//            {
//                m_LayerList.DoLayoutList();
//                m_AdditionalCameraDataSO.ApplyModifiedProperties();
//
//                if (errorCameras.Any())
//                {
//                    string errorString = "These cameras are not of a valid type:\n";
//                    string validCameras = "";
//                    foreach (var errorCamera in errorCameras)
//                    {
//                        errorString += errorCamera.name + "\n";
//                    }
//
//                    foreach (var validCameraType in validCameraTypes)
//                    {
//                        validCameras += validCameraType + "  ";
//                    }
//                    errorString += "Valid types are " + validCameras;
//                    EditorGUILayout.HelpBox( errorString, MessageType.Warning);
//                }
//                EditorGUILayout.Space();
//                EditorGUILayout.Space();
//            }
//            EditorGUILayout.EndFoldoutHeaderGroup();
//        }

        void DrawEnvironmentSettings()
        {
            m_EnvironmentSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_EnvironmentSettingsFoldout.value, Styles.environmentSettingsText);
            if (m_EnvironmentSettingsFoldout.value)
            {
                DrawClearFlags();

                if (GetBackgroundType((CameraClearFlags)settings.clearFlags.intValue) == BackgroundType.SolidColor)
                {
                    using (var group = new EditorGUILayout.FadeGroupScope(m_ShowBGColorAnim.faded))
                    {
                        if (group.visible)
                        {
                            settings.DrawBackgroundColor();
                        }
                    }
                }
                DrawVolumes();
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
                DrawRenderer();

                var selectedCameraType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;

                // MTT: Commented due to not implemented yet
//                if (selectedCameraType == CameraRenderType.Overlay)
//                {
//                    settings.DrawCullingMask();
//                    settings.DrawOcclusionCulling();
//                }
//                else
//                {
                    if (selectedCameraType == CameraRenderType.Base)
                    {
                        DrawPostProcessing();
                    }
                    settings.DrawCullingMask();
                    settings.DrawOcclusionCulling();

                    DrawOpaqueTexture();
                    DrawDepthTexture();
                    DrawRenderShadows();
                    DrawPriority();
                // MTT: Commented due to not implemented yet
                //}
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
                // If there is an output texture we convert it to output target texture camera and only check this if
                // target camera isn't already out-puting to texture
                if (camera.targetTexture != null && m_AdditionalCameraDataCameraOutputProp.intValue != (int)CameraOutput.Texture)
                {
                    m_AdditionalCameraDataCameraOutputProp.intValue = (int)CameraOutput.Texture;
                }

                int selectedCameraOutput = m_AdditionalCameraDataCameraOutputProp.intValue;

                EditorGUI.BeginChangeCheck();
                int selCameraOutput = EditorGUILayout.IntPopup(Styles.cameraOutput, selectedCameraOutput, Styles.m_CameraOutputTargets.ToArray(), Styles.additionalDataCameraOutputOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    m_AdditionalCameraDataCameraOutputProp.intValue = selCameraOutput;
                    if (selCameraOutput == (int)CameraOutput.Camera)
                    {
                        settings.targetTexture.objectReferenceValue = null;
                    }
                    m_AdditionalCameraDataSO.ApplyModifiedProperties();
                }

                CameraOutput selectedOutput = (CameraOutput)m_AdditionalCameraDataCameraOutputProp.intValue;
                if (selectedOutput == CameraOutput.Camera)
                {
                    // If output is Camera we do default
                    DrawHDR();
                    DrawMSAA();
                    settings.DrawNormalizedViewPort();
                    settings.DrawDynamicResolution();
                    settings.DrawMultiDisplay();
                }
                else if (selectedOutput == CameraOutput.Texture)
                {
                    // Else we have Texture and show DrawTargetTexture()
                    DrawTargetTexture();
                }

                // Third option comes later.

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawCameraType()
        {
            CameraRenderType selectedCameraType;
            selectedCameraType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;

            EditorGUI.BeginChangeCheck();
            int selCameraType = EditorGUILayout.IntPopup(Styles.cameraType, (int)selectedCameraType, Styles.m_CameraTypeNames.ToArray(), Styles.additionalDataCameraTypeOptions);
            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalCameraDataCameraTypeProp.intValue = selCameraType;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
                // MTT: Commented due to not implemented yet
                //UpdateCameras();
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
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(settings.targetTexture, Styles.targetTextureLabel);

            if (!settings.targetTexture.hasMultipleDifferentValues && m_UniversalRenderPipeline != null)
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

            EditorGUI.indentLevel--;
        }

        void DrawVolumes()
        {
            bool hasChanged = false;
            LayerMask selectedVolumeLayerMask;
            Transform selectedVolumeTrigger;
            if (m_AdditionalCameraDataSO == null)
            {
                selectedVolumeLayerMask = 1; // "Default"
                selectedVolumeTrigger = null;
            }
            else
            {
                m_AdditionalCameraDataSO.Update();
                selectedVolumeLayerMask = m_AdditionalCameraDataVolumeLayerMask.intValue;
                selectedVolumeTrigger = (Transform)m_AdditionalCameraDataVolumeTrigger.objectReferenceValue;
            }

            hasChanged |= DrawLayerMask(m_AdditionalCameraDataVolumeLayerMask, ref selectedVolumeLayerMask, Styles.volumeLayerMask);
            hasChanged |= DrawObjectField(m_AdditionalCameraDataVolumeTrigger, ref selectedVolumeTrigger, Styles.volumeTrigger);

            if (hasChanged)
            {
                if (m_AdditionalCameraDataSO == null)
                {
                    m_AdditionalCameraData = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
                    init(m_AdditionalCameraData);
                }

                m_AdditionalCameraDataVolumeLayerMask.intValue = selectedVolumeLayerMask;
                m_AdditionalCameraDataVolumeTrigger.objectReferenceValue = selectedVolumeTrigger;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
        }

        void DrawRenderer()
        {
            int selectedRendererOption;
            if (m_AdditionalCameraDataSO == null)
            {
                selectedRendererOption = -1;
            }
            else
            {
                m_AdditionalCameraDataSO.Update();
                selectedRendererOption = m_AdditionalCameraDataRendererProp.intValue;
            }

            EditorGUI.BeginChangeCheck();

            int selectedRenderer = EditorGUILayout.IntPopup(Styles.rendererType, selectedRendererOption, m_UniversalRenderPipeline.rendererDisplayList, UniversalRenderPipeline.asset.rendererIndexList);
            if (!m_UniversalRenderPipeline.ValidateRendererDataList())
            {
                EditorGUILayout.HelpBox(Styles.noRendererError, MessageType.Error);
            }
            else if (!m_UniversalRenderPipeline.ValidateRendererData(selectedRendererOption))
            {
                EditorGUILayout.HelpBox(Styles.missingRendererWarning, MessageType.Warning);
            }

            if (EditorGUI.EndChangeCheck())
            {
                if (m_AdditionalCameraDataSO == null)
                {
                    m_AdditionalCameraData = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
                    init(m_AdditionalCameraData);
                }

                m_AdditionalCameraDataRendererProp.intValue = selectedRenderer;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
        }

        void DrawPostProcessing()
        {
            bool hasChanged = false;
            bool selectedRenderPostProcessing;
            AntialiasingMode selectedAntialiasing;
            AntialiasingQuality selectedAntialiasingQuality;
            bool selectedStopNaN;
            bool selectedDithering;

            if (m_AdditionalCameraDataSO == null)
            {
                selectedRenderPostProcessing = false;
                selectedAntialiasing = AntialiasingMode.None;
                selectedAntialiasingQuality = AntialiasingQuality.High;
                selectedStopNaN = false;
                selectedDithering = false;
            }
            else
            {
                m_AdditionalCameraDataSO.Update();
                selectedRenderPostProcessing = m_AdditionalCameraDataRenderPostProcessing.boolValue;
                selectedAntialiasing = (AntialiasingMode)m_AdditionalCameraDataAntialiasing.intValue;
                selectedAntialiasingQuality = (AntialiasingQuality)m_AdditionalCameraDataAntialiasingQuality.intValue;
                selectedStopNaN = m_AdditionalCameraDataStopNaN.boolValue;
                selectedDithering = m_AdditionalCameraDataDithering.boolValue;
            }

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

                hasChanged |= DrawToggle(m_AdditionalCameraDataStopNaN, ref selectedStopNaN, Styles.stopNaN);
                hasChanged |= DrawToggle(m_AdditionalCameraDataDithering, ref selectedDithering, Styles.dithering);
                EditorGUI.indentLevel--;
            }

            if (hasChanged)
            {
                if (m_AdditionalCameraDataSO == null)
                {
                    m_AdditionalCameraData = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
                    init(m_AdditionalCameraData);
                }

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
            var defaultVal = value;
            bool hasChanged = false;
            var controlRect = BeginProperty(prop, style);

            EditorGUI.BeginChangeCheck();
            value = (T)EditorGUI.ObjectField(controlRect, style, value, typeof(T), true);
            if (EditorGUI.EndChangeCheck() && !Equals(defaultVal, value))
            {
                hasChanged = true;
            }

            EndProperty();
            return hasChanged;
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
            var propValue = (int)selectedDepthOption;
            if ((propValue == 2 && !m_UniversalRenderPipeline.supportsCameraDepthTexture) || propValue == 0)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.IntPopup(controlRectDepth, Styles.requireDepthTexture, 0, Styles.displayedDepthTextureOverride, Styles.additionalDataOptions);
                EditorGUI.EndDisabledGroup();
                defaultDrawOfDepthTextureUI = false;
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

        bool DrawIntPopup<T>(SerializedProperty prop, ref T value, GUIContent style, GUIContent[] optionNames, int[] optionValues)
            where T : Enum
        {
            var defaultVal = value;
            bool hasChanged = false;
            var controlRect = BeginProperty(prop, style);

            EditorGUI.BeginChangeCheck();
            value = (T)(object)EditorGUI.IntPopup(controlRect, style, (int)(object)value, optionNames, optionValues);
            if (EditorGUI.EndChangeCheck() && !Equals(defaultVal, value))
            {
                hasChanged = true;
            }

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

        void EndProperty()
        {
            if (m_AdditionalCameraDataSO != null)
                EditorGUI.EndProperty();
        }
    }

    [ScriptableRenderPipelineExtension(typeof(UniversalRenderPipelineAsset))]
    class UniversalRenderPipelineCameraContextualMenu : IRemoveAdditionalDataContextualMenu<Camera>
    {
        //The call is delayed to the dispatcher to solve conflict with other SRP
        public void RemoveComponent(Camera camera)
        {
            Undo.SetCurrentGroupName("Remove Universal Camera");
            var additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData)
            {
                Undo.DestroyObjectImmediate(additionalCameraData);
            }
            Undo.DestroyObjectImmediate(camera);
        }
    }
}
