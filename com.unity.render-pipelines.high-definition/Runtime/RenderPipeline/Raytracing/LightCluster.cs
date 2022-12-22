using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ray tracing light cluster.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Light Cluster")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Ray-Tracing-Light-Cluster")]
    public sealed class LightCluster : VolumeComponent
    {
        /// <summary>
        /// Controls the range of the cluster around the camera in meters.
        /// </summary>
        [Tooltip("Controls the range of the cluster around the camera in meters.")]
        public MinFloatParameter cameraClusterRange = new MinFloatParameter(10.0f, 0.001f);
        /// <summary>
        /// Default constructor for the light cluster volume component.
        /// </summary>
        public LightCluster()
        {
            displayName = "Light Cluster";
        }
    }
}
