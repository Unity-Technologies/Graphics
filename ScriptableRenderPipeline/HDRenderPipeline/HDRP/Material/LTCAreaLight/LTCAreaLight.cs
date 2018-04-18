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
        Texture2DArray m_LtcData; // 0: m_LtcGGXMatrix - RGBA, 2: m_LtcDisneyDiffuseMatrix - RGBA, 3: m_LtcMultiGGXFresnelDisneyDiffuse - RGB, A unused

        const int k_LtcLUTMatrixDim = 3; // size of the matrix (3x3)
        const int k_LtcLUTResolution = 64;

        LTCAreaLight()
        {
            m_refCounting = 0;
        }

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
        void LoadLUT(Texture2DArray tex, int arrayElement, TextureFormat format, float[] LtcGGXMagnitudeData, float[] LtcGGXFresnelData, float[] LtcDisneyDiffuseMagnitudeData)
        {
            const int count = k_LtcLUTResolution * k_LtcLUTResolution;
            Color[] pixels = new Color[count];

            for (int i = 0; i < count; i++)
            {
                // We store the result of the subtraction as a run-time optimization.
                // See the footnote 2 of "LTC Fresnel Approximation" by Stephen Hill.
                pixels[i] = new Color(LtcGGXMagnitudeData[i] - LtcGGXFresnelData[i], LtcGGXFresnelData[i], LtcDisneyDiffuseMagnitudeData[i], 1);
            }

            tex.SetPixels(pixels, arrayElement);
        }

        public void Build()
        {
            Debug.Assert(m_refCounting >= 0);

            if (m_refCounting == 0)
            {
                m_LtcData = new Texture2DArray(k_LtcLUTResolution, k_LtcLUTResolution, 3, TextureFormat.RGBAHalf, false /*mipmap*/, true /* linear */)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = CoreUtils.GetTextureAutoName(k_LtcLUTResolution, k_LtcLUTResolution, TextureFormat.RGBAHalf, depth: 3, dim: TextureDimension.Tex2DArray, name: "LTC_LUT")
                };

                LoadLUT(m_LtcData, 0, TextureFormat.RGBAHalf, s_LtcGGXMatrixData);
                LoadLUT(m_LtcData, 1, TextureFormat.RGBAHalf, s_LtcDisneyDiffuseMatrixData);
                // TODO: switch to RGBA64 when it becomes available.
                LoadLUT(m_LtcData, 2, TextureFormat.RGBAHalf, s_LtcGGXMagnitudeData, s_LtcGGXFresnelData, s_LtcDisneyDiffuseMagnitudeData);

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
