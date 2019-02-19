using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Lighting/Indirect Lighting Controller")]
    public class IndirectLightingController : VolumeComponent
    {
        public MinFloatParameter    indirectSpecularIntensity = new MinFloatParameter(1.0f, 0.0f);
        public MinFloatParameter    indirectDiffuseIntensity = new MinFloatParameter(1.0f, 0.0f);        
    }
}
