using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    namespace Unlit
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1100)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Color")]
            public Vector3 color;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1130)]
        public struct BSDFData
        {
            public Vector3 color;
        };
    }
}
