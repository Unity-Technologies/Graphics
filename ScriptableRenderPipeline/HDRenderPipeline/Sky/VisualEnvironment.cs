using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // TODO instead of hardcoing this, we need to generate the information from the existing sky currently implemented.
    public enum SkyType
    {
        None,
        HDRISky,
        ProceduralSky
    }
    
    [Serializable]
    public sealed class SkyTypeParameter : VolumeParameter<SkyType> { }


    [Serializable]
    public sealed class VisualEnvironment : VolumeComponent
    {
        public SkyTypeParameter skyType = new SkyTypeParameter { value = SkyType.ProceduralSky };
        public AtmosphericScattering.FogTypeParameter fogType = new AtmosphericScattering.FogTypeParameter { value = AtmosphericScattering.FogType.None };

        public void PushFogShaderParameters(CommandBuffer cmd, RenderingDebugSettings renderingDebug)
        {
            switch(fogType.value)
            {
                case AtmosphericScattering.FogType.None:
                    {
                        AtmosphericScattering.PushNeutralShaderParameters(cmd);
                        break;
                    }
                case AtmosphericScattering.FogType.Linear:
                    {
                        var fogSettings = VolumeManager.instance.GetComponent<LinearFog>();
                        fogSettings.PushShaderParameters(cmd, renderingDebug);
                        break;
                    }
                case AtmosphericScattering.FogType.Exponential:
                    {
                        var fogSettings = VolumeManager.instance.GetComponent<ExponentialFog>();
                        fogSettings.PushShaderParameters(cmd, renderingDebug);
                        break;
                    }
            }
        }
    }
}
