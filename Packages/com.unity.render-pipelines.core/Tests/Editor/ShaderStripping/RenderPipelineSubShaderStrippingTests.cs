using NUnit.Framework;
using UnityEditor.Shaders;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests.ShaderStripping
{
    class RenderPipelineSubShaderStrippingTests
    {
        // Minimal in-memory render pipeline used to simulate a transient runtime
        // GraphicsSettings.defaultRenderPipeline override, as render pipeline tests routinely do.
        class InMemoryTestRenderPipelineAsset : RenderPipelineAsset
        {
            public const string k_ShaderTag = "RenderPipelineSubShaderStrippingTestsRP";
            public override string renderPipelineShaderTag => k_ShaderTag;
            protected override RenderPipeline CreatePipeline() => null;
        }

        RenderPipelineAsset m_PreviousDefaultRenderPipeline;
        InMemoryTestRenderPipelineAsset m_InMemoryRenderPipeline;

        [SetUp]
        public void SetUp()
        {
            m_PreviousDefaultRenderPipeline = GraphicsSettings.defaultRenderPipeline;
            m_InMemoryRenderPipeline = ScriptableObject.CreateInstance<InMemoryTestRenderPipelineAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            GraphicsSettings.defaultRenderPipeline = m_PreviousDefaultRenderPipeline;
            if (m_InMemoryRenderPipeline != null)
                ScriptableObject.DestroyImmediate(m_InMemoryRenderPipeline);
        }

        // Regression guard for the RenderPipeline-tag subshader stripping reimport storm: the tag set that
        // drives shader import stripping must be sourced from the PERSISTED project render pipeline
        // configuration, never from the live (here runtime-overridden) GraphicsSettings.defaultRenderPipeline.
        // Otherwise an in-memory render pipeline assigned at runtime would change the shader import
        // dependency and reimport every RenderPipeline-tagged shader on each swap, timing out shader-heavy
        // tests on CI.
        [Test]
        public void GetActiveRenderPipelineShaderTagsForPlatform_IgnoresRuntimeInMemoryRenderPipeline()
        {
            Assert.IsFalse(EditorUtility.IsPersistent(m_InMemoryRenderPipeline),
                "Test precondition: the test render pipeline must be an in-memory (non-persisted) instance.");

            GraphicsSettings.defaultRenderPipeline = m_InMemoryRenderPipeline;

            string buildTargetGroupName = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString();
            string[] tags = RenderPipelineSubShaderStripping.GetActiveRenderPipelineShaderTagsForPlatform(buildTargetGroupName);

            if (tags != null)
                CollectionAssert.DoesNotContain(tags, InMemoryTestRenderPipelineAsset.k_ShaderTag,
                    "The in-memory runtime render pipeline tag leaked into the persisted-derived stripping tag set.");
        }
    }
}
