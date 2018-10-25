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
        public ClampedFloatParameter heightExponent         = new ClampedFloatParameter(0.5f, 0.001f, 1.0f);
        public ClampedFloatParameter anisotropy             = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public override void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo, meanFreePath, anisotropy);

            DensityVolumeEngineData data = param.ConvertToEngineData();

            cmd.SetGlobalInt(HDShaderIDs._AtmosphericScatteringType, (int)FogType.Volumetric);

            cmd.SetGlobalVector(HDShaderIDs._HeightFogBaseScattering, data.scattering);
            cmd.SetGlobalFloat(HDShaderIDs._HeightFogBaseExtinction,  data.extinction);

            float crBaseHeight = baseHeight;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                crBaseHeight -= hdCamera.camera.transform.position.y;
            }

            cmd.SetGlobalVector(HDShaderIDs._HeightFogExponents, new Vector2(heightExponent, 1.0f / heightExponent));
            cmd.SetGlobalFloat(HDShaderIDs._HeightFogBaseHeight, crBaseHeight);
            cmd.SetGlobalFloat(HDShaderIDs._GlobalFogAnisotropy, anisotropy);
        }
    }
}
