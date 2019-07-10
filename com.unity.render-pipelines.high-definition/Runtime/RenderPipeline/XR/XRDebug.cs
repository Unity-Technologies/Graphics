using System;

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class XRSystem
    {
        bool ProcessDebugMode(bool xrEnabled, Camera camera)
        {
            if (layoutOverride == XRLayoutOverride.None || camera.cameraType != CameraType.Game || xrEnabled)
                return false;

            if (layoutOverride == XRLayoutOverride.TestSinglePassOneEye)
            {
                var xrPass = XRPass.Create(framePasses.Count, camera.targetTexture);

                // 2x single-pass instancing
                for (int i = 0; i < 2; ++i)
                    xrPass.AddView(camera.projectionMatrix, camera.worldToCameraMatrix, camera.pixelRect);

                AddPassToFrame(camera, xrPass);
            }
            else if (layoutOverride == XRLayoutOverride.TestComposite)
            {
                Rect fullViewport = camera.pixelRect;

                // Split into 4 tiles covering the original viewport
                int tileCountX = 2;
                int tileCountY = 2;
                float splitRatio = 2.0f;

                // Use frustum planes to split the projection into 4 parts
                var frustumPlanes = camera.projectionMatrix.decomposeProjection;

                for (int tileY = 0; tileY < tileCountY; ++tileY)
                {
                    for (int tileX = 0; tileX < tileCountX; ++tileX)
                    {
                        var xrPass = XRPass.Create(framePasses.Count, camera.targetTexture);

                        float spliRatioX1 = Mathf.Pow((tileX + 0.0f) / tileCountX, splitRatio);
                        float spliRatioX2 = Mathf.Pow((tileX + 1.0f) / tileCountX, splitRatio);
                        float spliRatioY1 = Mathf.Pow((tileY + 0.0f) / tileCountY, splitRatio);
                        float spliRatioY2 = Mathf.Pow((tileY + 1.0f) / tileCountY, splitRatio);

                        var planes = frustumPlanes;
                        planes.left = Mathf.Lerp(frustumPlanes.left, frustumPlanes.right, spliRatioX1);
                        planes.right = Mathf.Lerp(frustumPlanes.left, frustumPlanes.right, spliRatioX2);
                        planes.bottom = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top, spliRatioY1);
                        planes.top = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top, spliRatioY2);

                        float tileOffsetX = spliRatioX1 * fullViewport.width;
                        float tileOffsetY = spliRatioY1 * fullViewport.height;
                        float tileSizeX = spliRatioX2 * fullViewport.width - tileOffsetX;
                        float tileSizeY = spliRatioY2 * fullViewport.height - tileOffsetY;

                        Rect viewport = new Rect(fullViewport.x + tileOffsetX, fullViewport.y + tileOffsetY, tileSizeX, tileSizeY);
                        Matrix4x4 proj = camera.orthographic ? Matrix4x4.Ortho(planes.left, planes.right, planes.bottom, planes.top, planes.zNear, planes.zFar) : Matrix4x4.Frustum(planes);

                        xrPass.AddView(proj, camera.worldToCameraMatrix, viewport);
                        AddPassToFrame(camera, xrPass);
                    }
                }
            }

            return true;
        }
    }
}
