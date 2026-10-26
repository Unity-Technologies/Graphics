using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace MultipleSRP.EditMode
{
    public class MultipleTargetShaderGraphTests
    {
        static TestCaseData[] s_TestCaseDataSerializedSubshaderTags =
        {
            new TestCaseData("RenderPipeline", 0)
                .SetName("HDRP Subshader. Index: 0")
                .Returns("HDRenderPipeline"),
            new TestCaseData("RenderPipeline", 1)
                .SetName("HDRP Subshader. Index: 1")
                .Returns("HDRenderPipeline"),
            new TestCaseData("RenderPipeline", 2)
                .SetName("URP Subshader. Index: 2")
                .Returns("UniversalPipeline")
        };

        ShaderData m_ShaderData;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("CrossPipelineTests/MultipleTargets");
            m_ShaderData = ShaderUtil.GetShaderData(shader);
        }

        [Test, TestCaseSource(nameof(s_TestCaseDataSerializedSubshaderTags))]
        public string RenderPipelineTagIsFound(string tagName, int subShaderIndex)
        {
            var tag = new ShaderTagId(tagName);
            var subshaderData = m_ShaderData.GetSerializedSubshader(subShaderIndex);
            return subshaderData.FindTagValue(tag).name;
        }
    }
}
