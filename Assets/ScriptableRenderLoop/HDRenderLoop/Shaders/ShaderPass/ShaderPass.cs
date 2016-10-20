using UnityEngine;
using UnityEngine.Rendering;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.ScriptableRenderLoop
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
