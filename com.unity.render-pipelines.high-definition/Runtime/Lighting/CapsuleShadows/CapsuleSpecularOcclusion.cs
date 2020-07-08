using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Capsule Specular Occlusion")]
    public class CapsuleSpecularOcclusion : VolumeComponent
    {

        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
        public BoolParameter monteCarlo = new BoolParameter(false);
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
