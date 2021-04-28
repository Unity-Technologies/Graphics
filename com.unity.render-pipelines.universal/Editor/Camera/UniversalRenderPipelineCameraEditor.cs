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
    using Styles = UniversalRenderPipelineCameraUI.Styles;

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

        public new void OnEnable()
        {
            base.OnEnable();
            settings.OnEnable();

            m_SerializedCamera = new UniversalRenderPipelineSerializedCamera(serializedObject);

            m_CommonCameraSettingsFoldout = new SavedBool($"{target.GetType()}.CommonCameraSettingsFoldout", false);
            m_EnvironmentSettingsFoldout = new SavedBool($"{target.GetType()}.EnvironmentSettingsFoldout", false);
            m_OutputSettingsFoldout = new SavedBool($"{target.GetType()}.OutputSettingsFoldout", false);
            m_RenderingSettingsFoldout = new SavedBool($"{target.GetType()}.RenderingSettingsFoldout", false);
            m_StackSettingsFoldout = new SavedBool($"{target.GetType()}.StackSettingsFoldout", false);

            m_ErrorIcon = LoadConsoleIcon(true);
            m_WarningIcon = LoadConsoleIcon(false);
            validCameras.Clear();
            m_TypeErrorCameras.Clear();
            m_OutputWarningCameras.Clear();

            UpdateAnimationValues(true);

            UpdateCameras();
        }

        void UpdateCameras()
        {
            m_SerializedCamera.Refresh();

            m_LayerList = new ReorderableList(m_SerializedCamera.serializedObject, m_SerializedCamera.cameras, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, Styles.cameras),
                drawElementCallback = DrawElementCallback,
                onSelectCallback = SelectElement,
                onRemoveCallback = RemoveCamera,
                onCanRemoveCallback = CanRemoveCamera,
                onAddDropdownCallback = AddCameraToCameraList
            };
        }

        bool CanRemoveCamera(ReorderableList list) => m_SerializedCamera.numCameras > 0;

        void RemoveCamera(ReorderableList list)
        {
            // As multi selection is disabled, selectedIndices will only return 1 element, remove that element from the list
            if (list.selectedIndices.Any())
            {
                m_SerializedCamera.cameras.DeleteArrayElementAtIndex(list.selectedIndices.First());
            }
            else
            {
                // Nothing selected, remove the last item on the list
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }

            // Force update the list as removed camera could been there
            m_TypeErrorCameras.Clear();
            m_OutputWarningCameras.Clear();
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

            (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera)overlayCamera = m_SerializedCamera[index];
            Camera cam = overlayCamera.camera;
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
                if (IsStackCameraOutputDirty(cam, overlayCamera.serializedCamera))
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

                EditorGUI.BeginProperty(rect, GUIContent.none, m_SerializedCamera.cameras.GetArrayElementAtIndex(index));
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

            m_SerializedCamera.cameras.InsertArrayElementAtIndex(m_SerializedCamera.numCameras);
            m_SerializedCamera.cameras.GetArrayElementAtIndex(m_SerializedCamera.numCameras - 1).objectReferenceValue = validCameras[selected];
            m_SerializedCamera.serializedAdditionalDataObject.ApplyModifiedProperties();

            m_SerializedCamera.Refresh();

            (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera)overlayCamera = m_SerializedCamera[m_SerializedCamera.numCameras - 1];
            UpdateStackCameraOutput(overlayCamera.camera, overlayCamera.serializedCamera);
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

            m_SerializedCamera.Update();
            UpdateAnimationValues(false);

            // Get the type of Camera we are using
            CameraRenderType camType = DrawCameraType();

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
            m_SerializedCamera.Apply();
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
                (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera)overlayCamera = m_SerializedCamera[i];
                if (overlayCamera.camera != null)
                    UpdateStackCameraOutput(overlayCamera.camera, overlayCamera.serializedCamera);
            }
        }

        private void UpdateStackCameraOutput(Camera cam, UniversalRenderPipelineSerializedCamera serializedCamera)
        {
            if ((CameraRenderType)serializedCamera.cameraType.intValue == CameraRenderType.Base)
                return;

            serializedCamera.Update();
            Undo.RecordObject(camera, Styles.inspectorOverlayCameraText);

            var serializedCameraSettings = serializedCamera.baseCameraSettings;

            bool isChanged = false;

            // Force same render texture
            RenderTexture targetTexture = settings.targetTexture.objectReferenceValue as RenderTexture;
            if (cam.targetTexture != targetTexture)
            {
                cam.targetTexture = targetTexture;
                isChanged = true;
            }

            // Force same hdr
            bool allowHDR = settings.HDR.boolValue;
            if (cam.allowHDR != allowHDR)
            {
                cam.allowHDR = allowHDR;
                isChanged = true;
            }

            // Force same mssa
            bool allowMSSA = settings.allowMSAA.boolValue;
            if (cam.allowMSAA != allowMSSA)
            {
                cam.allowMSAA = allowMSSA;
                isChanged = true;
            }

            // Force same viewport rect
            Rect rect = settings.normalizedViewPortRect.rectValue;
            if (cam.rect != rect)
            {
                cam.rect = settings.normalizedViewPortRect.rectValue;
                isChanged = true;
            }

            // Force same dynamic resolution
            bool allowDynamicResolution = settings.allowDynamicResolution.boolValue;
            if (serializedCamera.allowDynamicResolution.boolValue != allowDynamicResolution)
            {
                cam.allowDynamicResolution = allowDynamicResolution;
                isChanged = true;
            }

            // Force same target display
            int targetDisplay = settings.targetDisplay.intValue;
            if (cam.targetDisplay != targetDisplay)
            {
                cam.targetDisplay = targetDisplay;
                isChanged = true;
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            // Force same target display
            int selectedValue = !m_SerializedCamera.allowXRRendering.boolValue ? 0 : 1;
            int overlayCameraSelectedValue = !serializedCamera.allowXRRendering.boolValue ? 0 : 1;
            if (overlayCameraSelectedValue != selectedValue)
            {
                serializedCamera.allowXRRendering.boolValue = selectedValue == 1;
                isChanged = true;
            }
#endif

            if (isChanged)
            {
                EditorUtility.SetDirty(cam);
                serializedCamera.Apply();
            }
        }

        private bool IsStackCameraOutputDirty(Camera cam, UniversalRenderPipelineSerializedCamera serializedCamera)
        {
            serializedCamera.Update();

            // Force same render texture
            RenderTexture targetTexture = settings.targetTexture.objectReferenceValue as RenderTexture;
            if (cam.targetTexture != targetTexture)
                return true;

            // Force same hdr
            bool allowHDR = settings.HDR.boolValue;
            if (cam.allowHDR != allowHDR)
                return true;

            // Force same mssa
            bool allowMSSA = settings.allowMSAA.boolValue;
            if (cam.allowMSAA != allowMSSA)
                return true;

            // Force same viewport rect
            Rect rect = settings.normalizedViewPortRect.rectValue;
            if (cam.rect != rect)
                return true;

            // Force same dynamic resolution
            bool allowDynamicResolution = settings.allowDynamicResolution.boolValue;
            if (serializedCamera.allowDynamicResolution.boolValue != allowDynamicResolution)
                return true;

            // Force same target display
            int targetDisplay = settings.targetDisplay.intValue;
            if (cam.targetDisplay != targetDisplay)
                return true;

#if ENABLE_VR && ENABLE_XR_MODULE
            // Force same target display
            int selectedValue = !m_SerializedCamera.allowXRRendering.boolValue ? 0 : 1;
            int overlayCameraSelectedValue = !serializedCamera.allowXRRendering.boolValue ? 0 : 1;
            if (overlayCameraSelectedValue != selectedValue)
                return true;
#endif

            return false;
        }

        void DrawCommonSettings()
        {
            m_CommonCameraSettingsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_CommonCameraSettingsFoldout.value, Styles.projectionSettingsText);
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

            bool cameraStackingAvailable = m_SerializedCamera
                .camerasAdditionalData
                .All(c => c.scriptableRenderer?.supportedRenderingFeatures?.cameraStacking ?? false);

            if (!cameraStackingAvailable)
            {
                EditorGUILayout.HelpBox("The renderer used by this camera doesn't support camera stacking. Only Base camera will render.", MessageType.Warning);
                return;
            }

            if (m_StackSettingsFoldout.value)
            {
                m_LayerList.DoLayoutList();
                m_SerializedCamera.Apply();

                EditorGUI.indentLevel--;
                if (m_TypeErrorCameras.Any())
                {
                    var message = new StringBuilder();
                    message.Append("The type of the following Cameras must be Overlay render type: ");
                    foreach (var camera in m_TypeErrorCameras)
                    {
                        message.Append(camera.name);
                        if (camera != m_TypeErrorCameras.Last())
                            message.Append(", ");
                        else
                            message.Append(".");
                    }

                    CoreEditorUtils.DrawFixMeBox(message.ToString(), MessageType.Error, () => UpdateStackCemerasToOverlay());
                }

                if (m_OutputWarningCameras.Any())
                {
                    var message = new StringBuilder();
                    message.Append("The output properties of this Camera do not match the output properties of the following Cameras: ");
                    foreach (var camera in m_OutputWarningCameras)
                    {
                        message.Append(camera.name);
                        if (camera != m_OutputWarningCameras.Last())
                            message.Append(", ");
                        else
                            message.Append(".");
                    }

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
                    m_SerializedCamera.Apply();
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

        CameraRenderType DrawCameraType()
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

            return camType;
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
                m_SerializedCamera.Apply();
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
                m_SerializedCamera.Apply();
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

                EditorGUILayout.PropertyField(m_SerializedCamera.stopNaNs, Styles.stopNaN);
                showPostProcessWarning &= !ShowPostProcessingWarning(showPostProcessWarning && m_SerializedCamera.stopNaNs.boolValue);
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
