using UnityEngine;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class StackLit : RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1300)]
        public struct SurfaceData
        {
            // Standard
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            
            [SurfaceDataAttributes(new string[]{"Normal", "Normal View Space"}, true)]
            public Vector3 normalWS;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1400)]
        public struct BSDFData
        {
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            
            [SurfaceDataAttributes(new string[] { "Normal WS", "Normal View Space" }, true)]
            public Vector3 normalWS;
        };
    }
}
