using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using Styles = UniversalRenderPipelineCameraUI.Styles;

    [CustomEditor(typeof(Camera))]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class UniversalRenderPipelineCameraEditor : Editor
    {
        ReorderableList m_LayerList;
        
        CameraEditor.Settings m_Settings;
        protected CameraEditor.Settings settings => m_Settings ??= new CameraEditor.Settings(serializedObject);
        
        public Camera camera => target as Camera;
        static Camera selectedCameraInStack;

        List<Camera> validCameras = new List<Camera>();
        List<Camera> m_TypeErrorCameras = new List<Camera>();
        List<Camera> m_NotSupportedOverlayCameras = new List<Camera>();
        List<Camera> m_IncompatibleCameras = new List<Camera>();
        List<(Camera, UniversalRenderPipelineSerializedCamera)> m_OutputWarningCameras = new();

        UniversalRenderPipelineSerializedCamera m_SerializedCamera;

        public void OnEnable()
        {
            settings.OnEnable();
            selectedCameraInStack = null;
            m_SerializedCamera = new UniversalRenderPipelineSerializedCamera(serializedObject, settings);

            validCameras.Clear();
            m_TypeErrorCameras.Clear();
            m_NotSupportedOverlayCameras.Clear();
            m_IncompatibleCameras.Clear();
            m_OutputWarningCameras.Clear();

            UpdateCameras();

            Undo.undoRedoPerformed += ReconstructReferenceToAdditionalDataSO;
        }

        void ReconstructReferenceToAdditionalDataSO()
        {
            OnDisable();
            OnEnable();
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
            m_NotSupportedOverlayCameras.Clear();
            m_IncompatibleCameras.Clear();
            m_OutputWarningCameras.Clear();
        }

        void SelectElement(ReorderableList list)
        {
            var element = m_SerializedCamera.cameras.GetArrayElementAtIndex(list.index);
            selectedCameraInStack = element.objectReferenceValue as Camera;
            if (Event.current.clickCount == 2)
            {
                Selection.activeObject = selectedCameraInStack;
            }

            EditorGUIUtility.PingObject(selectedCameraInStack);
        }

        void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            rect.y += 1;

            (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera) overlayCamera = m_SerializedCamera[index];
            Camera cam = overlayCamera.camera;

            if (cam != null)
            {
                var baseAdditionalData = camera.GetUniversalAdditionalCameraData();
                bool outputWarning = false;

                // Checking if the Base Camera and the overlay cameras are of the same type.
                // If not, we report an error.
                var overlayAdditionalData = cam.GetUniversalAdditionalCameraData();
                var type = overlayAdditionalData.renderType;

                GUIContent errorContent = EditorGUIUtility.TrTextContent(type.GetName()); ;


                var renderer = overlayAdditionalData.scriptableRenderer;

                if (baseAdditionalData.scriptableRenderer.GetType() != renderer.GetType())
                {
                    if (!m_IncompatibleCameras.Contains(cam))
                    {
                        m_IncompatibleCameras.Add(cam);
                    }

                    errorContent = EditorGUIUtility.TrTextContent("",
                        $"Only cameras with compatible renderer types can be stacked. " +
                        $"The camera: {cam.name} are using the renderer {renderer.GetType().Name}, " +
                        $"but the base camera: {camera.name} are using {baseAdditionalData.scriptableRenderer.GetType().Name}. Will skip rendering", CoreEditorStyles.iconFail);
                }
                else if (m_IncompatibleCameras.Contains(cam))
                {
                    m_IncompatibleCameras.Remove(cam);
                }

                // Check if the renderer on the camera we are checking does indeed support overlay camera
                // This can fail due to changing the renderer in the UI to a renderer that does not support overlay cameras
                // The UI will not stop you from changing the renderer sadly so this will have to tell the user that the
                // entry in the stack now is invalid.
                else if (!renderer.SupportsCameraStackingType(CameraRenderType.Overlay))
                {
                    if (!m_NotSupportedOverlayCameras.Contains(cam))
                    {
                        m_NotSupportedOverlayCameras.Add(cam);
                    }

                    errorContent = EditorGUIUtility.TrTextContent("",
                        $"The camera: {cam.name} is using a renderer of type {renderer.GetType().Name} which does not support Overlay cameras in it's current state.", CoreEditorStyles.iconFail);
                }
                else if (m_NotSupportedOverlayCameras.Contains(cam))
                {
                    m_NotSupportedOverlayCameras.Remove(cam);
                }

                else if (type != CameraRenderType.Overlay)
                {
                    if (!m_TypeErrorCameras.Contains(cam))
                    {
                        m_TypeErrorCameras.Add(cam);
                    }
                    errorContent = EditorGUIUtility.TrTextContent(type.GetName(), $"Stack can only contain Overlay cameras. The camera: {cam.name} " +
                                                                                    $"has a type {type} that is not supported. Will skip rendering.",
                        CoreEditorStyles.iconFail);
                }
                else if (m_TypeErrorCameras.Contains(cam))
                {
                    m_TypeErrorCameras.Remove(cam);
                }

                if (IsStackCameraOutputDirty(cam, overlayCamera.serializedCamera))
                {
                    outputWarning = true;
                    if (!m_OutputWarningCameras.Exists(c => c.Item1 == cam))
                    {
                        m_OutputWarningCameras.Add((cam, overlayCamera.serializedCamera));
                    }
                }
                else
                {

                    m_OutputWarningCameras.RemoveAll(c => c.Item1 == cam);
                }


                GUIContent nameContent =
                    outputWarning ?
                    EditorGUIUtility.TrTextContent(cam.name, "Output properties do not match base camera", CoreEditorStyles.iconWarn) :
                    EditorGUIUtility.TrTextContent(cam.name);

                EditorGUI.BeginProperty(rect, GUIContent.none, m_SerializedCamera.cameras.GetArrayElementAtIndex(index));
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= 20f;

                using (var iconSizeScope = new EditorGUIUtility.IconSizeScope(new Vector2(rect.height, rect.height)))
                {
                    EditorGUI.LabelField(rect, nameContent, errorContent);
                }

                // Printing if Post Processing is on or not.
                var isPostActive = cam.GetUniversalAdditionalCameraData().renderPostProcessing;
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
                camera.GetUniversalAdditionalCameraData().UpdateCameraStack();

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
            // Need to do clear the list here otherwise the menu just fills up with more and more entries
            validCameras.Clear();
            // Need to get the base renderer here first
            var renderer = camera.GetUniversalAdditionalCameraData().scriptableRenderer.GetType();
            var allCameras = FindCamerasToReference(camera.gameObject);
            foreach (var camera in allCameras)
            {
                var component = camera.GetUniversalAdditionalCameraData();
                if (component != null)
                {
                    if (component.renderType == CameraRenderType.Overlay &&
                        component.scriptableRenderer.GetType() == renderer)
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

            (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera) overlayCamera = m_SerializedCamera[m_SerializedCamera.numCameras - 1];
            UpdateStackCameraOutput(overlayCamera.camera, overlayCamera.serializedCamera);
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= ReconstructReferenceToAdditionalDataSO;
        }

        // IsPreset is an internal API - lets reuse the usable part of this function
        // 93 is a "magic number" and does not represent a combination of other flags here
        internal static bool IsPresetEditor(UnityEditor.Editor editor)
        {
            return (int)((editor.target as Component).gameObject.hideFlags) == 93;
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

            if (IsPresetEditor(this))
            {
                UniversalRenderPipelineCameraUI.PresetInspector.Draw(m_SerializedCamera, this);
            }
            else
            {
                UniversalRenderPipelineCameraUI.Inspector.Draw(m_SerializedCamera, this);
            }

            m_SerializedCamera.Apply();
        }

        private void UpdateStackCameraToOverlay()
        {
            var additionalCameraData = selectedCameraInStack.GetUniversalAdditionalCameraData();
            if (additionalCameraData == null)
                return;

            if (additionalCameraData.renderType == CameraRenderType.Base)
            {
                Undo.RecordObject(additionalCameraData, Styles.inspectorOverlayCameraText);
                additionalCameraData.renderType = CameraRenderType.Overlay;
                EditorUtility.SetDirty(additionalCameraData);
            }
        }

        private void UpdateStackCameraOutput(Camera cam, UniversalRenderPipelineSerializedCamera serializedCamera)
        {
            if ((CameraRenderType)serializedCamera.cameraType.intValue == CameraRenderType.Base)
                return;

            serializedCamera.Update();
            Undo.RecordObject(camera, Styles.inspectorOverlayCameraText);

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

        internal void DrawStackSettings()
        {
            if (m_SerializedCamera.cameras.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Cannot multi edit stack of multiple cameras.", MessageType.Info);
                EditorGUILayout.EndFoldoutHeaderGroup();
                return;
            }

            bool cameraStackingAvailable = m_SerializedCamera
                .camerasAdditionalData
                .All(c => c.scriptableRenderer?.SupportsCameraStackingType(CameraRenderType.Base) ?? false);

            if (!cameraStackingAvailable)
            {
                EditorGUILayout.HelpBox("The renderer used by this camera doesn't support camera stacking. Only Base camera will render.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();

            m_LayerList.DoLayoutList();
            m_SerializedCamera.Apply();

            EditorGUI.indentLevel--;

            bool oldRichTextSupport = EditorStyles.helpBox.richText;
            EditorStyles.helpBox.richText = true;

            if (selectedCameraInStack != null)
            {
                if (m_IncompatibleCameras.Any())
                {
                    if (m_IncompatibleCameras.Contains(selectedCameraInStack))
                    {
                        var message = "This camera does not use the same type of renderer as the Base camera.";
                        EditorGUILayout.HelpBox(message, MessageType.Error);
                    }
                }

                if (m_NotSupportedOverlayCameras.Any())
                {
                    if (m_NotSupportedOverlayCameras.Contains(selectedCameraInStack))
                    {
                        var message = "This camera uses a renderer which does not support Overlays in it's current state.";
                        EditorGUILayout.HelpBox(message, MessageType.Error);
                    }
                }

                if (m_TypeErrorCameras.Any())
                {
                    if (m_TypeErrorCameras.Contains(selectedCameraInStack))
                    {
                        var message = "The type of this Camera must be Overlay render type.";
                        CoreEditorUtils.DrawFixMeBox(message, MessageType.Error, UpdateStackCameraToOverlay);
                    }
                }

                if (m_OutputWarningCameras.Any())
                {
                    var camIndex = m_OutputWarningCameras.FindIndex(c => c.Item1 == selectedCameraInStack);
                    if (camIndex != -1)
                    {
                        var message = "The output properties of this Camera do not match the output properties.";
                        if ((CameraRenderType)m_OutputWarningCameras[camIndex].Item2.cameraType.intValue == CameraRenderType.Base)
                        {
                            EditorGUILayout.HelpBox(message, MessageType.Warning);
                        }
                        else
                        {
                            CoreEditorUtils.DrawFixMeBox(message, MessageType.Warning,
                                () => UpdateStackCameraOutput(m_OutputWarningCameras[camIndex].Item1, m_OutputWarningCameras[camIndex].Item2));
                        }
                    }
                }
            }

            EditorStyles.helpBox.richText = oldRichTextSupport;
            EditorGUI.indentLevel++;

            EditorGUILayout.Space();
        }
    }
}
