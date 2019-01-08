using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    public class ScriptableCulling
    {
        enum FrustumPlanes
        {
            Left = 0,
            Right,
            Bottom,
            Top,
            Near,
            Far,
            Num,
        };

        static Plane MakeFrustumPlane(float a, float b, float c, float d)
        {
            Plane result = new Plane();
            Vector3 normal = new Vector3(a, b, c);
            float l = normal.magnitude;
            result.normal = normal / l;
            result.distance = d / l;
            return result;
        }

        static unsafe void ExtractMatrixPlanes(in Matrix4x4 matrix, Plane *outPlanes)
        {
            Vector4 tmpVec;
            Vector4 otherVec;

            tmpVec.x = matrix.m30;
            tmpVec.y = matrix.m31;
            tmpVec.z = matrix.m32;
            tmpVec.w = matrix.m33;

            otherVec.x = matrix.m00;
            otherVec.y = matrix.m01;
            otherVec.z = matrix.m02;
            otherVec.w = matrix.m03;

            // left & right
            outPlanes[(int)FrustumPlanes.Left] = MakeFrustumPlane(otherVec[0] + tmpVec[0], otherVec[1] + tmpVec[1], otherVec[2] + tmpVec[2], otherVec[3] + tmpVec[3]);
            outPlanes[(int)FrustumPlanes.Right] = MakeFrustumPlane(-otherVec[0] + tmpVec[0], -otherVec[1] + tmpVec[1], -otherVec[2] + tmpVec[2], -otherVec[3] + tmpVec[3]);

            // bottom & top
            otherVec.x = matrix.m10;
            otherVec.y = matrix.m11;
            otherVec.z = matrix.m12;
            otherVec.w = matrix.m13;

            outPlanes[(int)FrustumPlanes.Bottom] = MakeFrustumPlane(otherVec[0] + tmpVec[0], otherVec[1] + tmpVec[1], otherVec[2] + tmpVec[2], otherVec[3] + tmpVec[3]);
            outPlanes[(int)FrustumPlanes.Top] = MakeFrustumPlane(-otherVec[0] + tmpVec[0], -otherVec[1] + tmpVec[1], -otherVec[2] + tmpVec[2], -otherVec[3] + tmpVec[3]);

            otherVec.x = matrix.m20;
            otherVec.y = matrix.m21;
            otherVec.z = matrix.m22;
            otherVec.w = matrix.m23;

            // near & far
            outPlanes[(int)FrustumPlanes.Near] = MakeFrustumPlane(otherVec[0] + tmpVec[0], otherVec[1] + tmpVec[1], otherVec[2] + tmpVec[2], otherVec[3] + tmpVec[3]);
            outPlanes[(int)FrustumPlanes.Far] = MakeFrustumPlane(-otherVec[0] + tmpVec[0], -otherVec[1] + tmpVec[1], -otherVec[2] + tmpVec[2], -otherVec[3] + tmpVec[3]);
        }

        public static void FillCullingParameters(Camera camera, ref CullingParameters parameters)
        {
            parameters.viewProperties.viewID = camera.GetInstanceID();

            parameters.viewProperties.nearPlane = camera.nearClipPlane;
            parameters.viewProperties.farPlane = camera.farClipPlane;

            parameters.viewProperties.worldToClip = camera.cullingMatrix; // This will use explicit projection/culling matrices set on the camera.
            parameters.viewProperties.stereoWorldToClipLeft = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            parameters.viewProperties.stereoWorldToClipRight = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right) * camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
            parameters.viewProperties.viewPosition = camera.transform.position;
            parameters.viewProperties.stereoSeparation = camera.stereoEnabled ? camera.stereoSeparation : 0.0f;

            parameters.viewProperties.viewToWorld = camera.cameraToWorldMatrix;
            parameters.viewProperties.viewEuler = Mathf.Deg2Rad * camera.transform.rotation.eulerAngles;
            parameters.viewProperties.viewDirection = camera.transform.forward;
            parameters.viewProperties.screenRect = camera.pixelRect;
            parameters.viewProperties.fieldOfView = camera.fieldOfView;

            parameters.viewProperties.lodParameters.cameraPosition = camera.transform.position;
            parameters.viewProperties.lodParameters.fieldOfView = camera.fieldOfView;
            parameters.viewProperties.lodParameters.isOrthographic = camera.orthographic;
            parameters.viewProperties.lodParameters.orthoSize = camera.orthographicSize;

            parameters.cullingTestParameters.cullingMask = (uint)camera.cullingMask;
            parameters.cullingTestParameters.sceneMask = 0xFFFFFFFFFFFFFFFF;
            parameters.cullingTestParameters.accurateOcclusionThreshold = -1.0f;
            parameters.cullingTestParameters.occlusionCullingJobCount = 6;
            for (int i = 0; i < CullingTestParameters.layerCount; ++i)
            {
                parameters.cullingTestParameters.SetLayerCullingDistance(i, camera.layerCullDistances[i]);
            }


            unsafe
            {
                Plane* planes = stackalloc Plane[6];
                ExtractMatrixPlanes(parameters.viewProperties.worldToClip, planes);
                parameters.cullingTestParameters.cullingPlaneCount = 6;
                for (int  i = 0; i < 6; ++i)
                {
                    parameters.cullingTestParameters.SetCullingPlane(i, planes[i]);
                }
            }

        }
    }
}
