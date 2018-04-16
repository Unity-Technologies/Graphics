using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class VolumetricFog : AtmosphericScattering
    {
        private readonly static int m_ExpFogParam = Shader.PropertyToID("_ExpFogParameters");

        public ColorParameter        albedo       = new ColorParameter(new Color(0.5f, 0.5f, 0.5f));

        // Note: mean free path is a non-linear function of density.
        // You want to interpolate the ExtinctionCoefficient = 1 / MeanFreePath.
        public ClampedFloatParameter meanFreePath = new ClampedFloatParameter(10.0f, 0, 1000000);

        public ClampedFloatParameter asymmetry    = new ClampedFloatParameter(0.0f, -1, 1);

        public override void PushShaderParameters(CommandBuffer cmd, FrameSettings frameSettings)
        {
            cmd.SetGlobalInt(HDShaderIDs._AtmosphericScatteringType, (int)FogType.Volumetric);

            // cmd.SetGlobalVector(HDShaderIDs._GlobalScattering, properties.scattering);
            // cmd.SetGlobalFloat( HDShaderIDs._GlobalExtinction, properties.extinction);
            // cmd.SetGlobalFloat( HDShaderIDs._GlobalAsymmetry,  properties.asymmetry);
        }
    }

}
