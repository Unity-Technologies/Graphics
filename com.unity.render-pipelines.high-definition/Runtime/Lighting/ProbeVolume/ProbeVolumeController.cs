using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Probe Volume Global Illumination quality")]
    public class ProbeVolumeController : VolumeComponent
    {
        // TODO: Expose any ProbeVolume camera volume settings here.

        ProbeVolumeController()
        {
            displayName = "Probe Volume Global Illumination quality";
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
