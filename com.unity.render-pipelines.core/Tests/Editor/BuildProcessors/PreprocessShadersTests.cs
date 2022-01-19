using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.Rendering;

namespace UnityEngine.Rendering.Utils.Tests
{
    class PreprocessShadersTests
    {
        class StripOneVariant : IShaderVariantStripper
        {
            private int m_InputDataCall;

            public bool CanRemoveVariant(Shader shader, ShaderSnippetData shaderInput,
                ShaderCompilerData compilerData)
            {
                m_InputDataCall++;
                return m_InputDataCall % 2 == 0;
            }

            private static bool m_Active;
            public bool isActive => m_Active;
            public int priority => 0;
            public bool CanProcessVariant(Shader shader, ShaderSnippetData shaderInput) => true;
        }

        class StripNothing : IShaderVariantStripper
        {
            public bool CanRemoveVariant(Shader shader, ShaderSnippetData shaderInput,
                ShaderCompilerData compilerData) => false;

            private static bool m_Active;
            public bool isActive => m_Active;
            public int priority => 0;
            public bool CanProcessVariant(Shader shader, ShaderSnippetData shaderInput) => true;
        }

        class StripAll : IShaderVariantStripper
        {
            public bool CanRemoveVariant(Shader shader, ShaderSnippetData shaderInput,
                ShaderCompilerData compilerData) => true;

            private static bool m_Active;
            public bool isActive => m_Active;
            public int priority => 0;
            public bool CanProcessVariant(Shader shader, ShaderSnippetData shaderInput) => true;
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(null)
                .Returns(0)
                .SetName("Empty"),
            new TestCaseData(typeof(StripOneVariant))
                .Returns(1)
                .SetName(nameof(StripOneVariant)),
            new TestCaseData(typeof(StripNothing))
                .Returns(2)
                .SetName(nameof(StripNothing)),
            new TestCaseData(typeof(StripAll))
                .Returns(0)
                .SetName(nameof(StripAll)),
        };

        protected UnityEditor.Build.IPreprocessShaders preprocessShaders
        {
            get
            {
                Type type = null;

                foreach (var t in UnityEditor.TypeCache.GetTypesDerivedFrom<UnityEditor.Build.IPreprocessShaders>())
                {
                    if (t.FullName.Equals("UnityEditor.Rendering.PreprocessShaders"))
                    {
                        type = t;
                        break;
                    }
                }

                Assert.IsNotNull(type);

                return Activator.CreateInstance(type) as UnityEditor.Build.IPreprocessShaders;
            }
        }

        protected class BuiltInPipeline : IDisposable
        {
            private RenderPipelineAsset m_PreviousPipeline;

            public BuiltInPipeline()
            {
                m_PreviousPipeline = GraphicsSettings.defaultRenderPipeline;
                GraphicsSettings.defaultRenderPipeline = null;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    GraphicsSettings.defaultRenderPipeline = m_PreviousPipeline;
                }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public int Test_PreprocessShaders(Type preprocessType)
        {
            FieldInfo field = preprocessType?.GetField("m_Active", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, true);

            var variants = new List<UnityEditor.Rendering.ShaderCompilerData> { default, default };

            using (new BuiltInPipeline())
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                preprocessShaders.OnProcessShader(shader, default, variants);
            }

            field?.SetValue(null, false);

            return variants.Count;
        }
    }
}
