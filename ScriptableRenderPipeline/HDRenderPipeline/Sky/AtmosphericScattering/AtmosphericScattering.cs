using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public abstract class AtmosphericScattering : VolumeComponent
    {
        [GenerateHLSL]
        public enum FogType
        {
            None,
            Linear,
            Exponential
        }
        [Serializable]
        public sealed class FogTypeParameter : VolumeParameter<FogType> { }


         [GenerateHLSL]
        public enum FogColorMode
        {
            ConstantColor,
            SkyColor,
        }
        [Serializable]
        public sealed class FogColorParameter : VolumeParameter<FogColorMode> { }

        private readonly static int m_TypeParam = Shader.PropertyToID("_AtmosphericScatteringType");
        // Fog Color
        private readonly static int m_ColorModeParam = Shader.PropertyToID("_FogColorMode");
        private readonly static int m_FogColorDensityParam = Shader.PropertyToID("_FogColorDensity");
        private readonly static int m_MipFogParam = Shader.PropertyToID("_MipFogParameters");


        // Fog Color
        public FogColorParameter        colorMode = new FogColorParameter { value = FogColorMode.SkyColor };
        [Tooltip("Constant Fog Color")]
        public ColorParameter           color = new ColorParameter { value = Color.grey };
        public ClampedFloatParameter    density = new ClampedFloatParameter { value = 1.0f, min = 0.0f, max = 1.0f };
        [Tooltip("Maximum mip map used for mip fog (0 being lowest and 1 highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter { value = 1.0f, min = 0.0F, max = 1.0f };
        [Tooltip("Distance at which minimum mip of blurred sky texture is used as fog color.")]
        public ClampedFloatParameter mipFogNear = new ClampedFloatParameter { value = 0.0f, min = 0.0f, clampMode = ParameterClampMode.Min };
        [Tooltip("Distance at which maximum mip of blurred sky texture is used as fog color.")]
        public ClampedFloatParameter mipFogFar = new ClampedFloatParameter { value = 1000.0f, min = 0.0f, clampMode = ParameterClampMode.Min };

        public abstract void PushShaderParameters(CommandBuffer cmd, RenderingDebugSettings renderingDebug);

        public static void PushNeutralShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalFloat(m_TypeParam, (float)FogType.None);
        }

        public void PushShaderParametersCommon(CommandBuffer cmd, FogType type, RenderingDebugSettings renderingDebug)
        {
            if(renderingDebug.enableAtmosphericScattering)
                cmd.SetGlobalFloat(m_TypeParam, (float)type);
            else
                cmd.SetGlobalFloat(m_TypeParam, (float)FogType.None);

            // Fog Color
            cmd.SetGlobalFloat(m_ColorModeParam, (float)colorMode.value);
            cmd.SetGlobalColor(m_FogColorDensityParam, new Color(color.value.r, color.value.g, color.value.b, density));
            cmd.SetGlobalVector(m_MipFogParam, new Vector4(mipFogNear, mipFogFar, mipFogMaxMip, 0.0f));
        }
    }

}