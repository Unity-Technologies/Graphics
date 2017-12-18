using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Keep this class first in the file. Otherwise it seems that the script type is not registered properly.
    [Serializable]
    public sealed class VisualEnvironment : VolumeComponent
    {
        public SkyTypeParameter skyType = new SkyTypeParameter(SkyType.None);
        public FogTypeParameter fogType = new FogTypeParameter(FogType.None);

        public void PushFogShaderParameters(CommandBuffer cmd, RenderingDebugSettings renderingDebug)
        {
            switch (fogType.value)
            {
                case FogType.None:
                    {
                        AtmosphericScattering.PushNeutralShaderParameters(cmd);
                        break;
                    }
                case FogType.Linear:
                    {
                        var fogSettings = VolumeManager.instance.stack.GetComponent<LinearFog>();
                        fogSettings.PushShaderParameters(cmd, renderingDebug);
                        break;
                    }
                case FogType.Exponential:
                    {
                        var fogSettings = VolumeManager.instance.stack.GetComponent<ExponentialFog>();
                        fogSettings.PushShaderParameters(cmd, renderingDebug);
                        break;
                    }
            }
        }
    }

    // TODO instead of hardcoding this, we need to generate the information from the existing sky currently implemented.
    public enum SkyType
    {
        None,
        HDRISky,
        ProceduralSky
    }
    
    [Serializable]
    public sealed class SkyTypeParameter : VolumeParameter<SkyType>
    {
        public SkyTypeParameter(SkyType value, bool overrideState = false)
            : base(value, overrideState)
        {
        }
    }
}
