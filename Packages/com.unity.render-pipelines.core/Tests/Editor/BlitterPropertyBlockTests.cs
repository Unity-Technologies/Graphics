using NUnit.Framework;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Tests
{
    class BlitterPropertyBlockTests
    {
        const int k_Size = 16;
        const int k_MarkedMip = 3;

        static readonly Color32 k_Mip0Color = new Color32(255, 0, 0, 255);
        static readonly Color32 k_MarkedMipColor = new Color32(0, 255, 0, 255);
        static readonly Vector4 k_FullScaleBias = new Vector4(1, 1, 0, 0);

        Material m_Material;
        RTHandle m_Source;
        RenderTexture m_Destination;
        CommandBuffer m_Cmd;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Packages/com.unity.render-pipelines.core/Tests/Editor/BlitterPropertyBlockTest.shader");
            Assert.IsNotNull(shader, "Failed to load the BlitterPropertyBlockTest shader.");
            m_Material = CoreUtils.CreateEngineMaterial(shader);

            m_Source = RTHandles.Alloc(k_Size, k_Size, GraphicsFormat.R8G8B8A8_UNorm, filterMode: FilterMode.Point,
                useMipMap: true, autoGenerateMips: false, name: "BlitterTestSource");
            m_Destination = new RenderTexture(k_Size, k_Size, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None);
            m_Cmd = new CommandBuffer();

            m_Cmd.SetRenderTarget(m_Source, 0);
            m_Cmd.ClearRenderTarget(false, true, k_Mip0Color);
            m_Cmd.SetRenderTarget(m_Source, k_MarkedMip);
            m_Cmd.ClearRenderTarget(false, true, k_MarkedMipColor);
            Graphics.ExecuteCommandBuffer(m_Cmd);
            m_Cmd.Clear();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            m_Cmd?.Release();
            m_Source?.Release();
            CoreUtils.Destroy(m_Destination);
            CoreUtils.Destroy(m_Material);
        }

        [Test]
        public void MipLevelBlit_SamplesRequestedMip()
        {
            m_Cmd.SetRenderTarget(m_Destination);
            Blitter.BlitTexture(m_Cmd, m_Source, k_FullScaleBias, m_Material, 0, k_MarkedMip, 0);

            Assert.AreEqual(k_MarkedMipColor, ExecuteAndReadCenter());
        }

        [Test]
        public void MaterialBlitAfterMipLevelBlit_SamplesMipZero()
        {
            m_Cmd.SetRenderTarget(m_Destination);
            Blitter.BlitTexture(m_Cmd, m_Source, k_FullScaleBias, m_Material, 0, k_MarkedMip, 0);
            Blitter.BlitTexture(m_Cmd, m_Source, k_FullScaleBias, m_Material, 0);

            Assert.AreEqual(k_Mip0Color, ExecuteAndReadCenter(),
                "The material blit sampled the mip level of the previous blit.");
        }

        Color32 ExecuteAndReadCenter()
        {
            Graphics.ExecuteCommandBuffer(m_Cmd);
            m_Cmd.Clear();
            var request = AsyncGPUReadback.Request(m_Destination);
            request.WaitForCompletion();
            Assert.IsFalse(request.hasError, "GPU readback failed.");
            return request.GetData<Color32>()[k_Size / 2 * k_Size + k_Size / 2];
        }
    }
}
