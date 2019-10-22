using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Probe Volume Global Illumination quality")]
    public class ProbeVolumeController : VolumeComponent
    {
        [Tooltip("Controls the distance in world space to bias along the surface normal to mitigate self-shadow artifacts.")]
        public MinFloatParameter normalBiasWS = new MinFloatParameter(0.0f, 0.0f);

        ProbeVolumeController()
        {
            displayName = "Probe Volume Controller";
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
