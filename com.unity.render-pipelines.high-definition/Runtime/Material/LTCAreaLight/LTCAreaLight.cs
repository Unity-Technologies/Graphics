using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public partial class LTCAreaLight
    {
        static LTCAreaLight s_Instance;

        public static LTCAreaLight instance
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

        public const int k_LtcLUTMatrixDim = 3; // size of the matrix (3x3)
        public const int k_LtcLUTResolution = 64;

        LTCAreaLight()
        {
            m_refCounting = 0;
        }

        // Load LUT with one scalar in alpha of a tex2D
        public static void LoadLUT(Texture2DArray tex, int arrayElement, TextureFormat format, float[] LUTScalar)
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
        public static void LoadLUT(Texture2DArray tex, int arrayElement, TextureFormat format, double[,] LUTTransformInv)
        {
            const int count = k_LtcLUTResolution * k_LtcLUTResolution;
            Color[] pixels = new Color[count];

            for (int i = 0; i < count; i++)
            {
                // Both GGX and Disney Diffuse BRDFs have zero values in columns 1, 3, 5, 7.
                // Column 8 contains only ones.
                pixels[i] = new Color(  (float)LUTTransformInv[i, 0],   // Upload m00
                                        (float)LUTTransformInv[i, 2],   // Upload m20
                                        (float)LUTTransformInv[i, 4],   // Upload m11
                                        (float)LUTTransformInv[i, 6]);  // Upload m02
            }

            tex.SetPixels(pixels, arrayElement);
        }

        // BMAYAUX (18/08/29) New LUT shape: as advised by S.Hill, the matrix is now renormalized by its central m11 term
        // We still have only 4 coefficients to upload as a texture
        void LoadLUT2(Texture2DArray tex, int arrayElement, TextureFormat format, double[,] LUTTransformInv)
        {
            const int count = k_LtcLUTResolution * k_LtcLUTResolution;
            Color[] pixels = new Color[count];

            for (int i = 0; i < count; i++)
            {
                pixels[i] = new Color(  (float)LUTTransformInv[i, 0],   // Upload m00
                                        (float)LUTTransformInv[i, 2],   // Upload m20
                                        (float)LUTTransformInv[i, 6],   // Upload m02
                                        (float)LUTTransformInv[i, 8]);  // Upload m22
            }

            tex.SetPixels(pixels, arrayElement);
        }

        public void Build()
        {
            Debug.Assert(m_refCounting >= 0);

            if (m_refCounting == 0)
            {
                m_LtcData = new Texture2DArray(k_LtcLUTResolution, k_LtcLUTResolution, 2 + 6, TextureFormat.RGBAHalf, false /*mipmap*/, true /* linear */)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = CoreUtils.GetTextureAutoName(k_LtcLUTResolution, k_LtcLUTResolution, TextureFormat.RGBAHalf, depth: 2, dim: TextureDimension.Tex2DArray, name: "LTC_LUT")
                };

                LoadLUT(m_LtcData, 0, TextureFormat.RGBAHalf, s_LtcGGXMatrixData);
                LoadLUT(m_LtcData, 1, TextureFormat.RGBAHalf, s_LtcDisneyDiffuseMatrixData);


                // BMAYAUX (18/07/04) New BRDF Fittings
//Debug.Log( "Updating LTC tables!" );
                LoadLUT2(m_LtcData, 2, TextureFormat.RGBAHalf, s_LtcMatrixData_GGX);
                LoadLUT2(m_LtcData, 3, TextureFormat.RGBAHalf, s_LtcMatrixData_Disney);
                LoadLUT2(m_LtcData, 4, TextureFormat.RGBAHalf, s_LtcMatrixData_CookTorrance);
                LoadLUT2(m_LtcData, 5, TextureFormat.RGBAHalf, s_LtcMatrixData_Charlie);
                LoadLUT2(m_LtcData, 6, TextureFormat.RGBAHalf, s_LtcMatrixData_Ward);
                LoadLUT2(m_LtcData, 7, TextureFormat.RGBAHalf, s_LtcMatrixData_OrenNayar);

                m_LtcData.Apply();
            }

            m_refCounting++;
        }

        public void Cleanup()
        {
            m_refCounting--;

            if (m_refCounting == 0)
            {
                CoreUtils.Destroy(m_LtcData);
            }

            Debug.Assert(m_refCounting >= 0);
        }

        public void Bind()
        {
            Shader.SetGlobalTexture("_LtcData", m_LtcData);
        }
    }
}
