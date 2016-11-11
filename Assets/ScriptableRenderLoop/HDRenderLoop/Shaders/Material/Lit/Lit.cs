using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.ScriptableRenderLoop
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
            [SurfaceDataAttributes("Metallic")]
            public float metallic;
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
        // RenderLoop management
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact)]
        public enum GBufferMaterial
        {
            Count = 3
        };

        public partial class RenderLoop : Object
        {
            //-----------------------------------------------------------------------------
            // GBuffer management
            //-----------------------------------------------------------------------------

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
      
            //-----------------------------------------------------------------------------
            // Init precomputed texture
            //-----------------------------------------------------------------------------

            public bool isInit;                      

            // For image based lighting
            private Material m_InitPreFGD;  
            private RenderTexture m_PreIntegratedFGD;

            // For area lighting
            private Texture2D m_LtcGGXMatrix;
            private Texture2D m_LtcGGXMagnitude;
            const int k_LtcLUTMatrixDim = 3; // size of the matrix (3x3)
            const int k_LtcLUTResolution = 64;

            Material CreateEngineMaterial(string shaderPath)
            {
                Material mat = new Material(Shader.Find(shaderPath) as Shader);
                mat.hideFlags = HideFlags.HideAndDontSave;
                return mat;
            }            

            Texture2D CreateLUT(int width, int height, TextureFormat format, Color[] pixels)
            {
                Texture2D tex = new Texture2D(width, height, format, false /*mipmap*/, true /*linear*/);
                tex.hideFlags = HideFlags.HideAndDontSave;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }

            // Load LUT with one scalar in alpha of a tex2D
            Texture2D LoadLUT(TextureFormat format, float[] LUTScalar)
            {
                const int count = k_LtcLUTResolution * k_LtcLUTResolution;
                Color[] pixels = new Color[count];

                // amplitude
                for (int i = 0; i < count; i++)
                {
                    pixels[i] = new Color(0, 0, 0, LUTScalar[i]);
                }

                return CreateLUT(k_LtcLUTResolution, k_LtcLUTResolution, format, pixels);
            }

            // Load LUT with 3x3 matrix in RGBA of a tex2D (some part are zero)
            Texture2D LoadLUT(TextureFormat format, double[,] LUTTransformInv)
            {
                const int count = k_LtcLUTResolution * k_LtcLUTResolution;
                Color[] pixels = new Color[count];

                // transformInv
                for (int i = 0; i < count; i++)
                {
                    // Only columns 0, 2, 4 and 6 contain interesting values (at least in the case of GGX).
                    pixels[i] = new Color(  (float)LUTTransformInv[i, 0],
                                            (float)LUTTransformInv[i, 2],
                                            (float)LUTTransformInv[i, 4],
                                            (float)LUTTransformInv[i, 6]);
                }

                return CreateLUT(k_LtcLUTResolution, k_LtcLUTResolution, format, pixels);
            }

            public void Rebuild()
            {
                m_InitPreFGD = CreateEngineMaterial("Hidden/HDRenderLoop/PreIntegratedFGD");
                m_PreIntegratedFGD = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBHalf);

                m_LtcGGXMatrix = LoadLUT(TextureFormat.RGBAHalf, s_LtcGGXMatrixData);
                m_LtcGGXMagnitude = LoadLUT(TextureFormat.RGBAHalf, s_LtcGGXMagnitudeData);

                isInit = false;
            }

            public void OnDisable()
            {
                if (m_InitPreFGD) DestroyImmediate(m_InitPreFGD);
                // TODO: how to delete RenderTexture ?
                isInit = false;
            }

            public void RenderInit(UnityEngine.Experimental.Rendering.RenderLoop renderLoop)
            {
                var cmd = new CommandBuffer();
                cmd.name = "Init PreFGD";
                cmd.Blit(null, new RenderTargetIdentifier(m_PreIntegratedFGD), m_InitPreFGD, 0);
                renderLoop.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                isInit = true;
            }

            public void Bind()
            {
                Shader.SetGlobalTexture("_PreIntegratedFGD", m_PreIntegratedFGD);
                Shader.SetGlobalTexture("_LtcGGXMatrix", m_LtcGGXMatrix);
                Shader.SetGlobalTexture("_LtcGGXMagnitude", m_LtcGGXMagnitude);
            }
        }
    }
}
