using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class Lit : RenderPipelineMaterial
    {
        // Currently we have only one materialId (Standard GGX), so it is not store in the GBuffer and we don't test for it

        // If change, be sure it match what is done in Lit.hlsl: MaterialFeatureFlagsFromGBuffer
        // Material bit mask must match the size define LightDefinitions.s_MaterialFeatureMaskFlags value
        [GenerateHLSL(PackingRules.Exact)]
        public enum MaterialFeatureFlags
        {
            LitStandard             = 1 << 0,   // For material classification we need to identify that we are indeed use as standard material, else we are consider as sky/background element
            LitSpecularColor        = 1 << 1,   // LitSpecularColor is not use statically but only dynamically
            LitSubsurfaceScattering = 1 << 2,
            LitTransmission         = 1 << 3,
            LitAnisotropy           = 1 << 4,
            LitIridescence          = 1 << 5,
            LitClearCoat            = 1 << 6
        };

        [GenerateHLSL(PackingRules.Exact)]
        public enum RefractionMode
        {
            None = 0,
            Plane = 1,
            Sphere = 2
        };

        //-----------------------------------------------------------------------------
        // SurfaceData
        //-----------------------------------------------------------------------------

        // Main structure that store the user data (i.e user input of master node in material graph)
        [GenerateHLSL(PackingRules.Exact, false, true, 1000)]
        public struct SurfaceData
        {
            [SurfaceDataAttributes("MaterialFeatures")]
            public uint materialFeatures;

            // Standard
            [SurfaceDataAttributes("Base Color", false, true)]
            public Vector3 baseColor;
            [SurfaceDataAttributes("Specular Occlusion")]
            public float specularOcclusion;

            [SurfaceDataAttributes("Normal", true)]
            public Vector3 normalWS;
            [SurfaceDataAttributes("Smoothness")]
            public float perceptualSmoothness;

            [SurfaceDataAttributes("Ambient Occlusion")]
            public float ambientOcclusion;

            [SurfaceDataAttributes("Metallic")]
            public float metallic;

            [SurfaceDataAttributes("Coat mask")]
            public float coatMask;

            // MaterialFeature dependent attribute

            // Specular Color
            [SurfaceDataAttributes("Specular Color", false, true)]
            public Vector3 specularColor;

            // SSS
            [SurfaceDataAttributes("Diffusion Profile")]
            public uint diffusionProfile;
            [SurfaceDataAttributes("Subsurface Mask")]
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            [SurfaceDataAttributes("Thickness")]
            public float thickness;

            // Anisotropic
            [SurfaceDataAttributes("Tangent", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("Anisotropy")]
            public float anisotropy; // anisotropic ratio(0->no isotropic; 1->full anisotropy in tangent direction, -1->full anisotropy in bitangent direction)

            // Iridescence
            public float thicknessIrid;

            // Forward property only

            // Transparency
            // Reuse thickness from SSS

            [SurfaceDataAttributes("Index of refraction")]
            public float ior;
            [SurfaceDataAttributes("Transmittance Color")]
            public Vector3 transmittanceColor;
            [SurfaceDataAttributes("Transmittance Absorption Distance")]
            public float atDistance;
            [SurfaceDataAttributes("Transmittance mask")]
            public float transmittanceMask;
        };

        //-----------------------------------------------------------------------------
        // BSDFData
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact, false, true, 1030)]
        public struct BSDFData
        {
            public uint materialFeatures;

            [SurfaceDataAttributes("", false, true)]
            public Vector3 diffuseColor;
            public Vector3 fresnel0;

            public float specularOcclusion;

            [SurfaceDataAttributes("", true)]
            public Vector3 normalWS;
            public float perceptualRoughness;

            public float coatMask;

            // MaterialFeature dependent attribute

            // SpecularColor fold into fresnel0

            // SSS
            public uint diffusionProfile;
            public float subsurfaceMask;

            // Transmission
            // + Diffusion Profile
            public float thickness;
            public bool useThickObjectMode; // Read from the diffusion profile
            public Vector3 transmittance;   // Precomputation of transmittance

            // Anisotropic
            [SurfaceDataAttributes("", true)]
            public Vector3 tangentWS;
            [SurfaceDataAttributes("", true)]
            public Vector3 bitangentWS;
            public float roughnessT;
            public float roughnessB;
            public float anisotropy;

            // Iridescence
            public float thicknessIrid;

            // ClearCoat
            public float coatRoughness; // Automatically fill

            // Forward property only

            // Transparency
            public float ior;
            // Reuse thickness from SSS
            public Vector3 absorptionCoefficient;
            public float transmittanceMask;
        };

        //-----------------------------------------------------------------------------
        // RenderLoop management
        //-----------------------------------------------------------------------------

        [GenerateHLSL(PackingRules.Exact)]
        public enum GBufferMaterial
        {
            // Note: This count doesn't include the velocity buffer. On shader and csharp side the velocity buffer will be added by the framework
            Count = 4
        };

        //-----------------------------------------------------------------------------
        // GBuffer management
        //-----------------------------------------------------------------------------

        public override int GetMaterialGBufferCount() { return (int)GBufferMaterial.Count; }

        RenderTextureFormat[] m_RTFormat4 = { RenderTextureFormat.ARGB32, RenderTextureFormat.ARGB2101010, RenderTextureFormat.ARGB32, RenderTextureFormat.RGB111110Float };
        RenderTextureReadWrite[] m_RTReadWrite4 = { RenderTextureReadWrite.sRGB, RenderTextureReadWrite.Linear, RenderTextureReadWrite.Linear, RenderTextureReadWrite.Linear };

        public override void GetMaterialGBufferDescription(out RenderTextureFormat[] RTFormat, out RenderTextureReadWrite[] RTReadWrite)
        {
            RTFormat = m_RTFormat4;
            RTReadWrite = m_RTReadWrite4;
        }

        //-----------------------------------------------------------------------------
        // Init precomputed texture
        //-----------------------------------------------------------------------------

        bool m_isInit;

        // For image based lighting
        Material      m_InitPreFGD;
        RenderTexture m_PreIntegratedFGD;

        // For area lighting - We pack all texture inside a texture array to reduce the number of resource required
        Texture2DArray m_LtcData; // 0: m_LtcGGXMatrix - RGBA, 2: m_LtcDisneyDiffuseMatrix - RGBA, 3: m_LtcMultiGGXFresnelDisneyDiffuse - RGB, A unused

        const int k_LtcLUTMatrixDim  =  3; // size of the matrix (3x3)
        const int k_LtcLUTResolution = 64;


        // Load LUT with one scalar in alpha of a tex2D
        void LoadLUT(Texture2DArray tex, int arrayElement, TextureFormat format, float[] LUTScalar)
        {
            const int count = k_LtcLUTResolution * k_LtcLUTResolution;
            Color[] pixels = new Color[count];

            for (int i = 0; i < count; i++)
            {
                pixels[i] = new Color(0, 0, 0, LUTScalar[i]);
            }

            tex.SetPixels(pixels, arrayElement);
        }

        // Load LUT with 3x3 matrix in RGBA of a tex2D (some part are zero)
        void LoadLUT(Texture2DArray tex, int arrayElement, TextureFormat format, double[,] LUTTransformInv)
        {
            const int count = k_LtcLUTResolution * k_LtcLUTResolution;
            Color[] pixels = new Color[count];

            for (int i = 0; i < count; i++)
            {
                // Both GGX and Disney Diffuse BRDFs have zero values in columns 1, 3, 5, 7.
                // Column 8 contains only ones.
                pixels[i] = new Color((float)LUTTransformInv[i, 0],
                        (float)LUTTransformInv[i, 2],
                        (float)LUTTransformInv[i, 4],
                        (float)LUTTransformInv[i, 6]);
            }

            tex.SetPixels(pixels, arrayElement);
        }

        // Special-case function for 'm_LtcMultiGGXFresnelDisneyDiffuse'.
        void LoadLUT(Texture2DArray tex, int arrayElement, TextureFormat format,   float[] LtcGGXMagnitudeData,
            float[] LtcGGXFresnelData,
            float[] LtcDisneyDiffuseMagnitudeData)
        {
            const int count = k_LtcLUTResolution * k_LtcLUTResolution;
            Color[] pixels = new Color[count];

            for (int i = 0; i < count; i++)
            {
                // We store the result of the subtraction as a run-time optimization.
                // See the footnote 2 of "LTC Fresnel Approximation" by Stephen Hill.
                pixels[i] = new Color(LtcGGXMagnitudeData[i] - LtcGGXFresnelData[i],
                        LtcGGXFresnelData[i], LtcDisneyDiffuseMagnitudeData[i], 1);
            }

            tex.SetPixels(pixels, arrayElement);
        }

        public Lit() {}

        public override void Build(HDRenderPipelineAsset hdAsset)
        {
            m_InitPreFGD = CoreUtils.CreateEngineMaterial("Hidden/HDRenderPipeline/PreIntegratedFGD");

            m_PreIntegratedFGD = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
            m_PreIntegratedFGD.filterMode = FilterMode.Bilinear;
            m_PreIntegratedFGD.wrapMode = TextureWrapMode.Clamp;
            m_PreIntegratedFGD.hideFlags = HideFlags.DontSave;
            m_PreIntegratedFGD.Create();

            m_LtcData = new Texture2DArray(k_LtcLUTResolution, k_LtcLUTResolution, 3, TextureFormat.RGBAHalf, false /*mipmap*/, true /* linear */)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            LoadLUT(m_LtcData, 0, TextureFormat.RGBAHalf,   s_LtcGGXMatrixData);
            LoadLUT(m_LtcData, 1, TextureFormat.RGBAHalf,   s_LtcDisneyDiffuseMatrixData);
            // TODO: switch to RGBA64 when it becomes available.
            LoadLUT(m_LtcData, 2, TextureFormat.RGBAHalf,   s_LtcGGXMagnitudeData, s_LtcGGXFresnelData, s_LtcDisneyDiffuseMagnitudeData);

            m_LtcData.Apply();

            m_isInit = false;
        }

        public override void Cleanup()
        {
            CoreUtils.Destroy(m_InitPreFGD);

            // TODO: how to delete RenderTexture ? or do we need to do it ?
            m_isInit = false;
        }

        public override void RenderInit(CommandBuffer cmd)
        {
            if (m_isInit)
                return;

            using (new ProfilingSample(cmd, "Init PreFGD"))
            {
                CoreUtils.DrawFullScreen(cmd, m_InitPreFGD, new RenderTargetIdentifier(m_PreIntegratedFGD));
            }

            m_isInit = true;
        }

        public override void Bind()
        {
            Shader.SetGlobalTexture("_PreIntegratedFGD", m_PreIntegratedFGD);
            Shader.SetGlobalTexture("_LtcData", m_LtcData);
        }
    }
}
