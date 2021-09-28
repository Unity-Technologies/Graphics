namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Volume debug settings.
    /// </summary>
    public class HDVolumeDebugSettings : VolumeDebugSettings<HDAdditionalCameraData>
    {
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
#if UNITY_EDITOR
                if (m_SelectedCameraIndex <= 0 || m_SelectedCameraIndex > additionalCameraDatas.Count + 1)
                    return (LayerMask)0;
                if (m_SelectedCameraIndex == 1)
                    return -1;
                return additionalCameraDatas[m_SelectedCameraIndex - 2].volumeLayerMask;
#else
                if (m_SelectedCameraIndex <= 0 || m_SelectedCameraIndex > additionalCameraDatas.Count)
                    return (LayerMask)0;
                return additionalCameraDatas[m_SelectedCameraIndex - 1].volumeLayerMask;
#endif
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
