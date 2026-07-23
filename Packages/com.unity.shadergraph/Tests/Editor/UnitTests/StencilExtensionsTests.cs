using NUnit.Framework;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class StencilExtensionsTests
    {
        // CompFuncToShaderLabString

        [TestCase(CompareFunction.Never, "Never")]
        [TestCase(CompareFunction.Less, "Less")]
        [TestCase(CompareFunction.Equal, "Equal")]
        [TestCase(CompareFunction.LessEqual, "LEqual")]
        [TestCase(CompareFunction.Greater, "Greater")]
        [TestCase(CompareFunction.NotEqual, "NotEqual")]
        [TestCase(CompareFunction.GreaterEqual, "GEqual")]
        [TestCase(CompareFunction.Always, "Always")]
        public void CompFuncToShaderLabString_MapsAllValidValues(CompareFunction func, string expected)
        {
            Assert.AreEqual(expected, StencilExtensions.CompFuncToShaderLabString(func));
        }

        [Test]
        public void CompFuncToShaderLabString_ThrowsOnDisabled()
        {
            // CompareFunction.Disabled is a valid enum value but not a valid ShaderLab stencil
            // compare keyword. Translating it would silently corrupt the generated shader,
            // so the contract is to throw and force the caller to filter it upstream.
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => StencilExtensions.CompFuncToShaderLabString(CompareFunction.Disabled));
        }

        // StencilOpToShaderLabString

        [TestCase(StencilOp.Keep, "Keep")]
        [TestCase(StencilOp.Zero, "Zero")]
        [TestCase(StencilOp.Replace, "Replace")]
        [TestCase(StencilOp.IncrementSaturate, "IncrSat")]
        [TestCase(StencilOp.DecrementSaturate, "DecrSat")]
        [TestCase(StencilOp.Invert, "Invert")]
        [TestCase(StencilOp.IncrementWrap, "IncrWrap")]
        [TestCase(StencilOp.DecrementWrap, "DecrWrap")]
        public void StencilOpToShaderLabString_MapsAllValues(StencilOp op, string expected)
        {
            Assert.AreEqual(expected, StencilExtensions.StencilOpToShaderLabString(op));
        }

        // ToShaderString -- Ref / ReadMask / WriteMask

        [Test]
        public void ToShaderString_EmptyDescriptor_EmitsBareStencilBlock()
        {
            var d = new StencilDescriptor();
            Assert.AreEqual("Stencil\n{\n}", d.ToShaderString());
        }

        [Test]
        public void ToShaderString_RefOnly_EmitsRefLine()
        {
            var d = new StencilDescriptor { Ref = "1" };
            Assert.AreEqual("Stencil\n{\nRef 1\n}", d.ToShaderString());
        }

        [Test]
        public void ToShaderString_ReadMaskOnly_EmitsReadMaskLine()
        {
            var d = new StencilDescriptor { ReadMask = "15" };
            Assert.AreEqual("Stencil\n{\nReadMask 15\n}", d.ToShaderString());
        }

        [Test]
        public void ToShaderString_WriteMaskOnly_EmitsWriteMaskLine()
        {
            var d = new StencilDescriptor { WriteMask = "15" };
            Assert.AreEqual("Stencil\n{\nWriteMask 15\n}", d.ToShaderString());
        }

        [Test]
        public void ToShaderString_HeaderOrderIsRefReadMaskWriteMask()
        {
            var d = new StencilDescriptor { Ref = "1", ReadMask = "255", WriteMask = "15" };
            Assert.AreEqual(
                "Stencil\n{\nRef 1\nReadMask 255\nWriteMask 15\n}",
                d.ToShaderString());
        }

        // ToShaderString -- single-face vs two-face collapsing

        [Test]
        public void ToShaderString_FrontFaceOnly_EmitsUnsuffixedKeywords()
        {
            var d = new StencilDescriptor
            {
                Comp = "Equal",
                Pass = "Replace",
                Fail = "Keep",
                ZFail = "Zero",
            };
            Assert.AreEqual(
                "Stencil\n{\nComp Equal\nPass Replace\nFail Keep\nZFail Zero\n}",
                d.ToShaderString());
        }

        [Test]
        public void ToShaderString_BackFaceOnly_EmitsUnsuffixedKeywords()
        {
            // When only the *Back fields are set the output collapses to the unsuffixed
            // keyword (Comp / Pass / Fail / ZFail) -- i.e. ShaderLab treats it as a
            // single-face stencil even though the caller only filled the back side.
            // URP never hits this path (it always sets the front and conditionally adds back),
            // but the helper supports it symmetrically.
            var d = new StencilDescriptor
            {
                CompBack = "Equal",
                PassBack = "Replace",
                FailBack = "Keep",
                ZFailBack = "Zero",
            };
            Assert.AreEqual(
                "Stencil\n{\nComp Equal\nPass Replace\nFail Keep\nZFail Zero\n}",
                d.ToShaderString());
        }

        [Test]
        public void ToShaderString_FrontAndBackDifferent_EmitsSuffixedKeywordsForBoth()
        {
            var d = new StencilDescriptor
            {
                Comp = "Equal",
                Pass = "Replace",
                Fail = "Keep",
                ZFail = "Zero",
                CompBack = "Always",
                PassBack = "Keep",
                FailBack = "Zero",
                ZFailBack = "Invert",
            };
            Assert.AreEqual(
                "Stencil\n{\n" +
                "CompFront Equal\nCompBack Always\n" +
                "PassFront Replace\nPassBack Keep\n" +
                "FailFront Keep\nFailBack Zero\n" +
                "ZFailFront Zero\nZFailBack Invert\n" +
                "}",
                d.ToShaderString());
        }

        [Test]
        public void ToShaderString_FrontAndBackSameValue_KeepsFrontBackSplit()
        {
            // Documents current behavior: collapse to unsuffixed Comp/Pass/Fail/ZFail only
            // triggers when one side is empty -- not when both sides hold the same string.
            // Callers that want the collapse must set only one side.
            var d = new StencilDescriptor
            {
                Comp = "Equal",
                CompBack = "Equal",
            };
            Assert.AreEqual(
                "Stencil\n{\nCompFront Equal\nCompBack Equal\n}",
                d.ToShaderString());
        }

        // ToShaderString -- realistic full descriptor

        [Test]
        public void ToShaderString_FullSingleFaceDescriptor_MatchesExpectedOutput()
        {
            var d = new StencilDescriptor
            {
                Ref = "1",
                ReadMask = "15",
                WriteMask = "15",
                Comp = StencilExtensions.CompFuncToShaderLabString(CompareFunction.LessEqual),
                Pass = StencilExtensions.StencilOpToShaderLabString(StencilOp.IncrementSaturate),
                Fail = StencilExtensions.StencilOpToShaderLabString(StencilOp.Keep),
                ZFail = StencilExtensions.StencilOpToShaderLabString(StencilOp.DecrementWrap),
            };
            Assert.AreEqual(
                "Stencil\n{\n" +
                "Ref 1\nReadMask 15\nWriteMask 15\n" +
                "Comp LEqual\nPass IncrSat\nFail Keep\nZFail DecrWrap\n" +
                "}",
                d.ToShaderString());
        }

        [Test]
        public void ToShaderString_FullTwoFaceDescriptor_MatchesExpectedOutput()
        {
            var d = new StencilDescriptor
            {
                Ref = "1",
                ReadMask = "15",
                WriteMask = "15",
                Comp = StencilExtensions.CompFuncToShaderLabString(CompareFunction.LessEqual),
                Pass = StencilExtensions.StencilOpToShaderLabString(StencilOp.IncrementSaturate),
                Fail = StencilExtensions.StencilOpToShaderLabString(StencilOp.Keep),
                ZFail = StencilExtensions.StencilOpToShaderLabString(StencilOp.DecrementWrap),
                CompBack = StencilExtensions.CompFuncToShaderLabString(CompareFunction.GreaterEqual),
                PassBack = StencilExtensions.StencilOpToShaderLabString(StencilOp.DecrementSaturate),
                FailBack = StencilExtensions.StencilOpToShaderLabString(StencilOp.Replace),
                ZFailBack = StencilExtensions.StencilOpToShaderLabString(StencilOp.IncrementWrap),
            };
            Assert.AreEqual(
                "Stencil\n{\n" +
                "Ref 1\nReadMask 15\nWriteMask 15\n" +
                "CompFront LEqual\nCompBack GEqual\n" +
                "PassFront IncrSat\nPassBack DecrSat\n" +
                "FailFront Keep\nFailBack Replace\n" +
                "ZFailFront DecrWrap\nZFailBack IncrWrap\n" +
                "}",
                d.ToShaderString());
        }
    }
}
