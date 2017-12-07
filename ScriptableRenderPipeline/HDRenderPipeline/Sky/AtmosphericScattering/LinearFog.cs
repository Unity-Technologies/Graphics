using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class LinearFog : AtmosphericScattering
    {
        private readonly static int m_LinearFogParam = Shader.PropertyToID("_LinearFogParameters");

        // Linear Fog
        public ClampedFloatParameter    fogStart = new ClampedFloatParameter { value = 500.0f, min = 0.0f, clampMode = ParameterClampMode.Min };
        public ClampedFloatParameter    fogEnd = new ClampedFloatParameter { value = 1000.0f, min = 0.0f, clampMode = ParameterClampMode.Min };

        public override void PushShaderParameters(CommandBuffer cmd, RenderingDebugSettings renderingDebug)
        {
            PushShaderParametersCommon(cmd, FogType.Linear, renderingDebug);

            // Linear Fog
            cmd.SetGlobalVector(m_LinearFogParam, new Vector4(fogStart, fogEnd, 1.0f / (fogEnd - fogStart), 0.0f));
        }
    }

}