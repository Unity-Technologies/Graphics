using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition.LTC;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    internal enum LTCLightingModel
    {
        // Lit, Stack-Lit and Fabric/Silk
        GGX,
        DisneyDiffuse,

        // Fabric/CottonWool shader
        Charlie,
        FabricLambert,

        // Hair
        KajiyaKaySpecular,
        KajiyaKayDiffuse,
        Marschner,

        // Other
        CookTorrance,
        Ward,
        OrenNayar,
        Count
    }

    internal partial class LTCAreaLight
    {
        internal static IBRDF GetBRDFInterface(LTCLightingModel model)
        {
            switch (model)
            {
                case LTCLightingModel.GGX:
                    return new BRDF_GGX();
                case LTCLightingModel.DisneyDiffuse:
                    return new BRDF_Disney();

                case LTCLightingModel.Charlie:
                    return new BRDF_Charlie();
                case LTCLightingModel.FabricLambert:
                    return new BRDF_FabricLambert();

                case LTCLightingModel.KajiyaKaySpecular:
                    return new BRDF_KajiyaKaySpecular();
                case LTCLightingModel.KajiyaKayDiffuse:
                    return new BRDF_KajiyaKayDiffuse();
                case LTCLightingModel.Marschner:
                    return new BRDF_Marschner();

                case LTCLightingModel.CookTorrance:
                    return new BRDF_CookTorrance();
                case LTCLightingModel.Ward:
                    return new BRDF_Ward();
                case LTCLightingModel.OrenNayar:
                    return new BRDF_OrenNayar();
            }
            return new BRDF_GGX();
        }

        static LTCAreaLight s_Instance;

        internal static LTCAreaLight instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new LTCAreaLight();

                return s_Instance;
            }
        }

        int m_refCounting;

        // For area lighting - We pack all texture inside a texture array to reduce the number of resource required
        Texture2DArray m_LtcData; // 0: m_LtcGGXMatrix - RGBA, 1: m_LtcDisneyDiffuseMatrix - RGBA

        internal const int k_LtcLUTMatrixDim = 3; // size of the matrix (3x3)
        internal const int k_LtcLUTResolution = 64;

        internal LTCAreaLight()
        {
            m_refCounting = 0;
        }

        // Load LUT with 3x3 matrix in RGBA of a tex2D (some part are zero)
        internal static void LoadLUT(Texture2DArray tex, int arrayElement, GraphicsFormat format, double[,] LUTTransformInv)
        {
            const int count = k_LtcLUTResolution * k_LtcLUTResolution;
            Color[] pixels = new Color[count];

            float clampValue = (format == GraphicsFormat.R16G16B16A16_SFloat) ? 65504.0f : float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                // Both GGX and Disney Diffuse BRDFs have zero values in columns 1, 3, 5, 7.
                // Column 8 contains only ones.
                pixels[i] = new Color(Mathf.Min(clampValue, (float)LUTTransformInv[i, 0]),
                    Mathf.Min(clampValue, (float)LUTTransformInv[i, 2]),
                    Mathf.Min(clampValue, (float)LUTTransformInv[i, 4]),
                    Mathf.Min(clampValue, (float)LUTTransformInv[i, 6]));
            }

            tex.SetPixels(pixels, arrayElement);
        }

        internal void Build()
        {
            Debug.Assert(m_refCounting >= 0);

            if (m_refCounting == 0)
            {
                m_LtcData = new Texture2DArray(k_LtcLUTResolution, k_LtcLUTResolution, (int)LTCLightingModel.Count, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = CoreUtils.GetTextureAutoName(k_LtcLUTResolution, k_LtcLUTResolution, GraphicsFormat.R16G16B16A16_SFloat, depth: (int)LTCLightingModel.Count, dim: TextureDimension.Tex2DArray, name: "LTC_LUT")
                };

                LoadLUT(m_LtcData, (int)LTCLightingModel.GGX, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_GGX);
                LoadLUT(m_LtcData, (int)LTCLightingModel.DisneyDiffuse, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_Disney);

                LoadLUT(m_LtcData, (int)LTCLightingModel.Charlie, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_Charlie);
                LoadLUT(m_LtcData, (int)LTCLightingModel.FabricLambert, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_FabricLambert);

                LoadLUT(m_LtcData, (int)LTCLightingModel.KajiyaKaySpecular, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_KajiyaKaySpecular);
                LoadLUT(m_LtcData, (int)LTCLightingModel.KajiyaKayDiffuse, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_KajiyaKayDiffuse);
                // TODO: Generate the Marschner LCT Table

                LoadLUT(m_LtcData, (int)LTCLightingModel.CookTorrance, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_CookTorrance);
                LoadLUT(m_LtcData, (int)LTCLightingModel.Ward, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_Ward);
                LoadLUT(m_LtcData, (int)LTCLightingModel.OrenNayar, GraphicsFormat.R16G16B16A16_SFloat, s_LtcMatrixData_BRDF_OrenNayar);

                m_LtcData.Apply();
            }

            m_refCounting++;
        }

        internal void Cleanup()
        {
            m_refCounting--;

            if (m_refCounting == 0)
            {
                CoreUtils.Destroy(m_LtcData);
            }

            Debug.Assert(m_refCounting >= 0);
        }

        internal void Bind(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture("_LtcData", m_LtcData);
        }
    }
}
