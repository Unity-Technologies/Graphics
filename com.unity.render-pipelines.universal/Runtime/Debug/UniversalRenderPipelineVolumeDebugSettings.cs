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
        public override LayerMask selectedCameraLayerMask => selectedCamera != null ? selectedCamera.GetComponent<UniversalAdditionalCameraData>().volumeLayerMask : (LayerMask)0;

        /// <summary>Selected camera volume position.</summary>
        public override Vector3 selectedCameraPosition => selectedCamera != null ? selectedCamera.transform.position : Vector3.zero;
    }
}
