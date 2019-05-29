using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Ray Tracing/Light Cluster")]
    public sealed class LightCluster : VolumeComponent
    {
        [Tooltip("Controls the maximal number lights in a cell.")]
        public ClampedIntParameter maxNumLightsPercell = new ClampedIntParameter(10, 0, 24);

        [Tooltip("Controls the range of the cluster around the camera.")]
        public ClampedFloatParameter cameraClusterRange = new ClampedFloatParameter(10f, 0.001f, 50f);
    }
}
