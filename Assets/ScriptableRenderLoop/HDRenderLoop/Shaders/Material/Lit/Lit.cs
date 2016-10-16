using UnityEngine;
using UnityEngine.Rendering;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.ScriptableRenderLoop
{
    namespace Lit
    {
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialId
        {
            LitStandard = 0,
            LitSSS = 1,
            LitClearCoat = 2,
            LitSpecular = 3,
            LitAniso = 4 // Should be the last as it is not setup by the users but generated based on anisotropy property
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1000)]
        public struct SurfaceData
        {
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

        [GenerateHLSL(PackingRules.Exact, false, true, 1030)]
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
            public float anisotropy;

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

        //-----------------------------------------------------------------------------
        // GBuffer management
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact)]
        public enum GBufferMaterial
        {
            Count = 3
        };

        public class RenderLoop : Object
        {
            #if (VELOCITY_IN_GBUFFER)
            public const int s_GBufferCount = (int)GBufferMaterial.Count + 2; // +1 for emissive buffer
            #else
            public const int s_GBufferCount = (int)GBufferMaterial.Count + 1;
            #endif

            public int GetGBufferCount() { return s_GBufferCount; }

            public RenderTextureFormat[] RTFormat =
            {
                RenderTextureFormat.ARGB32,
                RenderTextureFormat.ARGB2101010,
                RenderTextureFormat.ARGB32,
                #if (VELOCITY_IN_GBUFFER)
                RenderTextureFormat.RGHalf,
                #endif
                RenderTextureFormat.RGB111110Float
            };

            public RenderTextureReadWrite[] RTReadWrite = 
            {
                RenderTextureReadWrite.sRGB,
                RenderTextureReadWrite.Linear,
                RenderTextureReadWrite.Linear,
                #if (VELOCITY_IN_GBUFFER)
                RenderTextureReadWrite.Linear,
                #endif
                RenderTextureReadWrite.Linear
            };
      
            public Material m_InitPreFGD;
            public bool isInit;
            private RenderTexture s_PreIntegratedFGD;

            Material CreateEngineMaterial(string shaderPath)
            {
                Material mat = new Material(Shader.Find(shaderPath) as Shader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                return mat;
            }

            public void Rebuild()
            {
                m_InitPreFGD = CreateEngineMaterial("Hidden/Unity/PreIntegratedFGD");
                s_PreIntegratedFGD = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBHalf);
                isInit = false;
            }

            public void OnDisable()
            {
                if (m_InitPreFGD) DestroyImmediate(m_InitPreFGD);
                // TODO: how to delete RenderTexture ?
                isInit = false;
            }

            public void RenderInit(UnityEngine.Rendering.RenderLoop renderLoop)
            {
                var cmd = new CommandBuffer();
                cmd.name = "Init PreFGD";
                cmd.Blit(null, new RenderTargetIdentifier(s_PreIntegratedFGD), m_InitPreFGD, 0);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                isInit = true;
            }

            public void Bind()
            {
                Shader.SetGlobalTexture("_PreIntegratedFGD", s_PreIntegratedFGD);
            }
        }
    }
}
