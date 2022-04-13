using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    public class CameraCacheTest
    {
        [Test]
        public void GetOrCreate_And_Dispose_Works()
        {
            var cache = new CameraCache<int>();

            // Create the camera
            var camera = cache.GetOrCreate(0, 0);
            Assert.IsNotNull(camera);

            // Get the same camera
            var sameCamera = cache.GetOrCreate(0, 0);
            Assert.IsNotNull(sameCamera);
            Assert.True(ReferenceEquals(camera, sameCamera));
            Assert.AreEqual(camera.GetInstanceID(), sameCamera.GetInstanceID());

            // Get another camera
            var otherCamera = cache.GetOrCreate(1, 0);
            Assert.IsNotNull(otherCamera);
            Assert.False(ReferenceEquals(camera, otherCamera));
            Assert.AreNotEqual(camera.GetInstanceID(), otherCamera.GetInstanceID());

            // Clear the cameras
            cache.Dispose();

            // Assert the cameras are destroyed on the C++ side
            Assert.True(camera.Equals(null));
            Assert.True(sameCamera.Equals(null));
            Assert.True(otherCamera.Equals(null));
        }

        [Test]
        public void GetOrCreate_And_Clear_Works()
        {
            using (var cache = new CameraCache<int>())
            {
                var cameras = new Camera[5];
                for (var i = 0; i < cameras.Length; ++i)
                {
                    cameras[i] = cache.GetOrCreate(i, 0);
                    Assert.IsNotNull(cameras[i]);
                }

                // Clear the cameras
                cache.Clear();

                // Assert the cameras are destroyed on the C++ side
                for (var i = 0; i < cameras.Length; ++i)
                    Assert.True(cameras[i].Equals(null));
            }
        }

        [Test]
        public void GetOrCreate_And_ClearCamerasUnusedFor_Works()
        {
            using (var cache = new CameraCache<int>())
            {
                // Create cameras
                var cameras = new Camera[5];
                for (var i = 0; i < cameras.Length; ++i)
                {
                    cameras[i] = cache.GetOrCreate(i, i);
                    Assert.IsNotNull(cameras[i]);
                }

                var frameCount = cameras.Length - 1;
                for (var frameWindow = frameCount; frameWindow >= 0; --frameWindow)
                {
                    // Clear the cameras older than i frames
                    cache.ClearCamerasUnusedFor(frameWindow, frameCount);
                    // Assert the cameras are destroyed on the C++ side if they are unused
                    // since i frames or more
                    var cameraAreDestroyedBeforeFrameCount = frameCount - frameWindow;
                    for (var j = 0; j < cameraAreDestroyedBeforeFrameCount; ++j)
                        Assert.True(cameras[j].Equals(null), $"Camera {j} is unused since {frameWindow} frames and must be destroyed.");
                    for (var j = cameraAreDestroyedBeforeFrameCount; j < cameras.Length; ++j)
                        Assert.False(cameras[j].Equals(null), $"Camera {j} is used since {frameWindow} frames and must be alive.");
                }
            }
        }

        [Test]
        public void UsingDisposedObject_Throws()
        {
            var cache = new CameraCache<int>();
            cache.Dispose();

            Assert.Throws<System.ObjectDisposedException>(() => cache.GetOrCreate(0, 0));
            Assert.Throws<System.ObjectDisposedException>(() => cache.ClearCamerasUnusedFor(0, 0));
            Assert.Throws<System.ObjectDisposedException>(() => cache.Clear());
            Assert.Throws<System.ObjectDisposedException>(() => cache.Dispose());
        }
    }
}
