using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Ray Tracing/Light Cluster (Preview)")]
    public sealed class LightCluster : VolumeComponent
    {
        [Tooltip("Controls the maximal number lights in a cell.")]
        public ClampedIntParameter maxNumLightsPercell = new ClampedIntParameter(10, 0, 24);

        [Tooltip("Controls the range of the cluster around the camera.")]
        public ClampedFloatParameter cameraClusterRange = new ClampedFloatParameter(10f, 0.001f, 50f);
    }
}
