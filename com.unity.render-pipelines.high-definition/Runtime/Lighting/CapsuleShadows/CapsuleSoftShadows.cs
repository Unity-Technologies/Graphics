using UnityEngine.Rendering;
using UnityEngine.Serialization;
using System;

// Do we even need this?

namespace UnityEngine.Rendering.HighDefinition
{
    [VolumeComponentMenu("Lighting/Capsule/Soft Shadows")]
    internal class CapsuleSoftShadows : VolumeComponent
    {
        public BoolParameter enabled = new BoolParameter(false);
        
        internal static bool IsCapsuleSoftShadowsEnabled(HDCamera hdCamera)
        {
            var capsuleSoftShadows = hdCamera.volumeStack.GetComponent<CapsuleSoftShadows>();
            return capsuleSoftShadows.enabled.value;
        }
    }
}