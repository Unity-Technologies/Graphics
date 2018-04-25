using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class VolumetricFog : AtmosphericScattering
    {
        public ColorParameter        albedo       = new ColorParameter(new Color(0.5f, 0.5f, 0.5f));
        public MinFloatParameter     meanFreePath = new MinFloatParameter(1000000.0f, 1.0f);
        public ClampedFloatParameter asymmetry    = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);

        // Override the volume blending function.
        public override void Override(VolumeComponent state, float interpFactor)
        {
            VolumetricFog other = state as VolumetricFog;

            float   thisExtinction  = VolumeRenderingUtils.ExtinctionFromMeanFreePath(meanFreePath);
            Vector3 thisScattering  = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(thisExtinction, (Vector3)(Vector4)albedo.value);

            float   otherExtinction = VolumeRenderingUtils.ExtinctionFromMeanFreePath(other.meanFreePath);
            Vector3 otherScattering = VolumeRenderingUtils.ScatteringFromExtinctionAndAlbedo(otherExtinction, (Vector3)(Vector4)other.albedo.value);

            float   blendExtinction =   Mathf.Lerp(otherExtinction, thisExtinction, interpFactor);
            Vector3 blendScattering = Vector3.Lerp(otherScattering, thisScattering, interpFactor);
            float   blendAsymmetry  =   Mathf.Lerp(other.asymmetry, asymmetry,      interpFactor);

            float   blendMeanFreePath = VolumeRenderingUtils.MeanFreePathFromExtinction(blendExtinction);
            Color   blendAlbedo       = (Color)(Vector4)VolumeRenderingUtils.AlbedoFromMeanFreePathAndScattering(blendMeanFreePath, blendScattering);
                    blendAlbedo.a     = 1.0f;

            if (meanFreePath.overrideState)
            {
                other.meanFreePath.value = blendMeanFreePath;
            }

            if (albedo.overrideState)
            {
                other.albedo.value = blendAlbedo;
            }

            if (asymmetry.overrideState)
            {
                other.asymmetry.value = blendAsymmetry;
            }
        }

        public override void PushShaderParameters(CommandBuffer cmd, FrameSettings frameSettings)
        {
            DensityVolumeParameters param;

            param.albedo       = albedo;
            param.meanFreePath = meanFreePath;
            param.asymmetry    = asymmetry;

            DensityVolumeData data = param.GetData();

            cmd.SetGlobalInt(HDShaderIDs._AtmosphericScatteringType, (int)FogType.Volumetric);

            cmd.SetGlobalVector(HDShaderIDs._GlobalScattering, data.scattering);
            cmd.SetGlobalFloat( HDShaderIDs._GlobalExtinction, data.extinction);
            cmd.SetGlobalFloat( HDShaderIDs._GlobalAsymmetry,  asymmetry);
        }
    }
}
