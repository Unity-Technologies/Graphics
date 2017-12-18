using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class AtmosphericScatteringSettings
    {
        [GenerateHLSL]
        public enum FogType
        {
            None,
            Linear,
            Exponential
        }

         [GenerateHLSL]
        public enum FogColorMode
        {
            ConstantColor,
            SkyColor,
        }

        private readonly static int m_TypeParam = Shader.PropertyToID("_AtmosphericScatteringType");
        // Fog Color
        private readonly static int m_ColorModeParam = Shader.PropertyToID("_FogColorMode");
        private readonly static int m_FogColorParam = Shader.PropertyToID("_FogColor");
        private readonly static int m_MipFogParam = Shader.PropertyToID("_MipFogParameters");
        // Linear Fog
        private readonly static int m_LinearFogParam = Shader.PropertyToID("_LinearFogParameters");
        // Exp Fog
        private readonly static int m_ExpFogParam = Shader.PropertyToID("_ExpFogParameters");


        public FogType      type;

        // Fog Color
        public FogColorMode colorMode = FogColorMode.SkyColor;
        public Color        fogColor = Color.grey;
        [Range(0.0f, 1.0f)]
        public float        mipFogMaxMip = 1.0f;
        public float        mipFogNear = 0.0f;
        public float        mipFogFar = 1000.0f;

        // Linear Fog
        [Range(0.0f, 1.0f)]
        public float        linearFogDensity = 1.0f;
        public float        linearFogStart = 500.0f;
        public float        linearFogEnd = 1000.0f;

        // Exponential fog
        //[Min(0.0f)] Not available until 2018.1
        public float        expFogDistance = 100.0f;
        [Range(0.0f, 1.0f)]
        public float        expFogDensity = 1.0f;

        public bool NeedFogRendering()
        {
            return type != FogType.None;
        }

        public void PushShaderParameters(CommandBuffer cmd, FrameSettings frameSettings)
        {
            if(frameSettings.enableAtmosphericScattering)
                cmd.SetGlobalFloat(m_TypeParam, (float)type);
            else
                cmd.SetGlobalFloat(m_TypeParam, (float)FogType.None);

            // Fog Color
            cmd.SetGlobalFloat(m_ColorModeParam, (float)colorMode);
            cmd.SetGlobalColor(m_FogColorParam, fogColor);
            cmd.SetGlobalVector(m_MipFogParam, new Vector4(mipFogNear, mipFogFar, mipFogMaxMip, 0.0f));
            // Linear Fog
            cmd.SetGlobalVector(m_LinearFogParam, new Vector4(linearFogStart, linearFogEnd, 1.0f / (linearFogEnd - linearFogStart), linearFogDensity));
            // Exp fog
            cmd.SetGlobalVector(m_ExpFogParam, new Vector4(Mathf.Max(0.0f, expFogDistance), expFogDensity, 0.0f, 0.0f));
        }
    }

}
