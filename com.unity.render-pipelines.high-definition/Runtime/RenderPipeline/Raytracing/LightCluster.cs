using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ray tracing light cluster.
    /// </summary>
    [Serializable, VolumeComponentMenu("Ray Tracing/Light Cluster (Preview)")]
    public sealed class LightCluster : VolumeComponent
    {
        /// <summary>
        /// Controls the maximal number lights in a cell.
        /// </summary>
        [Tooltip("Controls the maximal number lights in a cell.")]
        public ClampedIntParameter maxNumLightsPercell = new ClampedIntParameter(10, 0, 24);

        /// <summary>
        /// Controls the range of the cluster around the camera.
        /// </summary>
        [Tooltip("Controls the range of the cluster around the camera.")]
        public ClampedFloatParameter cameraClusterRange = new ClampedFloatParameter(10f, 0.001f, 50f);
    }
}
