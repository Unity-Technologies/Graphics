using NUnit.Framework;
using System;

namespace UnityEngine.Rendering.Tests
{
    unsafe class FixedBufferStringQueueTests
    {
        [Test]
        public void PushAndPopInBufferRange()
        {
            const int size = 512;
            var bufferStart = stackalloc byte[size];
            var buffer = new CoreUnsafeUtils.FixedBufferStringQueue(bufferStart, size);

            Assert.True(buffer.TryPush("Lorem ipsum dolor sit"));
            Assert.True(buffer.TryPush("amet, consectetur adipiscing"));
            Assert.True(buffer.TryPush("elit, sed do eiusmod"));
            Assert.True(buffer.TryPush("tempor incididunt ut labore"));

            Assert.AreEqual(4, buffer.Count);

            Assert.True(buffer.TryPop(out string v) && v == "Lorem ipsum dolor sit");
            Assert.True(buffer.TryPop(out v) && v == "amet, consectetur adipiscing");
            Assert.True(buffer.TryPop(out v) && v == "elit, sed do eiusmod");
            Assert.True(buffer.TryPop(out v) && v == "tempor incididunt ut labore");
        }

        [Test]
        public void PushAndPopOutOfBufferRange()
        {
            const int size = 64;
            var bufferStart = stackalloc byte[size];
            var buffer = new CoreUnsafeUtils.FixedBufferStringQueue(bufferStart, size);

            Assert.True(buffer.TryPush("Lorem ipsum dolor sit"));
            Assert.False(buffer.TryPush("amet, consectetur adipiscing"));

            Assert.AreEqual(1, buffer.Count);

            Assert.True(buffer.TryPop(out string v) && v == "Lorem ipsum dolor sit");
            Assert.False(buffer.TryPop(out v) && v == null);
        }

        [Test]
        public void PushAndPopOutOfBufferRange_StringSizeNotDivisibleBy4()
        {
            // UUM-104687: Buffer is created with size of 32 bytes. The test fills the first 30 bytes with a string,
            // so 2 bytes are left over in the buffer. After we pop the string out, we check that the next TryPop
            // doesn't try to read out of bounds when trying to read the string length.

            const int bufferLength = 32;
            const int bytesToFill = bufferLength - 2;
            const int bytesForString = bytesToFill - sizeof(int);
            const int numCharacters = bytesForString / sizeof(char);

            string testValue = new string('a', numCharacters);

            byte* bufferStart = stackalloc byte[bufferLength];
            CoreUnsafeUtils.FixedBufferStringQueue buffer = new CoreUnsafeUtils.FixedBufferStringQueue(bufferStart, bufferLength);

            Assume.That(buffer.TryPush(testValue));
            Assume.That(buffer.TryPop(out string v));
            Assert.False(buffer.TryPop(out v));
        }

        [Test]
        public void PushAndPopAndClear()
        {
            const int size = 128;
            var bufferStart = stackalloc byte[size];
            var buffer = new CoreUnsafeUtils.FixedBufferStringQueue(bufferStart, size);

            Assert.True(buffer.TryPush("Lorem ipsum dolor sit"));
            Assert.True(buffer.TryPush("amet, consectetur adipiscing"));
            Assert.False(buffer.TryPush("elit, sed do eiusmod"));

            Assert.AreEqual(2, buffer.Count);
            buffer.Clear();
            Assert.AreEqual(0, buffer.Count);

            Assert.True(buffer.TryPush("elit, sed do eiusmod"));
            Assert.True(buffer.TryPush("tempor incididunt ut labore"));
            Assert.True(buffer.TryPop(out string v) && v == "elit, sed do eiusmod");
            Assert.True(buffer.TryPop(out v) && v == "tempor incididunt ut labore");
        }

    }
}
