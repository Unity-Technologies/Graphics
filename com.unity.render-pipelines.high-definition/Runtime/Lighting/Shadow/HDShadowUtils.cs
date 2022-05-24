using UnityEngine.Rendering;
using Unity.Collections;

namespace UnityEngine.Rendering.HighDefinition
{
    // TODO remove every occurrence of ShadowSplitData in function parameters when we'll have scriptable culling
    static class HDShadowUtils
    {
        public const float k_MinShadowNearPlane = 0.0001f;
        public const float k_MaxShadowNearPlane = 10.0f;

        public static float Asfloat(uint val) { unsafe { return *((float*)&val); } }
        public static float Asfloat(int val)  { unsafe { return *((float*)&val); } }
        public static int Asint(float val)    { unsafe { return *((int*)&val); } }
        public static uint Asuint(float val)  { unsafe { return *((uint*)&val); } }

        static Plane[] s_CachedPlanes = new Plane[6];

        // Keep in sync with both HDShadowSampling.hlsl
        static float GetPunctualFilterWidthInTexels(HDShadowFilteringQuality quality)
        {
            switch (quality)
            {
                // Warning: these values have to match the algorithms used for shadow filtering (in HDShadowAlgorithm.hlsl)
                case HDShadowFilteringQuality.Low:
                    return 3; // PCF 3x3
                case HDShadowFilteringQuality.Medium:
                    return 5; // PCF 5x5
                default:
                    return 1; // Any non PCF algorithms
            }
        }

        public static void ExtractPointLightData(NativeArray<Matrix4x4> cubeMapFaces, VisibleLight visibleLight, Vector2 viewportSize, float nearPlane, float normalBiasMax, uint faceIndex, HDShadowFilteringQuality filteringQuality, bool usesReversedZInfo,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            float guardAngle = CalcGuardAnglePerspective(90.0f, viewportSize.x, GetPunctualFilterWidthInTexels(filteringQuality), normalBiasMax, 79.0f);
            ExtractPointLightMatrix(cubeMapFaces, visibleLight, faceIndex, nearPlane, guardAngle, usesReversedZInfo, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);
        }

        // TODO: box spot and pyramid spots with non 1 aspect ratios shadow are incorrectly culled, see when scriptable culling will be here
        public static void ExtractSpotLightData(SpotLightShape shape, float spotAngle, float nearPlane, float aspectRatio, float shapeWidth, float shapeHeight, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, HDShadowFilteringQuality filteringQuality, bool reverseZ,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            // There is no aspect ratio for non pyramid spot lights
            if (shape != SpotLightShape.Pyramid)
                aspectRatio = 1.0f;

            float guardAngle = CalcGuardAnglePerspective(spotAngle, viewportSize.x, GetPunctualFilterWidthInTexels(filteringQuality), normalBiasMax, 180.0f - spotAngle);
            ExtractSpotLightMatrix(visibleLight, spotAngle, nearPlane, guardAngle, aspectRatio, reverseZ, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);

            if (shape == SpotLightShape.Box)
            {
                projection = ExtractBoxLightProjectionMatrix(visibleLight.range, shapeWidth, shapeHeight, nearPlane);
                deviceProjection = GetGPUProjectionMatrix(projection, false, reverseZ);
                deviceProjectionYFlip = GetGPUProjectionMatrix(projection, true, reverseZ);
                InvertOrthographic(ref deviceProjectionYFlip, ref view, out invViewProjection);
            }
        }

        public static Matrix4x4 GetGPUProjectionMatrix(Matrix4x4 projectionMatrix, bool invertY, bool reverseZ)
        {
            Matrix4x4 gpuProjectionMatrix = projectionMatrix;
            if (invertY)
            {
                gpuProjectionMatrix.m10 = -gpuProjectionMatrix.m10;
                gpuProjectionMatrix.m11 = -gpuProjectionMatrix.m11;
                gpuProjectionMatrix.m12 = -gpuProjectionMatrix.m12;
                gpuProjectionMatrix.m13 = -gpuProjectionMatrix.m13;
            }

            // Now scale&bias to get Z range from -1..1 to 0..1 or 1..0
            // matrix = scaleBias * matrix
            //  1   0   0   0
            //  0   1   0   0
            //  0   0 0.5 0.5
            //  0   0   0   1
            gpuProjectionMatrix.m20 = gpuProjectionMatrix.m20 * (reverseZ ? -0.5f : 0.5f) + gpuProjectionMatrix.m30 * 0.5f;
            gpuProjectionMatrix.m21 = gpuProjectionMatrix.m21 * (reverseZ ? -0.5f : 0.5f) + gpuProjectionMatrix.m31 * 0.5f;
            gpuProjectionMatrix.m22 = gpuProjectionMatrix.m22 * (reverseZ ? -0.5f : 0.5f) + gpuProjectionMatrix.m32 * 0.5f;
            gpuProjectionMatrix.m23 = gpuProjectionMatrix.m23 * (reverseZ ? -0.5f : 0.5f) + gpuProjectionMatrix.m33 * 0.5f;

            return gpuProjectionMatrix;
        }

        public static void ExtractDirectionalLightData(VisibleLight visibleLight, Vector2 viewportSize, uint cascadeIndex, int cascadeCount, float[] cascadeRatios, float nearPlaneOffset, CullingResults cullResults, int lightIndex,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4     lightDir;

            Debug.Assert((uint)viewportSize.x == (uint)viewportSize.y, "Currently the cascaded shadow mapping code requires square cascades.");
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;

            // This used to be fixed to .6f, but is now configureable.
            splitData.shadowCascadeBlendCullingFactor = .6f;

            // get lightDir
            lightDir = visibleLight.GetForward();
            // TODO: At some point this logic should be moved to C#, then the parameters cullResults and lightIndex can be removed as well
            //       For directional lights shadow data is extracted from the cullResults, so that needs to be somehow provided here.
            //       Check ScriptableShadowsUtility.cpp ComputeDirectionalShadowMatricesAndCullingPrimitives(...) for details.
            Vector3 ratios = new Vector3();
            for (int i = 0, cnt = cascadeRatios.Length < 3 ? cascadeRatios.Length : 3; i < cnt; i++)
                ratios[i] = cascadeRatios[i];
            cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, (int)cascadeIndex, cascadeCount, ratios, (int)viewportSize.x, nearPlaneOffset, out view, out projection, out splitData);
            // and the compound (deviceProjection will potentially inverse-Z)
            deviceProjection = GL.GetGPUProjectionMatrix(projection, false);
            deviceProjectionYFlip = GL.GetGPUProjectionMatrix(projection, true);
            InvertOrthographic(ref deviceProjection, ref view, out invViewProjection);
        }

        // Currently area light shadows are not supported
        public static void ExtractRectangleAreaLightData(VisibleLight visibleLight, Vector3 shadowPosition, float areaLightShadowCone, float shadowNearPlane, Vector2 shapeSize, Vector2 viewportSize, float normalBiasMax, HDShadowFilteringQuality filteringQuality, bool reverseZ,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;
            float aspectRatio = shapeSize.x / shapeSize.y;
            float spotAngle = areaLightShadowCone;
            visibleLight.spotAngle = spotAngle;
            float guardAngle = CalcGuardAnglePerspective(visibleLight.spotAngle, viewportSize.x, GetPunctualFilterWidthInTexels(filteringQuality), normalBiasMax, 180.0f - visibleLight.spotAngle);

            ExtractSpotLightMatrix(visibleLight, visibleLight.spotAngle, shadowNearPlane, guardAngle, aspectRatio, reverseZ, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);
        }

        // Cubemap faces with flipped z coordinate.
        // These matrices do NOT match what we have in Skybox.cpp.
        // The C++ runtime flips y as well and requires patching up
        // the culling state. Using these matrices keeps the winding
        // order, but may need some special treatment if rendering
        // into an actual cubemap.
        public static readonly Matrix4x4[] kCubemapFaces = new Matrix4x4[]
        {
            new Matrix4x4( // pos X
                new Vector4(0.0f,  0.0f, -1.0f,  0.0f),
                new Vector4(0.0f,  1.0f,  0.0f,  0.0f),
                new Vector4(-1.0f,  0.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  0.0f,  1.0f)),
            new Matrix4x4( // neg x
                new Vector4(0.0f,  0.0f,  1.0f,  0.0f),
                new Vector4(0.0f,  1.0f,  0.0f,  0.0f),
                new Vector4(1.0f,  0.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  0.0f,  1.0f)),
            new Matrix4x4( // pos y
                new Vector4(1.0f,  0.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f, -1.0f,  0.0f),
                new Vector4(0.0f, -1.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  0.0f,  1.0f)),
            new Matrix4x4( // neg y
                new Vector4(1.0f,  0.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  1.0f,  0.0f),
                new Vector4(0.0f,  1.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  0.0f,  1.0f)),
            new Matrix4x4( // pos z
                new Vector4(1.0f,  0.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  1.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f, -1.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  0.0f,  1.0f)),
            new Matrix4x4( // neg z
                new Vector4(-1.0f,  0.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  1.0f,  0.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  1.0f,  0.0f),
                new Vector4(0.0f,  0.0f,  0.0f,  1.0f))
        };

        static void InvertView(ref Matrix4x4 view, out Matrix4x4 invview)
        {
            invview = Matrix4x4.zero;
            invview.m00 = view.m00; invview.m01 = view.m10; invview.m02 = view.m20;
            invview.m10 = view.m01; invview.m11 = view.m11; invview.m12 = view.m21;
            invview.m20 = view.m02; invview.m21 = view.m12; invview.m22 = view.m22;
            invview.m33 = 1.0f;
            invview.m03 = -(invview.m00 * view.m03 + invview.m01 * view.m13 + invview.m02 * view.m23);
            invview.m13 = -(invview.m10 * view.m03 + invview.m11 * view.m13 + invview.m12 * view.m23);
            invview.m23 = -(invview.m20 * view.m03 + invview.m21 * view.m13 + invview.m22 * view.m23);
        }

        static void InvertOrthographic(ref Matrix4x4 proj, ref Matrix4x4 view, out Matrix4x4 vpinv)
        {
            Matrix4x4 invview;
            InvertView(ref view, out invview);

            Matrix4x4 invproj = Matrix4x4.zero;
            invproj.m00 = 1.0f / proj.m00;
            invproj.m11 = 1.0f / proj.m11;
            invproj.m22 = 1.0f / proj.m22;
            invproj.m33 = 1.0f;
            invproj.m03 =   proj.m03 * invproj.m00;
            invproj.m13 =   proj.m13 * invproj.m11;
            invproj.m23 = -proj.m23 * invproj.m22;

            vpinv = invview * invproj;
        }

        static void InvertPerspective(ref Matrix4x4 proj, ref Matrix4x4 view, out Matrix4x4 vpinv)
        {
            Matrix4x4 invview;
            InvertView(ref view, out invview);

            Matrix4x4 invproj = Matrix4x4.zero;
            invproj.m00 = 1.0f / proj.m00;
            invproj.m03 = proj.m02 * invproj.m00;
            invproj.m11 = 1.0f / proj.m11;
            invproj.m13 = proj.m12 * invproj.m11;
            invproj.m22 = 0.0f;
            invproj.m23 = -1.0f;
            invproj.m33 = proj.m22 / proj.m23;
            invproj.m32 = invproj.m33 / proj.m22;

            // We explicitly perform the invview * invproj multiplication given that there are lots of 0s involved, so it will be much faster.
            vpinv.m00 = invview.m00 * invproj.m00;
            vpinv.m01 = invview.m01 * invproj.m11;
            vpinv.m02 = invview.m03 * invproj.m32;
            vpinv.m03 = invview.m00 * invproj.m03 + invview.m01 * invproj.m13 - invview.m02 + invview.m03 * invproj.m33;

            vpinv.m10 = invview.m10 * invproj.m00;
            vpinv.m11 = invview.m11 * invproj.m11;
            vpinv.m12 = invview.m13 * invproj.m32;
            vpinv.m13 = invview.m10 * invproj.m03 + invview.m11 * invproj.m13 - invview.m12 + invview.m13 * invproj.m33;

            vpinv.m20 = invview.m20 * invproj.m00;
            vpinv.m21 = invview.m21 * invproj.m11;
            vpinv.m22 = invview.m23 * invproj.m32;
            vpinv.m23 = invview.m20 * invproj.m03 + invview.m21 * invproj.m13 - invview.m22 + invview.m23 * invproj.m33;

            vpinv.m30 = 0.0f;
            vpinv.m31 = 0.0f;
            vpinv.m32 = invproj.m32;
            vpinv.m33 = invproj.m33;
        }

        public static Matrix4x4 ExtractSpotLightProjectionMatrix(float range, float spotAngle, float nearPlane, float aspectRatio, float guardAngle)
        {
            float fov = spotAngle + guardAngle;
            float nearZ = Mathf.Max(nearPlane, k_MinShadowNearPlane);

            float e = 1.0f / Mathf.Tan(fov / 180.0f * Mathf.PI / 2.0f);
            float a = aspectRatio;
            float n = nearZ;
            float f = n + range;

            // Unity does something messed up if the aspect ratio is less than 1. I assume it happens on the C++ side.
            // A workaround is to avoid using Matrix4x4.Perspective and build the matrix manually...
            Matrix4x4 mat = new Matrix4x4();

            if (a < 1)
            {
                mat.m00 = e;
                mat.m11 = e * a;
            }
            else
            {
                mat.m00 = e / a;
                mat.m11 = e;
            }

            mat.m22 = -(f + n)/(f - n);
            mat.m23 = -2 * f * n / (f - n);
            mat.m32 = -1;

            return mat;
        }

        public static Matrix4x4 ExtractBoxLightProjectionMatrix(float range, float width, float height, float nearPlane)
        {
            float nearZ = Mathf.Max(nearPlane, k_MinShadowNearPlane);
            return Matrix4x4.Ortho(-width / 2, width / 2, -height / 2, height / 2, nearZ, range);
        }

        static Matrix4x4 ExtractSpotLightMatrix(VisibleLight vl, float spotAngle, float nearPlane, float guardAngle, float aspectRatio, bool reverseZ, out Matrix4x4 view, out Matrix4x4 proj, out Matrix4x4 deviceProj, out Matrix4x4 deviceProjYFlip, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
        {
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            // get lightDir
            lightDir = vl.GetForward();
            // calculate view
            view = vl.localToWorldMatrix.inverse;
            view.m20 *= -1;
            view.m21 *= -1;
            view.m22 *= -1;
            view.m23 *= -1;

            // calculate projection
            proj = ExtractSpotLightProjectionMatrix(vl.range, spotAngle, nearPlane, aspectRatio, guardAngle);
            // and the compound (deviceProj will potentially inverse-Z)
            deviceProj = GetGPUProjectionMatrix(proj, false, reverseZ);
            deviceProjYFlip = GetGPUProjectionMatrix(proj, true, reverseZ);
            InvertPerspective(ref deviceProj, ref view, out vpinverse);
            return  CoreMatrixUtils.MultiplyPerspectiveMatrix(deviceProj, view);
        }

        static unsafe Matrix4x4 ExtractPointLightMatrix(NativeArray<Matrix4x4> cubemapFaces, VisibleLight vl, uint faceIdx, float nearPlane, float guardAngle, bool usesReversedZInfo, out Matrix4x4 view, out Matrix4x4 proj, out Matrix4x4 deviceProj, out Matrix4x4 deviceProjYFlip, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
        {
            if (faceIdx > (uint)CubemapFace.NegativeZ)
                Debug.LogError($"Tried to extract cubemap face {faceIdx}.");

            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);

            // get lightDir
            lightDir = vl.GetForward();
            // calculate the view matrices
            Vector3 lpos = vl.GetPosition();
            view = cubemapFaces[(int)faceIdx];
            Vector3 inverted_viewpos = cubemapFaces[(int)faceIdx].MultiplyPoint(-lpos);
            view.SetColumn(3, new Vector4(inverted_viewpos.x, inverted_viewpos.y, inverted_viewpos.z, 1.0f));

            float nearZ = Mathf.Max(nearPlane, k_MinShadowNearPlane);
            proj = Matrix4x4.Perspective(90.0f + guardAngle, 1.0f, nearZ, vl.range);
            // and the compound (deviceProj will potentially inverse-Z)
            deviceProj = GetGPUProjectionMatrix(proj, false, usesReversedZInfo);
            deviceProjYFlip = GetGPUProjectionMatrix(proj, true, usesReversedZInfo);
            InvertPerspective(ref deviceProj, ref view, out vpinverse);

            Matrix4x4 devProjView = CoreMatrixUtils.MultiplyPerspectiveMatrix(deviceProj, view);

            Plane* planes = stackalloc Plane[6];
            // We can avoid computing proj * view for frustum planes, if device has reversed Z we flip the culling planes as we should have computed them with proj
            CalculateFrustumPlanes(ref devProjView, planes);
            if (usesReversedZInfo)
            {
                var tmpPlane = planes[2];
                planes[2] = planes[3];
                planes[3] = tmpPlane;
            }
            splitData.cullingPlaneCount = 6;
            for (int i = 0; i < 6; i++)
                splitData.SetCullingPlane(i, planes[i]);

            return devProjView;
        }

         public static unsafe void CalculateFrustumPlanes(ref Matrix4x4 finalMatrix, Plane* outPlanes)
         {
            const int kPlaneFrustumLeft = 0;
            const int kPlaneFrustumRight = 1;
            const int kPlaneFrustumBottom = 2;
            const int kPlaneFrustumTop = 3;
            const int kPlaneFrustumNear = 4;
            const int kPlaneFrustumFar = 5;

            Vector4 tmpVec = default;
            Vector4 otherVec = default;

            tmpVec[0] = finalMatrix.m30;
            tmpVec[1] = finalMatrix.m31;
            tmpVec[2] = finalMatrix.m32;
            tmpVec[3] = finalMatrix.m33;

            otherVec[0] = finalMatrix.m00;
            otherVec[1] = finalMatrix.m01;
            otherVec[2] = finalMatrix.m02;
            otherVec[3] = finalMatrix.m03;

            // left & right
            float leftNormalX = otherVec[0] + tmpVec[0];
            float leftNormalY = otherVec[1] + tmpVec[1];
            float leftNormalZ = otherVec[2] + tmpVec[2];
            float leftDistance = otherVec[3] + tmpVec[3];
            float leftDot = leftNormalX * leftNormalX + leftNormalY * leftNormalY + leftNormalZ * leftNormalZ;
            float leftMagnitude = Mathf.Sqrt(leftDot);
            float leftInvMagnitude = 1.0f / leftMagnitude;
            leftNormalX *= leftInvMagnitude;
            leftNormalY *= leftInvMagnitude;
            leftNormalZ *= leftInvMagnitude;
            leftDistance *= leftInvMagnitude;
            outPlanes[kPlaneFrustumLeft].normal = new Vector3(leftNormalX, leftNormalY, leftNormalZ);
            outPlanes[kPlaneFrustumLeft].distance = leftDistance;

            float rightNormalX = -otherVec[0] + tmpVec[0];
            float rightNormalY = -otherVec[1] + tmpVec[1];
            float rightNormalZ = -otherVec[2] + tmpVec[2];
            float rightDistance = -otherVec[3] + tmpVec[3];
            float rightDot = rightNormalX * rightNormalX + rightNormalY * rightNormalY + rightNormalZ * rightNormalZ;
            float rightMagnitude = Mathf.Sqrt(rightDot);
            float rightInvMagnitude = 1.0f / rightMagnitude;
            rightNormalX *= rightInvMagnitude;
            rightNormalY *= rightInvMagnitude;
            rightNormalZ *= rightInvMagnitude;
            rightDistance *= rightInvMagnitude;
            outPlanes[kPlaneFrustumRight].normal = new Vector3(rightNormalX, rightNormalY, rightNormalZ);
            outPlanes[kPlaneFrustumRight].distance = rightDistance;

            // bottom & top
            otherVec[0] = finalMatrix.m10;
            otherVec[1] = finalMatrix.m11;
            otherVec[2] = finalMatrix.m12;
            otherVec[3] = finalMatrix.m13;

            float bottomNormalX = otherVec[0] + tmpVec[0];
            float bottomNormalY = otherVec[1] + tmpVec[1];
            float bottomNormalZ = otherVec[2] + tmpVec[2];
            float bottomDistance = otherVec[3] + tmpVec[3];
            float bottomDot = bottomNormalX * bottomNormalX + bottomNormalY * bottomNormalY + bottomNormalZ * bottomNormalZ;
            float bottomMagnitude = Mathf.Sqrt(bottomDot);
            float bottomInvMagnitude = 1.0f / bottomMagnitude;
            bottomNormalX *= bottomInvMagnitude;
            bottomNormalY *= bottomInvMagnitude;
            bottomNormalZ *= bottomInvMagnitude;
            bottomDistance *= bottomInvMagnitude;
            outPlanes[kPlaneFrustumBottom].normal = new Vector3(bottomNormalX, bottomNormalY, bottomNormalZ);
            outPlanes[kPlaneFrustumBottom].distance = bottomDistance;

            float topNormalX = -otherVec[0] + tmpVec[0];
            float topNormalY = -otherVec[1] + tmpVec[1];
            float topNormalZ = -otherVec[2] + tmpVec[2];
            float topDistance = -otherVec[3] + tmpVec[3];
            float topDot = topNormalX * topNormalX + topNormalY * topNormalY + topNormalZ * topNormalZ;
            float topMagnitude = Mathf.Sqrt(topDot);
            float topInvMagnitude = 1.0f / topMagnitude;
            topNormalX *= topInvMagnitude;
            topNormalY *= topInvMagnitude;
            topNormalZ *= topInvMagnitude;
            topDistance *= topInvMagnitude;
            outPlanes[kPlaneFrustumTop].normal = new Vector3(topNormalX, topNormalY, topNormalZ);
            outPlanes[kPlaneFrustumTop].distance = topDistance;

            // near & far
            otherVec[0] = finalMatrix.m20;
            otherVec[1] = finalMatrix.m21;
            otherVec[2] = finalMatrix.m22;
            otherVec[3] = finalMatrix.m23;

            float nearNormalX = otherVec[0] + tmpVec[0];
            float nearNormalY = otherVec[1] + tmpVec[1];
            float nearNormalZ = otherVec[2] + tmpVec[2];
            float nearDistance = otherVec[3] + tmpVec[3];
            float nearDot = nearNormalX * nearNormalX + nearNormalY * nearNormalY + nearNormalZ * nearNormalZ;
            float nearMagnitude = Mathf.Sqrt(nearDot);
            float nearInvMagnitude = 1.0f / nearMagnitude;
            nearNormalX *= nearInvMagnitude;
            nearNormalY *= nearInvMagnitude;
            nearNormalZ *= nearInvMagnitude;
            nearDistance *= nearInvMagnitude;
            outPlanes[kPlaneFrustumNear].normal = new Vector3(nearNormalX, nearNormalY, nearNormalZ);
            outPlanes[kPlaneFrustumNear].distance = nearDistance;

            float farNormalX = -otherVec[0] + tmpVec[0];
            float farNormalY = -otherVec[1] + tmpVec[1];
            float farNormalZ = -otherVec[2] + tmpVec[2];
            float farDistance = -otherVec[3] + tmpVec[3];
            float farDot = farNormalX * farNormalX + farNormalY * farNormalY + farNormalZ * farNormalZ;
            float farMagnitude = Mathf.Sqrt(farDot);
            float farInvMagnitude = 1.0f / farMagnitude;
            farNormalX *= farInvMagnitude;
            farNormalY *= farInvMagnitude;
            farNormalZ *= farInvMagnitude;
            farDistance *= farInvMagnitude;
            outPlanes[kPlaneFrustumFar].normal = new Vector3(farNormalX, farNormalY, farNormalZ);
            outPlanes[kPlaneFrustumFar].distance = farDistance;
        }

         public static unsafe void CalculateFrustumPlanes(ref Matrix4x4 finalMatrix, Vector4* outPlanes)
         {
            const int kPlaneFrustumLeft = 0;
            const int kPlaneFrustumRight = 1;
            const int kPlaneFrustumBottom = 2;
            const int kPlaneFrustumTop = 3;
            const int kPlaneFrustumNear = 4;
            const int kPlaneFrustumFar = 5;

            Vector4 tmpVec = default;
            Vector4 otherVec = default;

            tmpVec[0] = finalMatrix.m30;
            tmpVec[1] = finalMatrix.m31;
            tmpVec[2] = finalMatrix.m32;
            tmpVec[3] = finalMatrix.m33;

            otherVec[0] = finalMatrix.m00;
            otherVec[1] = finalMatrix.m01;
            otherVec[2] = finalMatrix.m02;
            otherVec[3] = finalMatrix.m03;

            // left & right
            float leftNormalX = otherVec[0] + tmpVec[0];
            float leftNormalY = otherVec[1] + tmpVec[1];
            float leftNormalZ = otherVec[2] + tmpVec[2];
            float leftDistance = otherVec[3] + tmpVec[3];
            float leftDot = leftNormalX * leftNormalX + leftNormalY * leftNormalY + leftNormalZ * leftNormalZ;
            float leftMagnitude = Mathf.Sqrt(leftDot);
            float leftInvMagnitude = 1.0f / leftMagnitude;
            leftNormalX *= leftInvMagnitude;
            leftNormalY *= leftInvMagnitude;
            leftNormalZ *= leftInvMagnitude;
            leftDistance *= leftInvMagnitude;
            outPlanes[kPlaneFrustumLeft] = new Vector4(leftNormalX, leftNormalY, leftNormalZ, leftDistance);

            float rightNormalX = -otherVec[0] + tmpVec[0];
            float rightNormalY = -otherVec[1] + tmpVec[1];
            float rightNormalZ = -otherVec[2] + tmpVec[2];
            float rightDistance = -otherVec[3] + tmpVec[3];
            float rightDot = rightNormalX * rightNormalX + rightNormalY * rightNormalY + rightNormalZ * rightNormalZ;
            float rightMagnitude = Mathf.Sqrt(rightDot);
            float rightInvMagnitude = 1.0f / rightMagnitude;
            rightNormalX *= rightInvMagnitude;
            rightNormalY *= rightInvMagnitude;
            rightNormalZ *= rightInvMagnitude;
            rightDistance *= rightInvMagnitude;
            outPlanes[kPlaneFrustumRight] = new Vector4(rightNormalX, rightNormalY, rightNormalZ, rightDistance);

            // bottom & top
            otherVec[0] = finalMatrix.m10;
            otherVec[1] = finalMatrix.m11;
            otherVec[2] = finalMatrix.m12;
            otherVec[3] = finalMatrix.m13;

            float bottomNormalX = otherVec[0] + tmpVec[0];
            float bottomNormalY = otherVec[1] + tmpVec[1];
            float bottomNormalZ = otherVec[2] + tmpVec[2];
            float bottomDistance = otherVec[3] + tmpVec[3];
            float bottomDot = bottomNormalX * bottomNormalX + bottomNormalY * bottomNormalY + bottomNormalZ * bottomNormalZ;
            float bottomMagnitude = Mathf.Sqrt(bottomDot);
            float bottomInvMagnitude = 1.0f / bottomMagnitude;
            bottomNormalX *= bottomInvMagnitude;
            bottomNormalY *= bottomInvMagnitude;
            bottomNormalZ *= bottomInvMagnitude;
            bottomDistance *= bottomInvMagnitude;
            outPlanes[kPlaneFrustumBottom] = new Vector4(bottomNormalX, bottomNormalY, bottomNormalZ, bottomDistance);

            float topNormalX = -otherVec[0] + tmpVec[0];
            float topNormalY = -otherVec[1] + tmpVec[1];
            float topNormalZ = -otherVec[2] + tmpVec[2];
            float topDistance = -otherVec[3] + tmpVec[3];
            float topDot = topNormalX * topNormalX + topNormalY * topNormalY + topNormalZ * topNormalZ;
            float topMagnitude = Mathf.Sqrt(topDot);
            float topInvMagnitude = 1.0f / topMagnitude;
            topNormalX *= topInvMagnitude;
            topNormalY *= topInvMagnitude;
            topNormalZ *= topInvMagnitude;
            topDistance *= topInvMagnitude;
            outPlanes[kPlaneFrustumTop] = new Vector4(topNormalX, topNormalY, topNormalZ, topDistance);

            // near & far
            otherVec[0] = finalMatrix.m20;
            otherVec[1] = finalMatrix.m21;
            otherVec[2] = finalMatrix.m22;
            otherVec[3] = finalMatrix.m23;

            float nearNormalX = otherVec[0] + tmpVec[0];
            float nearNormalY = otherVec[1] + tmpVec[1];
            float nearNormalZ = otherVec[2] + tmpVec[2];
            float nearDistance = otherVec[3] + tmpVec[3];
            float nearDot = nearNormalX * nearNormalX + nearNormalY * nearNormalY + nearNormalZ * nearNormalZ;
            float nearMagnitude = Mathf.Sqrt(nearDot);
            float nearInvMagnitude = 1.0f / nearMagnitude;
            nearNormalX *= nearInvMagnitude;
            nearNormalY *= nearInvMagnitude;
            nearNormalZ *= nearInvMagnitude;
            nearDistance *= nearInvMagnitude;
            outPlanes[kPlaneFrustumNear] = new Vector4(nearNormalX, nearNormalY, nearNormalZ, nearDistance);

            float farNormalX = -otherVec[0] + tmpVec[0];
            float farNormalY = -otherVec[1] + tmpVec[1];
            float farNormalZ = -otherVec[2] + tmpVec[2];
            float farDistance = -otherVec[3] + tmpVec[3];
            float farDot = farNormalX * farNormalX + farNormalY * farNormalY + farNormalZ * farNormalZ;
            float farMagnitude = Mathf.Sqrt(farDot);
            float farInvMagnitude = 1.0f / farMagnitude;
            farNormalX *= farInvMagnitude;
            farNormalY *= farInvMagnitude;
            farNormalZ *= farInvMagnitude;
            farDistance *= farInvMagnitude;
            outPlanes[kPlaneFrustumFar] = new Vector4(farNormalX, farNormalY, farNormalZ, farDistance);
         }

        static float CalcGuardAnglePerspective(float angleInDeg, float resolution, float filterWidth, float normalBiasMax, float guardAngleMaxInDeg)
        {
            float angleInRad  = angleInDeg * 0.5f * Mathf.Deg2Rad;
            float res         = 2.0f / resolution;
            float texelSize   = Mathf.Cos(angleInRad) * res;
            float beta        = normalBiasMax * texelSize * 1.4142135623730950488016887242097f;
            float guardAngle  = Mathf.Atan(beta);
            texelSize   = Mathf.Tan(angleInRad + guardAngle) * res;
            guardAngle  = Mathf.Atan((resolution + Mathf.Ceil(filterWidth)) * texelSize * 0.5f) * 2.0f * Mathf.Rad2Deg - angleInDeg;
            guardAngle *= 2.0f;

            return guardAngle < guardAngleMaxInDeg ? guardAngle : guardAngleMaxInDeg;
        }

        public static float GetSlopeBias(float baseBias, float normalizedSlopeBias)
        {
            return normalizedSlopeBias * baseBias;
        }
    }
}
