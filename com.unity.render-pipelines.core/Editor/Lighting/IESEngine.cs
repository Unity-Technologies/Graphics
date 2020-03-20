using System.IO;
using Unity.Collections;
using UnityEngine;

namespace UnityEditor.Rendering
{
    public class IESEngine
    {
        // k_MinTextureSize should be 32, but using a larger value to minimize Unity's issue with cubemap cookies made from low-resolution latitude-longitude images.
        // When used, such a cubemap cookie North-South axis is visually tilted compared to its point light Y axis.
        // In other words, when the light Y rotation is modified, the cookie highlights and shadows wriggles on the floor and walls.
        const int k_MinTextureSize =  256; // power of two >= 32
        const int k_MaxTextureSize = 2048; // power of two <= 2048

        IESReader m_IesReader = new IESReader();

        public float TotalLumens { get => m_IesReader.TotalLumens; }
        public float MaxCandelas { get => m_IesReader.MaxCandelas; }

        public string ReadFile(string iesFilePath)
        {
            if (!File.Exists(iesFilePath))
            {
               return "IES file does not exist.";
            }

            string errorMessage;

            try
            {
                errorMessage = m_IesReader.ReadFile(iesFilePath);
            }
            catch (IOException ioEx)
            {
                return ioEx.Message;
            }

            return errorMessage;
        }

        public string GetKeywordValue(string keyword)
        {
            return m_IesReader.KeywordDictionary.ContainsKey(keyword) ? m_IesReader.KeywordDictionary[keyword] : string.Empty;
        }

        public (int height, int width) GetTextureSize()
        {
            int verticalAngleCount   = m_IesReader.VerticalAngleCount;
            int horizontalAngleCount = m_IesReader.GetRemappedHorizontalAngleCount();

            int height = Mathf.NextPowerOfTwo(Mathf.Clamp(verticalAngleCount,   k_MinTextureSize, k_MaxTextureSize)); // for 180 latitudinal degrees
            int width  = Mathf.NextPowerOfTwo(Mathf.Clamp(horizontalAngleCount, k_MinTextureSize, k_MaxTextureSize)); // for 360 longitudinal degrees

            return (height, width);
        }

        public NativeArray<Color32> BuildTextureBuffer((int height, int width) size)
        {
            var textureBuffer = new NativeArray<Color32>(size.height * size.width, Allocator.Temp);

            for (int y = 0; y < size.height; y++)
            {
                float v = m_IesReader.ComputeVerticalAnglePosition(m_IesReader.RemapVerticalAngle(((float)y / (size.height - 1)) * 180f));

                var slice = new NativeSlice<Color32>(textureBuffer, y * size.width, size.width);

                for (int x = 0; x < size.width; x++)
                {
                    float u = m_IesReader.ComputeHorizontalAnglePosition(m_IesReader.RemapHorizontalAngle(((float)x / (size.width - 1)) * 360f));

                    byte value = (byte)((m_IesReader.InterpolateBilinear(u, v) / m_IesReader.MaxCandelas) * 255);

                    slice[x] = new Color32(value, value, value, value);
                }
            }

            return textureBuffer;
        }

        public NativeArray<Color32> BuildHemi((int height, int width) size)
        {
            var textureBuffer = new NativeArray<Color32>(size.height * size.width, Allocator.Temp);

            for (int y = 0; y < size.height; y++)
            {
                float v = m_IesReader.ComputeVerticalAnglePosition(m_IesReader.RemapVerticalAngle(((float)y / (size.height - 1)) * 180f));

                var slice = new NativeSlice<Color32>(textureBuffer, y * size.width, size.width);

                for (int x = 0; x < size.width; x++)
                {
                    float u = m_IesReader.ComputeHorizontalAnglePosition(m_IesReader.RemapHorizontalAngle(((float)x / (size.width - 1)) * 360f));

                    byte value = (byte)((m_IesReader.InterpolateBilinear(u, v) / m_IesReader.MaxCandelas) * 255);

                    slice[x] = new Color32(value, value, value, value);
                }
            }

            return textureBuffer;
        }

        public NativeArray<Color32> BuildSphere((int height, int width) size)
        {
            var textureBuffer = new NativeArray<Color32>(size.height * size.width, Allocator.Temp);

            for (int y = 0; y < size.height; y++)
            {
                float v = m_IesReader.ComputeVerticalAnglePosition(m_IesReader.RemapVerticalAngle(((float)y / (size.height - 1)) * 180f));

                var slice = new NativeSlice<Color32>(textureBuffer, y * size.width, size.width);

                for (int x = 0; x < size.width; x++)
                {
                    float u = m_IesReader.ComputeHorizontalAnglePosition(m_IesReader.RemapHorizontalAngle(((float)x / (size.width - 1)) * 360f));

                    byte value = (byte)((m_IesReader.InterpolateBilinear(u, v) / m_IesReader.MaxCandelas) * 255);

                    slice[x] = new Color32(value, value, value, value);
                }
            }

            return textureBuffer;
        }
    }
}
