using UnityEngine;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    [GenerateHLSL]
    public enum ShadowType
    {
        Spot,
        Point
    }

    [GenerateHLSL]
    public struct PunctualShadowData
    {
        // World to ShadowMap matrix
        // Include scale and bias for shadow atlas if any
        public Vector4 shadowMatrix1;
        public Vector4 shadowMatrix2;
        public Vector4 shadowMatrix3;
        public Vector4 shadowMatrix4;

        public ShadowType shadowType;
        public Vector3 unused;
    };
} // namespace UnityEngine.Experimental.ScriptableRenderLoop
