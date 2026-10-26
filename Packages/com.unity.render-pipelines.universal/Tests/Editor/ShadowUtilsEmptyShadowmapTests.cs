using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    sealed class ShadowUtilsEmptyShadowmapKeywordTests
    {
        [Test]
        public void GivenShadowsOffVariantsStripped_WhenRealtimeShadowsAreEnabled_ThenKeywordIsEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: true, shadowsEnabled: true);
            Assert.That(enableKeyword, Is.True, "With shadows-off variants stripped and realtime shadows enabled, the shadows-on variant is the only one available, so the keyword must be enabled.");
        }

        [Test]
        public void GivenShadowsOffVariantsStripped_WhenRealtimeShadowsAreDisabled_ThenKeywordIsNotEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: true, shadowsEnabled: false);
            Assert.That(enableKeyword, Is.False, "Realtime shadows are disabled, so the realtime shadow keyword must not be enabled even when the off variants are stripped.");
        }

        [Test]
        public void GivenShadowsOffVariantsKept_WhenRealtimeShadowsAreEnabled_ThenKeywordIsNotEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: false, shadowsEnabled: true);
            Assert.That(enableKeyword, Is.False, "The shadows-off variant is available, so the empty shadowmap path must select it rather than enabling the shadow keyword.");
        }

        [Test]
        public void GivenShadowsOffVariantsKept_WhenRealtimeShadowsAreDisabled_ThenKeywordIsNotEnabled()
        {
            bool enableKeyword = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants: false, shadowsEnabled: false);
            Assert.That(enableKeyword, Is.False, "Neither variant stripping nor realtime shadows apply, so the shadow keyword must stay disabled.");
        }
    }

    [TestFixture]
    sealed class ShadowUtilsEmptyShadowmapParamsTests
    {
        internal class EmptyShadowmapParamsCase
        {
            internal string name;
            internal bool stripShadowsOffVariants;
            internal bool shadowsEnabled;
            internal bool lightCastsBakedShadows;
            internal bool expectParamsComputed;

            public override string ToString()
            {
                return name;
            }
        }

        static IEnumerable EmptyShadowmapParamsCases()
        {
            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsDisabled_WhenALightCastsBakedShadows_ThenParamsAreComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = false,
                lightCastsBakedShadows = true,
                expectParamsComputed = true
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsDisabled_WhenNoLightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = false,
                lightCastsBakedShadows = false,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsEnabled_WhenALightCastsBakedShadows_ThenParamsAreComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = true,
                lightCastsBakedShadows = true,
                expectParamsComputed = true
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenStrippedOffVariantsAndRealtimeShadowsEnabled_WhenNoLightCastsBakedShadows_ThenParamsAreComputed",
                stripShadowsOffVariants = true,
                shadowsEnabled = true,
                lightCastsBakedShadows = false,
                expectParamsComputed = true
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsEnabled_WhenALightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = true,
                lightCastsBakedShadows = true,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsEnabled_WhenNoLightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = true,
                lightCastsBakedShadows = false,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsDisabled_WhenALightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = false,
                lightCastsBakedShadows = true,
                expectParamsComputed = false
            };

            yield return new EmptyShadowmapParamsCase
            {
                name = "GivenKeptOffVariantsAndRealtimeShadowsDisabled_WhenNoLightCastsBakedShadows_ThenParamsAreNotComputed",
                stripShadowsOffVariants = false,
                shadowsEnabled = false,
                lightCastsBakedShadows = false,
                expectParamsComputed = false
            };
        }

        [Test]
        [TestCaseSource(nameof(EmptyShadowmapParamsCases))]
        public void DecideEmptyShadowmapParams(EmptyShadowmapParamsCase testCase)
        {
            bool computeParams = ShadowUtils.ShouldComputeEmptyShadowmapParams(testCase.stripShadowsOffVariants, testCase.shadowsEnabled, testCase.lightCastsBakedShadows);
            Assert.That(computeParams, Is.EqualTo(testCase.expectParamsComputed),
                $"ShouldComputeEmptyShadowmapParams(strip: {testCase.stripShadowsOffVariants}, shadowsEnabled: {testCase.shadowsEnabled}, bakedShadows: {testCase.lightCastsBakedShadows}) returned the wrong decision.");
        }
    }
}