using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEditor.Rendering.Tests;

namespace UnityEngine.Rendering.Tests
{
    partial class RenderGraphTests : RenderGraphTestsCore
    {
        class ResettablePassData : IRenderGraphPassData
        {
            public bool wasReset = false;
            public void Reset() { wasReset = true; }
        }

        class DummyPassData { }

        [Test]
        public void RenderGraphObjectPool_Release_CallsResetOnIRenderGraphPassData()
        {
            var pool = new RenderGraphObjectPool();

            var data = pool.Get<ResettablePassData>();
            Assert.IsFalse(data.wasReset, "Reset() should not have been called before release");
            pool.Release(data);
            Assert.IsTrue(data.wasReset, "Reset() should have been called on release");

            // Same instance comes back since it's the only ResettablePassData in the pool
            var second = pool.Get<ResettablePassData>();
            Assert.IsTrue(second.wasReset, "Object retrieved from pool should have had Reset() called before being returned");
        }

        [Test]
        public void RenderGraphObjectPool_Release_DoesNotThrowForNonResettablePassData()
        {
            var pool = new RenderGraphObjectPool();
            var data = pool.Get<DummyPassData>();

            Assert.DoesNotThrow(() => pool.Release(data));
        }
    }
}
