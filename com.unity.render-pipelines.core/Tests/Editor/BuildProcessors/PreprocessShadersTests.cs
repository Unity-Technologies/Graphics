using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Rendering;
using UnityEngine.Assertions;
using Assert = NUnit.Framework.Assert;

namespace UnityEngine.Rendering.Utils.Tests
{
    public class PreprocessShadersTests
    {
        class PreprocessShaders : UnityEditor.Rendering.PreprocessShaders
        {
            public override bool exportLog => false;

            public override bool active => true;

            public override bool IsLogVariantsEnabled(Shader shader)
            {
                return false;
            }

            protected override bool CanRemoveInput(Shader shader, ShaderSnippetData snippetData, ShaderCompilerData inputData)
            {
                throw new NotImplementedException();
            }
        }

        class PreprocessShaders_SkipModule2: PreprocessShaders
        {
            private int m_InputDataCall = 0;

            protected override bool CanRemoveInput(Shader shader, UnityEditor.Rendering.ShaderSnippetData snippetData, UnityEditor.Rendering.ShaderCompilerData inputData)
            {
                m_InputDataCall++;
                return m_InputDataCall % 2 == 0;
            }
        }

        class PreprocessShaders_AllStrippped: PreprocessShaders
        {
            protected override bool CanRemoveInput(Shader shader, UnityEditor.Rendering.ShaderSnippetData snippetData, UnityEditor.Rendering.ShaderCompilerData inputData)
            {
                return true;
            }
        }

        class PreprocessShaders_NothingStrippped : PreprocessShaders
        {
            protected override bool CanRemoveInput(Shader shader, UnityEditor.Rendering.ShaderSnippetData snippetData, UnityEditor.Rendering.ShaderCompilerData inputData)
            {
                return false;
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData(typeof(PreprocessShaders), string.Empty, default, null)
                .Returns(0)
                .SetName("Null"),
            new TestCaseData(typeof(PreprocessShaders_SkipModule2), string.Empty, default, new List<UnityEditor.Rendering.ShaderCompilerData>())
                .Returns(0)
                .SetName("Empty"),
            new TestCaseData(typeof(PreprocessShaders_SkipModule2), "Hidden/Internal-Colored", default, new List<UnityEditor.Rendering.ShaderCompilerData> { default, default })
                .Returns(1)
                .SetName("OneVariantStripped"),
            new TestCaseData(typeof(PreprocessShaders_AllStrippped), "Hidden/Internal-Colored", default, new List<UnityEditor.Rendering.ShaderCompilerData> { default, default })
                .Returns(0)
                .SetName("AllStrippped"),
            new TestCaseData(typeof(PreprocessShaders_NothingStrippped), "Hidden/Internal-Colored", default, new List<UnityEditor.Rendering.ShaderCompilerData> { default, default })
                .Returns(2)
                .SetName("NothingStrippped")
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public int Test_PreprocessShaders(Type preprocessType, string shaderName,
            UnityEditor.Rendering.ShaderSnippetData shaderSnippetData,
            IList<UnityEditor.Rendering.ShaderCompilerData> compilerDataList)
        {
            var preprocessShaders = Activator.CreateInstance(preprocessType) as PreprocessShaders;
            Assert.IsTrue(preprocessShaders.active);

            bool exceptionRaised = false;

            try
            {
                preprocessShaders.OnProcessShader(Shader.Find(shaderName), shaderSnippetData, compilerDataList);
            }
            catch
            {
                exceptionRaised = true;
            }

            Assert.IsFalse(exceptionRaised);
            return compilerDataList?.Count ?? 0;
        }
    }
}
