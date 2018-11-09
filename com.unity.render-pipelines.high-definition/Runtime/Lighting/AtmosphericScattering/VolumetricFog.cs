using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class VolumetricFog : AtmosphericScattering
    {
        public ColorParameter        albedo                 = new ColorParameter(Color.white);
        public MinFloatParameter     meanFreePath           = new MinFloatParameter(1000000.0f, 1.0f);
        public FloatParameter        baseHeight             = new FloatParameter(0.0f);
        public MinFloatParameter     meanHeight             = new MinFloatParameter(10.0f, 1.0f);
        public ClampedFloatParameter anisotropy             = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public BoolParameter         enableDistantFog       = new BoolParameter(false);

        public override void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            PushShaderParametersCommon(hdCamera, cmd, FogType.Volumetric);

            DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo, meanFreePath, anisotropy);

            DensityVolumeEngineData data = param.ConvertToEngineData();

            cmd.SetGlobalVector(HDShaderIDs._HeightFogBaseScattering, data.scattering);
            cmd.SetGlobalFloat(HDShaderIDs._HeightFogBaseExtinction,  data.extinction);

            float crBaseHeight = baseHeight;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                crBaseHeight -= hdCamera.camera.transform.position.y;
            }

            // FogExponent = 1 / MeanHeight
            cmd.SetGlobalVector(HDShaderIDs._HeightFogExponents,  new Vector2(1.0f / meanHeight, meanHeight));
            cmd.SetGlobalFloat( HDShaderIDs._HeightFogBaseHeight, crBaseHeight);
            cmd.SetGlobalFloat( HDShaderIDs._GlobalFogAnisotropy, anisotropy);
            cmd.SetGlobalInt(   HDShaderIDs._EnableDistantFog,    enableDistantFog ? 1 : 0);
        }
    }
}
