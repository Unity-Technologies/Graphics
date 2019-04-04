using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Is = UnityEngine.TestTools.Constraints.Is;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools.Constraints;

namespace Tests
{
    public class MemoryTests
    {
        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator MemoryTests_RenderMotionBlurSceneToCubeMap_CheckThatNoGCMemoryWasAllocated()
        {
            SceneManager.LoadScene("Assets/Scenes/MotionBlur.unity");

            // Arbitrary wait for 5 frames for the scene to load, and other stuff to happen (like Realtime GI to appear ...)
            for (int i = 0; i < 5; ++i)
                yield return null;

            Camera cam = GameObject.FindObjectOfType<Camera>();
            Cubemap cubemap =  new Cubemap(128, TextureFormat.RGBA32, false);

            // render into cubemap while checking for memory allocation
            Assert.That(() => { bool b = cam.RenderToCubemap(cubemap); Assert.IsTrue(b); }, Is.Not.AllocatingGCMemory(), "The method tested allocated some memory");

            yield return null;
        }
    }
}
