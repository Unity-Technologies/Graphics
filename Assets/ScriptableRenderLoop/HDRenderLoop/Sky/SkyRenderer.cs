using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System;


namespace UnityEngine.Experimental.ScriptableRenderLoop
{

    abstract public class SkyRenderer
    {
        abstract public void Build();
        abstract public void Cleanup();
        abstract public void RenderSky(BuiltinSkyParameters builtinParams, SkyParameters skyParameters);
        abstract public bool IsSkyValid(SkyParameters skyParameters);
    }
}
