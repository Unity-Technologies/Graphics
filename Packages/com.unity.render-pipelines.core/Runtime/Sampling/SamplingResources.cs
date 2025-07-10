using System;
using System.Runtime.InteropServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.Sampling
{

    internal sealed class SamplingResources : IDisposable
    {
        internal enum ResourceType
        {
            BlueNoiseTextures = 1,
            SobolMatrices = 2,
            All = BlueNoiseTextures | SobolMatrices
        };

        private Texture2D m_SobolScramblingTile;
        private Texture2D m_SobolRankingTile;
        private Texture2D m_SobolOwenScrambled256Samples;
        private GraphicsBuffer m_SobolBuffer;

        static public readonly uint[] sobolMatrices = SobolData.SobolMatrices;

#if UNITY_EDITOR
        public void Load(uint resourceBitmask = (uint)ResourceType.BlueNoiseTextures)
        {
            if ((resourceBitmask & (uint)ResourceType.BlueNoiseTextures) != 0)
            {
                const string path = "Packages/com.unity.render-pipelines.core/Runtime/";

                m_SobolScramblingTile = AssetDatabase.LoadAssetAtPath<Texture2D>(path + "Sampling/Textures/SobolBlueNoise/ScramblingTile256SPP.png");
                m_SobolRankingTile = AssetDatabase.LoadAssetAtPath<Texture2D>(path + "Sampling/Textures/SobolBlueNoise/RankingTile256SPP.png");
                m_SobolOwenScrambled256Samples = AssetDatabase.LoadAssetAtPath<Texture2D>(path + "Sampling/Textures/SobolBlueNoise/SobolOwenScrambled256.png");
            }

            if ((resourceBitmask & (uint)ResourceType.SobolMatrices) != 0)
            {
                int sobolBufferSize = (int)(SobolData.SobolDims * SobolData.SobolSize);
                m_SobolBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sobolBufferSize, Marshal.SizeOf<uint>());
                m_SobolBuffer.SetData(SobolData.SobolMatrices);
            }
        }
#endif

        public static void Bind(CommandBuffer cmd, SamplingResources resources)
        {
            if (resources.m_SobolScramblingTile != null)
            {
                cmd.SetGlobalTexture(Shader.PropertyToID("_SobolScramblingTile"), resources.m_SobolScramblingTile);
                cmd.SetGlobalTexture(Shader.PropertyToID("_SobolRankingTile"), resources.m_SobolRankingTile);
                cmd.SetGlobalTexture(Shader.PropertyToID("_SobolOwenScrambledSequence"), resources.m_SobolOwenScrambled256Samples);
            }

            if (resources.m_SobolBuffer != null)
                cmd.SetGlobalBuffer("_SobolMatricesBuffer", resources.m_SobolBuffer);

        }

        public void Dispose()
        {
            m_SobolBuffer?.Dispose();
        }

    }
}


