using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests
{
    [HideInInspector]
    class VolumeComponentDecorators : VolumeComponent
    {
        [Tooltip("Increase to make the noise texture appear bigger and less")]
        public FloatParameter _NoiseTileSize = new FloatParameter(25.0f);

        [InspectorName("Color")]
        public ColorParameter _FogColor = new ColorParameter(Color.grey);

        [InspectorName("Size and occurrence"), Tooltip("Increase to make patches SMALLER, and frequent")]
        public ClampedFloatParameter _HighNoiseSpaceFreq = new ClampedFloatParameter(0.1f, 0.1f, 1f);
    }
}
