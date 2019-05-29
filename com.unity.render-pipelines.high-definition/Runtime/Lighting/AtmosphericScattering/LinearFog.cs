using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [VolumeComponentMenu("Fog/Linear Fog")]
    public class LinearFog : AtmosphericScattering
    {
        private readonly static int m_LinearFogParam = Shader.PropertyToID("_LinearFogParameters");

        [Tooltip("Sets the distance from the Camera at which the density of the fog starts to increase from 0.")]
        public MinFloatParameter    fogStart = new MinFloatParameter(500.0f, 0.0f);
        [Tooltip("Sets the distance from the Camera at which the density of the fog reaches its maximum value.")]
        public MinFloatParameter    fogEnd = new MinFloatParameter(1000.0f, 0.0f);

        [Tooltip("Sets the height at which the density of the fog starts to decrease.")]
        public FloatParameter fogHeightStart = new FloatParameter(0.0f);
        [Tooltip("Sets the height at which the density of the fog reaches 0.")]
        public FloatParameter fogHeightEnd = new FloatParameter(10.0f);

        public override void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            PushShaderParametersCommon(hdCamera, cmd, FogType.Linear);
            cmd.SetGlobalVector(m_LinearFogParam, new Vector4(fogStart.value, 1.0f / (fogEnd.value - fogStart.value), fogHeightEnd.value, 1.0f / (fogHeightEnd.value - fogHeightStart.value)));
        }
    }
}
