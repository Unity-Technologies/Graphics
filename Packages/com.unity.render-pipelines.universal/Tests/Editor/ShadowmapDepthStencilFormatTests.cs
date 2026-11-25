using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    sealed class ShadowmapDepthStencilFormatTests
    {
        static readonly GraphicsFormat[] s_AllDepthFormats =
        {
            GraphicsFormat.D16_UNorm,
            GraphicsFormat.D24_UNorm,
            GraphicsFormat.D32_SFloat,
            GraphicsFormat.D16_UNorm_S8_UInt,
            GraphicsFormat.D24_UNorm_S8_UInt,
            GraphicsFormat.D32_SFloat_S8_UInt,
        };

        static readonly GraphicsFormat[] s_StencilFormats =
        {
            GraphicsFormat.D16_UNorm_S8_UInt,
            GraphicsFormat.D24_UNorm_S8_UInt,
            GraphicsFormat.D32_SFloat_S8_UInt,
        };

        internal class FormatSelectionCase
        {
            internal string name;
            internal DepthBits depthBits;
            internal bool stencilBuffer;
            internal GraphicsFormat[] unsupportedFormats;
            internal GraphicsFormat expectedFormat;

            public override string ToString()
            {
                return name;
            }
        }

        static FormatSelectionCase Case(string name, DepthBits depthBits, bool stencilBuffer, GraphicsFormat[] unsupportedFormats, GraphicsFormat expectedFormat)
        {
            return new FormatSelectionCase
            {
                name = name,
                depthBits = depthBits,
                stencilBuffer = stencilBuffer,
                unsupportedFormats = unsupportedFormats,
                expectedFormat = expectedFormat
            };
        }

        static GraphicsFormat[] AllSupported()
        {
            return new GraphicsFormat[0];
        }

        static GraphicsFormat[] Unsupported(params GraphicsFormat[] formats)
        {
            return formats;
        }

        static IEnumerable FormatSelectionCases()
        {
            yield return Case("GivenAllFormatsSupported_When16BitWithoutStencil_ThenD16", DepthBits.Depth16, false, AllSupported(), GraphicsFormat.D16_UNorm);
            yield return Case("GivenAllFormatsSupported_When24BitWithoutStencil_ThenD24", DepthBits.Depth24, false, AllSupported(), GraphicsFormat.D24_UNorm);
            yield return Case("GivenAllFormatsSupported_When32BitWithoutStencil_ThenD32", DepthBits.Depth32, false, AllSupported(), GraphicsFormat.D32_SFloat);
            yield return Case("GivenAllFormatsSupported_When16BitWithStencil_ThenD16S8", DepthBits.Depth16, true, AllSupported(), GraphicsFormat.D16_UNorm_S8_UInt);
            yield return Case("GivenAllFormatsSupported_When24BitWithStencil_ThenD24S8", DepthBits.Depth24, true, AllSupported(), GraphicsFormat.D24_UNorm_S8_UInt);
            yield return Case("GivenAllFormatsSupported_When32BitWithStencil_ThenD32S8", DepthBits.Depth32, true, AllSupported(), GraphicsFormat.D32_SFloat_S8_UInt);

            yield return Case("GivenD16Unsupported_When16BitWithoutStencil_ThenD24",
                DepthBits.Depth16, false, Unsupported(GraphicsFormat.D16_UNorm), GraphicsFormat.D24_UNorm);
            yield return Case("GivenD16AndD24Unsupported_When16BitWithoutStencil_ThenD32",
                DepthBits.Depth16, false, Unsupported(GraphicsFormat.D16_UNorm, GraphicsFormat.D24_UNorm), GraphicsFormat.D32_SFloat);
            yield return Case("GivenD24Unsupported_When24BitWithoutStencil_ThenD32",
                DepthBits.Depth24, false, Unsupported(GraphicsFormat.D24_UNorm), GraphicsFormat.D32_SFloat);
            yield return Case("GivenD16Unsupported_When32BitWithoutStencil_ThenD32",
                DepthBits.Depth32, false, Unsupported(GraphicsFormat.D16_UNorm), GraphicsFormat.D32_SFloat);
            yield return Case("GivenD24AndD32Unsupported_When24BitWithoutStencil_ThenD24S8",
                DepthBits.Depth24, false, Unsupported(GraphicsFormat.D24_UNorm, GraphicsFormat.D32_SFloat), GraphicsFormat.D24_UNorm_S8_UInt);
            yield return Case("GivenOnlyD16AndD32S8Supported_When32BitWithoutStencil_ThenD32S8",
                DepthBits.Depth32, false, Unsupported(GraphicsFormat.D24_UNorm, GraphicsFormat.D32_SFloat, GraphicsFormat.D16_UNorm_S8_UInt, GraphicsFormat.D24_UNorm_S8_UInt), GraphicsFormat.D32_SFloat_S8_UInt);
            yield return Case("GivenDepthOnlyAndD16S8Unsupported_When16BitWithoutStencil_ThenD24S8",
                DepthBits.Depth16, false, Unsupported(GraphicsFormat.D16_UNorm, GraphicsFormat.D24_UNorm, GraphicsFormat.D32_SFloat, GraphicsFormat.D16_UNorm_S8_UInt), GraphicsFormat.D24_UNorm_S8_UInt);

            yield return Case("GivenD16S8Unsupported_When16BitWithStencil_ThenD24S8",
                DepthBits.Depth16, true, Unsupported(GraphicsFormat.D16_UNorm_S8_UInt), GraphicsFormat.D24_UNorm_S8_UInt);
            yield return Case("GivenD16S8AndD24S8Unsupported_When16BitWithStencil_ThenD32S8",
                DepthBits.Depth16, true, Unsupported(GraphicsFormat.D16_UNorm_S8_UInt, GraphicsFormat.D24_UNorm_S8_UInt), GraphicsFormat.D32_SFloat_S8_UInt);
            yield return Case("GivenD32S8Unsupported_When32BitWithStencil_ThenD32",
                DepthBits.Depth32, true, Unsupported(GraphicsFormat.D32_SFloat_S8_UInt), GraphicsFormat.D32_SFloat);
            yield return Case("GivenStencilFormatsUnsupported_When16BitWithStencil_ThenD16",
                DepthBits.Depth16, true, s_StencilFormats, GraphicsFormat.D16_UNorm);
            yield return Case("GivenStencilFormatsUnsupported_When24BitWithStencil_ThenD24",
                DepthBits.Depth24, true, s_StencilFormats, GraphicsFormat.D24_UNorm);
            yield return Case("GivenStencilFormatsAndD16Unsupported_When16BitWithStencil_ThenD24",
                DepthBits.Depth16, true, Unsupported(GraphicsFormat.D16_UNorm_S8_UInt, GraphicsFormat.D24_UNorm_S8_UInt, GraphicsFormat.D32_SFloat_S8_UInt, GraphicsFormat.D16_UNorm), GraphicsFormat.D24_UNorm);

            yield return Case("GivenNoFormatSupported_When16BitWithoutStencil_ThenRequestedD16", DepthBits.Depth16, false, s_AllDepthFormats, GraphicsFormat.D16_UNorm);
            yield return Case("GivenNoFormatSupported_When32BitWithoutStencil_ThenRequestedD32", DepthBits.Depth32, false, s_AllDepthFormats, GraphicsFormat.D32_SFloat);
            yield return Case("GivenNoFormatSupported_When32BitWithStencil_ThenRequestedD32S8", DepthBits.Depth32, true, s_AllDepthFormats, GraphicsFormat.D32_SFloat_S8_UInt);

            yield return Case("GivenAllFormatsSupported_WhenNoDepthBitsWithoutStencil_ThenD16", DepthBits.None, false, AllSupported(), GraphicsFormat.D16_UNorm);
            yield return Case("GivenAllFormatsSupported_WhenNoDepthBitsWithStencil_ThenD16S8", DepthBits.None, true, AllSupported(), GraphicsFormat.D16_UNorm_S8_UInt);
            yield return Case("GivenD16Unsupported_WhenNoDepthBitsWithoutStencil_ThenD24",
                DepthBits.None, false, Unsupported(GraphicsFormat.D16_UNorm), GraphicsFormat.D24_UNorm);
        }

        [Test]
        [Description("The shadow map format is the requested precision when supported, otherwise the first supported format with at least that precision, keeping stencil before dropping it.")]
        [TestCaseSource(nameof(FormatSelectionCases))]
        public void SelectShadowmapDepthStencilFormat(FormatSelectionCase testCase)
        {
            var unsupported = new HashSet<GraphicsFormat>(testCase.unsupportedFormats);
            GraphicsFormat format = ShadowUtils.SelectShadowmapDepthStencilFormat(testCase.depthBits, testCase.stencilBuffer, f => !unsupported.Contains(f));
            Assert.That(format, Is.EqualTo(testCase.expectedFormat),
                $"SelectShadowmapDepthStencilFormat(depthBits: {testCase.depthBits}, stencil: {testCase.stencilBuffer}) with {testCase.unsupportedFormats.Length} unsupported formats returned the wrong format.");
        }

        [Test]
        [Description("A 16-bit request picks the same format as the stencil selection that shipped before the precision option, for every combination of supported stencil formats.")]
        public void Given16BitRequest_WhenStencilFormatSupportVaries_ThenMatchesPreviousSelection()
        {
            for (int mask = 0; mask < (1 << s_StencilFormats.Length); ++mask)
            {
                var unsupported = new HashSet<GraphicsFormat>();
                for (int i = 0; i < s_StencilFormats.Length; ++i)
                {
                    if ((mask & (1 << i)) != 0)
                        unsupported.Add(s_StencilFormats[i]);
                }

                GraphicsFormat expected = GraphicsFormat.D16_UNorm;
                foreach (GraphicsFormat stencilFormat in s_StencilFormats)
                {
                    if (!unsupported.Contains(stencilFormat))
                    {
                        expected = stencilFormat;
                        break;
                    }
                }

                GraphicsFormat format = ShadowUtils.SelectShadowmapDepthStencilFormat(DepthBits.Depth16, true, f => !unsupported.Contains(f));
                Assert.That(format, Is.EqualTo(expected),
                    $"With {unsupported.Count} unsupported stencil formats (mask {mask}), the 16-bit stencil shadow map must keep the smallest supported depth-stencil format, or depth-only D16 when none is supported.");
            }
        }
    }
}
