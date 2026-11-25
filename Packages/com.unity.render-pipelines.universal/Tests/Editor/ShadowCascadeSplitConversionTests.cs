using NUnit.Framework;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowCascadeSplitConversionTests
    {
        const float k_Tolerance = 1e-5f;

        [TestCase(false, 50f, 100f)]
        [TestCase(true, 50f, 50f)]
        [TestCase(true, 0f, 0f)]
        public void SplitScale_IsPercentOrMaxDistance(bool useMetric, float maxDistance, float expected)
        {
            Assert.AreEqual(expected, ShadowCascadeSplitGUI.SplitScale(useMetric, maxDistance));
        }

        static readonly object[] s_SplitFromSliderValueCases =
        {
            new object[] { 25f, new Vector4(0.1f, 0.5f, 0.8f, 1f), 1, 3, 100f, 0.25f },
            new object[] { 12.5f, new Vector4(0.1f, 0.5f, 0.8f, 1f), 1, 3, 50f, 0.25f },
            new object[] { 5f, new Vector4(0.2f, 0.5f, 0.8f, 1f), 1, 3, 100f, 0.201f },
            new object[] { 90f, new Vector4(0.2f, 0.5f, 0.8f, 1f), 1, 3, 100f, 0.799f },
            new object[] { 100f, new Vector4(0.2f, 0.5f, 0.8f, 1f), 2, 3, 100f, 1f },
            new object[] { 100f, new Vector4(0.5f, 1f, 0f, 0f), 0, 1, 100f, 1f },
            new object[] { 30f, new Vector4(0.5f, 1f, 0f, 0f), 0, 1, 0f, 0f },
        };

        [TestCaseSource(nameof(s_SplitFromSliderValueCases))]
        public void SplitFromSliderValue_ConvertsAndStaysBetweenNeighbors(float scaledValue, Vector4 splits, int index, int splitCount, float scale, float expected)
        {
            Assert.AreEqual(expected, ShadowCascadeSplitGUI.SplitFromSliderValue(scaledValue, splits, index, splitCount, scale), k_Tolerance);
        }

        static readonly object[] s_LastBorderScaleCases =
        {
            new object[] { new Vector4(0.1f, 0.3f, 0.6f, 1f), 3, false, 50f, 100f },
            new object[] { new Vector4(0.1f, 0.3f, 0.6f, 1f), 3, true, 50f, 20f },
            new object[] { new Vector4(0.25f, 1f, 0f, 0f), 1, true, 40f, 30f },
            new object[] { new Vector4(1f, 0f, 0f, 0f), 0, true, 50f, 50f },
        };

        [TestCaseSource(nameof(s_LastBorderScaleCases))]
        public void LastBorderScale_IsPercentOrLastCascadeLength(Vector4 splits, int splitCount, bool useMetric, float maxDistance, float expected)
        {
            Assert.AreEqual(expected, ShadowCascadeSplitGUI.LastBorderScale(splits, splitCount, useMetric, maxDistance), k_Tolerance);
        }

        [TestCase(5f, 20f, 0.25f)]
        [TestCase(10f, 100f, 0.1f)]
        [TestCase(5f, 0f, 0f)]
        public void LastBorderFromSliderValue_DividesByScale(float scaledValue, float scale, float expected)
        {
            Assert.AreEqual(expected, ShadowCascadeSplitGUI.LastBorderFromSliderValue(scaledValue, scale), k_Tolerance);
        }

        [Test]
        public void ToCascades_GivesCascadeSizesBorderAndHandleStates()
        {
            var cascades = ShadowCascadeSplitGUI.ToCascades(new Vector4(0.1f, 0.3f, 0.6f, 1f), 0.2f, 4, new[] { true, false, true }, false);

            Assert.AreEqual(4, cascades.Length);
            Assert.AreEqual(0.1f, cascades[0].size, k_Tolerance);
            Assert.AreEqual(0.2f, cascades[1].size, k_Tolerance);
            Assert.AreEqual(0.3f, cascades[2].size, k_Tolerance);
            Assert.AreEqual(0.4f, cascades[3].size, k_Tolerance);
            Assert.AreEqual(0.2f, cascades[3].borderSize, k_Tolerance);

            Assert.AreEqual(ShadowCascadeGUI.HandleState.Enabled, cascades[0].cascadeHandleState);
            Assert.AreEqual(ShadowCascadeGUI.HandleState.Disabled, cascades[1].cascadeHandleState);
            Assert.AreEqual(ShadowCascadeGUI.HandleState.Enabled, cascades[2].cascadeHandleState);
            Assert.AreEqual(ShadowCascadeGUI.HandleState.Hidden, cascades[3].cascadeHandleState);
            Assert.AreEqual(ShadowCascadeGUI.HandleState.Hidden, cascades[0].borderHandleState);
            Assert.AreEqual(ShadowCascadeGUI.HandleState.Disabled, cascades[3].borderHandleState);
        }

        [Test]
        public void ToCascades_WithOneCascade_GivesTheWholeRange()
        {
            var cascades = ShadowCascadeSplitGUI.ToCascades(new Vector4(0.5f, 0.6f, 0.7f, 1f), 0.3f, 1, new bool[0], true);

            Assert.AreEqual(1, cascades.Length);
            Assert.AreEqual(1f, cascades[0].size, k_Tolerance);
            Assert.AreEqual(0.3f, cascades[0].borderSize, k_Tolerance);
            Assert.AreEqual(ShadowCascadeGUI.HandleState.Enabled, cascades[0].borderHandleState);
        }

        [Test]
        public void FromCascades_AfterMovingAHandle_GivesCumulativeSplitsAndBorder()
        {
            var splits = new Vector4(0.1f, 0.3f, 0.6f, 1f);
            var cascades = ShadowCascadeSplitGUI.ToCascades(splits, 0.2f, 4, new[] { true, true, true }, true);
            cascades[0].size += 0.05f;
            cascades[1].size -= 0.05f;
            cascades[3].borderSize = 0.35f;

            ShadowCascadeSplitGUI.FromCascades(cascades, ref splits, out float border);

            Assert.AreEqual(0.15f, splits.x, k_Tolerance);
            Assert.AreEqual(0.3f, splits.y, k_Tolerance);
            Assert.AreEqual(0.6f, splits.z, k_Tolerance);
            Assert.AreEqual(0.35f, border, k_Tolerance);
        }

        static readonly object[] s_CascadeCountChangeCases =
        {
            new object[] { new Vector4(0.35f, 0.2f, 0.467f, 0f), 3, new Vector3(0.35f, 0.351f, 0.467f) },
            new object[] { new Vector4(0.15f, 0.45f, 0.467f, 0f), 3, new Vector3(0.15f, 0.45f, 0.467f) },
            new object[] { new Vector4(0.067f, 0.2f, 0.467f, 0f), 1, new Vector3(0.067f, 0.2f, 0.467f) },
        };

        [TestCaseSource(nameof(s_CascadeCountChangeCases))]
        public void SortSplits_AfterCascadeCountChange_KeepsTheNewlyUsedSplitsIncreasing(Vector4 splits, int splitCount, Vector3 expected)
        {
            Vector4 sorted = ShadowCascadeSplitGUI.SortSplits(splits, splitCount);

            for (int i = 0; i < splitCount; ++i)
                Assert.AreEqual(expected[i], sorted[i], k_Tolerance, $"split {i}");
        }
    }
}
