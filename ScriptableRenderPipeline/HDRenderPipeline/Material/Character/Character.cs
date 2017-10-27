using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Character : RenderPipelineMaterial
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum CharacterMaterialID
        {
            Skin = 0,
            Hair = 1,
            Eye  = 2
        };

        //TODO: Feature Flags

        [GenerateHLSL(PackingRules.Exact, false, true, 4000)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("Diffuse Color", false, true)]
            public Vector3 diffuseColor;
            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;
            [SurfaceDataAttributes("Normal", true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;
            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            //Character Material ID Attributes
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy;
        }

        [GenerateHLSL(PackingRules.Exact, false, true, 4030)]
        public struct BSDFData
        {
            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;
            public float specularOcclusion;

            [SurfaceDataAttributes("", true)]
            public Vector3 normalWS;
            public float perceptualRoughness;
            public float roughness;

            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;
            public float roughnessT;
            public float roughnessB;
            public float anisotropy;
        }
    }
}
