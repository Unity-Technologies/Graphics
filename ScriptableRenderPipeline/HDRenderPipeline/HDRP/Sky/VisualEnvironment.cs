using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This enum is just here to centralize UniqueID values for skies provided with HDRP
    public enum SkyType
    {
        HDRISky = 1,
        ProceduralSky = 2
    }

    // Keep this class first in the file. Otherwise it seems that the script type is not registered properly.
    [Serializable]
    public sealed class VisualEnvironment : VolumeComponent
    {
        public IntParameter skyType = new IntParameter(0);
        public FogTypeParameter fogType = new FogTypeParameter(FogType.None);

        public void PushFogShaderParameters(CommandBuffer cmd, FrameSettings frameSettings)
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
                        fogSettings.PushShaderParameters(cmd, frameSettings);
                        break;
                    }
                case FogType.Exponential:
                    {
                        var fogSettings = VolumeManager.instance.stack.GetComponent<ExponentialFog>();
                        fogSettings.PushShaderParameters(cmd, frameSettings);
                        break;
                    }
            }
        }
    }
}
