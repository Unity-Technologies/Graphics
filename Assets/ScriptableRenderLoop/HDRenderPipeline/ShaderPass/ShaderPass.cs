using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.ScriptableRenderLoop
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
        LightTransport,
        DebugViewMaterial
    }
}
