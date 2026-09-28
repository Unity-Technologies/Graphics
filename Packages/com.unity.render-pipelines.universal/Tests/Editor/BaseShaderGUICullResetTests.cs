using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    class BaseShaderGUICullResetTests
    {
        const string k_NoSurfaceShaderSource = @"
Shader ""Test/BaseShaderGUICullResetTests/NoSurface""
{
    Properties { _Cull (""Cull"", Float) = 3 }
    SubShader
    {
        Tags { ""Queue""=""Geometry"" }
        Pass
        {
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            float4 vert(float4 v:POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { return fixed4(0, 0, 0, 1); }
            ENDCG
        }
    }
}";

        const string k_WithSurfaceShaderSource = @"
Shader ""Test/BaseShaderGUICullResetTests/WithSurface""
{
    Properties
    {
        _Surface (""Surface"", Float) = 0
        _Cull (""Cull"", Float) = 3
    }
    SubShader
    {
        Tags { ""Queue""=""Geometry"" }
        Pass
        {
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            float4 vert(float4 v:POSITION) : SV_POSITION { return UnityObjectToClipPos(v); }
            fixed4 frag() : SV_Target { return fixed4(0, 0, 0, 1); }
            ENDCG
        }
    }
}";

        Shader m_Shader;
        Material m_Material;

        [TearDown]
        public void TearDown()
        {
            if (m_Material != null) Object.DestroyImmediate(m_Material);
            if (m_Shader != null) Object.DestroyImmediate(m_Shader);
        }

        [Test]
        public void NoSurfaceProperty_StaleCullValue_IsResetToShaderDefault()
        {
            m_Shader = ShaderUtil.CreateShaderAsset(k_NoSurfaceShaderSource);
            m_Material = new Material(m_Shader);
            m_Material.SetFloat("_Cull", 2);

            BaseShaderGUI.UpdateMaterialSurfaceOptions(m_Material, automaticRenderQueue: true);

            Assert.AreEqual(3f, m_Material.GetFloat("_Cull"));
        }

        [Test]
        public void SurfaceProperty_StaleCullValue_IsLeftUntouched()
        {
            m_Shader = ShaderUtil.CreateShaderAsset(k_WithSurfaceShaderSource);
            m_Material = new Material(m_Shader);
            m_Material.SetFloat("_Cull", 2);

            BaseShaderGUI.UpdateMaterialSurfaceOptions(m_Material, automaticRenderQueue: true);

            Assert.AreEqual(2f, m_Material.GetFloat("_Cull"));
        }
    }
}
