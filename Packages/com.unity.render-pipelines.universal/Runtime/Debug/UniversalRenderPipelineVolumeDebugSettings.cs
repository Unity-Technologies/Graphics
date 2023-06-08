namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Volume debug settings.
    /// </summary>
    public partial class UniversalRenderPipelineVolumeDebugSettings : VolumeDebugSettings<UniversalAdditionalCameraData>
    {
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
                if (selectedCamera != null && selectedCamera.TryGetComponent<UniversalAdditionalCameraData>(out var selectedAdditionalCameraData))
                    return selectedAdditionalCameraData.volumeLayerMask;

                return 1; // "Default"
            }
        }

        /// <summary>Selected camera volume position.</summary>
        public override Vector3 selectedCameraPosition => selectedCamera != null ? selectedCamera.transform.position : Vector3.zero;
    }
}
