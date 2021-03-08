using System;
using System.Collections.Generic;
using System.Linq;
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

        ReorderableList m_LayerList;

        public Camera camera { get { return target as Camera; } }
        static List<Camera> k_Cameras;

        List<Camera> validCameras = new List<Camera>();
        // This is the valid list of types, so if we need to add more types we just add it here.
        List<CameraRenderType> validCameraTypes = new List<CameraRenderType> {CameraRenderType.Overlay};
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

        Dictionary<Object, UniversalAdditionalCameraData> m_AdditionalCameraDatas = new Dictionary<Object, UniversalAdditionalCameraData>();
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
#if ENABLE_VR && ENABLE_XR_MODULE
        SerializedProperty m_AdditionalCameraDataAllowXRRendering;
#endif
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
            m_ErrorIcon = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            validCameras.Clear();
            errorCameras.Clear();
            settings.OnEnable();

            init(additionalCameraList);

            UpdateAnimationValues(true);

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

            var length = m_AdditionalCameraDataCameras.arraySize;
            ++m_AdditionalCameraDataCameras.arraySize;
            m_AdditionalCameraDataCameras.serializedObject.ApplyModifiedProperties();
            m_AdditionalCameraDataCameras.GetArrayElementAtIndex(length).objectReferenceValue = validCameras[selected];
            m_AdditionalCameraDataCameras.serializedObject.ApplyModifiedProperties();
        }

        void init(List<Object> additionalCameraData)
        {
            if (additionalCameraData == null)
                return;

            m_AdditionalCameraDataSO = new SerializedObject(additionalCameraData.ToArray());
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
#if ENABLE_VR && ENABLE_XR_MODULE
            m_AdditionalCameraDataAllowXRRendering = m_AdditionalCameraDataSO.FindProperty("m_AllowXRRendering");
#endif
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
            m_AdditionalCameraDataSO.Update();
            UpdateAnimationValues(false);

            // Get the type of Camera we are using
            CameraRenderType camType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;

            DrawCameraType();

            EditorGUILayout.Space();
            // If we have different cameras selected that are of different types we do not allow multi editing and we do not draw any more UI.
            if (m_AdditionalCameraDataCameraTypeProp.hasMultipleDifferentValues)
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
                DrawOutputSettings(rpAsset);
                DrawStackSettings();
            }

            EditorGUI.indentLevel--;
            settings.ApplyModifiedProperties();
            m_AdditionalCameraDataSO.ApplyModifiedProperties();
        }

        void DrawCommonSettings()
        {
            m_CommonCameraSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_CommonCameraSettingsFoldout.value, UniversalRenderPipelineCameraUI.Styles.projectionSettingsText);
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
            m_StackSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_StackSettingsFoldout.value, UniversalRenderPipelineCameraUI.Styles.stackSettingsText);
            if (m_AdditionalCameraDataCameras.hasMultipleDifferentValues)
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
            m_EnvironmentSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_EnvironmentSettingsFoldout.value, UniversalRenderPipelineCameraUI.Styles.environmentSettingsText);
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
            m_RenderingSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_RenderingSettingsFoldout.value, UniversalRenderPipelineCameraUI.Styles.renderingSettingsText);
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
                    EditorGUILayout.PropertyField(m_AdditionalCameraClearDepth, UniversalRenderPipelineCameraUI.Styles.clearDepth);
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

        private bool IsAnyRendererHasPostProcessingEnabled(UniversalRenderPipelineAsset rpAsset)
        {
            int selectedRendererOption = m_AdditionalCameraDataRendererProp.intValue;

            if (selectedRendererOption < -1 || selectedRendererOption > rpAsset.m_RendererDataList.Length || m_AdditionalCameraDataRendererProp.hasMultipleDifferentValues)
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

        void DrawPostProcessingOverlay(UniversalRenderPipelineAsset rpAsset)
        {
            bool isPostProcessingEnabled = IsAnyRendererHasPostProcessingEnabled(rpAsset) && m_AdditionalCameraDataRenderPostProcessing.boolValue;

            EditorGUILayout.PropertyField(m_AdditionalCameraDataRenderPostProcessing, UniversalRenderPipelineCameraUI.Styles.renderPostProcessing);

            if (isPostProcessingEnabled)
                EditorGUILayout.HelpBox(UniversalRenderPipelineCameraUI.Styles.disabledPostprocessing, MessageType.Warning);
        }

        void DrawOutputSettings(UniversalRenderPipelineAsset rpAsset)
        {
            m_OutputSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_OutputSettingsFoldout.value, UniversalRenderPipelineCameraUI.Styles.outputSettingsText);
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
            int selectedRenderer = m_AdditionalCameraDataRendererProp.intValue;
            ScriptableRenderer scriptableRenderer = UniversalRenderPipeline.asset.GetRenderer(selectedRenderer);
            UniversalRenderer renderer = scriptableRenderer as UniversalRenderer;
            bool isDeferred = renderer != null ? renderer.renderingMode == RenderingMode.Deferred : false;

            EditorGUI.BeginChangeCheck();

            CameraRenderType originalCamType = (CameraRenderType)m_AdditionalCameraDataCameraTypeProp.intValue;
            CameraRenderType camType = (originalCamType != CameraRenderType.Base && isDeferred) ? CameraRenderType.Base : originalCamType;

            camType = (CameraRenderType)EditorGUILayout.EnumPopup(
                UniversalRenderPipelineCameraUI.Styles.cameraType,
                camType,
                e =>
                {
                    return isDeferred ? (CameraRenderType)e != CameraRenderType.Overlay : true;
                },
                false
            );

            if (EditorGUI.EndChangeCheck() || camType != originalCamType)
            {
                m_AdditionalCameraDataCameraTypeProp.intValue = (int)camType;

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
            BackgroundType backgroundType = GetBackgroundType((CameraClearFlags)settings.clearFlags.intValue);
            EditorGUI.showMixedValue = settings.clearFlags.hasMultipleDifferentValues;

            EditorGUI.BeginChangeCheck();
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, UniversalRenderPipelineCameraUI.Styles.backgroundType, settings.clearFlags);

            BackgroundType selectedType = (BackgroundType)EditorGUI.IntPopup(controlRect, UniversalRenderPipelineCameraUI.Styles.backgroundType, (int)backgroundType,
                UniversalRenderPipelineCameraUI.Styles.cameraBackgroundType, UniversalRenderPipelineCameraUI.Styles.cameraBackgroundValues);
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
            EditorGUILayout.PropertyField(settings.depth, UniversalRenderPipelineCameraUI.Styles.priority);
        }

        void DrawHDR()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, UniversalRenderPipelineCameraUI.Styles.allowHDR, settings.HDR);
            int selectedValue = !settings.HDR.boolValue ? 0 : 1;
            settings.HDR.boolValue = EditorGUI.IntPopup(controlRect, UniversalRenderPipelineCameraUI.Styles.allowHDR, selectedValue, UniversalRenderPipelineCameraUI.Styles.displayedCameraOptions, UniversalRenderPipelineCameraUI.Styles.cameraOptions) == 1;
            EditorGUI.EndProperty();
        }

        void DrawMSAA()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, UniversalRenderPipelineCameraUI.Styles.allowMSAA, settings.allowMSAA);
            int selectedValue = !settings.allowMSAA.boolValue ? 0 : 1;
            settings.allowMSAA.boolValue = EditorGUI.IntPopup(controlRect, UniversalRenderPipelineCameraUI.Styles.allowMSAA, selectedValue, UniversalRenderPipelineCameraUI.Styles.displayedCameraOptions, UniversalRenderPipelineCameraUI.Styles.cameraOptions) == 1;
            EditorGUI.EndProperty();
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        void DrawXRRendering()
        {
            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, UniversalRenderPipelineCameraUI.Styles.xrTargetEye, m_AdditionalCameraDataAllowXRRendering);
            int selectedValue = !m_AdditionalCameraDataAllowXRRendering.boolValue ? 0 : 1;
            m_AdditionalCameraDataAllowXRRendering.boolValue = EditorGUI.IntPopup(controlRect, UniversalRenderPipelineCameraUI.Styles.xrTargetEye, selectedValue, UniversalRenderPipelineCameraUI.Styles.xrTargetEyeOptions, UniversalRenderPipelineCameraUI.Styles.xrTargetEyeValues) == 1;
            EditorGUI.EndProperty();
        }

#endif

        void DrawTargetTexture(UniversalRenderPipelineAsset rpAsset)
        {
            EditorGUILayout.PropertyField(settings.targetTexture, UniversalRenderPipelineCameraUI.Styles.targetTextureLabel);

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
            if (m_AdditionalCameraDataSO == null)
            {
                selectedVolumeLayerMask = 1; // "Default"
                selectedVolumeTrigger = null;
            }
            else
            {
                selectedVolumeLayerMask = m_AdditionalCameraDataVolumeLayerMask.intValue;
                selectedVolumeTrigger = (Transform)m_AdditionalCameraDataVolumeTrigger.objectReferenceValue;
            }

            hasChanged |= DrawLayerMask(m_AdditionalCameraDataVolumeLayerMask, ref selectedVolumeLayerMask, UniversalRenderPipelineCameraUI.Styles.volumeLayerMask);
            hasChanged |= DrawObjectField(m_AdditionalCameraDataVolumeTrigger, ref selectedVolumeTrigger, UniversalRenderPipelineCameraUI.Styles.volumeTrigger);

            if (hasChanged)
            {
                m_AdditionalCameraDataVolumeLayerMask.intValue = selectedVolumeLayerMask;
                m_AdditionalCameraDataVolumeTrigger.objectReferenceValue = selectedVolumeTrigger;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
        }

        void DrawRenderer(UniversalRenderPipelineAsset rpAsset)
        {
            int selectedRendererOption = m_AdditionalCameraDataRendererProp.intValue;
            EditorGUI.BeginChangeCheck();

            Rect controlRect = EditorGUILayout.GetControlRect(true);
            EditorGUI.BeginProperty(controlRect, UniversalRenderPipelineCameraUI.Styles.rendererType, m_AdditionalCameraDataRendererProp);

            EditorGUI.showMixedValue = m_AdditionalCameraDataRendererProp.hasMultipleDifferentValues;
            int selectedRenderer = EditorGUI.IntPopup(controlRect, UniversalRenderPipelineCameraUI.Styles.rendererType, selectedRendererOption, rpAsset.rendererDisplayList, UniversalRenderPipeline.asset.rendererIndexList);
            EditorGUI.EndProperty();
            if (!rpAsset.ValidateRendererDataList())
            {
                EditorGUILayout.HelpBox(UniversalRenderPipelineCameraUI.Styles.noRendererError, MessageType.Error);
            }
            else if (!rpAsset.ValidateRendererData(selectedRendererOption))
            {
                EditorGUILayout.HelpBox(UniversalRenderPipelineCameraUI.Styles.missingRendererWarning, MessageType.Warning);
                var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                if (GUI.Button(rect, "Select Render Pipeline Asset"))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GetAssetPath(UniversalRenderPipeline.asset));
                }
                GUILayout.Space(5);
            }

            if (EditorGUI.EndChangeCheck())
            {
                m_AdditionalCameraDataRendererProp.intValue = selectedRenderer;
                m_AdditionalCameraDataSO.ApplyModifiedProperties();
            }
        }

        void DrawPostProcessing(UniversalRenderPipelineAsset rpAsset)
        {
            // We want to show post processing warning only once and below the first option
            // This way we will avoid cluttering the camera UI
            bool showPostProcessWarning = IsAnyRendererHasPostProcessingEnabled(rpAsset);

            EditorGUILayout.PropertyField(m_AdditionalCameraDataRenderPostProcessing, UniversalRenderPipelineCameraUI.Styles.renderPostProcessing);
            showPostProcessWarning &= !ShowPostProcessingWarning(showPostProcessWarning && m_AdditionalCameraDataRenderPostProcessing.boolValue);

            // Draw Final Post-processing
            DrawIntPopup(m_AdditionalCameraDataAntialiasing, UniversalRenderPipelineCameraUI.Styles.antialiasing, UniversalRenderPipelineCameraUI.Styles.antialiasingOptions, UniversalRenderPipelineCameraUI.Styles.antialiasingValues);

            // If AntiAliasing has mixed value we do not draw the sub menu
            if (!m_AdditionalCameraDataAntialiasing.hasMultipleDifferentValues)
            {
                var selectedAntialiasing = (AntialiasingMode)m_AdditionalCameraDataAntialiasing.intValue;

                if (selectedAntialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_AdditionalCameraDataAntialiasingQuality, UniversalRenderPipelineCameraUI.Styles.antialiasingQuality);
                    if (CoreEditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2))
                        EditorGUILayout.HelpBox("Sub-pixel Morphological Anti-Aliasing isn't supported on GLES2 platforms.", MessageType.Warning);
                    EditorGUI.indentLevel--;
                }
                showPostProcessWarning &= !ShowPostProcessingWarning(showPostProcessWarning && selectedAntialiasing != AntialiasingMode.None);

                EditorGUILayout.PropertyField(m_AdditionalCameraDataStopNaN, UniversalRenderPipelineCameraUI.Styles.stopNaN);
                showPostProcessWarning &= !ShowPostProcessingWarning(showPostProcessWarning && m_AdditionalCameraDataStopNaN.boolValue);
                EditorGUILayout.PropertyField(m_AdditionalCameraDataDithering, UniversalRenderPipelineCameraUI.Styles.dithering);
                ShowPostProcessingWarning(showPostProcessWarning && m_AdditionalCameraDataDithering.boolValue);
            }
        }

        private bool ShowPostProcessingWarning(bool condition)
        {
            if (!condition)
                return false;
            EditorGUILayout.HelpBox(UniversalRenderPipelineCameraUI.Styles.disabledPostprocessing, MessageType.Warning);
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
            EditorGUILayout.PropertyField(m_AdditionalCameraDataRenderDepthProp, UniversalRenderPipelineCameraUI.Styles.requireDepthTexture);
        }

        void DrawOpaqueTexture()
        {
            EditorGUILayout.PropertyField(m_AdditionalCameraDataRenderOpaqueProp, UniversalRenderPipelineCameraUI.Styles.requireOpaqueTexture);
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
            EditorGUILayout.PropertyField(m_AdditionalCameraDataRenderShadowsProp, UniversalRenderPipelineCameraUI.Styles.renderingShadows);
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
