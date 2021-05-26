using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.SceneManagement;
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

            public static GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.");
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Render Type", "Controls which type of camera this is.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Makes this camera render shadows.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture) null);
            public static GUIContent priority = EditorGUIUtility.TrTextContent("Priority", "A camera with a higher priority is drawn on top of a camera with a lower priority [ -100, 100 ].");
            public static GUIContent clearDepth = EditorGUIUtility.TrTextContent("Clear Depth", "If enabled, depth from the previous camera will be cleared.");

            public static GUIContent rendererType = EditorGUIUtility.TrTextContent("Renderer", "Controls which renderer this camera uses.");

            public static GUIContent volumeLayerMask = EditorGUIUtility.TrTextContent("Volume Mask", "This camera will only be affected by volumes in the selected scene-layers.");
            public static GUIContent volumeTrigger = EditorGUIUtility.TrTextContent("Volume Trigger", "A transform that will act as a trigger for volume blending. If none is set, the camera itself will act as a trigger.");

            public static GUIContent renderPostProcessing = EditorGUIUtility.TrTextContent("Post Processing", "Enable this to make this camera render post-processing effects.");
            public static GUIContent antialiasing = EditorGUIUtility.TrTextContent("Anti-aliasing", "The anti-aliasing method to use.");
            public static GUIContent antialiasingQuality = EditorGUIUtility.TrTextContent("Quality", "The quality level to use for the selected anti-aliasing method.");
            public static GUIContent stopNaN = EditorGUIUtility.TrTextContent("Stop NaN", "Automatically replaces NaN/Inf in shaders by a black pixel to avoid breaking some effects. This will affect performances and should only be used if you experience NaN issues that you can't fix. Has no effect on GLES2 platforms.");
            public static GUIContent dithering = EditorGUIUtility.TrTextContent("Dithering", "Applies 8-bit dithering to the final render to reduce color banding.");

            public static readonly GUIContent targetTextureLabel = EditorGUIUtility.TrTextContent("Output Texture", "The texture to render this camera into, if none then this camera renders to screen.");

            public static readonly GUIContent cameraStackNotSupportedMessage = EditorGUIUtility.TrTextContent("Camera Stacking not supported.", "The renderer used by this camera doesn't support camera stacking.");

            public static readonly string hdrDisabledWarning = "HDR rendering is disabled in the Universal Render Pipeline asset.";
            public static readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Universal Render Pipeline asset.";

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

        };

        ReorderableList m_LayerList;

        public Camera camera { get { return target as Camera; } }

        static List<Camera> k_Cameras;

        List<Camera> validCameras = new List<Camera>();
        // This is the valid list of types, so if we need to add more types we just add it here.
        List<CameraRenderType> validCameraTypes = new List<CameraRenderType>{CameraRenderType.Overlay};
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
		SerializedProperty m_AdditionalCameraDataCameras;
        SerializedProperty m_AdditionalCameraDataVolumeLayerMask;
        SerializedProperty m_AdditionalCameraDataVolumeTrigger;
        SerializedProperty m_AdditionalCameraDataRenderPostProcessing;
        SerializedProperty m_AdditionalCameraDataAntialiasing;
        SerializedProperty m_AdditionalCameraDataAntialiasingQuality;
        SerializedProperty m_AdditionalCameraDataStopNaN;
        SerializedProperty m_AdditionalCameraDataDithering;
        SerializedProperty m_AdditionalCameraClearDepth;

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

        public new void OnEnable()
        {
            m_UniversalRenderPipeline = UniversalRenderPipeline.asset;

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

            UpdateCameras();
        }
        void UpdateCameras()
        {
            var o = new PropertyFetcher<UniversalAdditionalCameraData>(m_AdditionalCameraDataSO);
            m_AdditionalCameraDataCameras = o.Find("m_Cameras");

            var camType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;
            if (camType == CameraRenderType.Base)
            {
                m_LayerList = new ReorderableList(m_AdditionalCameraDataSO, m_AdditionalCameraDataCameras, true, false, true, true);

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
                var type = cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderType;
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
                    GUIStyle errorStyle = new GUIStyle(EditorStyles.label) { padding = new RectOffset { left = -16 } };
                    m_NameContent.text = cam.name;
                    EditorGUI.LabelField(rect, m_NameContent, TempContent(type.GetName(), warningInfo, m_ErrorIcon), errorStyle);
                }
                else
                {
                    EditorGUI.LabelField(rect, cam.name, type.ToString());

                    // Printing if Post Processing is on or not.
                    var isPostActive = cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing;
                    if (isPostActive)
                    {
                        Rect selectRect = new Rect(rect.width - 20, rect.y, 50, EditorGUIUtility.singleLineHeight);

                        EditorGUI.LabelField(selectRect, "PP");
                    }
                }


                EditorGUIUtility.labelWidth = labelWidth;
            }
            else
            {
                camera.GetComponent<UniversalAdditionalCameraData>().UpdateCameraStack();

                // Need to clean out the errorCamera list here.
                errorCameras.Clear();
            }
        }

        // Modified version of StageHandle.FindComponentsOfType<T>()
        // This version more closely represents unity object referencing restrictions.
        // I added these restrictions:
        // - Can not reference scene object outside scene
        // - Can not reference cross scenes
        // - Can reference child objects if it is prefab
        Camera[] FindCamerasToReference(GameObject gameObject)
        {
            var scene = gameObject.scene;

            var inScene = !EditorUtility.IsPersistent(camera) || scene.IsValid();
            var inPreviewScene = EditorSceneManager.IsPreviewScene(scene) && scene.IsValid();
            var inCurrentScene = !EditorUtility.IsPersistent(camera) && scene.IsValid();

            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            List<Camera> result = new List<Camera>();
            if (!inScene)
            {
                foreach (var camera in cameras)
                {
                    if (camera.transform.IsChildOf(gameObject.transform))
                        result.Add(camera);
                }
            }
            else if (inPreviewScene)
            {
                foreach (var camera in cameras)
                {
                    if (camera.gameObject.scene == scene)
                        result.Add(camera);
                }
            }
            else if (inCurrentScene)
            {
                foreach (var camera in cameras)
                {
                    if (!EditorUtility.IsPersistent(camera) && !EditorSceneManager.IsPreviewScene(camera.gameObject.scene) && camera.gameObject.scene == scene)
                        result.Add(camera);
                }
            }

            return result.ToArray();
        }

        void AddCameraToCameraList(Rect rect, ReorderableList list)
        {
            // Need to do clear the list here otherwise the meu just fills up with more and more entries
            validCameras.Clear();
            var allCameras = FindCamerasToReference(camera.gameObject);
            foreach (var camera in allCameras)
            {
                var component = camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
                if (component != null)
                {
                    if (validCameraTypes.Contains(component.renderType))
                    {
                        validCameras.Add(camera);
                    }
                }
            }

            var names = new GUIContent[validCameras.Count];
            for (int i = 0; i < validCameras.Count; ++i)
            {
                names[i] = new GUIContent((i+1) + " " + validCameras[i].name);
            }

            if (!validCameras.Any())
            {
                names = new GUIContent[1];
                names[0] = new GUIContent("No Overlay Cameras exist.");
            }
            EditorUtility.DisplayCustomMenu(rect, names, -1, AddCameraToCameraListMenuSelected, null);
        }

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
            m_AdditionalCameraClearDepth = m_AdditionalCameraDataSO.FindProperty("m_ClearDepth");
            m_AdditionalCameraDataCameraTypeProp = m_AdditionalCameraDataSO.FindProperty("m_CameraType");

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

            // Get the type of Camera we are using
            CameraRenderType camType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;

            DrawCameraType(camType);
            EditorGUILayout.Space();

            EditorGUI.indentLevel++;

            DrawCommonSettings();
            DrawRenderingSettings(camType);
            DrawEnvironmentSettings(camType);

            // Settings only relevant to base cameras
            if (camType == CameraRenderType.Base)
            {
                DrawOutputSettings();
                DrawStackSettings();
            }

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

        void DrawStackSettings()
        {
            m_StackSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_StackSettingsFoldout.value, Styles.stackSettingsText);
            ScriptableRenderer.RenderingFeatures supportedRenderingFeatures = m_AdditionalCameraData?.scriptableRenderer?.supportedRenderingFeatures;

            if (supportedRenderingFeatures != null && supportedRenderingFeatures.cameraStacking == false)
            {
                EditorGUILayout.HelpBox("The renderer used by this camera doesn't support camera stacking. Only Base camera will render.", MessageType.Warning);
                return;
            }

            // TODO: Warn when MultiPass is active and enabled so we show in the UI camera stacking is not supported.
            // Seems like the stereo rendering mode only changes in playmode. Check the reason so we can enable this check.
//#if ENABLE_VR
//            if (UnityEngine.XR.XRSettings.stereoRenderingMode == UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass)
//            {
//                EditorGUILayout.HelpBox("Camera Stacking is not supported in Multi Pass stereo mode. Only Base camera will render.", MessageType.Warning);
//                return;
//            }
//#endif

#if POST_PROCESSING_STACK_2_0_0_OR_NEWER
            if (m_UniversalRenderPipeline.postProcessingFeatureSet == PostProcessingFeatureSet.PostProcessingV2)
            {
                EditorGUILayout.HelpBox("Camera Stacking is not supported with Post-processing V2. Only Base camera will render.", MessageType.Warning);
                return;
            }
#endif

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
                    EditorGUILayout.HelpBox(errorString, MessageType.Warning);
                }
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawEnvironmentSettings(CameraRenderType camType)
        {
            m_EnvironmentSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_EnvironmentSettingsFoldout.value, Styles.environmentSettingsText);
            if (m_EnvironmentSettingsFoldout.value)
            {
                if (camType == CameraRenderType.Base)
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
                }
                DrawVolumes();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawRenderingSettings(CameraRenderType camType)
        {
            m_RenderingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_RenderingSettingsFoldout.value, Styles.renderingSettingsText);
            if (m_RenderingSettingsFoldout.value)
            {
                DrawRenderer();

                if (camType == CameraRenderType.Base)
                {
                    DrawPostProcessing();
                }
                else if (camType == CameraRenderType.Overlay)
                {
                    DrawPostProcessingOverlay();
                    EditorGUILayout.PropertyField(m_AdditionalCameraClearDepth, Styles.clearDepth);
                    m_AdditionalCameraDataSO.ApplyModifiedProperties();
                }

                DrawRenderShadows();

                if (camType == CameraRenderType.Base)
                {
                    DrawPriority();
                    DrawOpaqueTexture();
                    DrawDepthTexture();
                }

                settings.DrawCullingMask();
                settings.DrawOcclusionCulling();

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawPostProcessingOverlay()
        {
            bool hasChanged = false;
            bool selectedRenderPostProcessing;

            if (m_AdditionalCameraDataSO == null)
            {
                selectedRenderPostProcessing = false;
            }
            else
            {
                m_AdditionalCameraDataSO.Update();
                selectedRenderPostProcessing = m_AdditionalCameraDataRenderPostProcessing.boolValue;
            }

            hasChanged |= DrawToggle(m_AdditionalCameraDataRenderPostProcessing, ref selectedRenderPostProcessing, Styles.renderPostProcessing);

            if (hasChanged)
            {
                if (m_AdditionalCameraDataSO == null)
                {
                    m_AdditionalCameraData = Undo.AddComponent<UniversalAdditionalCameraData>(camera.gameObject);
                    init(m_AdditionalCameraData);
                }

                m_AdditionalCameraDataRenderPostProcessing.boolValue = selectedRenderPostProcessing;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
        }

        void DrawOutputSettings()
        {
            m_OutputSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_OutputSettingsFoldout.value, Styles.outputSettingsText);
            if (m_OutputSettingsFoldout.value)
            {
                DrawTargetTexture();

                if (camera.targetTexture == null)
                {
                    DrawHDR();
                    DrawMSAA();
                    settings.DrawNormalizedViewPort();
                    settings.DrawDynamicResolution();
                    settings.DrawMultiDisplay();
                }
                else
                {
                    settings.DrawNormalizedViewPort();
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                DrawVRSettings();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawCameraType(CameraRenderType camType)
        {
            EditorGUI.BeginChangeCheck();
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.cameraType, m_AdditionalCameraDataCameraTypeProp);
            int selCameraType = EditorGUI.IntPopup(controlRect, Styles.cameraType, (int)camType, Styles.m_CameraTypeNames.ToArray(), Styles.additionalDataCameraTypeOptions);
            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalCameraDataCameraTypeProp.intValue = selCameraType;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
                UpdateCameras();

                // ScriptableRenderContext.SetupCameraProperties still depends on camera target texture
                // In order for overlay camera not to override base camera target texture we null it here
                if (camType == CameraRenderType.Overlay && settings.targetTexture.objectReferenceValue != null)
                    settings.targetTexture.objectReferenceValue = null;
            }
        }

        void DrawClearFlags()
        {
            // Converts between ClearFlags and Background Type.
            BackgroundType backgroundType = GetBackgroundType((CameraClearFlags) settings.clearFlags.intValue);

            EditorGUI.BeginChangeCheck();
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.backgroundType, settings.clearFlags);

            BackgroundType selectedType = (BackgroundType)EditorGUI.IntPopup(controlRect, Styles.backgroundType, (int)backgroundType,
                Styles.cameraBackgroundType, Styles.cameraBackgroundValues);
            EditorGUI.EndProperty();

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

            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.rendererType, m_AdditionalCameraDataRendererProp);

            EditorGUI.showMixedValue = m_AdditionalCameraDataRendererProp.hasMultipleDifferentValues;
            int selectedRenderer = EditorGUI.IntPopup(controlRect, Styles.rendererType, selectedRendererOption, m_UniversalRenderPipeline.rendererDisplayList, UniversalRenderPipeline.asset.rendererIndexList);

            EditorGUI.EndProperty();
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

            if (UniversalRenderPipeline.asset?.postProcessingFeatureSet != PostProcessingFeatureSet.PostProcessingV2)
            {
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
        public void RemoveComponent(Camera camera, IEnumerable<Component> dependencies)
        {
            // do not use keyword is to remove the additional data. It will not work
            dependencies = dependencies.Where(c => c.GetType() != typeof(UniversalAdditionalCameraData));
            if (dependencies.Count() > 0)
            {
                EditorUtility.DisplayDialog("Can't remove component", $"Can't remove Camera because {dependencies.First().GetType().Name} depends on it.", "Ok");
                return;
            }

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
