using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.AnimatedValues;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

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

        static class Styles
        {
            // Groups
            public static GUIContent commonCameraSettingsText = EditorGUIUtility.TrTextContent("Projection", "These settings control how the camera views the world.");
            public static GUIContent environmentSettingsText = EditorGUIUtility.TrTextContent("Environment", "These settings control what the camera background looks like.");
            public static GUIContent outputSettingsText = EditorGUIUtility.TrTextContent("Output", "These settings control how the camera output is formatted.");
            public static GUIContent renderingSettingsText = EditorGUIUtility.TrTextContent("Rendering", "These settings control for the specific rendering features for this camera.");
            public static GUIContent stackSettingsText = EditorGUIUtility.TrTextContent("Stack", "The list of overlay cameras assigned to this camera.");

            public static GUIContent backgroundType = EditorGUIUtility.TrTextContent("Background Type", "Controls how to initialize the Camera's background.\n\nSkybox initializes camera with Skybox, defaulting to a background color if no skybox is found.\n\nSolid Color initializes background with the background color.\n\nUninitialized has undefined values for the camera background. Use this only if you are rendering all pixels in the Camera's view.");
            public static GUIContent cameraType = EditorGUIUtility.TrTextContent("Render Type", "Defines if a camera renders directly to a target or overlays on top of another camera’s output. Overlay option is not available when Deferred Render Data is in use.");
            public static GUIContent renderingShadows = EditorGUIUtility.TrTextContent("Render Shadows", "Makes this camera render shadows.");
            public static GUIContent requireDepthTexture = EditorGUIUtility.TrTextContent("Depth Texture", "On makes this camera create a _CameraDepthTexture, which is a copy of the rendered depth values.\nOff makes the camera not create a depth texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent requireOpaqueTexture = EditorGUIUtility.TrTextContent("Opaque Texture", "On makes this camera create a _CameraOpaqueTexture, which is a copy of the rendered view.\nOff makes the camera not create an opaque texture.\nUse Pipeline Settings applies settings from the Render Pipeline Asset.");
            public static GUIContent allowMSAA = EditorGUIUtility.TrTextContent("MSAA", "Use Multi Sample Anti-Aliasing to reduce aliasing.");
            public static GUIContent allowHDR = EditorGUIUtility.TrTextContent("HDR", "High Dynamic Range gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light.", (Texture)null);
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

            public static GUIContent cameras = EditorGUIUtility.TrTextContent("Cameras", "The list of overlay cameras assigned to this camera.");

#if ENABLE_VR && ENABLE_XR_MODULE
            public static GUIContent[] xrTargetEyeOptions =
            {
                new GUIContent("None"),
                new GUIContent("Both"),
            };
            public static int[] xrTargetEyeValues = { 0, 1 };
            public static readonly GUIContent xrTargetEye = EditorGUIUtility.TrTextContent("Target Eye", "Allows XR rendering if target eye sets to both eye. Disable XR for this camera otherwise.");
#endif
            public static readonly GUIContent targetTextureLabel = EditorGUIUtility.TrTextContent("Output Texture", "The texture to render this camera into, if none then this camera renders to screen.");

            public static readonly string hdrDisabledWarning = "HDR rendering is disabled in the Universal Render Pipeline asset.";
            public static readonly string mssaDisabledWarning = "Anti-aliasing is disabled in the Universal Render Pipeline asset.";

            public static readonly string missingRendererWarning = "The currently selected Renderer is missing from the Universal Render Pipeline asset.";
            public static readonly string noRendererError = "There are no valid Renderers available on the Universal Render Pipeline asset.";
            public static readonly string disabledPostprocessing = "Post Processing is currently disabled on the current Universal Render Pipeline renderer.";

            public static GUIContent[] cameraBackgroundType =
            {
                new GUIContent("Skybox"),
                new GUIContent("Solid Color"),
                new GUIContent("Uninitialized"),
            };

            public static int[] cameraBackgroundValues = { 0, 1, 2 };

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

            // Beautified anti-aliasing options
            public static GUIContent[] antialiasingOptions =
            {
                new GUIContent("None"),
                new GUIContent("Fast Approximate Anti-aliasing (FXAA)"),
                new GUIContent("Subpixel Morphological Anti-aliasing (SMAA)"),
            };
            public static int[] antialiasingValues = { 0, 1, 2 };

            public static string inspectorOverlayCameraText = L10n.Tr("Inspector Overlay Camera");
        }

        ReorderableList m_LayerList;

        public Camera camera { get { return target as Camera; } }
        static List<Camera> k_Cameras;

        List<Camera> validCameras = new List<Camera>();
        List<Camera> m_TypeErrorCameras = new List<Camera>();
        List<Camera> m_OutputWarningCameras = new List<Camera>();
        Texture2D m_ErrorIcon;
        Texture2D m_WarningIcon;

        // Temporary saved bools for foldout header
        SavedBool m_CommonCameraSettingsFoldout;
        SavedBool m_EnvironmentSettingsFoldout;
        SavedBool m_OutputSettingsFoldout;
        SavedBool m_RenderingSettingsFoldout;
        SavedBool m_StackSettingsFoldout;

        // Animation Properties
        public bool isSameClearFlags { get { return !settings.clearFlags.hasMultipleDifferentValues; } }
        public bool isSameOrthographic { get { return !settings.orthographic.hasMultipleDifferentValues; } }

        Dictionary<Object, UniversalAdditionalCameraData> m_AdditionalCameraDatas = new Dictionary<Object, UniversalAdditionalCameraData>();

        readonly AnimBool m_ShowBGColorAnim = new AnimBool();
        readonly AnimBool m_ShowOrthoAnim = new AnimBool();
        readonly AnimBool m_ShowTargetEyeAnim = new AnimBool();

        UniversalRenderPipelineSerializedCamera m_SerializedCamera;

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
            SetAnimationTarget(m_ShowTargetEyeAnim, initialize, settings.targetEye.intValue != (int)StereoTargetEyeMask.Both);
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
            base.OnEnable();

            m_CommonCameraSettingsFoldout = new SavedBool($"{target.GetType()}.CommonCameraSettingsFoldout", false);
            m_EnvironmentSettingsFoldout = new SavedBool($"{target.GetType()}.EnvironmentSettingsFoldout", false);
            m_OutputSettingsFoldout = new SavedBool($"{target.GetType()}.OutputSettingsFoldout", false);
            m_RenderingSettingsFoldout = new SavedBool($"{target.GetType()}.RenderingSettingsFoldout", false);
            m_StackSettingsFoldout = new SavedBool($"{target.GetType()}.StackSettingsFoldout", false);

            var additionalCameraList = new List<Object>();
            foreach (var cameraTarget in targets)
            {
                var additionData = (cameraTarget as Component).gameObject.GetComponent<UniversalAdditionalCameraData>();
                if (additionData == null)
                    additionData = (cameraTarget as Component).gameObject.AddComponent<UniversalAdditionalCameraData>();
                m_AdditionalCameraDatas[cameraTarget] = additionData;
                additionalCameraList.Add(additionData);
            }
            m_ErrorIcon = LoadConsoleIcon(true);
            m_WarningIcon = LoadConsoleIcon(false);
            validCameras.Clear();
            m_TypeErrorCameras.Clear();
            m_OutputWarningCameras.Clear();
            settings.OnEnable();

            init(additionalCameraList);

            UpdateAnimationValues(true);
            UpdateCameraTypeIntPopupData();

            UpdateCameras();
        }

        void UpdateCameras()
        {
            m_SerializedCamera.RefreshCameras();

            var camType = (CameraRenderType)m_SerializedCamera.cameraType.intValue;
            if (camType != CameraRenderType.Base)
                return;

            m_LayerList = new ReorderableList(m_SerializedCamera.serializedObject, m_SerializedCamera.cameras, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, Styles.cameras),
                drawElementCallback = DrawElementCallback,
                onSelectCallback = SelectElement,
                onRemoveCallback = list =>
                {
                    m_SerializedCamera.cameras.DeleteArrayElementAtIndex(list.index);
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    m_SerializedCamera.serializedObject.ApplyModifiedProperties();

                    // Force update the list as removed camera could been there
                    m_TypeErrorCameras.Clear();
                    m_OutputWarningCameras.Clear();
                },
                onAddDropdownCallback = AddCameraToCameraList
            };
        }

        void SelectElement(ReorderableList list)
        {
            var element = m_SerializedCamera.cameras.GetArrayElementAtIndex(list.index);
            var cam = element.objectReferenceValue as Camera;
            if (Event.current.clickCount == 2)
            {
                Selection.activeObject = cam;
            }

            EditorGUIUtility.PingObject(cam);
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 1;

            var element = m_SerializedCamera.cameras.GetArrayElementAtIndex(index);

            var cam = element.objectReferenceValue as Camera;
            if (cam != null)
            {
                bool typeError = false;
                var type = cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderType;
                if (type != CameraRenderType.Overlay)
                {
                    typeError = true;
                    if (!m_TypeErrorCameras.Contains(cam))
                    {
                        m_TypeErrorCameras.Add(cam);
                    }
                }
                else if (m_TypeErrorCameras.Contains(cam))
                {
                    m_TypeErrorCameras.Remove(cam);
                }

                bool outputWarning = false;
                if (IsStackCameraOutputDirty(cam))
                {
                    outputWarning = true;
                    if (!m_OutputWarningCameras.Contains(cam))
                    {
                        m_OutputWarningCameras.Add(cam);
                    }
                }
                else if (m_OutputWarningCameras.Contains(cam))
                {
                    m_OutputWarningCameras.Remove(cam);
                }

                GUIContent nameContent =
                    outputWarning ?
                    EditorGUIUtility.TrTextContent(cam.name, "Output properties do not match base camera", m_WarningIcon) :
                    EditorGUIUtility.TrTextContent(cam.name);

                GUIContent typeContent =
                    typeError ?
                    EditorGUIUtility.TrTextContent(type.GetName(), "Not a supported type", m_ErrorIcon) :
                    EditorGUIUtility.TrTextContent(type.GetName());

                EditorGUI.BeginProperty(rect, GUIContent.none, element);
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= 20f;

                using (var iconSizeScope = new EditorGUIUtility.IconSizeScope(new Vector2(rect.height, rect.height)))
                {
                    EditorGUI.LabelField(rect, nameContent, typeContent);
                }

                // Printing if Post Processing is on or not.
                var isPostActive = cam.gameObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing;
                if (isPostActive)
                {
                    Rect selectRect = new Rect(rect.width - 20, rect.y, 50, EditorGUIUtility.singleLineHeight);

                    EditorGUI.LabelField(selectRect, "PP");
                }
                EditorGUI.EndProperty();

                EditorGUIUtility.labelWidth = labelWidth;
            }
            else
            {
                camera.GetComponent<UniversalAdditionalCameraData>().UpdateCameraStack();

                // Need to clean out the errorCamera list here.
                m_TypeErrorCameras.Clear();
                m_OutputWarningCameras.Clear();
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
                    if (component.renderType == CameraRenderType.Overlay)
                    {
                        validCameras.Add(camera);
                    }
                }
            }

            var names = new GUIContent[validCameras.Count];
            for (int i = 0; i < validCameras.Count; ++i)
            {
                names[i] = new GUIContent((i + 1) + " " + validCameras[i].name);
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
            if (!validCameras.Any())
                return;

            var length = m_SerializedCamera.cameras.arraySize;
            ++m_SerializedCamera.cameras.arraySize;
            m_SerializedCamera.cameras.serializedObject.ApplyModifiedProperties();
            m_SerializedCamera.cameras.GetArrayElementAtIndex(length).objectReferenceValue = validCameras[selected];
            m_SerializedCamera.cameras.serializedObject.ApplyModifiedProperties();

            UpdateStackCameraOutput(validCameras[selected]);
        }

        void init(List<Object> additionalCameraData)
        {
            if (additionalCameraData == null)
                return;

            m_SerializedCamera = new UniversalRenderPipelineSerializedCamera(new SerializedObject(additionalCameraData.ToArray()));
        }

        public new void OnDisable()
        {
            base.OnDisable();
            m_ShowBGColorAnim.valueChanged.RemoveListener(Repaint);
            m_ShowOrthoAnim.valueChanged.RemoveListener(Repaint);
            m_ShowTargetEyeAnim.valueChanged.RemoveListener(Repaint);
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
            var rpAsset = UniversalRenderPipeline.asset;
            if (rpAsset == null)
            {
                base.OnInspectorGUI();
                return;
            }

            settings.Update();
            m_SerializedCamera.serializedObject.Update();
            UpdateAnimationValues(false);

            // Get the type of Camera we are using
            CameraRenderType camType = (CameraRenderType)m_SerializedCamera.cameraType.intValue;

            DrawCameraType();

            EditorGUILayout.Space();
            // If we have different cameras selected that are of different types we do not allow multi editing and we do not draw any more UI.
            if (m_SerializedCamera.cameraType.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit cameras of different types.", MessageType.Info);
                return;
            }

            EditorGUI.indentLevel++;

            DrawCommonSettings();
            DrawRenderingSettings(camType, rpAsset);
            DrawEnvironmentSettings(camType);

            if (camType == CameraRenderType.Base)
            {
                // Settings only relevant to base cameras
                EditorGUI.BeginChangeCheck();
                DrawOutputSettings(rpAsset);
                if (EditorGUI.EndChangeCheck())
                    UpdateStackCamerasOutput();
                DrawStackSettings();
            }

            EditorGUI.indentLevel--;
            settings.ApplyModifiedProperties();
            m_SerializedCamera.serializedObject.ApplyModifiedProperties();
        }

        private void UpdateStackCemerasToOverlay()
        {
            int cameraCount = m_SerializedCamera.cameras.arraySize;
            for (int i = 0; i < cameraCount; ++i)
            {
                SerializedProperty cameraProperty = m_SerializedCamera.cameras.GetArrayElementAtIndex(i);

                var camera = cameraProperty.objectReferenceValue as Camera;
                if (camera == null)
                    continue;

                var additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                if (additionalCameraData == null)
                    continue;

                Undo.RecordObject(camera, Styles.inspectorOverlayCameraText);
                if (additionalCameraData.renderType == CameraRenderType.Base)
                {
                    additionalCameraData.renderType = CameraRenderType.Overlay;
                    EditorUtility.SetDirty(camera);
                }
            }
        }

        private void UpdateStackCamerasOutput()
        {
            int cameraCount = m_SerializedCamera.cameras.arraySize;
            for (int i = 0; i < cameraCount; ++i)
            {
                SerializedProperty cameraProperty = m_SerializedCamera.cameras.GetArrayElementAtIndex(i);
                Camera overlayCamera = cameraProperty.objectReferenceValue as Camera;
                if (overlayCamera != null)
                    UpdateStackCameraOutput(overlayCamera);
            }
        }

        private void UpdateStackCameraOutput(Camera camera)
        {
            Undo.RecordObject(camera, Styles.inspectorOverlayCameraText);

            bool isChanged = false;

            // Force same render texture
            RenderTexture targetTexture = settings.targetTexture.objectReferenceValue as RenderTexture;
            if (camera.targetTexture != targetTexture)
            {
                camera.targetTexture = targetTexture;
                isChanged = true;
            }

            // Force same hdr
            bool allowHDR = settings.HDR.boolValue;
            if (camera.allowHDR != allowHDR)
            {
                camera.allowHDR = allowHDR;
                isChanged = true;
            }

            // Force same mssa
            bool allowMSSA = settings.allowMSAA.boolValue;
            if (camera.allowMSAA != allowMSSA)
            {
                camera.allowMSAA = allowMSSA;
                isChanged = true;
            }

            // Force same viewport rect
            Rect rect = settings.normalizedViewPortRect.rectValue;
            if (camera.rect != rect)
            {
                camera.rect = settings.normalizedViewPortRect.rectValue;
                isChanged = true;
            }

            // Force same dynamic resolution
            bool allowDynamicResolution = settings.allowDynamicResolution.boolValue;
            if (camera.allowDynamicResolution != allowDynamicResolution)
            {
                camera.allowDynamicResolution = allowDynamicResolution;
                isChanged = true;
            }

            // Force same target display
            int targetDisplay = settings.targetDisplay.intValue;
            if (camera.targetDisplay != targetDisplay)
            {
                camera.targetDisplay = targetDisplay;
                isChanged = true;
            }

            // Force same target display todo
            StereoTargetEyeMask stereoTargetEye = (StereoTargetEyeMask)settings.targetEye.intValue;
            if (camera.stereoTargetEye != stereoTargetEye)
            {
                camera.stereoTargetEye = stereoTargetEye;
                isChanged = true;
            }

            if (isChanged)
                EditorUtility.SetDirty(camera);
        }

        private bool IsStackCameraOutputDirty(Camera camera)
        {
            // Force same render texture
            RenderTexture targetTexture = settings.targetTexture.objectReferenceValue as RenderTexture;
            if (camera.targetTexture != targetTexture)
                return true;

            // Force same hdr
            bool allowHDR = settings.HDR.boolValue;
            if (camera.allowHDR != allowHDR)
                return true;

            // Force same mssa
            bool allowMSSA = settings.allowMSAA.boolValue;
            if (camera.allowMSAA != allowMSSA)
                return true;

            // Force same viewport rect
            Rect rect = settings.normalizedViewPortRect.rectValue;
            if (camera.rect != rect)
                return true;

            // Force same dynamic resolution
            bool allowDynamicResolution = settings.allowDynamicResolution.boolValue;
            if (camera.allowDynamicResolution != allowDynamicResolution)
                return true;

            // Force same target display
            int targetDisplay = settings.targetDisplay.intValue;
            if (camera.targetDisplay != targetDisplay)
                return true;

            // Force same target display
            StereoTargetEyeMask stereoTargetEye = (StereoTargetEyeMask)settings.targetEye.intValue;
            if (camera.stereoTargetEye != stereoTargetEye)
                return true;

            return false;
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
            if (m_SerializedCamera.cameras.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit stack of multiple cameras.", MessageType.Info);
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            ScriptableRenderer.RenderingFeatures supportedRenderingFeatures = m_AdditionalCameraDatas[target]?.scriptableRenderer?.supportedRenderingFeatures;

            if (supportedRenderingFeatures != null && supportedRenderingFeatures.cameraStacking == false)
            {
                EditorGUILayout.HelpBox("The renderer used by this camera doesn't support camera stacking. Only Base camera will render.", MessageType.Warning);
                return;
            }

            if (m_StackSettingsFoldout.value)
            {
                m_LayerList.DoLayoutList();
                m_SerializedCamera.serializedObject.ApplyModifiedProperties();

                EditorGUI.indentLevel--;
                if (m_TypeErrorCameras.Any())
                {
                    var message = new StringBuilder();
                    foreach (var camera in m_TypeErrorCameras)
                    {
                        message.Append(camera.name);
                        if (camera != m_TypeErrorCameras.Last())
                            message.Append(", ");
                        else
                            message.Append(" ");
                    }
                    message.Append("needs to be Overlay render type.");

                    CoreEditorUtils.DrawFixMeBox(message.ToString(), MessageType.Error, () => UpdateStackCemerasToOverlay());
                }

                if (m_OutputWarningCameras.Any())
                {
                    var message = new StringBuilder();
                    foreach (var camera in m_OutputWarningCameras)
                    {
                        message.Append(camera.name);
                        if (camera != m_OutputWarningCameras.Last())
                            message.Append(", ");
                        else
                            message.Append(" ");
                    }
                    message.Append("output properties do not match base cameras.");

                    CoreEditorUtils.DrawFixMeBox(message.ToString(), MessageType.Warning, () => UpdateStackCamerasOutput());
                }
                EditorGUI.indentLevel++;

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
                    if (!settings.clearFlags.hasMultipleDifferentValues)
                    {
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
                }
                DrawVolumes();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawRenderingSettings(CameraRenderType camType, UniversalRenderPipelineAsset rpAsset)
        {
            m_RenderingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_RenderingSettingsFoldout.value, Styles.renderingSettingsText);
            if (m_RenderingSettingsFoldout.value)
            {
                DrawRenderer(rpAsset);

                if (camType == CameraRenderType.Base)
                {
                    DrawPostProcessing(rpAsset);
                }
                else if (camType == CameraRenderType.Overlay)
                {
                    DrawPostProcessingOverlay(rpAsset);
                    EditorGUILayout.PropertyField(m_SerializedCamera.clearDepth, Styles.clearDepth);
                    m_SerializedCamera.serializedObject.ApplyModifiedProperties();
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

        private bool IsAnyRendererHasPostProcessingEnabled(UniversalRenderPipelineAsset rpAsset)
        {
            int selectedRendererOption = m_SerializedCamera.renderer.intValue;

            if (selectedRendererOption < -1 || selectedRendererOption > rpAsset.m_RendererDataList.Length || m_SerializedCamera.renderer.hasMultipleDifferentValues)
                return false;

            var rendererData = selectedRendererOption == -1 ? rpAsset.m_RendererData : rpAsset.m_RendererDataList[selectedRendererOption];

            var fowardRendererData = rendererData as UniversalRendererData;
            if (fowardRendererData != null && fowardRendererData.postProcessData == null)
                return true;

            var fenderer2DData = rendererData as UnityEngine.Experimental.Rendering.Universal.Renderer2DData;
            if (fenderer2DData != null && fenderer2DData.postProcessData == null)
                return true;

            return false;
        }

        private static Texture2D LoadConsoleIcon(bool isError)
        {
            string pathToIcon = "icons/";

            // Handle different skin
            if (EditorGUIUtility.isProSkin)
                pathToIcon += "d_";

            // Handle different icon
            if (isError)
                pathToIcon += "console.erroricon";
            else
                pathToIcon += "console.warnicon";

            // Handle different resolution
            if (EditorGUIUtility.pixelsPerPoint > 1.0f)
            {
                pathToIcon += "@2x";
            }

            pathToIcon += ".png";

            Texture2D icon = EditorGUIUtility.Load(pathToIcon) as Texture2D;

            return icon;
        }

        void DrawPostProcessingOverlay(UniversalRenderPipelineAsset rpAsset)
        {
            bool isPostProcessingEnabled = IsAnyRendererHasPostProcessingEnabled(rpAsset) && m_SerializedCamera.renderPostProcessing.boolValue;

            EditorGUILayout.PropertyField(m_SerializedCamera.renderPostProcessing, Styles.renderPostProcessing);

            if (isPostProcessingEnabled)
                EditorGUILayout.HelpBox(Styles.disabledPostprocessing, MessageType.Warning);
        }

        void DrawOutputSettings(UniversalRenderPipelineAsset rpAsset)
        {
            m_OutputSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_OutputSettingsFoldout.value, Styles.outputSettingsText);
            if (m_OutputSettingsFoldout.value)
            {
                DrawTargetTexture(rpAsset);

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
#if ENABLE_VR && ENABLE_XR_MODULE
                DrawXRRendering();
#endif
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawCameraType()
        {
            int selectedRenderer = m_SerializedCamera.renderer.intValue;
            ScriptableRenderer scriptableRenderer = UniversalRenderPipeline.asset.GetRenderer(selectedRenderer);
            UniversalRenderer renderer = scriptableRenderer as UniversalRenderer;
            bool isDeferred = renderer != null ? renderer.renderingMode == RenderingMode.Deferred : false;

            EditorGUI.BeginChangeCheck();

            //EditorGUILayout.PropertyField(m_AdditionalCameraDataCameraTypeProp, Styles.cameraType);

            CameraRenderType originalCamType = (CameraRenderType)m_SerializedCamera.cameraType.intValue;
            CameraRenderType camType = (originalCamType != CameraRenderType.Base && isDeferred) ? CameraRenderType.Base : originalCamType;

            camType = (CameraRenderType)EditorGUILayout.EnumPopup(
                Styles.cameraType,
                camType,
                e =>
                {
                    return isDeferred ? (CameraRenderType)e != CameraRenderType.Overlay : true;
                },
                false
            );

            if (EditorGUI.EndChangeCheck() || camType != originalCamType)
            {
                m_SerializedCamera.cameraType.intValue = (int)camType;

                UpdateCameras();
            }
        }

        void DrawClearFlags()
        {
            // Converts between ClearFlags and Background Type.
            BackgroundType backgroundType = GetBackgroundType((CameraClearFlags)settings.clearFlags.intValue);
            EditorGUI.showMixedValue = settings.clearFlags.hasMultipleDifferentValues;

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

                settings.clearFlags.intValue = (int)selectedClearFlags;
            }
        }

        void DrawPriority()
        {
            EditorGUILayout.PropertyField(settings.depth, Styles.priority);
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

#if ENABLE_VR && ENABLE_XR_MODULE
        void DrawXRRendering()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.xrTargetEye, m_SerializedCamera.allowXRRendering);
            int selectedValue = !m_SerializedCamera.allowXRRendering.boolValue ? 0 : 1;
            m_SerializedCamera.allowXRRendering.boolValue = EditorGUI.IntPopup(controlRect, Styles.xrTargetEye, selectedValue, Styles.xrTargetEyeOptions, Styles.xrTargetEyeValues) == 1;
            EditorGUI.EndProperty();
        }

#endif

        void DrawTargetTexture(UniversalRenderPipelineAsset rpAsset)
        {
            EditorGUILayout.PropertyField(settings.targetTexture, Styles.targetTextureLabel);

            if (!settings.targetTexture.hasMultipleDifferentValues && rpAsset != null)
            {
                var texture = settings.targetTexture.objectReferenceValue as RenderTexture;
                int pipelineSamplesCount = rpAsset.msaaSampleCount;

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
            if (m_SerializedCamera == null)
            {
                selectedVolumeLayerMask = 1; // "Default"
                selectedVolumeTrigger = null;
            }
            else
            {
                selectedVolumeLayerMask = m_SerializedCamera.volumeLayerMask.intValue;
                selectedVolumeTrigger = (Transform)m_SerializedCamera.volumeTrigger.objectReferenceValue;
            }

            hasChanged |= DrawLayerMask(m_SerializedCamera.volumeLayerMask, ref selectedVolumeLayerMask, Styles.volumeLayerMask);
            hasChanged |= DrawObjectField(m_SerializedCamera.volumeTrigger, ref selectedVolumeTrigger, Styles.volumeTrigger);

            if (hasChanged)
            {
                m_SerializedCamera.volumeLayerMask.intValue = selectedVolumeLayerMask;
                m_SerializedCamera.volumeTrigger.objectReferenceValue = selectedVolumeTrigger;
                m_SerializedCamera.serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawRenderer(UniversalRenderPipelineAsset rpAsset)
        {
            int selectedRendererOption = m_SerializedCamera.renderer.intValue;
            EditorGUI.BeginChangeCheck();

            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, Styles.rendererType, m_SerializedCamera.renderer);

            EditorGUI.showMixedValue = m_SerializedCamera.renderer.hasMultipleDifferentValues;
            int selectedRenderer = EditorGUI.IntPopup(controlRect, Styles.rendererType, selectedRendererOption, rpAsset.rendererDisplayList, UniversalRenderPipeline.asset.rendererIndexList);
            EditorGUI.EndProperty();
            if (!rpAsset.ValidateRendererDataList())
            {
                EditorGUILayout.HelpBox(Styles.noRendererError, MessageType.Error);
            }
            else if (!rpAsset.ValidateRendererData(selectedRendererOption))
            {
                EditorGUILayout.HelpBox(Styles.missingRendererWarning, MessageType.Warning);
                var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                if (GUI.Button(rect, "Select Render Pipeline Asset"))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GetAssetPath(UniversalRenderPipeline.asset));
                }
                GUILayout.Space(5);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_SerializedCamera.renderer.intValue = selectedRenderer;
                m_SerializedCamera.serializedObject.ApplyModifiedProperties();
            }
        }

        void DrawPostProcessing(UniversalRenderPipelineAsset rpAsset)
        {
            // We want to show post processing warning only once and below the first option
            // This way we will avoid cluttering the camera UI
            bool showPostProcessWarning = IsAnyRendererHasPostProcessingEnabled(rpAsset);

            EditorGUILayout.PropertyField(m_SerializedCamera.renderPostProcessing, Styles.renderPostProcessing);
            showPostProcessWarning &= !ShowPostProcessingWarning(showPostProcessWarning && m_SerializedCamera.renderPostProcessing.boolValue);

            // Draw Final Post-processing
            DrawIntPopup(m_SerializedCamera.antialiasing, Styles.antialiasing, Styles.antialiasingOptions, Styles.antialiasingValues);

            // If AntiAliasing has mixed value we do not draw the sub menu
            if (!m_SerializedCamera.antialiasing.hasMultipleDifferentValues)
            {
                var selectedAntialiasing = (AntialiasingMode)m_SerializedCamera.antialiasing.intValue;

                if (selectedAntialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_SerializedCamera.antialiasingQuality, Styles.antialiasingQuality);
                    if (CoreEditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2))
                        EditorGUILayout.HelpBox("Sub-pixel Morphological Anti-Aliasing isn't supported on GLES2 platforms.", MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
                showPostProcessWarning &= !ShowPostProcessingWarning(showPostProcessWarning && selectedAntialiasing != AntialiasingMode.None);

                EditorGUILayout.PropertyField(m_SerializedCamera.stopNaN, Styles.stopNaN);
                showPostProcessWarning &= !ShowPostProcessingWarning(showPostProcessWarning && m_SerializedCamera.stopNaN.boolValue);
                EditorGUILayout.PropertyField(m_SerializedCamera.dithering, Styles.dithering);
                ShowPostProcessingWarning(showPostProcessWarning && m_SerializedCamera.dithering.boolValue);
            }
        }

        private bool ShowPostProcessingWarning(bool condition)
        {
            if (!condition)
                return false;
            EditorGUILayout.HelpBox(Styles.disabledPostprocessing, MessageType.Warning);
            return true;
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
            where T : Object
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
            EditorGUILayout.PropertyField(m_SerializedCamera.renderDepth, Styles.requireDepthTexture);
        }

        void DrawOpaqueTexture()
        {
            EditorGUILayout.PropertyField(m_SerializedCamera.renderOpaque, Styles.requireOpaqueTexture);
        }

        void DrawIntPopup(SerializedProperty prop, GUIContent style, GUIContent[] optionNames, int[] optionValues)
        {
            var controlRect = BeginProperty(prop, style);

            EditorGUI.BeginChangeCheck();
            var value = EditorGUI.IntPopup(controlRect, style, prop.intValue, optionNames, optionValues);
            if (EditorGUI.EndChangeCheck())
            {
                prop.intValue = value;
            }

            EndProperty();
        }

        Rect BeginProperty(SerializedProperty prop, GUIContent style)
        {
            var controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, style, prop);
            return controlRect;
        }

        void DrawRenderShadows()
        {
            EditorGUILayout.PropertyField(m_SerializedCamera.renderShadows, Styles.renderingShadows);
        }

        void EndProperty()
        {
            if (m_SerializedCamera != null)
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
            if (dependencies.Any())
            {
                EditorUtility.DisplayDialog("Can't remove component", $"Can't remove Camera because {dependencies.First().GetType().Name} depends on it.", "Ok");
                return;
            }

            var isAssetEditing = EditorUtility.IsPersistent(camera);
            try
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StartAssetEditing();
                }
                Undo.SetCurrentGroupName("Remove Universal Camera");
                var additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                if (additionalCameraData != null)
                {
                    Undo.DestroyObjectImmediate(additionalCameraData);
                }
                Undo.DestroyObjectImmediate(camera);
            }
            finally
            {
                if (isAssetEditing)
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }
    }
}
