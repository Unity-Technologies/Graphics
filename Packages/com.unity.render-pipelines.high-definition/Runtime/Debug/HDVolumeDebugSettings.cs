using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Represents the debug settings for handling volumes in the Rendering Debugger.
    /// This class extends <see cref="VolumeDebugSettings{HDAdditionalCameraData}"/> and allows customization of the volume stack,
    /// layer mask, and position for the selected camera in the HDRP pipeline. It manages the camera-specific configurations
    /// to ensure accurate rendering in the volume system.
    /// </summary>
    /// <remarks>
    /// This class provides access to debug settings for the volume stack and layer mask in the High Definition Render Pipeline (HDRP).
    /// It is useful for visualizing and adjusting volume settings for specific cameras during development.
    /// </remarks>
    [Obsolete("This is not longer supported Please use DebugDisplaySettingsVolume. #from(6000.2)", false)]
    public partial class HDVolumeDebugSettings : VolumeDebugSettings<HDAdditionalCameraData>
    {
        /// <summary>
        /// Gets the selected camera's volume stack. If no camera is selected, it defaults to the global volume stack.
        /// This stack holds all the volume components affecting the camera.
        /// </summary>
        /// <remarks>
        /// This property retrieves the volume stack associated with the currently selected camera. If the camera is not found,
        /// the global volume stack is used as a fallback. This stack defines how volumes are applied to the camera's view.
        /// </remarks>
        public override VolumeStack selectedCameraVolumeStack
        {
            get
            {
                Camera cam = selectedCamera;
                if (cam == null)
                    return null;

                var stack = HDCamera.GetOrCreate(cam).volumeStack;
                return stack ?? VolumeManager.instance.stack;
            }
        }

        /// <summary>
        /// Gets the selected camera's volume layer mask. This determines which volumes will affect the selected camera.
        /// If the scene view is active, it will use the main camera's volume layer mask.
        /// </summary>
        /// <remarks>
        /// This property manages the layer mask for volumes, which is important for determining which volumes apply to the selected camera.
        /// It considers special cases like the scene view camera to provide accurate results in different contexts.
        /// </remarks>
        public override LayerMask selectedCameraLayerMask
        {
            get
            {
                if (selectedCamera != null)
                {
    #if UNITY_EDITOR
                    // For scene view, use main camera volume layer mask. See HDCamera.cs
                    if (selectedCamera == SceneView.lastActiveSceneView.camera)
                    {
                        var mainCamera = Camera.main;
                        if (mainCamera != null &&
                            mainCamera.TryGetComponent<HDAdditionalCameraData>(out var sceneCameraAdditionalCameraData))
                            return sceneCameraAdditionalCameraData.volumeLayerMask;
                        return HDCamera.GetSceneViewLayerMaskFallback();
                    }
    #endif
                    if (selectedCamera.TryGetComponent<HDAdditionalCameraData>(out var selectedCameraAdditionalData))
                        return selectedCameraAdditionalData.volumeLayerMask;
                }

                return 1; // Default
            }
        }

        /// <summary>
        /// Gets the selected camera's volume position. If no camera is selected, the position defaults to <see cref="Vector3.zero"/>.
        /// </summary>
        /// <remarks>
        /// This property returns the position of the volume anchor for the selected camera. If the camera's anchor has not been initialized,
        /// it will attempt to retrieve or set the anchor manually. This is important for adjusting the position of the camera in relation to the volume system.
        /// </remarks>
        public override Vector3 selectedCameraPosition
        {
            get
            {
                Camera cam = selectedCamera;
                if (cam == null)
                    return Vector3.zero;

                var anchor = HDCamera.GetOrCreate(cam).volumeAnchor;
                if (anchor == null) // If the HDCamera has not been initialized
                {
                    // Manually update the stack
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
