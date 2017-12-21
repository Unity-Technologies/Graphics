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

        public MinFloatParameter    fogStart = new MinFloatParameter(500.0f, 0.0f);
        public MinFloatParameter    fogEnd = new MinFloatParameter(1000.0f, 0.0f);

        public override void PushShaderParameters(CommandBuffer cmd, FrameSettings frameSettings)
        {
            PushShaderParametersCommon(cmd, FogType.Linear, frameSettings);
            cmd.SetGlobalVector(m_LinearFogParam, new Vector4(fogStart, fogEnd, 1.0f / (fogEnd - fogStart), 0.0f));
        }
    }

}