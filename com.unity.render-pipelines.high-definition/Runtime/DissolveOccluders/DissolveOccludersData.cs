// custom-begin:
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEngine.Rendering.HighDefinition
{
    public static class DissolveOccludersData
    {
        [GenerateHLSL]
        public struct DissolveOccludersCylinder
        {
            public Vector4 ellipseFromNDCScaleBias;
            public Vector2 alphaFromEllipseScaleBias;
            public float positionNDCZ;
            public float positionWSY;
        }
    }
}
// custom-end