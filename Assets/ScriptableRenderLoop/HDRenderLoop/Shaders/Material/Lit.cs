using UnityEngine;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.ScriptableRenderLoop
{
    namespace Lit
    {
        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, true, 1000)]
        public struct SurfaceData
        {
            public enum MaterialId
            {
                LIT_STANDARD = 0,
                LIT_SSS = 1,
                LIT_CLEARCOAT = 2,
                LIT_SPECULAR = 3
            };

            [SurfaceDataAttributes("Base Color")]
            public Vector3 baseColor;
            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [SurfaceDataAttributes("Normal")]
            public Vector3 normalWS;
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;
            [SurfaceDataAttributes("Material ID")]
            public MaterialId materialId;

            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            // MaterialId dependent attribute

            // standard
            [SurfaceDataAttributes("Tangent")]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction)
            [SurfaceDataAttributes("Metalic")]
            public float metalic;
            [SurfaceDataAttributes("Specular")]
            public float specular; // 0.02, 0.04, 0.16, 0.2

            // SSS
            [SurfaceDataAttributes("SubSurface Radius")]
            public float subSurfaceRadius;
            [SurfaceDataAttributes("Thickness")]
            public float thickness;
            [SurfaceDataAttributes("SubSurface Profile")]
            public int subSurfaceProfile;

            // Clearcoat
            [SurfaceDataAttributes("Coat Normal")]
            public Vector3 coatNormalWS;
            [SurfaceDataAttributes("Coat Smoothness")]
            public float coatPerceptualSmoothness;

            // SpecColor
            [SurfaceDataAttributes("Specular Color")]
            public Vector3 specularColor;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, true, 1030)]
        public struct BSDFData
        {
            public Vector3 diffuseColor;

            public Vector3 fresnel0;

            public float specularOcclusion;

            public Vector3 normalWS;
            public float perceptualRoughness;
            public float roughness;
            public float materialId;

            // MaterialId dependent attribute

            // standard
            public Vector3 tangentWS;
            public Vector3 bitangentWS;
            public float roughnessT;
            public float roughnessB;

            // fold into fresnel0

            // SSS
            public float subSurfaceRadius;
            public float thickness;
            public int subSurfaceProfile;

            // Clearcoat
            public Vector3 coatNormalWS;
            public float coatRoughness;

            // SpecColor
            // fold into fresnel0
        };
    }
}