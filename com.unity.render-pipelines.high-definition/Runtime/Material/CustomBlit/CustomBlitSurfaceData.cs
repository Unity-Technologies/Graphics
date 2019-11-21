using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HIghDefinition
{
    // Main structure that store the user data (i.e user input of master node in material graph)
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct SurfaceData
    {          
        [SurfaceDataAttributes("Output")]
        public Vector4 output;
    };
}
