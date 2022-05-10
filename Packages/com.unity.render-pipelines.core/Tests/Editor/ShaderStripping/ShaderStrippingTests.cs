using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Tests.ShaderStripping
{
    public class VariantStrippingTests
    {
        #region Strippers
        abstract class ShaderVariantStripperTest : IShaderVariantStripper
        {
            public bool active => false;

            public abstract bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData);
        }

        class StripHalf : ShaderVariantStripperTest
        {
            private int m_InputDataCall;

            public override bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData)
            {
                m_InputDataCall++;
                return m_InputDataCall % 2 == 0;
            }
        }

        class StripNothing : ShaderVariantStripperTest
        {
            public override bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData) => false;
        }

        class StripAll : ShaderVariantStripperTest
        {
            public override bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData) => true;
        }

        class CallbacksAreCalledStripper : ShaderVariantStripperTest, IShaderVariantStripperScope, IShaderVariantStripperSkipper
        {
            internal static List<string> s_Callbacks = new List<string> ();

            public void AfterShaderStripping(Shader shader)
            {
                s_Callbacks.Add(nameof(AfterShaderStripping));
            }

            public void BeforeShaderStripping(Shader shader)
            {
                s_Callbacks.Add(nameof(BeforeShaderStripping));
            }

            public override bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData)
            {
                s_Callbacks.Add(nameof(CanRemoveVariant));
                return false;
            }

            public bool SkipShader([DisallowNull] Shader shader, ShaderSnippetData shaderVariant)
            {
                s_Callbacks.Add(nameof(SkipShader));
                return false;
            }
        }

        class SkipReturnsTrue : ShaderVariantStripperTest, IShaderVariantStripperSkipper
        {
            internal static bool s_CanRemoveCalled = false;
            internal static bool s_SkipShaderIsCalled = false;

            public override bool CanRemoveVariant([DisallowNull] Shader shader, ShaderSnippetData shaderVariant, ShaderCompilerData shaderCompilerData)
            {
                s_CanRemoveCalled = true;
                return false;
            }

            public bool SkipShader([DisallowNull] Shader shader, ShaderSnippetData shaderVariant)
            {
                s_SkipShaderIsCalled = true;
                return true;
            }
        }

        class ShaderPrepocessorTests : ShaderPreprocessor<Shader, ShaderSnippetData>
        {
            public ShaderPrepocessorTests(Type type)
                : base(new IVariantStripper<Shader, ShaderSnippetData>[] { Activator.CreateInstance(type) as IVariantStripper<Shader, ShaderSnippetData> })
            {
            }

            public bool TryProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> inputData, out Exception error)
            {
                return TryStripShaderVariants(shader, snippet, inputData, out error);
            }
        }

        #endregion

        ShaderPrepocessorTests m_ShaderVariantStripper;
        private RenderPipelineAsset m_PreviousPipeline;
        ShaderStrippingReportScope m_ReportTestScope;

        [SetUp]
        public void Setup()
        {
            m_PreviousPipeline = GraphicsSettings.defaultRenderPipeline;
            GraphicsSettings.defaultRenderPipeline = null;
            m_ReportTestScope = new ShaderStrippingReportScope();
            m_ReportTestScope.OnPreprocessBuild(default);
        }

        [TearDown]
        public void TearDown()
        {
            m_ShaderVariantStripper = null;
            GraphicsSettings.defaultRenderPipeline = m_PreviousPipeline;
            m_ReportTestScope.OnPostprocessBuild(default);
            m_ReportTestScope = null;
        }

        static TestCaseData[] s_TestCaseDatas =
        {
             new TestCaseData(typeof(StripNothing), Shader.Find("Hidden/Internal-Colored"), new List<Rendering.ShaderCompilerData> { default, default })
                .SetName("Given a stripper that does nothing, the variants are kept")
                .Returns(2),
             new TestCaseData(typeof(StripAll), Shader.Find("Hidden/Internal-Colored"), new List<Rendering.ShaderCompilerData> { default, default })
                .SetName("Given a stripper that strip everything, the variants are stripped")
                .Returns(0),
             new TestCaseData(typeof(StripHalf), Shader.Find("Hidden/Internal-Colored"), new List<Rendering.ShaderCompilerData> { default, default, default, default, default, default })
                .SetName("Given a stripper that reduces the variants to the half, just half of the variants are stripped")
                .Returns(3),
             new TestCaseData(typeof(StripNothing), Shader.Find("DummyPipeline/VariantStrippingTestsShader"), new List<Rendering.ShaderCompilerData> { default, default })
                .SetName("Given a shader that is not from the current pipeline, all the variants are stripped")
                .Ignore("Disabled for now, until shader tags are fixed")
                .Returns(0),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public int CheckNumberOfVariantsIsCorrect(Type preprocessType, Shader shader, List<Rendering.ShaderCompilerData> variants)
        {
            m_ShaderVariantStripper = new (preprocessType);
            Assert.IsTrue(m_ShaderVariantStripper.TryProcessShader(shader, default, variants, out var error));
            return variants.Count;
        }

        static TestCaseData[] s_ExceptionTestCaseDatas =
        {
            new TestCaseData(typeof(StripAll), null, new List<Rendering.ShaderCompilerData> { default, default })
                .SetName("Given a null shader, argument null exception is raised")
                .Returns(typeof(ArgumentNullException)),
            new TestCaseData(typeof(StripAll), Shader.Find("Hidden/Internal-Colored"), null)
                .SetName("Given a null variants collection, argument null exception is raised")
                .Returns(typeof(ArgumentNullException)),
        };

        [Test, TestCaseSource(nameof(s_ExceptionTestCaseDatas))]
        public Type CheckExceptionsAreRaised(Type preprocessType, Shader shader, List<Rendering.ShaderCompilerData> variants)
        {
            m_ShaderVariantStripper = new(preprocessType);
            Assert.IsFalse(m_ShaderVariantStripper.TryProcessShader(shader, default, variants, out var error));
            return error.GetType();
        }

        static List<string> s_ExpectedCallbackOrder = new List<string>() {
                nameof(CallbacksAreCalledStripper.BeforeShaderStripping),
                nameof(CallbacksAreCalledStripper.SkipShader),
                nameof(CallbacksAreCalledStripper.CanRemoveVariant),
                nameof(CallbacksAreCalledStripper.AfterShaderStripping) };

        [Test]
        [Category("Callbacks")]
        public void GivenAnStripperImplementingAllTheCallbacksTheyAreExecutedProperly()
        {
            CallbacksAreCalledStripper.s_Callbacks.Clear();
            m_ShaderVariantStripper = new(typeof(CallbacksAreCalledStripper));
            m_ShaderVariantStripper.TryProcessShader(Shader.Find("Hidden/Internal-Colored"), default, new List<Rendering.ShaderCompilerData> { default }, out var error);
            Assert.AreEqual(s_ExpectedCallbackOrder, CallbacksAreCalledStripper.s_Callbacks);
        }

        [Test]
        [Category("Callbacks")]
        public void GivenAnStripperSkippingAShaderTheCallbackCanRemoveIsNotCalled()
        {
            m_ShaderVariantStripper = new(typeof(SkipReturnsTrue));
            m_ShaderVariantStripper.TryProcessShader(Shader.Find("Hidden/Internal-Colored"), default, new List<Rendering.ShaderCompilerData> { default, default }, out var error);
            Assert.IsTrue(SkipReturnsTrue.s_SkipShaderIsCalled, "IShaderVariantStripperSkipper.SkipShader was supossed to be called");
            Assert.IsFalse(SkipReturnsTrue.s_CanRemoveCalled, "IVariantStripper.CanRemoveVariant was supossed to NOT be called");
        }
    }
}
