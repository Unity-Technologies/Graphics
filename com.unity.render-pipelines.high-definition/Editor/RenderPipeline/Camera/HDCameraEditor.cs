using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [CustomEditorForRenderPipeline(typeof(Camera), typeof(HDRenderPipelineAsset))]
    [CanEditMultipleObjects]
    partial class HDCameraEditor : Editor
    {
        [MenuItem("CONTEXT/Camera/Remove Component", false, 0)]
        static void RemoveCamera(MenuCommand menuCommand)
        {
            GameObject go = ((Camera)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Camera camera = go.GetComponent<Camera>();
            HDAdditionalCameraData cameraAdditionalData = go.GetComponent<HDAdditionalCameraData>();

            Assert.IsNotNull(camera);
            Assert.IsNotNull(cameraAdditionalData);

            Undo.SetCurrentGroupName("Remove HD Camera");
            Undo.DestroyObjectImmediate(camera);
            Undo.DestroyObjectImmediate(cameraAdditionalData);
        }

        [MenuItem("CONTEXT/Camera/Reset", false, 0)]
        static void ResetCamera(MenuCommand menuCommand)
        {
            GameObject go = ((Camera)menuCommand.context).gameObject;

            Assert.IsNotNull(go);

            Camera camera = go.GetComponent<Camera>();
            HDAdditionalCameraData cameraAdditionalData = go.GetComponent<HDAdditionalCameraData>();

            Assert.IsNotNull(camera);
            Assert.IsNotNull(cameraAdditionalData);

            Undo.SetCurrentGroupName("Reset HD Camera");
            Undo.RecordObjects(new UnityEngine.Object[] { camera, cameraAdditionalData }, "Reset HD Camera");
            camera.Reset();
            // To avoid duplicating init code we copy default settings to Reset additional data
            // Note: we can't call this code inside the HDAdditionalCameraData, thus why we don't wrap it in a Reset() function
            HDUtils.s_DefaultHDAdditionalCameraData.CopyTo(cameraAdditionalData);
        }

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
}
