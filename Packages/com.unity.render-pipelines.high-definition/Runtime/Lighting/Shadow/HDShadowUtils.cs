using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

namespace UnityEngine.Rendering.HighDefinition
{
    // TODO remove every occurrence of ShadowSplitData in function parameters when we'll have scriptable culling
    static class HDShadowUtils
    {
        public const int k_MaxShadowSplitCount = 6;
        public const float k_MinShadowNearPlane = 0.01f;
        public const float k_MaxShadowNearPlane = 10.0f;

        public static float Asfloat(uint val) { unsafe { return *((float*)&val); } }
        public static float Asfloat(int val) { unsafe { return *((float*)&val); } }
        public static int Asint(float val) { unsafe { return *((int*)&val); } }
        public static uint Asuint(float val) { unsafe { return *((uint*)&val); } }

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

        public static void ExtractPointLightData(NativeArray<Matrix4x4> cubeMapFaces, VisibleLight visibleLight, Vector2 viewportSize, float nearPlane, float normalBiasMax, uint faceIndex, HDShadowFilteringQuality filteringQuality, bool reverseZ,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Vector4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            float guardAngle = CalcGuardAnglePerspective(90.0f, viewportSize.x, GetPunctualFilterWidthInTexels(filteringQuality), normalBiasMax, 79.0f);
            ExtractPointLightMatrix(cubeMapFaces, visibleLight, faceIndex, nearPlane, guardAngle, reverseZ, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);
        }

        // TODO: box spot and pyramid spots with non 1 aspect ratios shadow are incorrectly culled, see when scriptable culling will be here
        public static void ExtractSpotLightData(float spotAngle, float nearPlane, float aspectRatio, float shapeWidth, float shapeHeight, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, HDShadowFilteringQuality filteringQuality, bool reverseZ,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Vector4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            // There is no aspect ratio for non pyramid spot lights
            if (visibleLight.lightType != LightType.Pyramid)
                aspectRatio = 1.0f;

            if (visibleLight.lightType != LightType.Box)
                nearPlane = Mathf.Max(HDShadowUtils.k_MinShadowNearPlane, nearPlane);

            float guardAngle = CalcGuardAnglePerspective(spotAngle, viewportSize.x, GetPunctualFilterWidthInTexels(filteringQuality), normalBiasMax, 180.0f - spotAngle);
            ExtractSpotLightMatrix(visibleLight, forwardOffset: 0, spotAngle, nearPlane, guardAngle, aspectRatio, reverseZ, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);

            if (visibleLight.lightType == LightType.Box)
            {
                projection = ExtractBoxLightProjectionMatrix(visibleLight.range, shapeWidth, shapeHeight, nearPlane);

                // update the culling planes to match the box shape
                InvertView(ref view, out var lightToWorld);
                Vector3 xDir = lightToWorld.GetColumn(0);
                Vector3 yDir = lightToWorld.GetColumn(1);
                Vector3 zDir = -lightToWorld.GetColumn(2); // flip z so that it points in the same direction as the near plane and range
                Vector3 center = lightToWorld.GetColumn(3);
                splitData.cullingPlaneCount = 6;
                splitData.SetCullingPlane(0, new Plane(xDir, center - xDir * (0.5f * shapeWidth)));
                splitData.SetCullingPlane(1, new Plane(-xDir, center + xDir * (0.5f * shapeWidth)));
                splitData.SetCullingPlane(2, new Plane(yDir, center - yDir * (0.5f * shapeHeight)));
                splitData.SetCullingPlane(3, new Plane(-yDir, center + yDir * (0.5f * shapeHeight)));
                splitData.SetCullingPlane(4, new Plane(zDir, center + zDir * nearPlane));
                splitData.SetCullingPlane(5, new Plane(-zDir, center + zDir * visibleLight.range));

                Matrix4x4 deviceProjectionMatrix = GetGPUProjectionMatrix(projection, false, reverseZ);
                deviceProjection = new Vector4(deviceProjectionMatrix.m00, deviceProjectionMatrix.m11, deviceProjectionMatrix.m22, deviceProjectionMatrix.m23);
                deviceProjectionYFlip = GetGPUProjectionMatrix(projection, true, reverseZ);
                InvertOrthographic(ref deviceProjectionYFlip, ref view, out invViewProjection);

                splitData.cullingMatrix = projection * view;
                splitData.cullingNearPlane = nearPlane;
            }
        }

        public static Matrix4x4 GetGPUProjectionMatrix(Matrix4x4 projectionMatrix, bool invertY, bool reverseZ)
        {
            float4x4 gpuProjectionMatrix = math.transpose(projectionMatrix);
            if (invertY)
            {
                gpuProjectionMatrix.c1 = -gpuProjectionMatrix.c1;
            }

            // Now scale&bias to get Z range from -1..1 to 0..1 or 1..0
            // matrix = scaleBias * matrix
            //  1   0   0   0
            //  0   1   0   0
            //  0   0 0.5 0.5
            //  0   0   0   1
            float multiplier = reverseZ ? -0.5f : 0.5f;
            gpuProjectionMatrix.c2 = gpuProjectionMatrix.c2 * multiplier + gpuProjectionMatrix.c3 * 0.5f;

            return math.transpose(gpuProjectionMatrix);
        }

        public static void ExtractDirectionalLightData(Vector2 viewportSize, uint cascadeIndex, int cascadeCount, Vector3 cascadeRatios, float nearPlaneOffset, CullingResults cullResults, int lightIndex,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjectionMatrix, out Vector4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Debug.Assert((uint)viewportSize.x == (uint)viewportSize.y, "Currently the cascaded shadow mapping code requires square cascades.");

            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            splitData.shadowCascadeBlendCullingFactor = .6f; // This used to be fixed to .6f, but is now configureable.

            // TODO: At some point this logic should be moved to C#, then the parameters cullResults and lightIndex can be removed as well
            //       For directional lights shadow data is extracted from the cullResults, so that needs to be somehow provided here.
            //       Check ScriptableShadowsUtility.cpp ComputeDirectionalShadowMatricesAndCullingPrimitives(...) for details.
            cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, (int)cascadeIndex, cascadeCount, cascadeRatios, (int)viewportSize.x, nearPlaneOffset, out view, out projection, out splitData);

            // and the compound (deviceProjection will potentially inverse-Z)
            deviceProjectionMatrix = GL.GetGPUProjectionMatrix(projection, false);
            deviceProjection = new Vector4(deviceProjectionMatrix.m00, deviceProjectionMatrix.m11, deviceProjectionMatrix.m22, deviceProjectionMatrix.m23);
            deviceProjectionYFlip = GL.GetGPUProjectionMatrix(projection, true);
            InvertOrthographic(ref deviceProjectionMatrix, ref view, out invViewProjection);
        }

        public static void ExtractRectangleAreaLightData(VisibleLight visibleLight, float forwardOffset, float areaLightShadowCone, float shadowNearPlane, Vector2 shapeSize, Vector2 viewportSize, float normalBiasMax, bool reverseZ,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Vector4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;
            float aspectRatio = shapeSize.x / shapeSize.y;
            float spotAngle = areaLightShadowCone;
            visibleLight.spotAngle = spotAngle;
            float guardAngle = CalcGuardAnglePerspective(visibleLight.spotAngle, viewportSize.x, 1, normalBiasMax, 180.0f - visibleLight.spotAngle);

            ExtractSpotLightMatrix(visibleLight, forwardOffset, visibleLight.spotAngle, shadowNearPlane, guardAngle, aspectRatio, reverseZ,  out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);
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
            invproj.m03 = proj.m03 * invproj.m00;
            invproj.m13 = proj.m13 * invproj.m11;
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

            mat.m22 = -(f + n) / (f - n);
            mat.m23 = -2 * f * n / (f - n);
            mat.m32 = -1;

            return mat;
        }

        public static Matrix4x4 ExtractBoxLightProjectionMatrix(float range, float width, float height, float nearPlane)
        {
            return Matrix4x4.Ortho(-width / 2, width / 2, -height / 2, height / 2, nearPlane, range);
        }

        static Matrix4x4 ExtractSpotLightMatrix(VisibleLight vl, float forwardOffset, float spotAngle, float nearPlane, float guardAngle, float aspectRatio, bool reverseZ, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 deviceProj, out Matrix4x4 deviceProjYFlip, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
        {
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            lightDir = vl.GetForward();

            // calculate view
            Matrix4x4 localToWorldOffset = vl.localToWorldMatrix;
            CoreMatrixUtils.MatrixTimesTranslation(ref localToWorldOffset, Vector3.forward * forwardOffset);
            view = localToWorldOffset.inverse;
            view.m20 *= -1;
            view.m21 *= -1;
            view.m22 *= -1;
            view.m23 *= -1;

            // calculate projection
            proj = ExtractSpotLightProjectionMatrix(vl.range - forwardOffset, spotAngle, nearPlane - forwardOffset, aspectRatio, guardAngle);

            // and the compound (deviceProj will potentially inverse-Z)
            Matrix4x4 deviceProjMatrix = GetGPUProjectionMatrix(proj, false, reverseZ);
            deviceProjYFlip = GetGPUProjectionMatrix(proj, true, reverseZ);
            InvertPerspective(ref deviceProjMatrix, ref view, out vpinverse);

            deviceProj = new Vector4(deviceProjMatrix.m00, deviceProjMatrix.m11, deviceProjMatrix.m22, deviceProjMatrix.m23);

            Matrix4x4 viewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(proj, view);
            float4 planesLeft;
            float4 planesRight;
            float4 planesBottom;
            float4 planesTop;
            float4 planesNear;
            float4 planesFar;
            CalculateFrustumPlanes(viewProj, out planesLeft, out planesRight, out planesBottom, out planesTop, out planesNear, out planesFar);
            // We can avoid computing proj * view for frustum planes, if device has reversed Z we flip the culling planes as we should have computed them with proj
            if (reverseZ)
            {
                var tmpPlane = planesBottom;
                planesBottom = planesTop;
                planesTop = tmpPlane;
            }
            splitData.cullingPlaneCount = 6;

            Plane leftPlane = new Plane();
            leftPlane.normal = planesLeft.xyz;
            leftPlane.distance = planesLeft.w;
            splitData.SetCullingPlane(0, leftPlane);

            Plane rightPlane = new Plane();
            rightPlane.normal = planesRight.xyz;
            rightPlane.distance = planesRight.w;
            splitData.SetCullingPlane(1, rightPlane);
            Plane bottomPlane = new Plane();
            bottomPlane.normal = planesBottom.xyz;
            bottomPlane.distance = planesBottom.w;
            splitData.SetCullingPlane(2, bottomPlane);
            Plane topPlane = new Plane();
            topPlane.normal = planesTop.xyz;
            topPlane.distance = planesTop.w;
            splitData.SetCullingPlane(3, topPlane);
            Plane planeNear = new Plane();
            planeNear.normal = planesNear.xyz;
            planeNear.distance = planesNear.w;
            splitData.SetCullingPlane(4, planeNear);
            Plane planeFar = new Plane();
            planeFar.normal = planesFar.xyz;
            planeFar.distance = planesFar.w;
            splitData.SetCullingPlane(5, planeFar);

            Matrix4x4 deviceViewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(deviceProjMatrix, view);

            splitData.cullingMatrix = deviceViewProj;
            splitData.cullingNearPlane = nearPlane - forwardOffset;
            return deviceViewProj;
        }

        static unsafe void ExtractPointLightMatrix(NativeArray<Matrix4x4> cubemapFaces, VisibleLight vl, uint faceIdx, float nearPlane, float guardAngle, bool reverseZ, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 deviceProjection, out Matrix4x4 deviceProjYFlip, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
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
            proj = SetPerspective(90.0f + guardAngle, 1.0f, nearZ, vl.range);
            // and the compound (deviceProj will potentially inverse-Z)
            Matrix4x4 deviceProj = GetGPUProjectionMatrix(proj, false, reverseZ);
            deviceProjection = new Vector4(deviceProj.m00, deviceProj.m11, deviceProj.m22, deviceProj.m23);
            deviceProjYFlip = GetGPUProjectionMatrix(proj, true, reverseZ);
            InvertPerspective(ref deviceProj, ref view, out vpinverse);

            Matrix4x4 viewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(proj, view);
            SetSplitDataCullingPlanesFromViewProjMatrix(ref splitData, viewProj, reverseZ);

            Matrix4x4 deviceViewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(deviceProj, view);

            splitData.cullingMatrix = deviceViewProj;
            splitData.cullingNearPlane = nearZ;
        }

        static Matrix4x4 SetPerspective(
            float fovy,
            float aspect,
            float zNear,
            float zFar)
        {
            float cotangent, deltaZ;
            float radians = Deg2Rad(fovy / 2.0f);
            cotangent = math.cos(radians) / math.sin(radians);
            deltaZ = zNear - zFar;

            float4x4 mat = default;
            mat[0] = new float4(cotangent / aspect, 0f, 0f, 0f);
            mat[1] = new float4(0f, cotangent, 0f, 0f);
            mat[2] = new float4(0f, 0f, (zFar + zNear) / deltaZ, -1f);
            mat[3] = new float4(0f, 0f, 2.0F * zNear * zFar / deltaZ, 0f);

            return mat;
        }

        static float Deg2Rad(float deg)
        {
            const float kPI = 3.14159265358979323846264338327950288419716939937510F;
            // TODO : should be deg * kDeg2Rad, but can't be changed,
            // because it changes the order of operations and that affects a replay in some RegressionTests
            return deg / 360.0F * 2.0F * kPI;
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

         public static unsafe void CalculateFrustumPlanes(float4x4 finalMatrix, out float4 outPlanesLeft, out float4 outPlanesRight, out float4 outPlanesBottom, out float4 outPlanesTop, out float4 outPlanesNear, out float4 outPlanesFar)
         {
            // const int kPlaneFrustumLeft = 0;
            // const int kPlaneFrustumRight = 1;
            // const int kPlaneFrustumBottom = 2;
            // const int kPlaneFrustumTop = 3;
            // const int kPlaneFrustumNear = 4;
            // const int kPlaneFrustumFar = 5;

            finalMatrix = math.transpose(finalMatrix);

            float4 tmpVec = finalMatrix.c3;
            float4 otherVec = finalMatrix.c0;
            //
            // tmpVec[0] = finalMatrix.m30;
            // tmpVec[1] = finalMatrix.m31;
            // tmpVec[2] = finalMatrix.m32;
            // tmpVec[3] = finalMatrix.m33;
            //
            // otherVec[0] = finalMatrix.m00;
            // otherVec[1] = finalMatrix.m01;
            // otherVec[2] = finalMatrix.m02;
            // otherVec[3] = finalMatrix.m03;

            // left & right
            float4 leftNormalAndDist = otherVec + tmpVec;
            float4 leftNormalZeroedDist = math.asfloat(math.asuint(leftNormalAndDist) & new uint4(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0));
            float leftDotProduct = math.dot(leftNormalZeroedDist, leftNormalZeroedDist);
            float leftMagnitude = math.sqrt(leftDotProduct);
            float leftInvMagnitude = 1.0f / leftMagnitude;
            leftNormalAndDist *= leftInvMagnitude;
            outPlanesLeft = leftNormalAndDist;

            float4 rightNormalAndDist = -otherVec + tmpVec;
            float4 rightNormalZeroedDist = math.asfloat(math.asuint(rightNormalAndDist) & new uint4(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0));
            float rightDotProduct = math.dot(rightNormalZeroedDist, rightNormalZeroedDist);
            float rightMagnitude = math.sqrt(rightDotProduct);
            float rightInvMagnitude = 1.0f / rightMagnitude;
            rightNormalAndDist *= rightInvMagnitude;
            outPlanesRight = rightNormalAndDist;

            // bottom & top
            otherVec = finalMatrix.c1;
            // otherVec[0] = finalMatrix.m10;
            // otherVec[1] = finalMatrix.m11;
            // otherVec[2] = finalMatrix.m12;
            // otherVec[3] = finalMatrix.m13;

            float4 bottomNormalAndDist = otherVec + tmpVec;
            float4 bottomNormalZeroedDist = math.asfloat(math.asuint(bottomNormalAndDist) & new uint4(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0));
            float bottomDotProduct = math.dot(bottomNormalZeroedDist, bottomNormalZeroedDist);
            float bottomMagnitude = math.sqrt(bottomDotProduct);
            float bottomInvMagnitude = math.rcp(bottomMagnitude);
            bottomNormalAndDist *= bottomInvMagnitude;
            outPlanesBottom = bottomNormalAndDist;

            float4 topNormalAndDist = -otherVec + tmpVec;
            float4 topNormalZeroedDist = math.asfloat(math.asuint(topNormalAndDist) & new uint4(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0));
            float topDotProduct = math.dot(topNormalZeroedDist, topNormalZeroedDist);
            // float topNormalX = -otherVec[0] + tmpVec[0];
            // float topNormalY = -otherVec[1] + tmpVec[1];
            // float topNormalZ = -otherVec[2] + tmpVec[2];
            // float topDistance = -otherVec[3] + tmpVec[3];
            //float topDot = topNormalX * topNormalX + topNormalY * topNormalY + topNormalZ * topNormalZ;
            float topMagnitude = math.sqrt(topDotProduct);
            float topInvMagnitude = math.rcp(topMagnitude);
            topNormalAndDist *= topInvMagnitude;
            // topNormalX *= topInvMagnitude;
            // topNormalY *= topInvMagnitude;
            // topNormalZ *= topInvMagnitude;
            // topDistance *= topInvMagnitude;
            outPlanesTop = topNormalAndDist;

            // near & far
            otherVec = finalMatrix.c2;
            // otherVec[0] = finalMatrix.m20;
            // otherVec[1] = finalMatrix.m21;
            // otherVec[2] = finalMatrix.m22;
            // otherVec[3] = finalMatrix.m23;

            float4 nearNormalAndDist = otherVec + tmpVec;
            float4 nearNormalZeroedDist = math.asfloat(math.asuint(nearNormalAndDist) & new uint4(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0));
            float nearDotProduct = math.dot(nearNormalZeroedDist, nearNormalZeroedDist);
            // float nearNormalX = otherVec[0] + tmpVec[0];
            // float nearNormalY = otherVec[1] + tmpVec[1];
            // float nearNormalZ = otherVec[2] + tmpVec[2];
            // float nearDistance = otherVec[3] + tmpVec[3];
            // float nearDot = nearNormalX * nearNormalX + nearNormalY * nearNormalY + nearNormalZ * nearNormalZ;
            float nearMagnitude = math.sqrt(nearDotProduct);
            float nearInvMagnitude = math.rcp(nearMagnitude);
            nearNormalAndDist *= nearInvMagnitude;
            // nearNormalX *= nearInvMagnitude;
            // nearNormalY *= nearInvMagnitude;
            // nearNormalZ *= nearInvMagnitude;
            // nearDistance *= nearInvMagnitude;
            outPlanesNear = nearNormalAndDist;

            float4 farNormalAndDist = -otherVec + tmpVec;
            float4 farNormalZeroedDist = math.asfloat(math.asuint(farNormalAndDist) & new uint4(0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0));
            float farDotProduct = math.dot(farNormalZeroedDist, farNormalZeroedDist);
            // float farNormalX = -otherVec[0] + tmpVec[0];
            // float farNormalY = -otherVec[1] + tmpVec[1];
            // float farNormalZ = -otherVec[2] + tmpVec[2];
            // float farDistance = -otherVec[3] + tmpVec[3];
            // float farDot = farNormalX * farNormalX + farNormalY * farNormalY + farNormalZ * farNormalZ;
            float farMagnitude = math.sqrt(farDotProduct);
            float farInvMagnitude = math.rcp(farMagnitude);
            farNormalAndDist *= farInvMagnitude;
            // farNormalX *= farInvMagnitude;
            // farNormalY *= farInvMagnitude;
            // farNormalZ *= farInvMagnitude;
            // farDistance *= farInvMagnitude;
            outPlanesFar = farNormalAndDist;
         }

        static float CalcGuardAnglePerspective(float angleInDeg, float resolution, float filterWidth, float normalBiasMax, float guardAngleMaxInDeg)
        {
            float angleInRad  = angleInDeg * 0.5f * Mathf.Deg2Rad;
            float res         = 2.0f / resolution;
            float texelSize   = math.cos(angleInRad) * res;
            float beta        = normalBiasMax * texelSize * 1.4142135623730950488016887242097f;
            float guardAngle  = math.atan(beta);
            texelSize   = math.tan(angleInRad + guardAngle) * res;
            guardAngle  = math.atan((resolution + math.ceil(filterWidth)) * texelSize * 0.5f) * 2.0f * Mathf.Rad2Deg - angleInDeg;
            guardAngle *= 2.0f;

            return guardAngle < guardAngleMaxInDeg ? guardAngle : guardAngleMaxInDeg;
        }

        public static float GetSlopeBias(float baseBias, float normalizedSlopeBias)
        {
            return normalizedSlopeBias * baseBias;
        }

        static void SetSplitDataCullingPlanesFromViewProjMatrix(ref ShadowSplitData splitData, Matrix4x4 matrix, bool reverseZ)
        {
            float4 planesLeft;
            float4 planesRight;
            float4 planesBottom;
            float4 planesTop;
            float4 planesNear;
            float4 planesFar;
            CalculateFrustumPlanes(matrix, out planesLeft, out planesRight, out planesBottom, out planesTop, out planesNear, out planesFar);

            if (reverseZ)
            {
                var tmpPlane = planesBottom;
                planesBottom = planesTop;
                planesTop = tmpPlane;
            }
            splitData.cullingPlaneCount = 6;

            Plane leftPlane = new Plane();
            leftPlane.normal = planesLeft.xyz;
            leftPlane.distance = planesLeft.w;
            splitData.SetCullingPlane(0, leftPlane);

            Plane rightPlane = new Plane();
            rightPlane.normal = planesRight.xyz;
            rightPlane.distance = planesRight.w;
            splitData.SetCullingPlane(1, rightPlane);
            Plane bottomPlane = new Plane();
            bottomPlane.normal = planesBottom.xyz;
            bottomPlane.distance = planesBottom.w;
            splitData.SetCullingPlane(2, bottomPlane);
            Plane topPlane = new Plane();
            topPlane.normal = planesTop.xyz;
            topPlane.distance = planesTop.w;
            splitData.SetCullingPlane(3, topPlane);
            Plane planeNear = new Plane();
            planeNear.normal = planesNear.xyz;
            planeNear.distance = planesNear.w;
            splitData.SetCullingPlane(4, planeNear);
            Plane planeFar = new Plane();
            planeFar.normal = planesFar.xyz;
            planeFar.distance = planesFar.w;
            splitData.SetCullingPlane(5, planeFar);
        }
    }
}
