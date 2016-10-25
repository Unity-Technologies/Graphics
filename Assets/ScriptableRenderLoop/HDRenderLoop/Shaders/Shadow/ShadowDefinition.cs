using UnityEngine;

namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    //-----------------------------------------------------------------------------
    // structure definition
    //-----------------------------------------------------------------------------

    [GenerateHLSL]
    public enum ShadowType
    {
        Spot,
        Directional,
        Point  
    };

    // TODO: we may have to add various parameters here for shadow
    // A point light is 6x PunctualShadowData
    [GenerateHLSL]
    public struct PunctualShadowData
    {
        // World to ShadowMap matrix
        // Include scale and bias for shadow atlas if any
        public Matrix4x4 worldToShadow;

        public ShadowType shadowType;
        public Vector3 unused;
    };

} // namespace UnityEngine.Experimental.ScriptableRenderLoop
