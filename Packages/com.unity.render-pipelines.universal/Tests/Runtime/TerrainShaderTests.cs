using NUnit.Framework;
using System;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// Tests for terrain shaders in the Universal Render Pipeline.
    /// Verifies that terrain detail shaders are available through both Shader.Find() and UniversalRenderPipelineAsset properties.
    /// </summary>
    class TerrainShaderTests
    {
        UniversalRenderPipelineAsset m_UrpAsset;

        [SetUp]
        public void Setup()
        {
            // Only run this with URP in smoke tests.
            if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset)
                Assert.Ignore("URP Only test");

            m_UrpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }

        /// <summary>
        /// Tests that the URP terrain detail shaders can be found using Shader.Find().
        /// </summary>
        [TestCaseSource(nameof(ShaderNames))]
        public void TerrainShaders_ShaderFind_ReturnsValidShader(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader, $"{shaderName} should be found via Shader.Find()");
            Assert.IsTrue(shader.isSupported, $"{shaderName} should be supported on current platform");
        }

        private static readonly string[] ShaderNames =
        {
            "Hidden/TerrainEngine/Details/UniversalPipeline/Vertexlit",
            "Hidden/TerrainEngine/Details/UniversalPipeline/WavingDoublePass",
            "Hidden/TerrainEngine/Details/UniversalPipeline/BillboardWavingDoublePass"
        };

        /// <summary>
        /// Tests that the UniversalRenderPipelineAsset terrain detail shader properties returns valid shaders.
        /// </summary>
        [TestCaseSource(nameof(URPShaderAccessors))]
        public void UniversalRenderPipelineAsset_TerrainShaders_ReturnsValidShaders(Func<UniversalRenderPipelineAsset, Shader> getShader, string description)
        {
            Assert.IsNotNull(m_UrpAsset, "UniversalRenderPipelineAsset should be available");
            var shader = getShader(m_UrpAsset);
            Assert.IsNotNull(shader, $"{description} should not be null");
            Assert.IsTrue(shader.isSupported, $"{description} should be supported on current platform");
        }

        private static readonly object[] URPShaderAccessors =
        {
            new object[] { new Func<UniversalRenderPipelineAsset, Shader>(a => a.terrainDetailLitShader), "URP terrainDetailLitShader" },
            new object[] { new Func<UniversalRenderPipelineAsset, Shader>(a => a.terrainDetailGrassShader), "URP terrainDetailGrassShader" },
            new object[] { new Func<UniversalRenderPipelineAsset, Shader>(a => a.terrainDetailGrassBillboardShader), "URP terrainDetailGrassBillboardShader" }
        };

    }
}