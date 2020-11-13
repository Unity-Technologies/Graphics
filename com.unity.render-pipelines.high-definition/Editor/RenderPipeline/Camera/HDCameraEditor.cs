using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        SerializedHDCamera m_SerializedCamera;

        RenderTexture m_PreviewTexture;
        Camera m_PreviewCamera;
        HDAdditionalCameraData m_PreviewAdditionalCameraData;

        void OnEnable()
        {
            m_SerializedCamera = new SerializedHDCamera(serializedObject);

            m_PreviewCamera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();
            m_PreviewCamera.enabled = false;
            m_PreviewCamera.cameraType = CameraType.Preview; // Must be init before adding HDAdditionalCameraData
            m_PreviewAdditionalCameraData = m_PreviewCamera.gameObject.AddComponent<HDAdditionalCameraData>();
            // Say that we are a camera editor preview and not just a regular preview
            m_PreviewAdditionalCameraData.isEditorCameraPreview = true;
        }

        void OnDisable()
        {
            if (m_PreviewTexture != null)
            {
                m_PreviewTexture.Release();
                m_PreviewTexture = null;
            }
            DestroyImmediate(m_PreviewCamera.gameObject);
            m_PreviewCamera = null;
        }

        public override void OnInspectorGUI()
        {
            m_SerializedCamera.Update();

            HDCameraUI.Inspector.Draw(m_SerializedCamera, this);

            m_SerializedCamera.Apply();
        }

        RenderTexture GetPreviewTextureWithSize(int width, int height)
        {
            if (m_PreviewTexture == null || m_PreviewTexture.width != width || m_PreviewTexture.height != height)
            {
                if (m_PreviewTexture != null)
                    m_PreviewTexture.Release();

                m_PreviewTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
                m_PreviewTexture.enableRandomWrite = true;
                m_PreviewTexture.Create();
            }
            return m_PreviewTexture;
        }
    }
    
    [ScriptableRenderPipelineExtension(typeof(HDRenderPipelineAsset))]
    class HDCameraContextualMenu : IRemoveAdditionalDataContextualMenu<Camera>
    {
        //The call is delayed to the dispatcher to solve conflict with other SRP
        public void RemoveComponent(Camera camera, IEnumerable<Component> dependencies)
        {
            // do not use keyword is to remove the additional data. It will not work
            dependencies = dependencies.Where(c => c.GetType() != typeof(HDAdditionalCameraData));
            if (dependencies.Count() > 0)
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

                Undo.SetCurrentGroupName("Remove HD Camera");
                var additionalCameraData = camera.GetComponent<HDAdditionalCameraData>();
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

        [MenuItem("CONTEXT/Camera/Reset", false, 0)]
        static void ResetCamera(MenuCommand menuCommand)
        {
            // Grab the current HDRP asset, we should not be executing this code if HDRP is null
            var hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
            if (hdrp == null)
                return;

            GameObject go = ((Camera)menuCommand.context).gameObject;
            Assert.IsNotNull(go);

            Camera camera = go.GetComponent<Camera>();
            Assert.IsNotNull(camera);

            // Try to grab the HDAdditionalCameraData component, it is possible that the component is null of the camera was created without an asset assigned and the inspector
            // was kept on while assigning the asset and then triggering the reset.
            HDAdditionalCameraData cameraAdditionalData;
            if ((!go.TryGetComponent<HDAdditionalCameraData>(out cameraAdditionalData)))
            {
                cameraAdditionalData = go.AddComponent<HDAdditionalCameraData>();
            }
            Assert.IsNotNull(cameraAdditionalData);

            Undo.SetCurrentGroupName("Reset HD Camera");
            Undo.RecordObjects(new UnityEngine.Object[] { camera, cameraAdditionalData }, "Reset HD Camera");
            camera.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalCameraData, thus why we don't wrap it in a Reset() function
            HDUtils.s_DefaultHDAdditionalCameraData.CopyTo(cameraAdditionalData);
        }
    }
}
