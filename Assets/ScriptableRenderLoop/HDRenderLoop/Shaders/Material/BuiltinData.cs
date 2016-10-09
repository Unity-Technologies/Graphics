using UnityEngine;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.ScriptableRenderLoop
{
    namespace Builtin
    {
        //-----------------------------------------------------------------------------
        // BuiltinData
        // This structure include common data that should be present in all material
        // and are independent from the BSDF parametrization.
        // Note: These parameters can be store in GBuffer if the writer wants
        //-----------------------------------------------------------------------------
        [GenerateHLSL(PackingRules.Exact, true, 100)]
        public struct BuiltinData
        {
            public float opacity;

            // These are lighting data.
            // We would prefer to split lighting and material information but for performance reasons, 
            // those lighting information are fill 
            // at the same time than material information.
            public Vector3 bakeDiffuseLighting; // This is the result of sampling lightmap/lightprobe/proxyvolume

            public Vector3 emissiveColor;
            public float emissiveIntensity;

            // These is required for motion blur and temporalAA
            public Vector2 velocity;

            // Distortion
            public Vector2 distortion;
            public float distortionBlur;           // Define the color buffer mipmap level to use
        };
    }
}
