using NUnit.Framework;
using UnityEditor.Rendering.Universal;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowCascadeSplitGUITests
    {
        const float k_Tolerance = 1e-5f;

        static readonly object[] s_SortSplitsCases =
        {
            new object[] { new Vector4(0.067f, 0.2f, 0.467f, 0f), 3, new Vector3(0.067f, 0.2f, 0.467f) },
            new object[] { new Vector4(0.1f, 0.3f, 0.467f, 0f), 2, new Vector3(0.1f, 0.3f, 0.467f) },
            new object[] { new Vector4(0.25f, 0.2f, 0.467f, 0f), 1, new Vector3(0.25f, 0.2f, 0.467f) },
            new object[] { new Vector4(0.8f, 0.9f, 0.467f, 0f), 3, new Vector3(0.8f, 0.9f, 0.901f) },
            new object[] { new Vector4(0.5f, 0.5f, 0.5f, 0f), 3, new Vector3(0.5f, 0.501f, 0.502f) },
            new object[] { new Vector4(1f, 1f, 1f, 0f), 3, new Vector3(0.997f, 0.998f, 0.999f) },
            new object[] { new Vector4(1f, 0.2f, 0.467f, 0f), 1, new Vector3(0.999f, 0.2f, 0.467f) },
        };

        [TestCaseSource(nameof(s_SortSplitsCases))]
        public void SortSplits_ReturnsIncreasingSplitsBelowOne(Vector4 splits, int splitCount, Vector3 expected)
        {
            Vector4 sorted = ShadowCascadeSplitGUI.SortSplits(splits, splitCount);

            for (int i = 0; i < splitCount; ++i)
                Assert.AreEqual(expected[i], sorted[i], k_Tolerance, $"split {i}");
            Assert.AreEqual(1f, sorted[splitCount], $"split {splitCount}");
        }
    }
}
