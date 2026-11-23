#if U2D_ANIMATION_INSTALLED
using NUnit.Framework;
using UnityEngine.U2D.Animation;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowCaster2DTests
    {
        GameObject m_Obj;

        [SetUp]
        public void Setup()
        {
            m_Obj = new GameObject();
            m_Obj.AddComponent<SpriteRenderer>();
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_Obj);
        }

        [Test]
        public void AddShadowCaster2DWithSpriteSkin()
        {
            m_Obj.AddComponent<SpriteSkin>();
            ShadowCaster2D shadowCaster2D = m_Obj.AddComponent<ShadowCaster2D>();

// ShadowCaster2D.shadowShape2DProvider is always null on Standalone.
#if UNITY_EDITOR
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.TypeOf(typeof(ShadowShape2DProvider_SpriteSkin)));
#else
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.Null);
#endif
        }

        [Test]
        public void AddShadowCaster2DWithSpriteSkinWhenInactive()
        {
            m_Obj.AddComponent<SpriteSkin>();
            m_Obj.SetActive(false);
            ShadowCaster2D shadowCaster2D = m_Obj.AddComponent<ShadowCaster2D>();
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.Null);

            m_Obj.SetActive(true);
// ShadowCaster2D.shadowShape2DProvider is always null on Standalone.
#if UNITY_EDITOR
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.TypeOf(typeof(ShadowShape2DProvider_SpriteSkin)));
#else
            Assert.That(shadowCaster2D.shadowShape2DProvider, Is.Null);
#endif
        }
    }
}
#endif
