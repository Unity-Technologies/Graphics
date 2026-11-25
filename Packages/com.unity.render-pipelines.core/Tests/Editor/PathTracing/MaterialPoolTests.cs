using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.Experimental.Rendering;
using UnityEngine.PathTracing.Core;
using UnityEngine.TestTools;

namespace UnityEngine.PathTracing.Tests
{
    internal class MaterialPoolTests
    {
        private MaterialPool _pool;
        private readonly List<Object> _objects = new();

        [SetUp]
        public void Setup()
        {
            var worldResources = new WorldResourceSet();
            worldResources.LoadFromAssetDatabase();
            _pool = new MaterialPool(worldResources.SetAlphaChannelShader, worldResources.BlitCubemap, worldResources.BlitGrayScaleCookie, maxSliceCount: 1);
        }

        [TearDown]
        public void TearDown()
        {
            _pool?.Dispose();
            foreach (var obj in _objects)
                Object.DestroyImmediate(obj);
            _objects.Clear();
        }

        private RenderTexture CreateRenderTexture(int size)
        {
            var texture = new RenderTexture(size, size, 0, GraphicsFormat.R8G8B8A8_UNorm);
            texture.Create();
            _objects.Add(texture);
            return texture;
        }

        private MaterialPool.MaterialDescriptor CreateDescriptor(int albedoSize)
        {
            return new MaterialPool.MaterialDescriptor
            {
                Albedo = CreateRenderTexture(albedoSize),
                AlbedoScale = Vector2.one,
                EmissionType = Core.MaterialPropertyType.None,
                TransmissionChannels = TransmissionChannels.None,
                Alpha = 1.0f,
            };
        }

        [Test]
        public void AddMaterial_AtlasFull_IsRejected()
        {
            Assert.IsTrue(_pool.AddMaterial(1, CreateDescriptor(256), UVChannel.UV0));
            Assert.IsFalse(_pool.AddMaterial(2, CreateDescriptor(256), UVChannel.UV0));
            Assert.AreEqual(1, _pool.MaterialCount);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UpdateMaterial_ShrinkAtLimit_FreesSpaceForMoreMaterials()
        {
            Assert.IsTrue(_pool.AddMaterial(1, CreateDescriptor(256), UVChannel.UV0));
            Assert.IsTrue(_pool.UpdateMaterial(1, CreateDescriptor(64), UVChannel.UV0));

            _pool.GetAtlasUsage(out var albedo, out _, out _);
            Assert.AreEqual(1, albedo.UsedSliceCount);

            Assert.IsTrue(_pool.AddMaterial(2, CreateDescriptor(64), UVChannel.UV0));
            Assert.AreEqual(2, _pool.MaterialCount);
        }

        [Test]
        public void UpdateMaterial_GrowAtLimit_UpdatesInPlace()
        {
            Assert.IsTrue(_pool.AddMaterial(1, CreateDescriptor(64), UVChannel.UV0));
            Assert.IsTrue(_pool.AddMaterial(2, CreateDescriptor(128), UVChannel.UV0));
            Assert.IsTrue(_pool.AddMaterial(3, CreateDescriptor(64), UVChannel.UV0));

            var grown = CreateDescriptor(256);
            grown.EmissionType = Core.MaterialPropertyType.Color;
            grown.EmissionColor = Vector3.one;
            Assert.IsTrue(_pool.UpdateMaterial(1, grown, UVChannel.UV0), "Without room for a bigger node, the texture should be scaled into the existing one.");
            Assert.IsTrue(_pool.IsEmissive(1, out _), "The rest of the material update should be applied.");

            _pool.GetAtlasUsage(out var albedo, out _, out _);
            Assert.AreEqual(1, albedo.UsedSliceCount);
            Assert.AreEqual(3, _pool.MaterialCount);
        }
    }
}
