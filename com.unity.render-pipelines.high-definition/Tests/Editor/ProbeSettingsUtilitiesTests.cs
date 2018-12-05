using NUnit.Framework;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public class ProbeSettingsUtilitiesTests
    {
        [Test]
        public void ApplyObliqueNearClipPlane()
        {
            var probeSettings = ProbeSettings.@default;
            var probePosition = ProbeCapturePositionSettings.@default;
            var cameraSettings = CameraSettings.@default;
            var cameraPosition = CameraPositionSettings.@default;

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
            var probeSettings = ProbeSettings.@default;
            var probePosition = ProbeCapturePositionSettings.@default;
            var cameraSettings = CameraSettings.@default;
            var cameraPosition = CameraPositionSettings.@default;

            ProbeSettingsUtilities.ApplyMirroredReferenceTransform(
                ref probeSettings, ref probePosition,
                ref cameraSettings, ref cameraPosition
            );

            Assert.AreEqual(true, cameraSettings.invertFaceCulling);
        }
    }
}
