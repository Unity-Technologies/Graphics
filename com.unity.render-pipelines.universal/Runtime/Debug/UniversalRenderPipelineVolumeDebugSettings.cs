using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Volume debug settings.
    /// </summary>
    public class UniversalRenderPipelineVolumeDebugSettings : VolumeDebugSettings<UniversalAdditionalCameraData>
    {
        /// <summary>
        /// Specifies the render pipeline for this volume settings
        /// </summary>
        public override Type targetRenderPipeline => typeof(UniversalRenderPipeline);

        /// <summary>Selected camera volume stack.</summary>
        public override VolumeStack selectedCameraVolumeStack
        {
            get
            {
                Camera cam = selectedCamera;
                if (cam == null)
                    return null;

                var additionalCameraData = selectedCamera.GetComponent<UniversalAdditionalCameraData>();
                if (additionalCameraData == null)
                    return null;

                var stack = additionalCameraData.volumeStack;
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

                return cam.transform.position;
            }
        }
    }
}
