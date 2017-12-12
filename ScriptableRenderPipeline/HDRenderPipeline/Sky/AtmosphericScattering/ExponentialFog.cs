using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ExponentialFog : AtmosphericScattering
    {
        private readonly static int m_ExpFogParam = Shader.PropertyToID("_ExpFogParameters");

        public MinFloatParameter fogDistance = new MinFloatParameter(200.0f, 0.0f);

        public override void PushShaderParameters(CommandBuffer cmd, RenderingDebugSettings renderingDebug)
        {
            PushShaderParametersCommon(cmd, FogType.Exponential, renderingDebug);
            cmd.SetGlobalVector(m_ExpFogParam, new Vector4(Mathf.Max(0.0f, fogDistance), 0.0f, 0.0f, 0.0f));
        }
    }

}