using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    // UUM-152882: edits to a Light2D's custom material must reach the material the light is drawn
    // with, without a domain reload or Play mode change.
    class Light2DCustomMaterialSyncTests
    {
        const string k_ColorShader = @"
Shader ""Hidden/Test/ColorLight2D""
{
    Properties
    {
        _Volumetric (""Volumetric"", Color) = (1, 1, 1, 1)
        _SrcBlend (""SrcBlend"", Float) = 5
        _DstBlend (""DstBlend"", Float) = 10
        _VolSrcBlend (""VolSrcBlend"", Float) = 5
        _VolDstBlend (""VolDstBlend"", Float) = 10
    }
    SubShader
    {
        Pass
        {
            Name ""Light2D""
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 _Volumetric;
            float4 vert(float4 pos : POSITION) : SV_POSITION { return pos; }
            half4 frag() : SV_Target { return _Volumetric; }
            ENDHLSL
        }
    }
}";

        const string k_DualPassShader = @"
Shader ""Hidden/Test/DualPassLight2D""
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
        Pass
        {
            Name ""Light2DVolumetric""
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            float4 vert(float4 pos : POSITION) : SV_POSITION { return pos; }
            half4 frag() : SV_Target { return 1; }
            ENDHLSL
        }
    }
}";

        static readonly int k_VolumetricID = Shader.PropertyToID("_Volumetric");
        static readonly int k_SrcBlendID = Shader.PropertyToID("_SrcBlend");
        static readonly int k_DstBlendID = Shader.PropertyToID("_DstBlend");
        static readonly int k_VolSrcBlendID = Shader.PropertyToID("_VolSrcBlend");
        static readonly int k_VolDstBlendID = Shader.PropertyToID("_VolDstBlend");

        Shader m_Shader;
        Material m_Material;
        GameObject m_GameObject;
        Renderer2DData m_RendererData;

        [SetUp]
        public void SetUp()
        {
            m_Shader = ShaderUtil.CreateShaderAsset(k_ColorShader);
            m_Material = new Material(m_Shader);
            m_GameObject = new GameObject("Light2D");
            m_RendererData = ScriptableObject.CreateInstance<Renderer2DData>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_RendererData != null)
            {
                m_RendererData.ClearLightMaterialCache();
                Object.DestroyImmediate(m_RendererData);
            }
            if (m_GameObject != null)
                Object.DestroyImmediate(m_GameObject);
            if (m_Material != null)
                Object.DestroyImmediate(m_Material);
            if (m_Shader != null)
                Object.DestroyImmediate(m_Shader);
        }

        [Test]
        public void GetLightMaterial_SourceMaterialColorChanged_DrawMaterialFollows()
        {
            var light = m_GameObject.AddComponent<Light2D>();
            light.material = m_Material;

            m_Material.SetColor(k_VolumetricID, Color.yellow);
            var first = m_RendererData.GetLightMaterial(light, true, false);
            Assert.AreEqual(Color.yellow, first.GetColor(k_VolumetricID));

            m_Material.SetColor(k_VolumetricID, Color.red);
            var second = m_RendererData.GetLightMaterial(light, true, false);

            Assert.AreEqual(Color.red, second.GetColor(k_VolumetricID),
                "The material used to draw the light should pick up the new color on the source material.");
        }

        // The sync copies the source's blend floats onto the clone; the per-light blend state must
        // still win, and the clone must stay the same object so light batching is unchanged.
        [Test]
        public void GetLightMaterial_SourceMaterialChanged_KeepsCloneAndPerLightBlendState()
        {
            var light = m_GameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.overlapOperation = Light2D.OverlapOperation.Additive;
            light.material = m_Material;

            m_Material.SetFloat(k_SrcBlendID, (float)BlendMode.SrcAlpha);
            m_Material.SetFloat(k_DstBlendID, (float)BlendMode.OneMinusSrcAlpha);
            m_Material.SetFloat(k_VolSrcBlendID, (float)BlendMode.SrcAlpha);
            m_Material.SetFloat(k_VolDstBlendID, (float)BlendMode.OneMinusSrcAlpha);
            m_Material.SetColor(k_VolumetricID, Color.yellow);
            var first = m_RendererData.GetLightMaterial(light, false, false);

            m_Material.SetColor(k_VolumetricID, Color.red);
            var second = m_RendererData.GetLightMaterial(light, false, false);

            Assert.AreSame(first, second, "The cached clone should be reused, not replaced.");
            Assert.AreNotSame(m_Material, second, "The source material should not be drawn directly.");
            Assert.AreEqual(Color.red, second.GetColor(k_VolumetricID));
            Assert.AreEqual((float)BlendMode.One, second.GetFloat(k_SrcBlendID), "_SrcBlend");
            Assert.AreEqual((float)BlendMode.One, second.GetFloat(k_DstBlendID), "_DstBlend");
            Assert.AreEqual((float)BlendMode.One, second.GetFloat(k_VolSrcBlendID), "_VolSrcBlend");
            Assert.AreEqual((float)BlendMode.One, second.GetFloat(k_VolDstBlendID), "_VolDstBlend");

            // The source keeps its own values; only the clone gets the per-light blend state.
            Assert.AreEqual((float)BlendMode.SrcAlpha, m_Material.GetFloat(k_SrcBlendID));
        }

        // Swapping the shader on the source must reach the clone, so the pass count used to pick
        // the volumetric pass matches the shader the user assigned.
        [Test]
        public void GetLightMaterial_SourceShaderChanged_CloneUsesNewShader()
        {
            var light = m_GameObject.AddComponent<Light2D>();
            light.material = m_Material;
            var first = m_RendererData.GetLightMaterial(light, false, false);
            Assert.AreEqual(1, first.passCount);

            var dualPassShader = ShaderUtil.CreateShaderAsset(k_DualPassShader);
            try
            {
                m_Material.shader = dualPassShader;
                var second = m_RendererData.GetLightMaterial(light, false, false);

                Assert.AreSame(first, second, "The cached clone should be reused, not replaced.");
                Assert.AreSame(dualPassShader, second.shader);
                Assert.AreEqual(2, second.passCount);
            }
            finally
            {
                m_RendererData.ClearLightMaterialCache();
                Object.DestroyImmediate(dualPassShader);
            }
        }
    }
}
