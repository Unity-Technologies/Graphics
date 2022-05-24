using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Tests
{
    public class MultiTargetSGTests
    {
        ShaderData m_ShaderData;

        [SetUp]
        public void SetUp()
        {
            var shader = Shader.Find("Shader Graphs/MultiTargetSG");
            m_ShaderData = ShaderUtil.GetShaderData(shader);
        }

        static TestCaseData[] s_TestCaseDatasSerializedSubshaderTags =
        {
            new TestCaseData("RenderPipeline", 0)
                .Returns("HDRenderPipeline"),
            new TestCaseData("RenderPipeline", 1)
                .Returns("HDRenderPipeline"),
            new TestCaseData("RenderPipeline", 2)
                .Returns("UniversalPipeline"),
            new TestCaseData("RenderPipeline", 3)
                .Returns("UniversalPipeline"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatasSerializedSubshaderTags))]
        public string RenderPipelineTagIsFound(string tagName, int subShaderIndex)
        {
            ShaderTagId tag = new ShaderTagId(tagName);
            var subshaderData = m_ShaderData.GetSerializedSubshader(subShaderIndex);
            return subshaderData.FindTagValue(tag).name;
        }
    }
}
