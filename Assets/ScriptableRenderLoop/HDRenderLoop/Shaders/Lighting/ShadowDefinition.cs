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
        Directional,
        Point  
    };

    // A point light is 6x PunctualShadowData
    [GenerateHLSL]
    public struct PunctualShadowData
    {
        // World to ShadowMap matrix
        // Include scale and bias for shadow atlas if any
        public Vector4 worldToShadow0;
        public Vector4 worldToShadow1;
        public Vector4 worldToShadow2;
        public Vector4 worldToShadow3;

        public ShadowType shadowType;
        public Vector3 unused;
    };
} // namespace UnityEngine.Experimental.ScriptableRenderLoop
