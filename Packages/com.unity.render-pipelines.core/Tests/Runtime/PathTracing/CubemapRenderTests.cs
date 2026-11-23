#if UNITY_EDITOR
using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
#endif

namespace UnityEngine.PathTracing.Tests
{
    // The shader sources stay outside the UNITY_EDITOR block. Player builds skip that block, and the compiler
    // then reads the #if lines inside the shader strings as C# preprocessor directives
    internal partial class CubemapRenderTests
    {
        // CreateUncompiledShader replaces UNIQUE_ID with a new GUID, so the shaders are never in the shader cache yet
        const string k_SolidColorShaderSource =
@"Shader ""Hidden/Tests/CubemapRenderUncompiledSolidColor""
{
    Properties { _Color (""Color"", Vector) = (0,0,0,0) }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            // UNIQUE_ID
            float4 _Color;
            float4 Vert(uint vertexID : SV_VertexID) : SV_POSITION
            {
                float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
                return float4(uv * 2.0 - 1.0, 0.0, 1.0);
            }
            float4 Frag() : SV_Target { return _Color; }
            ENDHLSL
        }
    }
    Fallback Off
}";

        // Outputs magenta unless the no-sun keyword is enabled, like the draw does
        const string k_CubemapMaterialShaderSource =
@"Shader ""Hidden/Tests/CubemapRenderUncompiledCubemapMaterial""
{
    Properties { _Color (""Color"", Vector) = (0,0,0,0) }
    SubShader
    {
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_local __ _SUNDISK_NONE
            // UNIQUE_ID
            float4 _Color;
            float4 Vert(float3 position : POSITION) : SV_POSITION { return float4(position.xy, 0.5, 1.0); }
            float4 Frag() : SV_Target
            {
            #if defined(_SUNDISK_NONE)
                return _Color;
            #else
                return float4(1.0, 0.0, 1.0, 1.0);
            #endif
            }
            ENDHLSL
        }
    }
    Fallback Off
}";
    }

#if UNITY_EDITOR
    internal partial class CubemapRenderTests
    {
        const float k_TimeoutSeconds = 60.0f;
        const int k_CubemapMaterialResolution = 4;

        bool m_OldAsyncShaderCompilation;
        bool m_OldAllowAsync;
        Shader m_SolidColorShader;
        Shader m_CubemapMaterialShader;
        Material m_CubemapMaterial;
        Mesh m_FullscreenMesh;
        CubemapRender m_CubemapRender;
        CommandBuffer m_Cmd;

        [SetUp]
        public void SetUp()
        {
            m_OldAsyncShaderCompilation = EditorSettings.asyncShaderCompilation;
            m_OldAllowAsync = ShaderUtil.allowAsyncCompilation;
            EditorSettings.asyncShaderCompilation = true;
            ShaderUtil.allowAsyncCompilation = true;

            m_SolidColorShader = CreateUncompiledShader(k_SolidColorShaderSource);
            m_CubemapMaterialShader = CreateUncompiledShader(k_CubemapMaterialShaderSource);
            m_CubemapMaterial = new Material(m_CubemapMaterialShader);

            m_FullscreenMesh = new Mesh
            {
                vertices = new[] { new Vector3(-1, -1, 0), new Vector3(3, -1, 0), new Vector3(-1, 3, 0) },
                triangles = new[] { 0, 1, 2 },
                bounds = new Bounds(Vector3.zero, Vector3.one * 1000.0f),
            };

            m_CubemapRender = new CubemapRender(m_FullscreenMesh, m_FullscreenMesh, m_SolidColorShader);
            m_Cmd = new CommandBuffer { name = "CubemapRenderTests" };
        }

        [TearDown]
        public void TearDown()
        {
            m_Cmd?.Release();
            m_CubemapRender?.Dispose();
            Object.DestroyImmediate(m_FullscreenMesh);
            Object.DestroyImmediate(m_CubemapMaterial);
            Object.DestroyImmediate(m_CubemapMaterialShader);
            Object.DestroyImmediate(m_SolidColorShader);
            ShaderUtil.allowAsyncCompilation = m_OldAllowAsync;
            EditorSettings.asyncShaderCompilation = m_OldAsyncShaderCompilation;
        }

        // A unique source guarantees the shader is not in the shader cache yet
        static Shader CreateUncompiledShader(string source)
            => ShaderUtil.CreateShaderAsset(source.Replace("UNIQUE_ID", Guid.NewGuid().ToString()), false);

        void UpdateCubemapRender(int resolution)
        {
            Assert.IsTrue(ShaderUtil.allowAsyncCompilation, "The test expects async shader compilation to be enabled.");
            m_Cmd.Clear();
            m_CubemapRender.Update(m_Cmd, null, resolution, out _);
            Graphics.ExecuteCommandBuffer(m_Cmd);
        }

        IEnumerator UpdateUntilCubemapIsUpToDate(int resolution)
        {
            float startTime = Time.realtimeSinceStartup;
            while (m_CubemapRender.Hash == 0)
            {
                if (Time.realtimeSinceStartup - startTime > k_TimeoutSeconds)
                    Assert.Fail($"The environment cubemap was not drawn within {k_TimeoutSeconds} seconds.");

                UpdateCubemapRender(resolution);
                yield return null;
            }
        }

        void AssertCubemapEquals(Color expected)
        {
            var cubemap = m_CubemapRender.GetCubemap();
            var request = AsyncGPUReadback.Request(cubemap, 0, 0, 1, 0, 1, 0, 6);
            request.WaitForCompletion();
            Assert.IsFalse(request.hasError, "Failed to read back the environment cubemap.");

            for (int face = 0; face < 6; ++face)
            {
                Color actual = request.GetData<Color>(face)[0];
                string message = $"Face {(CubemapFace)face} is {actual} but expected {expected}. The cubemap was marked up to date without being drawn correctly.";
                Assert.AreEqual(expected.r, actual.r, 1e-4f, message);
                Assert.AreEqual(expected.g, actual.g, 1e-4f, message);
                Assert.AreEqual(expected.b, actual.b, 1e-4f, message);
            }
        }

        [UnityTest]
        public IEnumerator ColorMode_WhenShaderIsCompilingAsync_CubemapIsOnlyMarkedUpToDateOnceDrawn()
        {
            var expected = new Color(0.8f, 0.3f, 0.1f, 1.0f);
            m_CubemapRender.SetMode(CubemapRender.Mode.Color);
            m_CubemapRender.SetColor(expected);

            yield return UpdateUntilCubemapIsUpToDate(1);

            AssertCubemapEquals(expected);
        }

        [UnityTest]
        public IEnumerator MaterialMode_WhenShaderIsCompilingAsync_DefersDrawInsteadOfCompilingSynchronously()
        {
            var expected = new Color(0.2f, 0.5f, 0.9f, 1.0f);
            m_CubemapMaterial.SetVector("_Color", expected);
            m_CubemapRender.SetMode(CubemapRender.Mode.Material);
            m_CubemapRender.SetMaterial(m_CubemapMaterial);

            UpdateCubemapRender(k_CubemapMaterialResolution);
            Assert.AreEqual(0, m_CubemapRender.Hash, "The first frame should defer the draw while the cubemap material shader compiles, not block on a synchronous compile.");

            yield return UpdateUntilCubemapIsUpToDate(k_CubemapMaterialResolution);

            AssertCubemapEquals(expected);
            Assert.IsFalse(m_CubemapMaterial.IsKeywordEnabled("_SUNDISK_NONE"), "The no-sun keyword should not stay enabled on the cubemap material.");
        }
    }
#endif
}
