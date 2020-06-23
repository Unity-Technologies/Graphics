using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Capsule/Ambient Occlusion")]
    internal class CapsuleAmbientOcclusion : VolumeComponent
    {

        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 4f);
    }


    // We probably want to do Capsule AO in the context of the AO system as we should probably merge SSAO and this together (or at least share resources) 
    partial class AmbientOcclusionSystem
    {
        internal void DispatchCapsuleOcclusion(CommandBuffer cmd, HDCamera camera, ComputeBuffer visibleCapsules)
        {
        }
    }

} // UnityEngine.Experimental.Rendering.HDPipeline
