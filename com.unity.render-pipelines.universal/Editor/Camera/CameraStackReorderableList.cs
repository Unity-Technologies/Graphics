using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class CameraStackReorderableList
    {
        static class Styles
        {
            public static GUIContent cameras = EditorGUIUtility.TrTextContent("Cameras", "The list of overlay cameras assigned to this camera.");
        }

        readonly ReorderableList m_LayerList;

        UniversalRenderPipelineSerializedCamera serializedCamera { get; }
        Camera camera { get; }
        UniversalAdditionalCameraData additionalCameraData { get; }
        CameraEditor.Settings settings { get; }

        #region Validation

        public bool hasErrors => typeErrorCameras.Count > 0;
        public string errorMessage =>
            $"The type of the following Cameras must be Overlay render type: {string.Join(", ", typeErrorCameras.Select(c => c.name).Distinct())}.";
        List<Camera> typeErrorCameras { get; } = new List<Camera>();

        public bool hasWarnings => outputWarningCameras.Count > 0;
        public string warningMessage =>
            $"The output properties of this Camera do not match the output properties of the following Cameras: {string.Join(", ", outputWarningCameras.Select(c => c.name).Distinct())}.";
        List<Camera> outputWarningCameras { get; } = new List<Camera>();

        void ClearValidation()
        {
            outputWarningCameras.Clear();
            typeErrorCameras.Clear();
        }

        (bool errorsFound, bool warningsFound) SearchForCameraIssues(Camera cam, CameraRenderType type)
        {
            bool warningsFound = false;
            if (IsStackCameraOutputDirty(cam))
            {
                if (!outputWarningCameras.Contains(cam))
                    outputWarningCameras.Add(cam);

                warningsFound = true;
            }
            else
            {
                if (outputWarningCameras.Contains(cam))
                    outputWarningCameras.Remove(cam);
            }

            bool errorsFound = false;
            if (type != CameraRenderType.Overlay)
            {
                if (!typeErrorCameras.Contains(cam))
                    typeErrorCameras.Add(cam);

                errorsFound = true;
            }
            else
            {
                if (typeErrorCameras.Contains(cam))
                    typeErrorCameras.Remove(cam);
            }

            return (errorsFound, warningsFound);
        }

        #endregion

        public event Action<Camera> OnCameraAdded;

        public CameraStackReorderableList(Camera camera, CameraEditor.Settings settings, UniversalRenderPipelineSerializedCamera serializedCamera)
        {
            this.serializedCamera = serializedCamera;
            this.camera = camera;
            additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            this.settings = settings;

            m_LayerList = new ReorderableList(serializedCamera.serializedObject, serializedCamera.cameras, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, Styles.cameras),
                drawElementCallback = DrawElementCallback,
                onSelectCallback = SelectElement,
                onRemoveCallback = RemoveCamera,
                onAddDropdownCallback = AddCameraToCameraList,
                onCanRemoveCallback = list => list.count > 0
            };
        }

        void RemoveCamera(ReorderableList list)
        {
            // As multi selection is disabled, selectedIndices will only return 1 element, remove that element from the list
            if (list.selectedIndices.Any())
            {
                serializedCamera.cameras.DeleteArrayElementAtIndex(list.selectedIndices.First());
            }
            else
            {
                // Nothing selected, remove the last item on the list
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            }

            serializedCamera.serializedObject.ApplyModifiedProperties();

            ClearValidation();
        }

        void SelectElement(ReorderableList list)
        {
            var cam = serializedCamera[list.index];
            if (cam == null)
                return;

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

            var element = serializedCamera.cameras.GetArrayElementAtIndex(index);

            var cam = element.objectReferenceValue as Camera;
            if (cam != null)
            {
                var currentAdditionalCameraData = cam.gameObject.GetComponent<UniversalAdditionalCameraData>();

                var type = currentAdditionalCameraData.renderType;
                (bool typeError, bool outputWarning) = SearchForCameraIssues(cam, type);
                
                GUIContent nameContent =
                    outputWarning ?
                    EditorGUIUtility.TrTextContent(cam.name, "Output properties do not match base Camera", CoreEditorStyles.iconWarn) :
                    EditorGUIUtility.TrTextContent(cam.name);
                
                GUIContent typeContent =
                    typeError ?
                    EditorGUIUtility.TrTextContent(type.GetName(), "Not a supported type", CoreEditorStyles.iconFail) :
                    EditorGUIUtility.TrTextContent(type.GetName());

                EditorGUI.BeginProperty(rect, GUIContent.none, element);
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= 20f;

                using (var iconSizeScope = new EditorGUIUtility.IconSizeScope(new Vector2(rect.height, rect.height)))
                {
                    EditorGUI.LabelField(rect, nameContent, typeContent);
                }

                // Printing if Post Processing is on or not.
                var isPostActive = currentAdditionalCameraData.renderPostProcessing;
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
                additionalCameraData.UpdateCameraStack();

                // Need to clean out the errorCamera list here.
                ClearValidation();
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

        List<Camera> overlayCameras = new List<Camera>();

        void AddCameraToCameraList(Rect rect, ReorderableList list)
        {
            // Need to do clear the list here otherwise the meu just fills up with more and more entries
            overlayCameras.Clear();
            var allCameras = FindCamerasToReference(camera.gameObject);
            foreach (var camera in allCameras)
            {
                var component = camera.gameObject.GetComponent<UniversalAdditionalCameraData>();
                if (component != null)
                {
                    if (component.renderType == CameraRenderType.Overlay)
                    {
                        overlayCameras.Add(camera);
                    }
                }
            }

            var names = new GUIContent[overlayCameras.Count];
            for (int i = 0; i < overlayCameras.Count; ++i)
            {
                names[i] = new GUIContent((i + 1) + " " + overlayCameras[i].name);
            }

            if (!overlayCameras.Any())
            {
                names = new GUIContent[1];
                names[0] = new GUIContent("No Overlay Cameras exist.");
            }
            EditorUtility.DisplayCustomMenu(rect, names, -1, AddCameraToCameraListMenuSelected, null);
        }

        void AddCameraToCameraListMenuSelected(object userData, string[] options, int selected)
        {
            if (!overlayCameras.Any())
                return;

            var length = serializedCamera.cameras.arraySize;
            ++serializedCamera.cameras.arraySize;
            serializedCamera.cameras.serializedObject.ApplyModifiedProperties();
            serializedCamera.cameras.GetArrayElementAtIndex(length).objectReferenceValue = overlayCameras[selected];
            serializedCamera.cameras.serializedObject.ApplyModifiedProperties();

            OnCameraAdded?.Invoke(overlayCameras[selected]);
        }

        public void OnGUI()
        {
            m_LayerList.DoLayoutList();
        }

        bool IsStackCameraOutputDirty(Camera camera)
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
    }
}
