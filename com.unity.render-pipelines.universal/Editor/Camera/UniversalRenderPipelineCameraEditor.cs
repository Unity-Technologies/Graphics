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

        CameraStackReorderableList m_LayerList;

        public Camera camera { get { return target as Camera; } }
        static List<Camera> k_Cameras;

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

            m_SerializedCamera = new UniversalRenderPipelineSerializedCamera(serializedObject, settings);

            m_CommonCameraSettingsFoldout = new SavedBool($"{target.GetType()}.CommonCameraSettingsFoldout", false);
            m_EnvironmentSettingsFoldout = new SavedBool($"{target.GetType()}.EnvironmentSettingsFoldout", false);
            m_OutputSettingsFoldout = new SavedBool($"{target.GetType()}.OutputSettingsFoldout", false);
            m_RenderingSettingsFoldout = new SavedBool($"{target.GetType()}.RenderingSettingsFoldout", false);
            m_StackSettingsFoldout = new SavedBool($"{target.GetType()}.StackSettingsFoldout", false);

            UpdateAnimationValues(true);

            UpdateCameras();
        }

        void UpdateCameras()
        {
            m_SerializedCamera.Refresh();

            var camType = (CameraRenderType) m_SerializedCamera.cameraType.intValue;
            if (camType != CameraRenderType.Base)
                return;

            m_LayerList = new CameraStackReorderableList(camera, settings, m_SerializedCamera);
            m_LayerList.OnCameraAdded += UpdateStackCameraOutput;
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

        private void UpdateStackCamerasToOverlay()
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
                EditorGUILayout.Space();
                m_LayerList.OnGUI();
                m_SerializedCamera.Apply();

                using (new EditorGUI.IndentLevelScope(-1))
                {
                    if (m_LayerList.hasErrors)
                        CoreEditorUtils.DrawFixMeBox(m_LayerList.errorMessage, MessageType.Error, UpdateStackCamerasToOverlay);

                    if (m_LayerList.hasWarnings)
                        CoreEditorUtils.DrawFixMeBox(m_LayerList.warningMessage, MessageType.Warning, UpdateStackCamerasOutput);
                }

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
