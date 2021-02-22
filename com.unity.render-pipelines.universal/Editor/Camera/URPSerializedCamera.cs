using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class URPSerializedCamera
    {
        internal SerializedObject serializedObject;

        internal SerializedProperty renderShadows { get; }
        internal SerializedProperty renderDepth { get; }
        internal SerializedProperty renderOpaque { get; }
        internal SerializedProperty renderer { get; }
        internal SerializedProperty cameraType { get; }
        internal SerializedProperty cameras { get; }
        internal SerializedProperty volumeLayerMask { get; }
        internal SerializedProperty volumeTrigger { get; }
        internal SerializedProperty renderPostProcessing { get; }
        internal SerializedProperty antialiasing { get; }
        internal SerializedProperty antialiasingQuality { get; }
        internal SerializedProperty stopNaN { get; }
        internal SerializedProperty dithering { get; }
        internal SerializedProperty clearDepth { get; }
#if ENABLE_VR && ENABLE_XR_MODULE
        internal SerializedProperty allowXRRendering { get; }
#endif

        internal URPSerializedCamera(SerializedObject serializedObject)
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
            var o = new PropertyFetcher<UniversalAdditionalCameraData>(serializedObject);
            cameras = o.Find("m_Cameras");
#if ENABLE_VR && ENABLE_XR_MODULE
            allowXRRendering = serializedObject.FindProperty("m_AllowXRRendering");
#endif
        }

        internal void Update()
        {
            serializedObject.Update();
        }

        internal void Apply()
        {
            serializedObject.ApplyModifiedProperties();

        }
    }
}
