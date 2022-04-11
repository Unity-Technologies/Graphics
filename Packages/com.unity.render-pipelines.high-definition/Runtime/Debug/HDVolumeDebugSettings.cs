using System;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Volume debug settings.
    /// </summary>
    public class HDVolumeDebugSettings : VolumeDebugSettings<HDAdditionalCameraData>
    {
        /// <summary>
        /// Specifies the render pipeline for this volume settings
        /// </summary>
        public override Type targetRenderPipeline => typeof(HDRenderPipeline);

        /// <summary>Selected camera volume stack.</summary>
        public override VolumeStack selectedCameraVolumeStack
        {
            get
            {
                Camera cam = selectedCamera;
                if (cam == null)
                    return null;
                var stack = HDCamera.GetOrCreate(cam).volumeStack;
                if (stack != null)
                    return stack;
                return VolumeManager.instance.stack;
            }
        }

        /// <summary>Selected camera volume layer mask.</summary>
        public override LayerMask selectedCameraLayerMask
        {
            get
            {
                if (selectedCamera == null)
                    return (LayerMask)0;

#if UNITY_EDITOR
                // For scene view, use main camera volume layer mask. See HDCamera.cs
                if (selectedCamera == SceneView.lastActiveSceneView.camera)
                {
                    var mainCamera = Camera.main;
                    if (mainCamera != null && mainCamera.TryGetComponent<HDAdditionalCameraData>(out var mainCamAdditionalData))
                        return mainCamAdditionalData.volumeLayerMask;
                    return HDCamera.GetSceneViewLayerMaskFallback();
                }
#endif

                return selectedCamera.GetComponent<HDAdditionalCameraData>().volumeLayerMask;
            }
        }

        /// <summary>Selected camera volume position.</summary>
        public override Vector3 selectedCameraPosition
        {
            get
            {
                Camera cam = selectedCamera;
                if (cam == null)
                    return Vector3.zero;

                var anchor = HDCamera.GetOrCreate(cam).volumeAnchor;
                if (anchor == null) // means the hdcamera has not been initialized
                {
                    // So we have to update the stack manually
                    if (cam.TryGetComponent<HDAdditionalCameraData>(out var data))
                        anchor = data.volumeAnchorOverride;
                    if (anchor == null) anchor = cam.transform;
                    var stack = selectedCameraVolumeStack;
                    if (stack != null)
                        VolumeManager.instance.Update(stack, anchor, selectedCameraLayerMask);
                }
                return anchor.position;
            }
        }
    }
}
