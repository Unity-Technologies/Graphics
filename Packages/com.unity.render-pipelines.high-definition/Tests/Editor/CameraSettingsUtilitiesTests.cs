using UnityEditor.Rendering.TestFramework;
using NUnit.Framework;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class CameraSettingsUtilitiesTests
    {
        Object m_ToClean;

        // deactivate this test for template package making issue
        //[Test]
        public void ApplySettings()
        {
            for (int i = 0; i < 10; ++i)
            {
                var perspectiveMatrix = Matrix4x4.Perspective(
                    RandomUtilities.RandomFloat(i, 2943.06587f) * 30.0f + 75.0f,
                    RandomUtilities.RandomFloat(i, 6402.79532f) * 0.5f + 1,
                    RandomUtilities.RandomFloat(i, 8328.97521f) * 10.0f + 10f,
                    RandomUtilities.RandomFloat(i, 6875.12374f) * 100.0f + 1000.0f
                );
                var worldToCameraMatrix = GeometryUtils.CalculateWorldToCameraMatrixRHS(
                    RandomUtilities.RandomVector3(i),
                    RandomUtilities.RandomQuaternion(i)
                );

                var settings = new CameraSettings
                {
                    bufferClearing = new CameraSettings.BufferClearing
                    {
                        backgroundColorHDR = RandomUtilities.RandomColor(i),
                        clearColorMode = RandomUtilities.RandomEnumIndex<HDAdditionalCameraData.ClearColorMode>(i),
                        clearDepth = RandomUtilities.RandomBool(i)
                    },
                    culling = new CameraSettings.Culling
                    {
                        cullingMask = RandomUtilities.RandomInt(i),
                        useOcclusionCulling = RandomUtilities.RandomBool(i + 0.5f),
                    },
                    renderingPathCustomFrameSettings = default,
                    renderingPathCustomFrameSettingsOverrideMask = default,
                    frustum = new CameraSettings.Frustum
                    {
                        aspect = RandomUtilities.RandomFloat(i, 6724.2745f) * 0.5f + 1,
                        nearClipPlaneRaw = RandomUtilities.RandomFloat(i, 7634.7235f) * 10.0f + 10f,
                        farClipPlaneRaw = RandomUtilities.RandomFloat(i, 1935.3234f) * 100.0f + 1000.0f,
                        fieldOfView = RandomUtilities.RandomFloat(i, 9364.2534f) * 30.0f + 75.0f,
                        mode = RandomUtilities.RandomEnumIndex<CameraSettings.Frustum.Mode>(i * 2.5f),
                        projectionMatrix = perspectiveMatrix
                    },
                    volumes = new CameraSettings.Volumes
                    {
                        anchorOverride = null,
                        layerMask = RandomUtilities.RandomInt(i * 3.5f)
                    },
                    customRenderingSettings = RandomUtilities.RandomBool(i * 4.5f)
                };
                FrameSettingsField field = RandomUtilities.RandomEnumIndex<FrameSettingsField>(i * 5.25f);
                settings.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)field] = true;
                settings.renderingPathCustomFrameSettings.SetEnabled(field, RandomUtilities.RandomBool(i));
                var position = new CameraPositionSettings
                {
                    mode = RandomUtilities.RandomEnumIndex<CameraPositionSettings.Mode>(i),
                    position = RandomUtilities.RandomVector3(i * 5.5f),
                    rotation = RandomUtilities.RandomQuaternion(i * 6.5f),
                    worldToCameraMatrix = worldToCameraMatrix
                };

                var go = new GameObject("TestObject");
                m_ToClean = go;
                var cam = go.AddComponent<Camera>();

                cam.ApplySettings(settings);
                cam.ApplySettings(position);

                var add = cam.GetComponent<HDAdditionalCameraData>();
                Assert.True(add != null && !add.Equals(null));

                // Position
                switch (position.mode)
                {
                    case CameraPositionSettings.Mode.UseWorldToCameraMatrixField:
                        AssertUtilities.AssertAreEqual(position.worldToCameraMatrix, cam.worldToCameraMatrix);
                        break;
                    case CameraPositionSettings.Mode.ComputeWorldToCameraMatrix:
                        AssertUtilities.AssertAreEqual(position.position, cam.transform.position);
                        AssertUtilities.AssertAreEqual(position.rotation, cam.transform.rotation);
                        AssertUtilities.AssertAreEqual(position.ComputeWorldToCameraMatrix(), cam.worldToCameraMatrix);
                        break;
                }
                // Frustum
                switch (settings.frustum.mode)
                {
                    case CameraSettings.Frustum.Mode.UseProjectionMatrixField:
                        AssertUtilities.AssertAreEqual(settings.frustum.projectionMatrix, cam.projectionMatrix);
                        break;
                    case CameraSettings.Frustum.Mode.ComputeProjectionMatrix:
                        Assert.AreEqual(settings.frustum.nearClipPlane, cam.nearClipPlane);
                        Assert.AreEqual(settings.frustum.farClipPlane, cam.farClipPlane);
                        Assert.AreEqual(settings.frustum.fieldOfView, cam.fieldOfView);
                        Assert.AreEqual(settings.frustum.aspect, cam.aspect);
                        AssertUtilities.AssertAreEqual(settings.frustum.ComputeProjectionMatrix(), cam.projectionMatrix);
                        break;
                }
                // Culling
                Assert.AreEqual(settings.culling.useOcclusionCulling, cam.useOcclusionCulling);
                Assert.AreEqual(settings.culling.cullingMask, (LayerMask)cam.cullingMask);
                // Buffer clearing
                Assert.AreEqual(settings.bufferClearing.clearColorMode, add.clearColorMode);
                Assert.AreEqual(settings.bufferClearing.backgroundColorHDR, add.backgroundColorHDR);
                Assert.AreEqual(settings.bufferClearing.clearDepth, add.clearDepth);
                // Volumes
                Assert.AreEqual(settings.volumes.layerMask, add.volumeLayerMask);
                Assert.AreEqual(settings.volumes.anchorOverride, add.volumeAnchorOverride);
                //FrameSettings
                Assert.AreEqual(settings.renderingPathCustomFrameSettings, add.renderingPathCustomFrameSettings);
                Assert.AreEqual(settings.renderingPathCustomFrameSettingsOverrideMask, add.renderingPathCustomFrameSettingsOverrideMask);
                // HD Specific
                Assert.AreEqual(settings.customRenderingSettings, add.customRenderingSettings);

                Object.DestroyImmediate(go);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (m_ToClean != null)
                CoreUtils.Destroy(m_ToClean);
        }
    }
}
