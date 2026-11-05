using NUnit.Framework;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Tests
{
    // Covers the bookkeeping Texture2DAtlas does to decide whether a slot has to be re-uploaded. The cases that
    // matter are the ones where the atlas invalidates a slot on its own: the source texture is untouched, so an
    // update count alone cannot tell the atlas that its copy is gone.
    class Texture2DAtlasTests
    {
        const int k_AtlasSize = 128;
        const int k_TextureSize = 32;

        Texture2DAtlas m_Atlas;
        RenderTexture m_RenderTexture;

        [SetUp]
        public void SetUp()
        {
            m_Atlas = new Texture2DAtlas(k_AtlasSize, k_AtlasSize, GraphicsFormat.R8G8B8A8_UNorm, name: "Texture2DAtlas Tests", useMipMap: false);

            m_RenderTexture = new RenderTexture(k_TextureSize, k_TextureSize, 0) { hideFlags = HideFlags.HideAndDontSave };
            m_RenderTexture.Create();
        }

        [TearDown]
        public void TearDown()
        {
            m_Atlas.Release();
            m_Atlas = null;

            m_RenderTexture.Release();
            Object.DestroyImmediate(m_RenderTexture);
            m_RenderTexture = null;
        }

        [Test, NUnit.Framework.Property("Jira", "UUM-139389")]
        public void ReallocatedSlotNeedsUploadEvenThoughTheRenderTextureIsUnchanged()
        {
            var identifier = m_Atlas.GetTextureIdentifier(m_RenderTexture);
            var scaleOffset = Vector4.zero;

            Assert.IsTrue(m_Atlas.AllocateTextureWithoutBlit(identifier, k_TextureSize, k_TextureSize, ref scaleOffset),
                "The atlas should have room for the render texture.");

            m_Atlas.NeedsUpdate(m_RenderTexture); // Caches the current update count, as a frame using the cookie would.

            uint updateCountBeforeReallocation = m_RenderTexture.updateCount;

            // What PowerOfTwoTextureAtlas.ReserveSpace does when a cookie changes resolution: the slot is handed
            // back to the allocator and reserved again. The render texture is untouched, so it is the atlas, not
            // the source, that knows the slot is empty.
            Assert.IsTrue(m_Atlas.AllocateTextureWithoutBlit(identifier, k_TextureSize, k_TextureSize, ref scaleOffset));

            Assert.AreEqual(updateCountBeforeReallocation, m_RenderTexture.updateCount,
                "Sanity check: nothing should have rendered into the render texture.");

            Assert.IsTrue(m_Atlas.NeedsUpdate(m_RenderTexture),
                "The new slot holds no data yet, so it needs to be uploaded even though the update count is unchanged.");
        }

        [Test, NUnit.Framework.Property("Jira", "UUM-139389")]
        public void SlotStaysOutOfDateUntilItIsUploaded()
        {
            // Guards the fallback on the atlas' own view of the slot. The update count only answers "did the source
            // change", so once it has been cached every later call compares equal and the answer has to come from
            // somewhere else. Without the fallback this returns false while the slot is still empty.
            var scaleOffset = Vector4.zero;
            Assert.IsTrue(m_Atlas.AllocateTextureWithoutBlit(m_Atlas.GetTextureIdentifier(m_RenderTexture), k_TextureSize, k_TextureSize, ref scaleOffset));

            Assert.IsTrue(m_Atlas.NeedsUpdate(m_RenderTexture),
                "The slot has never been uploaded.");

            Assert.IsTrue(m_Atlas.NeedsUpdate(m_RenderTexture),
                "Asking twice does not upload anything, so the slot is still empty and still needs an upload.");
        }

        // The overload taking a raw update count is used by callers that render into the atlas themselves rather
        // than going through a blit, so the atlas is never told the content landed. For those, the cached count is
        // the only record that a slot holds anything, and it has to be dropped whenever the slot is emptied.
        [Test, NUnit.Framework.Property("Jira", "UUM-139389")]
        public void SlotWithCallerSuppliedUpdateCountNeedsUploadAfterReallocation()
        {
            var identifier = m_Atlas.GetTextureIdentifier(m_RenderTexture);
            var scaleOffset = Vector4.zero;
            const int constantUpdateCount = 7; // A decal that never changes: the count alone can never ask for an upload.

            Assert.IsTrue(m_Atlas.AllocateTextureWithoutBlit(identifier, k_TextureSize, k_TextureSize, ref scaleOffset));

            Assert.IsTrue(m_Atlas.NeedsUpdate(identifier, constantUpdateCount),
                "A freshly allocated slot holds nothing yet.");

            Assert.IsFalse(m_Atlas.NeedsUpdate(identifier, constantUpdateCount),
                "Nothing changed since the upload, so the caller must not redraw the slot every frame.");

            // PowerOfTwoTextureAtlas.ReserveSpace on a resolution change, and RelayoutEntries, both hand the slot
            // back to the allocator and reserve it again. The source is untouched, so the count is unchanged.
            Assert.IsTrue(m_Atlas.AllocateTextureWithoutBlit(identifier, k_TextureSize / 2, k_TextureSize / 2, ref scaleOffset));

            Assert.IsTrue(m_Atlas.NeedsUpdate(identifier, constantUpdateCount),
                "The new slot is empty, so it needs an upload even though the caller's update count is unchanged.");
        }

        [Test]
        public void RegularTextureNeedsUploadWhileTheSlotIsEmpty()
        {
            // The non render texture path has no update count to compare, so the atlas' own view of the slot is the
            // only thing that can ask for the upload.
            var texture = new Texture2D(k_TextureSize, k_TextureSize) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var scaleOffset = Vector4.zero;
                Assert.IsTrue(m_Atlas.AllocateTextureWithoutBlit(m_Atlas.GetTextureIdentifier(texture), k_TextureSize, k_TextureSize, ref scaleOffset));

                Assert.IsTrue(m_Atlas.NeedsUpdate(texture),
                    "The slot has never been uploaded.");
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void SlotWithCallerSuppliedUpdateCountOutsideTheAtlasDoesNotNeedUpdate()
        {
            // Same reasoning as TextureOutsideTheAtlasDoesNotNeedUpdate: with no slot there is nothing to refresh,
            // and the caller would otherwise render over whatever sits at the atlas origin.
            Assert.IsFalse(m_Atlas.NeedsUpdate(m_Atlas.GetTextureIdentifier(m_RenderTexture), 7),
                "An identifier the atlas has never allocated has no slot to refresh.");
        }

        [Test]
        public void TextureOutsideTheAtlasDoesNotNeedUpdate()
        {
            // Not an academic case: HDRP's LightCookieManager.Fetch2DCookie logs when a cookie is missing from the
            // atlas but still calls NeedsUpdate, and IsCached leaves its scale-offset at zero on a miss. Reporting
            // an update for a texture with no slot would blit it over the atlas origin, on top of another light's
            // cookie, every frame the atlas is out of space.
            Assert.IsFalse(m_Atlas.NeedsUpdate(m_RenderTexture),
                    "A texture the atlas has never allocated has no slot to refresh.");
            }

        [Test]
        public void UploadedSlotIsUpToDate()
        {
            // The other direction of SlotStaysOutOfDateUntilItIsUploaded: once the content lands the atlas has to
            // stop asking, or every consumer re-uploads every frame.
            var atlas = new FakeBlitAtlas(useMipMap: false);
            try
            {
                var scaleOffset = Vector4.zero;
                Assert.IsTrue(atlas.AllocateTextureWithoutBlit(atlas.GetTextureIdentifier(m_RenderTexture), k_TextureSize, k_TextureSize, ref scaleOffset));
                Assert.IsTrue(atlas.NeedsUpdate(m_RenderTexture), "The slot has never been uploaded.");

                atlas.BlitTexture(null, scaleOffset, m_RenderTexture, Vector4.zero);

                Assert.IsFalse(atlas.NeedsUpdate(m_RenderTexture),
                    "The slot holds the current content, so nothing needs re-uploading.");
            }
            finally
            {
                atlas.Release();
            }
        }

        [Test]
        public void SlotUploadedWithoutMipsNeedsUploadOnlyWhenMipsAreRequested()
        {
            // The mip levels are tracked separately from the slot itself, so a mip0-only upload still satisfies a
            // caller that does not sample mips and still fails one that does.
            var atlas = new FakeBlitAtlas(useMipMap: true);
            try
            {
                var scaleOffset = Vector4.zero;
                Assert.IsTrue(atlas.AllocateTextureWithoutBlit(atlas.GetTextureIdentifier(m_RenderTexture), k_TextureSize, k_TextureSize, ref scaleOffset));
                Assert.IsTrue(atlas.NeedsUpdate(m_RenderTexture), "The slot has never been uploaded.");

                atlas.BlitTexture(null, scaleOffset, m_RenderTexture, Vector4.zero, blitMips: false);

                Assert.IsFalse(atlas.NeedsUpdate(m_RenderTexture, needMips: false),
                    "Mip 0 is current and the caller does not need mips.");
                Assert.IsTrue(atlas.NeedsUpdate(m_RenderTexture, needMips: true),
                    "Only mip 0 was uploaded, so a caller that samples mips still needs an upload.");
            }
            finally
            {
                atlas.Release();
            }
        }

        // Stands in for a real blit: does the bookkeeping a blit would, without going through Blitter and therefore
        // without an initialized render pipeline. PowerOfTwoTextureAtlas overrides BlitTexture the same way.
        class FakeBlitAtlas : Texture2DAtlas
        {
            public FakeBlitAtlas(bool useMipMap)
                : base(k_AtlasSize, k_AtlasSize, GraphicsFormat.R8G8B8A8_UNorm, name: "FakeBlitAtlas Tests", useMipMap: useMipMap)
            {
            }

            public override void BlitTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, TextureIdentifier overrideIdentifier = default)
                => MarkGPUTextureValid(overrideIdentifier != default ? overrideIdentifier : GetTextureIdentifier(texture), blitMips);
        }
    }
}
