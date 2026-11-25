using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    sealed class UniversalRenderPipelineAssetShadowmapDepthBitsTests
    {
        UniversalRenderPipelineAsset m_Asset;

        [SetUp]
        public void SetUp()
        {
            m_Asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Asset);
        }

        [Test]
        [Description("A newly created asset renders both shadow maps with 16-bit depth.")]
        public void GivenNewAsset_WhenCreated_ThenBothShadowmapsUse16BitDepth()
        {
            Assert.That(m_Asset.mainLightShadowmapDepthBits, Is.EqualTo(DepthBits.Depth16), "A new asset must default the main light shadow map to 16-bit depth.");
            Assert.That(m_Asset.additionalLightsShadowmapDepthBits, Is.EqualTo(DepthBits.Depth16), "A new asset must default the additional lights shadow map atlas to 16-bit depth.");
        }

        [Test]
        [Description("An asset already at the last version keeps the precision it was saved with.")]
        public void GivenAssetAtLastVersion_WhenDeserialized_ThenShadowmapDepthBitsAreUnchanged()
        {
            Assume.That(m_Asset.IsAtLastVersion(), Is.True, "A new asset must start at the last version.");
            m_Asset.mainLightShadowmapDepthBits = DepthBits.Depth32;
            m_Asset.additionalLightsShadowmapDepthBits = DepthBits.Depth32;

            m_Asset.OnAfterDeserialize();

            Assert.That(m_Asset.mainLightShadowmapDepthBits, Is.EqualTo(DepthBits.Depth32), "Deserializing an asset at the last version must not change the main light shadow map precision.");
            Assert.That(m_Asset.additionalLightsShadowmapDepthBits, Is.EqualTo(DepthBits.Depth32), "Deserializing an asset at the last version must not change the additional lights shadow map precision.");
        }
    }
}
