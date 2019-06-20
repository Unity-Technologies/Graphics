using NUnit.Framework;

namespace UnityEngine.TestTools.Graphics.Tests
{
    public class FailedImageMessageTests
    {
        [Test]
        public void SerializationRoundtrip_DefaultInstance()
        {
            var message = new FailedImageMessage();

            var data = message.Serialize();
            var deserialized = FailedImageMessage.Deserialize(data);

            AssertAreEqual(deserialized, message);
        }

        [TestCase(null, null, null, null, null)]
        [TestCase("", "", new byte[0], new byte[0], new byte[0])]
        [TestCase("Foo", "Bar", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, null, null)]
        [TestCase("Foo", "Bar", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, new byte[] { 42, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, null)]
        [TestCase("Foo", "Bar", new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, new byte[] { 42, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, new byte[] { 42, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })]
        public void SerializationRoundtrip_AllFieldsAreSerializedAndDeserialized(string pathName, string imageName, byte[] expectedImage,  byte[] actualImage, byte[] diffImage)
        {
            var message = new FailedImageMessage
            {
                PathName = pathName,
                ImageName = imageName,
                ExpectedImage = expectedImage,
                ActualImage = actualImage,
                DiffImage = diffImage,
            };

            var data = message.Serialize();
            var deserialized = FailedImageMessage.Deserialize(data);

            AssertAreEqual(deserialized, message);
        }

        private static void AssertAreEqual(FailedImageMessage deserialized, FailedImageMessage message)
        {
            Assert.That(deserialized.ImageName, Is.EqualTo(message.ImageName));
            Assert.That(deserialized.PathName, Is.EqualTo(message.PathName));
            Assert.That(deserialized.ExpectedImage, Is.EqualTo(message.ExpectedImage));
            Assert.That(deserialized.ActualImage, Is.EqualTo(message.ActualImage));
            Assert.That(deserialized.DiffImage, Is.EqualTo(message.DiffImage));
        }
    }
}
