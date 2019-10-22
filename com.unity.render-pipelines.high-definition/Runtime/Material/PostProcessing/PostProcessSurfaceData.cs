using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Main structure that store the user data (i.e user input of master node in material graph)
    [GenerateHLSL(PackingRules.Exact, false)]
    public struct PostProcessSurfaceData
    {          
        [SurfaceDataAttributes("Output")]
        public Vector3 output;
    };
}
