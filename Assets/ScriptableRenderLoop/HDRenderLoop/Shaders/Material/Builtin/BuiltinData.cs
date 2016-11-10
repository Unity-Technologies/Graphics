using UnityEngine;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.ScriptableRenderLoop
{
    namespace Builtin
    {
        //-----------------------------------------------------------------------------
        // BuiltinData
        // This structure include common data that should be present in all material
        // and are independent from the BSDF parametrization.
        // Note: These parameters can be store in GBuffer if the writer wants
        //-----------------------------------------------------------------------------
        [GenerateHLSL(PackingRules.Exact, false, true, 100)]
        public struct BuiltinData
        {
            [SurfaceDataAttributes("Opacity")]
            public float opacity;

            // These are lighting data.
            // We would prefer to split lighting and material information but for performance reasons,
            // those lighting information are fill
            // at the same time than material information.
            [SurfaceDataAttributes("Bake Diffuse Lighting")]
            public Vector3 bakeDiffuseLighting; // This is the result of sampling lightmap/lightprobe/proxyvolume

            [SurfaceDataAttributes("Emissive Color")]
            public Vector3 emissiveColor;
            [SurfaceDataAttributes("Emissive Intensity")]
            public float emissiveIntensity;

            // These is required for motion blur and temporalAA
            [SurfaceDataAttributes("Velocity")]
            public Vector2 velocity;

            // Distortion
            [SurfaceDataAttributes("Distortion")]
            public Vector2 distortion;
            [SurfaceDataAttributes("Distortion Blur")]
            public float distortionBlur;           // Define the color buffer mipmap level to use
        };

        //-----------------------------------------------------------------------------
        // LighTransportData
        // This struct is use to store information for Enlighten/Progressive light mapper. both at runtime or off line.
        //-----------------------------------------------------------------------------
        [GenerateHLSL(PackingRules.Exact, false, true, 120)]
        public struct LighTransportData
        {
            public Vector3 diffuseColor;
            public Vector3 emissiveColor;
        };

        [GenerateHLSL(PackingRules.Exact)]
        public enum GBufferBuiltin
        {
            Count = 1
        };

        public class RenderLoop : Object
        {
            // Note: Velocity buffer must be the last buffer of a GBuffer pass if applicable
            public RenderTextureFormat GetVelocityBufferFormat()
            {
                return RenderTextureFormat.RGHalf;
            }

            public RenderTextureReadWrite GetVelocityBufferReadWrite()
            {
                return RenderTextureReadWrite.Linear;
            }

            //-----------------------------------------------------------------------------
            // GBuffer management
            //-----------------------------------------------------------------------------

            public int GetGBufferCount() { return (int)GBufferBuiltin.Count; }

            public RenderTextureFormat[] RTFormat =
            {
                RenderTextureFormat.RGB111110Float
            };

            public RenderTextureReadWrite[] RTReadWrite =
            {
                RenderTextureReadWrite.Linear
            };

            public void Rebuild()
            {
            }
        }
    }
}
