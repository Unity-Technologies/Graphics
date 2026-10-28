using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace UnityEngine.PathTracing.Tests
{
    internal class TextureSlotAllocatorTests
    {
        private TextureSlotAllocator _allocator;
        private readonly List<RenderTexture> _sources = new();

        [SetUp]
        public void Setup()
        {
            _allocator = new TextureSlotAllocator(256, GraphicsFormat.R8G8B8A8_UNorm, FilterMode.Bilinear, maxSliceCount: 4);
        }

        [TearDown]
        public void TearDown()
        {
            _allocator?.Dispose();
            foreach (var source in _sources)
            {
                source.Release();
                Object.DestroyImmediate(source);
            }
            _sources.Clear();
        }

        private RenderTexture CreateSource(int size, Color color)
        {
            var texture = new RenderTexture(size, size, 0, GraphicsFormat.R8G8B8A8_UNorm);
            texture.Create();
            var previous = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(false, true, color);
            RenderTexture.active = previous;
            _sources.Add(texture);
            return texture;
        }

        private TextureSlotAllocator.TextureLocation Add(RenderTexture source)
        {
            return _allocator.AddTexture(source, Vector2.one, Vector2.zero);
        }

        [Test]
        public void AddTexture_BeyondSliceLimit_ReturnsInvalidWithoutErrors()
        {
            var source = CreateSource(256, Color.red);
            int[] expectedCapacities = { 1, 2, 4, 4 };
            for (int i = 0; i < 4; ++i)
            {
                Assert.IsTrue(Add(source).IsValid, $"Add {i} should succeed.");
                Assert.AreEqual(expectedCapacities[i], _allocator.GetUsage().CapacitySliceCount, $"Capacity after add {i}.");
            }

            Assert.IsFalse(Add(source).IsValid, "Adding past the slice limit should fail.");
            Assert.AreEqual(4, _allocator.GetUsage().UsedSliceCount);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AddTexture_AfterRemoveAtLimit_SucceedsWithoutGrowing()
        {
            var source = CreateSource(256, Color.red);
            var locations = new TextureSlotAllocator.TextureLocation[4];
            for (int i = 0; i < 4; ++i)
                locations[i] = Add(source);

            _allocator.RemoveTexture(locations[2]);

            Assert.IsTrue(Add(source).IsValid);
            Assert.AreEqual(4, _allocator.GetUsage().CapacitySliceCount);
        }

        [Test]
        public void GetUsage_EmptiedSlice_IsNotCountedAsUsed()
        {
            var source = CreateSource(256, Color.red);
            var first = Add(source);
            Add(source);

            _allocator.RemoveTexture(first);

            var usage = _allocator.GetUsage();
            Assert.AreEqual(1, usage.UsedSliceCount);
            Assert.AreEqual(2, usage.CapacitySliceCount);

            Add(CreateSource(64, Color.green));
            Assert.AreEqual(2, _allocator.GetUsage().UsedSliceCount, "The emptied slice should be reused.");
        }

        [Test]
        public void AddTexture_TextureLargerThanSlice_ReusesFreedSlice()
        {
            var source = CreateSource(512, Color.red);
            var location = Add(source);
            for (int i = 1; i < 4; ++i)
                Add(CreateSource(256, Color.green));
            _allocator.RemoveTexture(location);

            Assert.IsTrue(Add(source).IsValid, "A texture larger than a slice should fit in a freed slice.");
            Assert.AreEqual(4, _allocator.GetUsage().UsedSliceCount);
        }

        [Test]
        public void AddTexture_SixteenQuarterSizeTextures_FitInOneSlice()
        {
            var source = CreateSource(64, Color.red);
            for (int i = 0; i < 16; ++i)
                Assert.IsTrue(Add(source).IsValid);

            var usage = _allocator.GetUsage();
            Assert.AreEqual(1, usage.UsedSliceCount);
            Assert.AreEqual(1, usage.CapacitySliceCount);
        }

        [Test]
        public void GetUsage_ReportsAllocatedBytes()
        {
            Add(CreateSource(256, Color.red));
            Add(CreateSource(256, Color.red));

            Assert.AreEqual(2L * 256 * 256 * 4, _allocator.GetUsage().AllocatedBytes);
            Assert.AreEqual(4, _allocator.GetUsage().MaxSliceCount);
        }

        [Test]
        public void AddTexture_ResizeAtlas_PreservesExistingSliceContent()
        {
            Add(CreateSource(256, Color.green));
            Add(CreateSource(256, Color.blue));
            Assert.AreEqual(2, _allocator.GetUsage().CapacitySliceCount);

            Add(CreateSource(256, Color.red));
            Assert.AreEqual(4, _allocator.GetUsage().CapacitySliceCount, "The third add should have resized the atlas.");

            var request = AsyncGPUReadback.Request(_allocator.Texture, 0, 0, 1, 0, 1, 0, 1);
            request.WaitForCompletion();
            Assert.IsFalse(request.hasError);
            Color32 pixel = request.GetData<Color32>()[0];
            Assert.AreEqual(new Color32(0, 255, 0, 255), pixel);

            request = AsyncGPUReadback.Request(_allocator.Texture, 0, 0, 1, 0, 1, 1, 1);
            request.WaitForCompletion();
            Assert.IsFalse(request.hasError);
            pixel = request.GetData<Color32>()[0];
            Assert.AreEqual(new Color32(0, 0, 255, 255), pixel);
        }
    }
}
