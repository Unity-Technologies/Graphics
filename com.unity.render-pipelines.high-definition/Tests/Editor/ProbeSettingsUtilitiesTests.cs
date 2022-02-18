using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class ProbeSettingsUtilitiesTests
    {
        [Test]
        public void ApplyObliqueNearClipPlane()
        {
            var probeSettings = ProbeSettings.NewDefault();
            var probePosition = ProbeCapturePositionSettings.NewDefault();
            var cameraSettings = CameraSettings.NewDefault();
            var cameraPosition = CameraPositionSettings.NewDefault();

            probeSettings.proxySettings.capturePositionProxySpace = new Vector3(0, 1, -1);

            cameraPosition.worldToCameraMatrix = Matrix4x4.TRS(
                probeSettings.proxySettings.capturePositionProxySpace,
                Quaternion.LookRotation(Vector3.forward),
                Vector3.one
                ).inverse;

            ProbeSettingsUtilities.ApplyObliqueNearClipPlane(
                ref probeSettings, ref probePosition,
                ref cameraSettings, ref cameraPosition
            );

            Assert.AreEqual(CameraSettings.Frustum.Mode.UseProjectionMatrixField, cameraSettings.frustum.mode);
        }

        [Test]
        public void ApplyMirroredReferenceTransform()
        {
            var probeSettings = ProbeSettings.NewDefault();
            var probePosition = ProbeCapturePositionSettings.NewDefault();
            var cameraSettings = CameraSettings.NewDefault();
            var cameraPosition = CameraPositionSettings.NewDefault();

            ProbeSettingsUtilities.ApplyMirroredReferenceTransform(
                ref probeSettings, ref probePosition,
                ref cameraSettings, ref cameraPosition
            );

            Assert.AreEqual(true, cameraSettings.invertFaceCulling);
        }
    }
}
