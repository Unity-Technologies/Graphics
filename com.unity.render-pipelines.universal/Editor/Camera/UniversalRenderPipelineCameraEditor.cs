using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using Styles = UniversalRenderPipelineCameraUI.Styles;

    [CustomEditorForRenderPipeline(typeof(Camera), typeof(UniversalRenderPipelineAsset))]
    [CanEditMultipleObjects]
    class UniversalRenderPipelineCameraEditor : CameraEditor
    {
        ReorderableList m_LayerList;

        public Camera camera => target as Camera;

        List<Camera> validCameras = new List<Camera>();
        List<Camera> m_TypeErrorCameras = new List<Camera>();
        List<Camera> m_OutputWarningCameras = new List<Camera>();

        UniversalRenderPipelineSerializedCamera m_SerializedCamera;

        public new void OnEnable()
        {
            base.OnEnable();
            settings.OnEnable();

            m_SerializedCamera = new UniversalRenderPipelineSerializedCamera(serializedObject, settings);

            validCameras.Clear();
            m_TypeErrorCameras.Clear();
            m_OutputWarningCameras.Clear();

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

            (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera) overlayCamera = m_SerializedCamera[index];
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
                    EditorGUIUtility.TrTextContent(cam.name, "Output properties do not match base camera", CoreEditorStyles.iconWarn) :
                    EditorGUIUtility.TrTextContent(cam.name);

                GUIContent typeContent =
                    typeError ?
                    EditorGUIUtility.TrTextContent(type.GetName(), "Not a supported type", CoreEditorStyles.iconFail) :
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

            (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera) overlayCamera = m_SerializedCamera[m_SerializedCamera.numCameras - 1];
            UpdateStackCameraOutput(overlayCamera.camera, overlayCamera.serializedCamera);
        }

        public new void OnDisable()
        {
            base.OnDisable();
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
                (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera) overlayCamera = m_SerializedCamera[i];
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
                .All(c => c.scriptableRenderer?.supportedRenderingFeatures?.cameraStacking ?? false);

            if (!cameraStackingAvailable)
            {
                EditorGUILayout.HelpBox("The renderer used by this camera doesn't support camera stacking. Only Base camera will render.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();

            m_LayerList.DoLayoutList();
            m_SerializedCamera.Apply();

            EditorGUI.indentLevel--;
            if (m_TypeErrorCameras.Any())
            {
                var message = new StringBuilder();
                message.Append("The type of the following Cameras must be Overlay render type: ");
                foreach (var cam in m_TypeErrorCameras)
                {
                    message.Append(cam.name);
                    message.Append(cam != m_TypeErrorCameras.Last() ? ", " : ".");
                }

                CoreEditorUtils.DrawFixMeBox(message.ToString(), MessageType.Error, UpdateStackCamerasToOverlay);
            }

            if (m_OutputWarningCameras.Any())
            {
                var message = new StringBuilder();
                message.Append("The output properties of this Camera do not match the output properties of the following Cameras: ");
                foreach (var cam in m_OutputWarningCameras)
                {
                    message.Append(cam.name);
                    message.Append(cam != m_OutputWarningCameras.Last() ? ", " : ".");
                }

                CoreEditorUtils.DrawFixMeBox(message.ToString(), MessageType.Warning, () => UpdateStackCamerasOutput());
            }
            EditorGUI.indentLevel++;

            EditorGUILayout.Space();
        }
    }
}
