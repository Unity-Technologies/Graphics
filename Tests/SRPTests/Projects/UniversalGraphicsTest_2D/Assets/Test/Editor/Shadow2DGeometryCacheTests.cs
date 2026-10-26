using System.Reflection;
using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// Covers the cache Shadow2DGeometry keeps of Shadow2DGeometrySettings (UUM-154097): that it
    /// holds between invalidations, that every path which changes the setting clears it, and that
    /// the global override still takes precedence over the cached project value.
    /// </summary>
    /// <remarks>
    /// These write to the project's real graphics settings, because that is what is being cached.
    /// Every test restores the original version in TearDown, through the setter, so the cache is
    /// cleared on the way out as well.
    /// </remarks>
    class Shadow2DGeometryCacheTests
    {
        static readonly FieldInfo k_GeometryVersionField =
            typeof(Shadow2DGeometrySettings).GetField("m_GeometryVersion", BindingFlags.NonPublic | BindingFlags.Instance);

        Shadow2DGeometrySettings m_Settings;
        Shadow2DGeometrySettings.GeometryVersion m_OriginalVersion;

        [SetUp]
        public void Setup()
        {
            Assert.IsTrue(GraphicsSettings.TryGetRenderPipelineSettings(out m_Settings),
                "The test project must carry Shadow2DGeometrySettings in its URP global settings.");
            Assert.IsNotNull(k_GeometryVersionField, "Shadow2DGeometrySettings.m_GeometryVersion was renamed.");

            m_OriginalVersion = m_Settings.geometryVersion;
            Shadow2DGeometry.globalGeneratorOverride = null;
            Shadow2DGeometry.InvalidateCache();
        }

        [TearDown]
        public void Cleanup()
        {
            Shadow2DGeometry.globalGeneratorOverride = null;

            if (m_Settings != null)
                m_Settings.geometryVersion = m_OriginalVersion;
        }

        static Shadow2DGeometrySettings.GeometryVersion Other(Shadow2DGeometrySettings.GeometryVersion version)
        {
            return version == Shadow2DGeometrySettings.GeometryVersion.Legacy
                ? Shadow2DGeometrySettings.GeometryVersion.Enhanced
                : Shadow2DGeometrySettings.GeometryVersion.Legacy;
        }

        static string FormatFor(Shadow2DGeometrySettings.GeometryVersion version)
        {
            return version >= Shadow2DGeometrySettings.GeometryVersion.Enhanced
                ? SoftShadowGeometryGenerator.k_Id
                : LegacyShadowGeometryGenerator.k_Id;
        }

        static void AssertResolvesTo(Shadow2DGeometrySettings.GeometryVersion expected, string because)
        {
            Assert.AreEqual(expected, Shadow2DGeometry.geometryVersion, because);
            Assert.AreEqual(FormatFor(expected), Shadow2DGeometry.projectFormat, because);
            Assert.AreEqual(expected >= Shadow2DGeometrySettings.GeometryVersion.Enhanced, Shadow2DGeometry.enhancedGeometryEnabled, because);
            Assert.AreEqual(FormatFor(expected), new ShadowMesh2D().activeGeneratorId, because);
        }

        [Test]
        public void SetterChange_IsSeenImmediately()
        {
            AssertResolvesTo(m_OriginalVersion, "before any change");

            var flipped = Other(m_OriginalVersion);
            m_Settings.geometryVersion = flipped;
            AssertResolvesTo(flipped, "after a change through the geometryVersion setter");

            m_Settings.geometryVersion = m_OriginalVersion;
            AssertResolvesTo(m_OriginalVersion, "after changing it back");
        }

        // Writes the backing field directly, the way the Graphics settings inspector and Undo do. The
        // stale read is the cache doing its job; the render is what must clear it.
        [Test]
        public void EditBypassingSetter_IsSeenAfterRenderedFrame()
        {
            AssertResolvesTo(m_OriginalVersion, "before any change");

            var flipped = Other(m_OriginalVersion);
            k_GeometryVersionField.SetValue(m_Settings, flipped);

            AssertResolvesTo(m_OriginalVersion, "a write that bypasses the setter is not seen until the cache is refreshed");

            RenderOneFrame();

            AssertResolvesTo(flipped, "after a rendered frame, which refreshes the cache in the editor");
        }

        [Test]
        public void InvalidateCache_RereadsSettings()
        {
            AssertResolvesTo(m_OriginalVersion, "before any change");

            var flipped = Other(m_OriginalVersion);
            k_GeometryVersionField.SetValue(m_Settings, flipped);
            Shadow2DGeometry.InvalidateCache();

            AssertResolvesTo(flipped, "after an explicit invalidation");
        }

        [TestCase(Shadow2DGeometrySettings.GeometryVersion.Legacy)]
        [TestCase(Shadow2DGeometrySettings.GeometryVersion.Enhanced)]
        public void GlobalOverride_TakesPrecedenceOverCachedVersion(Shadow2DGeometrySettings.GeometryVersion projectVersion)
        {
            m_Settings.geometryVersion = projectVersion;

            // Read once first, so the override is tested against a filled cache.
            Assert.AreEqual(projectVersion, Shadow2DGeometry.geometryVersion);

            Shadow2DGeometry.globalGeneratorOverride = SoftShadowGeometryGenerator.k_Id;
            Assert.IsTrue(Shadow2DGeometry.enhancedGeometryEnabled, "a SoftShadow override enables enhanced geometry");
            Assert.AreEqual(SoftShadowGeometryGenerator.k_Id, Shadow2DGeometry.ResolveId(null));

            Shadow2DGeometry.globalGeneratorOverride = LegacyShadowGeometryGenerator.k_Id;
            Assert.IsFalse(Shadow2DGeometry.enhancedGeometryEnabled, "a Legacy override disables enhanced geometry");
            Assert.AreEqual(LegacyShadowGeometryGenerator.k_Id, Shadow2DGeometry.ResolveId(null));

            Shadow2DGeometry.globalGeneratorOverride = null;
            AssertResolvesTo(projectVersion, "clearing the override returns to the project version");
        }

        static void RenderOneFrame()
        {
            var go = new GameObject("Shadow2DGeometryCacheTests Camera");
            var target = new RenderTexture(16, 16, 24);
            try
            {
                var camera = go.AddComponent<Camera>();
                camera.targetTexture = target;
                camera.Render();
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(target);
            }
        }
    }
}
