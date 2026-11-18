using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    class ShadowsVolumeOverrideTests
    {
        ShadowsVolumeOverride m_Shadows;

        [SetUp]
        public void SetUp()
        {
            m_Shadows = ScriptableObject.CreateInstance<ShadowsVolumeOverride>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Shadows);
        }

        [Test]
        [Description("The component defaults match the URP asset defaults, so an asset left at its defaults needs no migrated override.")]
        public void Defaults_MatchUniversalRenderPipelineAssetDefaults()
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
#pragma warning disable 618 // Obsolete warning
                Assert.AreEqual(asset.m_ObsoleteShadowDistance, m_Shadows.maxShadowDistance.value);
                Assert.AreEqual(asset.m_ObsoleteShadowCascadeCount, m_Shadows.cascadeCount.value);
                Assert.AreEqual(asset.m_ObsoleteCascade4Split.x, m_Shadows.cascadeSplit0.value);
                Assert.AreEqual(asset.m_ObsoleteCascade4Split.y, m_Shadows.cascadeSplit1.value);
                Assert.AreEqual(asset.m_ObsoleteCascade4Split.z, m_Shadows.cascadeSplit2.value);
                Assert.AreEqual(asset.m_ObsoleteCascadeBorder, m_Shadows.lastCascadeBorder.value);
#pragma warning restore 618 // Obsolete warning
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }

            Assert.AreEqual(50f, m_Shadows.maxShadowDistance.value);
            Assert.AreEqual(1, m_Shadows.cascadeCount.value);
            Assert.AreEqual(0.067f, m_Shadows.cascadeSplit0.value);
            Assert.AreEqual(0.2f, m_Shadows.cascadeSplit1.value);
            Assert.AreEqual(0.467f, m_Shadows.cascadeSplit2.value);
            Assert.AreEqual(0.2f, m_Shadows.lastCascadeBorder.value);
        }

        static readonly object[] k_CascadeSplitCases =
        {
            new object[] { 1, new Vector3(0.125f, 0.25f, 0.5f), new Vector3(1f, 0f, 0f) },
            new object[] { 2, new Vector3(0.125f, 0.25f, 0.5f), new Vector3(0.125f, 1f, 0f) },
            new object[] { 3, new Vector3(0.125f, 0.25f, 0.5f), new Vector3(0.125f, 0.25f, 0f) },
            new object[] { 4, new Vector3(0.125f, 0.25f, 0.5f), new Vector3(0.125f, 0.25f, 0.5f) },
            new object[] { 1, new Vector3(0.5f, 0.25f, 0.125f), new Vector3(1f, 0f, 0f) },
            new object[] { 2, new Vector3(0.5f, 0.25f, 0.125f), new Vector3(0.5f, 1f, 0f) },
            new object[] { 3, new Vector3(0.5f, 0.25f, 0.125f), new Vector3(0.5f, 0.5f, 0f) },
            new object[] { 4, new Vector3(0.5f, 0.25f, 0.125f), new Vector3(0.5f, 0.5f, 0.5f) },
        };

        [Test, TestCaseSource(nameof(k_CascadeSplitCases))]
        [Description("GetCascadeSplit returns the split vector the URP asset produced for each cascade count and keeps the splits in ascending order.")]
        public void GetCascadeSplit_MatchesAssetLayoutForCascadeCount(int cascadeCount, Vector3 splits, Vector3 expected)
        {
            m_Shadows.cascadeCount.value = cascadeCount;
            m_Shadows.cascadeSplit0.value = splits.x;
            m_Shadows.cascadeSplit1.value = splits.y;
            m_Shadows.cascadeSplit2.value = splits.z;

            Assert.AreEqual(expected, m_Shadows.GetCascadeSplit());
        }
    }
}
