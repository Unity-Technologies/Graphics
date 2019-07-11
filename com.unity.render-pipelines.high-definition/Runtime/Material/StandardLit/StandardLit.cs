namespace UnityEngine.Rendering.HighDefinition
{
    partial class StandardLit
    {
        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------
        [GenerateHLSL]
        public struct StandardBSDFData
        {
            // GBuffer0
            public Vector3 baseColor;
            public float specularOcclusion;

            // GBuffer1
            public Vector3 normalWS;
            public float perceptualRoughness;

            // Gbuffer2
            public Vector3 fresnel0;
            public float coatMask;

            // Gbuffer3
            public Vector3 emissiveAndBaked;

            // Gbuffer4
            public uint renderingLayers;

            // Gbuffer5
            public Vector4 shadowMasks;

            // Lighting flag
            public uint isUnlit;
        }
    }
}
