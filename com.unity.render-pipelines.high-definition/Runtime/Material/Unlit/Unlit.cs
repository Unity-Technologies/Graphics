using UnityEngine;
using System;
using UnityEngine.Experimental.Rendering.HDPipeline.Attributes;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class Unlit : RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, false, true, 300)]
        public struct SurfaceData
        {
            [MaterialSharedPropertyMapping(MaterialSharedProperty.Albedo)]
            [SurfaceDataAttributes("Color", false, true)]
            public Vector3 color;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, false, true, 350)]
        public struct BSDFData
        {
            [SurfaceDataAttributes("", false, true)]
            public Vector3 color;
        };
    }
}
