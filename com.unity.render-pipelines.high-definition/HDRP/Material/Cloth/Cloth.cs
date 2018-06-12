using UnityEngine;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ShaderName : RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        // The number '10000' must be a unique value across all material. It give an id to each of the parameters
        // so debug material mode can retrieve it and display it. With 10000, it mean that debug values
        // are sign from 10000 with incremental order.
        [GenerateHLSL(PackingRules.Exact, false, true, 10000)]
        public struct SurfaceData
        {
            // Standard
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            
            [SurfaceDataAttributes("Normal", true)]
            public Vector3 normalWS;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 10100)]
        public struct BSDFData
        {
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            
            [SurfaceDataAttributes("Normal", true)]
            public Vector3 normalWS;
        };
    }
}
