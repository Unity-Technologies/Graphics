using NUnit.Framework;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Tests
{
    class CameraTargetUpdateCountTests
    {
        [Test]
        public void RepeatedCameraRenderKeepsBumpingUpdateCount()
        {
            // Regression test for IN-138734 / UUM-139389: cookie frozen because updateCount
            // was bumped only when the RT was first created, then never again. A camera
            // rendering into the same RT every frame must produce a fresh bump each time.
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                return;

            var cameraGO = new GameObject("UpdateCountTestCamera") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(64, 64, 24) { hideFlags = HideFlags.HideAndDontSave };
            rt.Create();

            var camera = cameraGO.AddComponent<Camera>();
            camera.targetTexture = rt;

            uint before = rt.updateCount;
            camera.Render();
            uint afterFirst = rt.updateCount;
            camera.Render();
            uint afterSecond = rt.updateCount;

            camera.targetTexture = null; // unbind before Release: releasing an RT still set as targetTexture logs an error
            rt.Release();
            Object.DestroyImmediate(rt);
            GameObject.DestroyImmediate(cameraGO);

            Assert.Greater(afterFirst, before, "First render should bump updateCount.");
            Assert.Greater(afterSecond, afterFirst, "Second render should bump updateCount again.");
        }

        [Test]
        public void RepeatedSingleCameraRequestKeepsBumpingUpdateCount()
        {
            // Regression test for UUM-139389: ProcessRenderRequests()'s SingleCameraRequest branch renders
            // via RenderSingleCameraInternal() directly instead of going through RenderCameras(), so it needs
            // its own updateCount bump. Mirrors RepeatedCameraRenderKeepsBumpingUpdateCount but for
            // RenderPipeline.SubmitRenderRequest(camera, SingleCameraRequest) instead of camera.Render().
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                return;

            var cameraGO = new GameObject("UpdateCountTestCamera") { hideFlags = HideFlags.HideAndDontSave };
            var rt = new RenderTexture(64, 64, 24) { hideFlags = HideFlags.HideAndDontSave };
            rt.Create();

            var camera = cameraGO.AddComponent<Camera>();
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };

            uint before = rt.updateCount;
            RenderPipeline.SubmitRenderRequest(camera, request);
            uint afterFirst = rt.updateCount;
            RenderPipeline.SubmitRenderRequest(camera, request);
            uint afterSecond = rt.updateCount;

            rt.Release();
            Object.DestroyImmediate(rt);
            GameObject.DestroyImmediate(cameraGO);

            Assert.Greater(afterFirst, before, "First render should bump updateCount.");
            Assert.Greater(afterSecond, afterFirst, "Second render should bump updateCount again.");
        }
    }
}
