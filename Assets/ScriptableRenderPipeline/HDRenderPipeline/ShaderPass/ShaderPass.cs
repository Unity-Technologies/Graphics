using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderPass
    {
        GBuffer,
        Forward,
        ForwardUnlit,
        DepthOnly,
        Velocity,
        Distortion,
        LightTransport
    }
}
