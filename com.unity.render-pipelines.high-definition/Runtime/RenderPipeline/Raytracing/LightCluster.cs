using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ray tracing light cluster.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Light Cluster (Preview)")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Ray-Tracing-Light-Cluster" + Documentation.endURL)]
    public sealed class LightCluster : VolumeComponent
    {
        /// <summary>
        /// Controls the range of the cluster around the camera.
        /// </summary>
        [Tooltip("Controls the range of the cluster around the camera.")]
        public MinFloatParameter cameraClusterRange = new MinFloatParameter(10.0f, 0.001f);
        /// <summary>
        /// Default constructor for the light cluster volume component.
        /// </summary>
        public LightCluster()
        {
            displayName = "Light Cluster (Preview)";
        }
    }
}
