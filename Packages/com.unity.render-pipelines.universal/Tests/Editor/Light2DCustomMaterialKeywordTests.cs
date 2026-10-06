using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEditor.Rendering.Universal.Tests
{
    // A custom Light2D material whose shader does not declare the 2D lighting keywords must not
    // log "Local keyword X doesn't exist in the shader." when the keyword setters run per draw.
    // A shader that does declare them must still get them set through the command buffer.
    class Light2DCustomMaterialKeywordTests
    {
        const string k_KeywordlessShader = @"
Shader ""Hidden/Test/KeywordlessLight2D""
{
    SubShader
    {
        Pass
        {
            Name ""Light2D""
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 pos : POSITION) : SV_POSITION { return pos; }
            half4 frag() : SV_Target { return 1; }
            ENDHLSL
        }
    }
}";

        const string k_KeywordShader = @"
Shader ""Hidden/Test/KeywordLight2D""
{
    SubShader
    {
        Pass
        {
            Name ""Light2D""
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ USE_VOLUMETRIC
            #pragma multi_compile_local _ USE_ADDITIVE_BLENDING
            #pragma multi_compile_local _ USE_POINT_LIGHT_COOKIES
            #pragma multi_compile_local _ USE_NORMAL_MAP
            float4 vert(float4 pos : POSITION) : SV_POSITION { return pos; }
            half4 frag() : SV_Target { return 1; }
            ENDHLSL
        }
    }
}";

        const string k_PartialKeywordShader = @"
Shader ""Hidden/Test/PartialKeywordLight2D""
{
    SubShader
    {
        Pass
        {
            Name ""Light2D""
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ USE_VOLUMETRIC
            #pragma multi_compile_local _ USE_NORMAL_MAP
            float4 vert(float4 pos : POSITION) : SV_POSITION { return pos; }
            half4 frag() : SV_Target { return 1; }
            ENDHLSL
        }
    }
}";

        static readonly string[] k_LightKeywords =
        {
            "USE_VOLUMETRIC", "USE_ADDITIVE_BLENDING", "USE_POINT_LIGHT_COOKIES", "USE_NORMAL_MAP"
        };

        Shader m_Shader;
        Material m_Material;
        CommandBuffer m_CommandBuffer;

        void Create(string shaderSource)
        {
            m_Shader = ShaderUtil.CreateShaderAsset(shaderSource);
            m_Material = new Material(m_Shader);
            m_CommandBuffer = new CommandBuffer();
        }

        [TearDown]
        public void TearDown()
        {
            m_CommandBuffer?.Release();
            m_CommandBuffer = null;
            if (m_Material != null)
                Object.DestroyImmediate(m_Material);
            m_Material = null;
            if (m_Shader != null)
                Object.DestroyImmediate(m_Shader);
            m_Shader = null;
        }

        void SetAllKeywords(bool enabled)
        {
            var cmd = CommandBufferHelpers.GetRasterCommandBuffer(m_CommandBuffer);
            RendererLighting.SetVolumetricKeyword(cmd, m_Material, enabled);
            RendererLighting.SetAdditiveBlendingKeyword(cmd, m_Material, enabled);
            RendererLighting.SetPointLightCookiesKeyword(cmd, m_Material, enabled);
            RendererLighting.SetNormalMapKeyword(cmd, m_Material, enabled);
        }

        [Test]
        public void KeywordSetters_ShaderWithoutLightingKeywords_LogNoErrors()
        {
            Create(k_KeywordlessShader);

            SetAllKeywords(true);
            SetAllKeywords(false);

            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(0, m_CommandBuffer.sizeInBytes, "No keyword command should be recorded for undeclared keywords.");
        }

        [Test]
        public void KeywordSetters_ShaderWithLightingKeywords_SetKeywordsOnMaterial()
        {
            Create(k_KeywordShader);
            foreach (var name in k_LightKeywords)
                Assert.IsTrue(m_Shader.keywordSpace.FindKeyword(name).isValid, $"{name} should be declared by the test shader.");

            SetAllKeywords(true);
            Assert.Greater(m_CommandBuffer.sizeInBytes, 0);
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            foreach (var name in k_LightKeywords)
                Assert.IsTrue(m_Material.IsKeywordEnabled(new LocalKeyword(m_Shader, name)), $"{name} should be enabled.");

            m_CommandBuffer.Clear();
            SetAllKeywords(false);
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            foreach (var name in k_LightKeywords)
                Assert.IsFalse(m_Material.IsKeywordEnabled(new LocalKeyword(m_Shader, name)), $"{name} should be disabled.");

            LogAssert.NoUnexpectedReceived();
        }

        // A shader that declares only some of the keywords gets those set and the rest skipped.
        [Test]
        public void KeywordSetters_ShaderWithSomeLightingKeywords_SetsOnlyDeclaredKeywords()
        {
            Create(k_PartialKeywordShader);

            SetAllKeywords(true);
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            Assert.IsTrue(m_Material.IsKeywordEnabled(new LocalKeyword(m_Shader, "USE_VOLUMETRIC")));
            Assert.IsTrue(m_Material.IsKeywordEnabled(new LocalKeyword(m_Shader, "USE_NORMAL_MAP")));
            Assert.AreEqual(2, m_Material.enabledKeywords.Length, "Only the two declared keywords should be enabled.");

            m_CommandBuffer.Clear();
            SetAllKeywords(false);
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            Assert.AreEqual(0, m_Material.enabledKeywords.Length, "The declared keywords should be disabled again.");

            LogAssert.NoUnexpectedReceived();
        }

        // ShadowRendering's custom caster keywords go through the same helper on an UnsafeCommandBuffer.
        [Test]
        public void SetLocalKeywordIfDeclared_UnsafeCommandBuffer_SkipsUndeclaredKeyword()
        {
            Create(k_KeywordlessShader);
            var cmd = CommandBufferHelpers.GetUnsafeCommandBuffer(m_CommandBuffer);
            RendererLighting.SetLocalKeywordIfDeclared(cmd, m_Material, "USE_VOLUMETRIC", true);

            LogAssert.NoUnexpectedReceived();
            Assert.AreEqual(0, m_CommandBuffer.sizeInBytes, "No keyword command should be recorded for an undeclared keyword.");
        }

        [Test]
        public void SetLocalKeywordIfDeclared_UnsafeCommandBuffer_SetsDeclaredKeyword()
        {
            Create(k_KeywordShader);
            var cmd = CommandBufferHelpers.GetUnsafeCommandBuffer(m_CommandBuffer);
            RendererLighting.SetLocalKeywordIfDeclared(cmd, m_Material, "USE_VOLUMETRIC", true);
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            Assert.IsTrue(m_Material.IsKeywordEnabled(new LocalKeyword(m_Shader, "USE_VOLUMETRIC")));

            LogAssert.NoUnexpectedReceived();
        }
    }
}
