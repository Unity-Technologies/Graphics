using NUnit.Framework;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class HDCameraTests
    {
        [SetUp]
        public void Setup()
        {
            if (GraphicsSettings.currentRenderPipeline is not HDRenderPipelineAsset)
                Assert.Ignore("This is an HDRP Tests, and the current pipeline is not HDRP.");
        }

        [Test]
        public void TestCameraRelativeRendering()
        {
            GameObject cameraGameObject = new GameObject("Camera");
            var camera = cameraGameObject.AddComponent<Camera>();
            var hdCamera = new HDCamera(camera);

            if (!VolumeManager.instance.isInitialized)
                VolumeManager.instance.Initialize();

            var positionSettings = new CameraPositionSettings();
            positionSettings.position = new Vector3(100.0f, 300.0f, 500.0f);
            positionSettings.rotation = Quaternion.Euler(62.34f, 185.53f, 323.563f);

            var resolution = new Vector4(1920.0f, 1080.0f, 1.0f / 1920.0f, 1.0f / 1080.0f);
            float aspect = resolution.x * resolution.w;

            camera.worldToCameraMatrix = positionSettings.ComputeWorldToCameraMatrix();
            camera.projectionMatrix = Matrix4x4.Perspective(75.0f, aspect, 0.1f, 1000.0f);

            var view = camera.worldToCameraMatrix;
            var proj = camera.projectionMatrix;

            // Minimal setup for ComputePixelCoordToWorldSpaceViewDirectionMatrix().
            var viewConstants = new HDCamera.ViewConstants();
            viewConstants.viewMatrix = view;
            viewConstants.projMatrix = proj;
            viewConstants.invViewProjMatrix = (proj * view).inverse;

            // hdCamera.xr must be initialized for ComputePixelCoordToWorldSpaceViewDirectionMatrix().
            var hdrp = HDRenderPipeline.currentPipeline;
            hdCamera.Update(hdCamera.frameSettings, hdrp, XRSystem.emptyPass, allocateHistoryBuffers: false);

            var matrix0 = hdCamera.ComputePixelCoordToWorldSpaceViewDirectionMatrix(viewConstants, resolution, aspect, 0);
            var matrix1 = hdCamera.ComputePixelCoordToWorldSpaceViewDirectionMatrix(viewConstants, resolution, aspect, 1);

            // These matrices convert a clip space position to a world space view direction,
            // therefore should be same regardless of Camera Relative Rendering.
            Assert.AreEqual(matrix0, matrix1, $"{matrix0} != {matrix1}");

            CoreUtils.Destroy(cameraGameObject);
        }
    }
}
