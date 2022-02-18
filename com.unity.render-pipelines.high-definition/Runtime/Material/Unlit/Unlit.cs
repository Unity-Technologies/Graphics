using UnityEngine.Rendering.HighDefinition.Attributes;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Rendering.HighDefinition
{
    class Unlit : RenderPipelineMaterial
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

            // Both normalWS and shadowTint are used for shadow mattes

            [MaterialSharedPropertyMapping(MaterialSharedProperty.Normal)]
            // We should preprocessor: "defined(_ENABLE_SHADOW_MATTE)" however it will break compatibility with earlier version than 21.2 for shader.
            // Also maybe it could be used in the future for other purpose than shadow matte, so let it like that.
            [SurfaceDataAttributes(new string[] { "Normal", "Normal View Space" }, true)]
            public Vector3 normalWS;

            [SurfaceDataAttributes("Shadow Tint", false, true, preprocessor: "defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_PATH_TRACING)")]
            public Vector4 shadowTint;
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
