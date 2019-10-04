using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.TestTools.Constraints;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    using Is = UnityEngine.TestTools.Constraints.Is;

    unsafe class FixedAllocatorTests
    {
        [Test]
        public void Ctor_Throws_WhenBufferIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => { var a = new FixedAllocator(null, 1); });
        }

        [Test]
        public void Ctor_Throws_WhenBufferIsTooSmall()
        {
            var ptr = stackalloc byte[1];
            Assert.Throws<ArgumentException>(() => { var a = new FixedAllocator(ptr, 1); });
        }

        [Test]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(64)]
        [TestCase(128)]
        [TestCase(256)]
        [TestCase(512)]
        public void Allocate_Works(int bufferSize)
        {
            var size = MemoryUtilities.Pad((uint)bufferSize, 4)
                       + (int)FixedAllocator.OverheadSize
                       + (int)FixedAllocator.OverheadSizePerAllocation;
            var ptr = stackalloc byte[(int)size];

            FixedAllocator alloc = default;
            byte* allocatedPtr = default;
            Assert.That(() =>
            {
                alloc = new FixedAllocator(ptr, (ulong)size);
                allocatedPtr = (byte*)alloc.Allocate((ulong)bufferSize);
            }, Is.Not.AllocatingGCMemory());
            
            Assert.True(
               ptr <= allocatedPtr && allocatedPtr + bufferSize <= ptr + size,
               $"Allocated memory {(ulong)allocatedPtr} - {(ulong)(allocatedPtr + bufferSize)} " +
               $"is not in allocation range {(ulong)ptr} - {(ulong)(ptr + size)}"
            );

            Assert.Throws<OutOfMemoryException>(() => alloc.Allocate(1));
        }

        [Test]
        public void API_Throws_WhenCurrentThreadIsInvalid()
        {
            var bufferPtr = stackalloc byte[64];
            var alloc = new FixedAllocator(bufferPtr, 64);
            var allocatedPtr = alloc.Allocate(1);
            // Use a nullable type to have a reference type that can be shared in another thread.
            bool? allThrows = null;

            var thread = new Thread(StartThread);
            thread.Start();
            thread.Join();

            void StartThread()
            {
                Assert.Throws<InvalidOperationException>(() => alloc.Allocate(1));
                Assert.Throws<InvalidOperationException>(() => alloc.Deallocate(allocatedPtr));
                Assert.Throws<InvalidOperationException>(() => alloc.Reallocate(allocatedPtr, 1));

                allThrows = true;
            }

            Assert.True(allThrows);
        }

        [Test]
        public void API_Throws_WhenAddressIsInvalid()
        {
            var bufferPtr = stackalloc byte[64];
            var alloc = new FixedAllocator(bufferPtr, 64);

            Assert.Throws<ArgumentException>(() => alloc.Deallocate((void*)1)); // Outside allocated range
            Assert.Throws<ArgumentException>(() => alloc.Deallocate(bufferPtr + 1)); // Not padded
            Assert.Throws<ArgumentException>(() => alloc.Reallocate((void*)1, 1)); // Outside allocated range
            Assert.Throws<ArgumentException>(() => alloc.Reallocate(bufferPtr + 1, 1)); // Not padded
        }

        [Test]
        public void Reallocate_Works_WithSmallerSize()
        {
            var bufferPtr = stackalloc byte[72];

            FixedAllocator alloc = default;
            byte* ptr = default, newPtr = default;

            Assert.That(() =>
            {
                alloc = new FixedAllocator(bufferPtr, 72);

                ptr = (byte*)alloc.Allocate(32);
                for (byte i = 0; i < 32; ++i)
                    ptr[i] = i;

                newPtr = (byte*)alloc.Reallocate(ptr, 28);

            }, Is.Not.AllocatingGCMemory());
            
            Assert.AreEqual((ulong)ptr, (ulong)newPtr);
        }

        [Test]
        public void Reallocate_Works_WithBiggerSize()
        {
            var bufferPtr = stackalloc byte[96];

            FixedAllocator alloc = default;
            byte* ptr = default, newPtr = default;

            Assert.That(() =>
            {
                alloc = new FixedAllocator(bufferPtr, 96);

                ptr = (byte*)alloc.Allocate(16);
                for (byte i = 0; i < 16; ++i)
                    ptr[i] = i;

                newPtr = (byte*)alloc.Reallocate(ptr, 32);

            }, Is.Not.AllocatingGCMemory());

            for (byte i = 0; i < 16; ++i)
                Assert.AreEqual(i, newPtr[i]);
            for (byte i = 0; i < 16; ++i)
                Assert.AreEqual(0, newPtr[i + 16]);
        }
    }
}
