using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class UniversalRenderPipelineSerializedCamera : ISerializedCamera
    {
        public SerializedObject serializedObject { get; }
        public SerializedObject serializedAdditionalDataObject { get; }
        public CameraEditor.Settings baseCameraSettings { get; }

        // This one is internal in UnityEditor for whatever reason...
        public SerializedProperty projectionMatrixMode { get; }

        // Common properties
        public SerializedProperty dithering { get; }
        public SerializedProperty stopNaNs { get; }
        public SerializedProperty allowDynamicResolution { get; }
        public SerializedProperty volumeLayerMask { get; }
        public SerializedProperty clearDepth { get; }
        public SerializedProperty antialiasing { get; }

        // URP specific properties
        public SerializedProperty renderShadows { get; }
        public SerializedProperty renderDepth { get; }
        public SerializedProperty renderOpaque { get; }
        public SerializedProperty renderer { get; }
        public SerializedProperty cameraType { get; }
        public SerializedProperty cameras { get; set; }
        public SerializedProperty volumeTrigger { get; }
        public SerializedProperty renderPostProcessing { get; }
        public SerializedProperty antialiasingQuality { get; }
#if ENABLE_VR && ENABLE_XR_MODULE
        public SerializedProperty allowXRRendering { get; }
#endif

        public (Camera camera, UniversalRenderPipelineSerializedCamera serializedCamera) this[int index]
        {
            get
            {
                if (index < 0 || index >= numCameras)
                    throw new ArgumentOutOfRangeException($"{index} is out of bounds [0 - {numCameras}]");

                // Return the camera on that index
                return (cameras.GetArrayElementAtIndex(index).objectReferenceValue as Camera, cameraSerializedObjects[index]);
            }
        }

        public int numCameras => cameras.arraySize;

        UniversalRenderPipelineSerializedCamera[] cameraSerializedObjects { get; set; }

        public UniversalAdditionalCameraData[] camerasAdditionalData { get; }

        public UniversalRenderPipelineSerializedCamera(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            projectionMatrixMode = serializedObject.FindProperty("m_projectionMatrixMode");

            allowDynamicResolution = serializedObject.FindProperty("m_AllowDynamicResolution");

            baseCameraSettings = new CameraEditor.Settings(serializedObject);
            baseCameraSettings.OnEnable();

            camerasAdditionalData = CoreEditorUtils
                .GetAdditionalData<UniversalAdditionalCameraData>(serializedObject.targetObjects);
            serializedAdditionalDataObject = new SerializedObject(camerasAdditionalData);

            // Common properties
            stopNaNs = serializedAdditionalDataObject.FindProperty("m_StopNaN");
            dithering = serializedAdditionalDataObject.FindProperty("m_Dithering");
            antialiasing = serializedAdditionalDataObject.FindProperty("m_Antialiasing");
            volumeLayerMask = serializedAdditionalDataObject.FindProperty("m_VolumeLayerMask");
            clearDepth = serializedAdditionalDataObject.FindProperty("m_ClearDepth");

            // URP specific properties
            renderShadows = serializedAdditionalDataObject.FindProperty("m_RenderShadows");
            renderDepth = serializedAdditionalDataObject.FindProperty("m_RequiresDepthTextureOption");
            renderOpaque = serializedAdditionalDataObject.FindProperty("m_RequiresOpaqueTextureOption");
            renderer = serializedAdditionalDataObject.FindProperty("m_RendererIndex");
            volumeLayerMask = serializedAdditionalDataObject.FindProperty("m_VolumeLayerMask");
            volumeTrigger = serializedAdditionalDataObject.FindProperty("m_VolumeTrigger");
            renderPostProcessing = serializedAdditionalDataObject.FindProperty("m_RenderPostProcessing");
            antialiasingQuality = serializedAdditionalDataObject.FindProperty("m_AntialiasingQuality");
            cameraType = serializedAdditionalDataObject.FindProperty("m_CameraType");

#if ENABLE_VR && ENABLE_XR_MODULE
            allowXRRendering = serializedAdditionalDataObject.FindProperty("m_AllowXRRendering");
#endif

            Refresh();
        }

        /// <summary>
        /// Updates the internal serialized objects
        /// </summary>
        public void Update()
        {
            baseCameraSettings.Update();
            serializedObject.Update();
            serializedAdditionalDataObject.Update();

            for (int i = 0; i < numCameras; ++i)
            {
                cameraSerializedObjects[i].Update();
            }
        }

        /// <summary>
        /// Applies the modified properties to the serialized objects
        /// </summary>
        public void Apply()
        {
            baseCameraSettings.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();
            serializedAdditionalDataObject.ApplyModifiedProperties();

            for (int i = 0; i < numCameras; ++i)
            {
                cameraSerializedObjects[i].Apply();
            }
        }

        /// <summary>
        /// Refreshes the serialized properties from the serialized objects
        /// </summary>
        public void Refresh()
        {
            var o = new PropertyFetcher<UniversalAdditionalCameraData>(serializedAdditionalDataObject);
            cameras = o.Find("m_Cameras");

            cameraSerializedObjects = new UniversalRenderPipelineSerializedCamera[numCameras];
            for (int i = 0; i < numCameras; ++i)
            {
                cameraSerializedObjects[i] = new UniversalRenderPipelineSerializedCamera(
                    new SerializedObject(cameras.GetArrayElementAtIndex(i).objectReferenceValue as Camera));
            }
        }
    }
}
