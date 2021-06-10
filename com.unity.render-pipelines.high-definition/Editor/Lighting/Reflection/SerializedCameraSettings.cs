using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal class SerializedCameraSettingsOverride
    {
        internal SerializedProperty root;

        internal SerializedProperty camera;

        public SerializedCameraSettingsOverride(SerializedProperty root)
        {
            this.root = root;

            camera = root.Find((CameraSettingsOverride p) => p.camera);
        }
    }

    internal class SerializedCameraSettings
    {
        internal SerializedProperty root;

        internal SerializedFrameSettings frameSettings;

        internal SerializedProperty bufferClearColorMode;
        internal SerializedProperty bufferClearBackgroundColorHDR;
        internal SerializedProperty bufferClearClearDepth;
        internal SerializedProperty volumesLayerMask;
        internal SerializedProperty volumesAnchorOverride;
        internal SerializedProperty frustumMode;
        internal SerializedProperty frustumAspect;
        internal SerializedProperty frustumFarClipPlane;
        internal SerializedProperty frustumNearClipPlane;
        internal SerializedProperty frustumFieldOfView;
        internal SerializedProperty frustumProjectionMatrix;
        internal SerializedProperty cullingUseOcclusionCulling;
        internal SerializedProperty cullingCullingMask;
        internal SerializedProperty cullingInvertFaceCulling;
        internal SerializedProperty customRenderingSettings;
        internal SerializedProperty flipYMode;
        internal SerializedProperty probeLayerMask;

        internal SerializedCameraSettings(SerializedProperty root)
        {
            this.root = root;

            frameSettings = new SerializedFrameSettings(
                root.Find((CameraSettings s) => s.renderingPathCustomFrameSettings),
                root.Find((CameraSettings s) => s.renderingPathCustomFrameSettingsOverrideMask)
            );

            bufferClearColorMode = root.FindPropertyRelative("bufferClearing.clearColorMode");
            bufferClearBackgroundColorHDR = root.FindPropertyRelative("bufferClearing.backgroundColorHDR");
            bufferClearClearDepth = root.FindPropertyRelative("bufferClearing.clearDepth");
            volumesLayerMask = root.FindPropertyRelative("volumes.layerMask");
            volumesAnchorOverride = root.FindPropertyRelative("volumes.anchorOverride");
            frustumMode = root.FindPropertyRelative("frustum.mode");
            frustumAspect = root.FindPropertyRelative("frustum.aspect");
            frustumFarClipPlane = root.FindPropertyRelative("frustum.farClipPlaneRaw");
            frustumNearClipPlane = root.FindPropertyRelative("frustum.nearClipPlaneRaw");
            frustumFieldOfView = root.FindPropertyRelative("frustum.fieldOfView");
            frustumProjectionMatrix = root.FindPropertyRelative("frustum.projectionMatrix");
            cullingUseOcclusionCulling = root.FindPropertyRelative("culling.useOcclusionCulling");
            cullingCullingMask = root.FindPropertyRelative("culling.cullingMask");
            cullingInvertFaceCulling = root.FindPropertyRelative("invertFaceCulling");
            customRenderingSettings = root.FindPropertyRelative("customRenderingSettings");
            flipYMode = root.FindPropertyRelative("flipYMode");
            probeLayerMask = root.FindPropertyRelative("probeLayerMask");
        }
    }
}
