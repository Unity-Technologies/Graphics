using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ExponentialFog : AtmosphericScattering
    {
        // Exp Fog
        private readonly static int m_ExpFogParam = Shader.PropertyToID("_ExpFogParameters");

        // Exponential fog
        public ClampedFloatParameter fogDistance = new ClampedFloatParameter { value = 100.0f, min = 0.0f, clampMode = ParameterClampMode.Min };

        public override void PushShaderParameters(CommandBuffer cmd, RenderingDebugSettings renderingDebug)
        {
            PushShaderParametersCommon(cmd, FogType.Exponential, renderingDebug);

            cmd.SetGlobalVector(m_ExpFogParam, new Vector4(Mathf.Max(0.0f, fogDistance), 0.0f, 0.0f, 0.0f));
        }
    }

}