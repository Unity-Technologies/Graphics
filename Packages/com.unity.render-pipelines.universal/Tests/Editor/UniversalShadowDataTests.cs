using NUnit.Framework;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    [TestFixture]
    sealed class UniversalShadowDataTests
    {
        UniversalShadowData m_ShadowData;

        [SetUp]
        public void SetUp()
        {
            m_ShadowData = new UniversalShadowData();
        }

        [TearDown]
        public void TearDown()
        {
            m_ShadowData = null;
        }

        [Test]
        public void GivenFreshlyConstructedShadowData_WhenCachingWasNeverConfigured_ThenShadowMapCachingIsDisabled()
        {
            Assert.That(m_ShadowData.shadowMapCachingEnabled, Is.False, "Shadow map caching must default to disabled, since nothing assigns it outside an XR QuadViews setup.");
        }

        [Test]
        public void GivenFreshlyConstructedShadowData_WhenCachingWasNeverConfigured_ThenCachedShadowMapIsNotUsed()
        {
            Assert.That(m_ShadowData.useCachedShadowMap, Is.False, "useCachedShadowMap must default to false, otherwise the shadow passes would reuse an atlas that was never rendered.");
        }

        [Test]
        public void GivenShadowDataThatUsedACachedShadowMap_WhenResetCalled_ThenCachedShadowMapIsNotUsed()
        {
            m_ShadowData.useCachedShadowMap = true;
            m_ShadowData.Reset();
            Assert.That(m_ShadowData.useCachedShadowMap, Is.False, "Reset() must clear useCachedShadowMap, otherwise a reused UniversalShadowData makes the next camera reuse the previous camera's shadow atlas.");
        }

        [Test]
        public void GivenShadowDataWithCachingEnabled_WhenResetCalled_ThenShadowMapCachingIsDisabled()
        {
            m_ShadowData.shadowMapCachingEnabled = true;
            m_ShadowData.Reset();
            Assert.That(m_ShadowData.shadowMapCachingEnabled, Is.False, "Reset() must clear shadowMapCachingEnabled, otherwise a reused UniversalShadowData leaks the XR QuadViews caching state into a non-XR camera.");
        }
    }
}