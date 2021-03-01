using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class UniversalRenderPipelineSerializedCamera
    {
        public SerializedObject serializedObject { get; }
        public SerializedProperty renderShadows { get; }
        public SerializedProperty renderDepth { get; }
        public SerializedProperty renderOpaque { get; }
        public SerializedProperty renderer { get; }
        public SerializedProperty cameraType { get; }
        public SerializedProperty cameras { get; set; }
        public SerializedProperty volumeLayerMask { get; }
        public SerializedProperty volumeTrigger { get; }
        public SerializedProperty renderPostProcessing { get; }
        public SerializedProperty antialiasing { get; }
        public SerializedProperty antialiasingQuality { get; }
        public SerializedProperty stopNaN { get; }
        public SerializedProperty dithering { get; }
        public SerializedProperty clearDepth { get; }
#if ENABLE_VR && ENABLE_XR_MODULE
        public SerializedProperty allowXRRendering { get; }
#endif

        public UniversalRenderPipelineSerializedCamera(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            renderShadows = serializedObject.FindProperty("m_RenderShadows");
            renderDepth = serializedObject.FindProperty("m_RequiresDepthTextureOption");
            renderOpaque = serializedObject.FindProperty("m_RequiresOpaqueTextureOption");
            renderer = serializedObject.FindProperty("m_RendererIndex");
            volumeLayerMask = serializedObject.FindProperty("m_VolumeLayerMask");
            volumeTrigger = serializedObject.FindProperty("m_VolumeTrigger");
            renderPostProcessing = serializedObject.FindProperty("m_RenderPostProcessing");
            antialiasing = serializedObject.FindProperty("m_Antialiasing");
            antialiasingQuality = serializedObject.FindProperty("m_AntialiasingQuality");
            stopNaN = serializedObject.FindProperty("m_StopNaN");
            dithering = serializedObject.FindProperty("m_Dithering");
            clearDepth = serializedObject.FindProperty("m_ClearDepth");
            cameraType = serializedObject.FindProperty("m_CameraType");
            cameras = serializedObject.FindProperty("m_Cameras");
#if ENABLE_VR && ENABLE_XR_MODULE
            allowXRRendering = serializedObject.FindProperty("m_AllowXRRendering");
#endif
        }

        internal void RefreshCameras()
        {
            var o = new PropertyFetcher<UniversalAdditionalCameraData>(serializedObject);
            cameras = o.Find("m_Cameras");
        }
    }
}
