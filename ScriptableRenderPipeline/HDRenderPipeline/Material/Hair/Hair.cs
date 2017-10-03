using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Hair : RenderPipelineMaterial
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 2570)]
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

            // MaterialId dependent attribute

            // standard
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction)

            [SurfaceDataAttributes("FrontFacing")]
            public bool isFrontFace;

        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 2600)]
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

            public int materialId;

            // MaterialId dependent attribute

            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;
            public float roughnessT;
            public float roughnessB;
            public float anisotropy;

            public bool isFrontFace; //
        };
    }
}
