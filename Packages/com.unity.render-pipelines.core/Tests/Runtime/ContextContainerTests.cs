using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace UnityEngine.Rendering.Tests
{

    class TestData : ContextItem
    {
        public int x;
        public float y;
        public bool z;

        public override void Reset()
        {
            x = 0;
            y = 0f;
            // Reuse z without clearing.
        }
    }

    class OtherTestData : ContextItem
    {
        public List<int> list = new();

        public override void Reset()
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = 0;
            }
        }
    }

    class ContextContainerTests
    {
        ContextContainer m_container = new();

        [OneTimeSetUp]
        public void FirstCreationOfData()
        {
            CreateMemoryAlloc();
            m_container.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            m_container.Dispose();
            TestData data = m_container.Create<TestData>();
            data.x = 0;
            data.y = 0f;
            data.z = false;

            OtherTestData other = m_container.Create<OtherTestData>();
            other.list.Clear();

            m_container.Dispose();
        }

        [Test]
        public void ReuseData()
        {
            {
                TestData data = CreateTestData();
                Assert.That(data.x, Is.EqualTo(0));
                Assert.That(data.y, Is.EqualTo(0f));
                Assert.That(data.z, Is.EqualTo(false));
                data.x = -23;
                data.y = 8.4f;
                data.z = true;
                m_container.Dispose();
            }
            {
                TestData data = CreateTestData();
                Assert.That(data.x, Is.EqualTo(0));
                Assert.That(data.y, Is.EqualTo(0f));
                Assert.That(data.z, Is.EqualTo(true));
                m_container.Dispose();
            }
        }

        [Test]
        public void GetData()
        {
            Assert.Throws<InvalidOperationException>(() => m_container.Get<TestData>());
            {
                TestData data = CreateTestData();
                Assert.That(data.x, Is.EqualTo(0));
                Assert.That(data.y, Is.EqualTo(0f));
                Assert.That(data.z, Is.EqualTo(false));
                data.x = -23;
                data.y = 8.4f;
                data.z = true;
            }
            {
                TestData data = m_container.Get<TestData>();
                Assert.That(data.x, Is.EqualTo(-23));
                Assert.That(data.y, Is.EqualTo(8.4f));
                Assert.That(data.z, Is.EqualTo(true));
                m_container.Dispose();
            }
            Assert.Throws<InvalidOperationException>(() => m_container.Get<TestData>());
        }

        [Test]
        public void ContainsData()
        {
            // Initially does not container TestData:
            Assert.False(m_container.Contains<TestData>());
            Assert.Throws<InvalidOperationException>(() => m_container.Get<TestData>());

            // Create TestData without error and the container should now contain TestData:
            Assert.DoesNotThrow(() => CreateTestData());
            Assert.True(m_container.Contains<TestData>());
            // Create again should throw error:
            Assert.Throws<InvalidOperationException>(() => CreateTestData());
            Assert.True(m_container.Contains<TestData>());
            Assert.DoesNotThrow(() => m_container.Get<TestData>());

            // Clear the container from created items, TestData should no longer be contained:
            m_container.Dispose();
            Assert.False(m_container.Contains<TestData>());
            Assert.Throws<InvalidOperationException>(() => m_container.Get<TestData>());

            // GetOrCreate should create should set contains to true without error:
            Assert.DoesNotThrow(() => GetOrCreateTestData());
            Assert.True(m_container.Contains<TestData>());
            Assert.DoesNotThrow(() => m_container.Get<TestData>());

            // GetOrCreate should get without error and contains should still be true:
            Assert.DoesNotThrow(() => GetOrCreateTestData());
            Assert.True(m_container.Contains<TestData>());
            Assert.DoesNotThrow(() => m_container.Get<TestData>());
        }

        [Test]
        public void ReuseList()
        {
            {
                OtherTestData other = CreateOtherTestData();
                Assert.That(other.list, Is.EqualTo(new List<int>()));
                other.list.Add(4);
                other.list.Add(8);
                other.list.Add(3);
                other.list.Add(-7);
            }
            {
                OtherTestData other = m_container.Get<OtherTestData>();
                Assert.That(other.list, Is.EqualTo(new List<int>() { 4, 8, 3, -7 }));
                m_container.Dispose();
            }
            {
                OtherTestData other = CreateOtherTestData();
                Assert.That(other.list, Is.EqualTo(new List<int>() { 0, 0, 0, 0 }));
            }

        }

        // Need to have one create location to avoid debug allocations.
        TestData CreateTestData()
        {
            return m_container.Create<TestData>();
        }

        // Need to have one create location to avoid debug allocations.
        TestData GetOrCreateTestData()
        {
            return m_container.GetOrCreate<TestData>();
        }

        // Need to have one create location to avoid debug allocations.
        OtherTestData CreateOtherTestData()
        {
            return m_container.Create<OtherTestData>();
        }

#if CONTEXT_CONTAINER_ALLOCATOR_DEBUG
        public void CreateMemoryAlloc()
        {
            //Alloc for Create TestData
            Assert.That(() => { CreateTestData(); }, Is.AllocatingGCMemory());
            m_container.Dispose();
            Assert.That(() => { CreateTestData(); }, Is.Not.AllocatingGCMemory());
            m_container.Dispose();
            Assert.That(() => { GetOrCreateTestData(); }, Is.AllocatingGCMemory());
            m_container.Dispose();
            Assert.That(() => { GetOrCreateTestData(); }, Is.Not.AllocatingGCMemory());

            //Alloc for Create OtherTestData
            Assert.That(() => { CreateOtherTestData(); }, Is.AllocatingGCMemory());
            m_container.Dispose();
            Assert.That(() => { CreateOtherTestData(); }, Is.Not.AllocatingGCMemory());
        }
#else
        public void CreateMemoryAlloc()
        {
            //TestData
            Assert.That(() => { CreateTestData(); }, Is.AllocatingGCMemory());
            m_container.Dispose();
            Assert.That(() => { GetOrCreateTestData(); }, Is.Not.AllocatingGCMemory());
            m_container.Dispose();
            Assert.That(() => { m_container.Create<TestData>(); }, Is.Not.AllocatingGCMemory());

            //OtherTestData
            Assert.That(() => { CreateOtherTestData(); }, Is.AllocatingGCMemory());
            m_container.Dispose();
            Assert.That(() => { m_container.Create<OtherTestData>(); }, Is.Not.AllocatingGCMemory());
        }
#endif
    }
}
