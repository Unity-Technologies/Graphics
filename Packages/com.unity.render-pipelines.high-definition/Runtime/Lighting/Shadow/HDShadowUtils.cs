using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    // TODO remove every occurrence of ShadowSplitData in function parameters when we'll have scriptable culling
    static class HDShadowUtils
    {
        public static readonly float k_MinShadowNearPlane = 0.01f;
        public static readonly float k_MaxShadowNearPlane = 10.0f;

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

        public static void ExtractPointLightData(VisibleLight visibleLight, Vector2 viewportSize, float nearPlane, float normalBiasMax, uint faceIndex, HDShadowFilteringQuality filteringQuality,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            float guardAngle = CalcGuardAnglePerspective(90.0f, viewportSize.x, GetPunctualFilterWidthInTexels(filteringQuality), normalBiasMax, 79.0f);
            ExtractPointLightMatrix(visibleLight, faceIndex, nearPlane, guardAngle, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);
        }

        // TODO: box spot and pyramid spots with non 1 aspect ratios shadow are incorrectly culled, see when scriptable culling will be here
        public static void ExtractSpotLightData(SpotLightShape shape, float spotAngle, float nearPlane, float aspectRatio, float shapeWidth, float shapeHeight, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, HDShadowFilteringQuality filteringQuality,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            // There is no aspect ratio for non pyramid spot lights
            if (shape != SpotLightShape.Pyramid)
                aspectRatio = 1.0f;

            if (shape != SpotLightShape.Box)
                nearPlane = Mathf.Max(HDShadowUtils.k_MinShadowNearPlane, nearPlane);

            float guardAngle = CalcGuardAnglePerspective(spotAngle, viewportSize.x, GetPunctualFilterWidthInTexels(filteringQuality), normalBiasMax, 180.0f - spotAngle);
            ExtractSpotLightMatrix(visibleLight, forwardOffset: 0, spotAngle, nearPlane, guardAngle, aspectRatio, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);

            if (shape == SpotLightShape.Box)
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

                deviceProjection = GL.GetGPUProjectionMatrix(projection, false);
                deviceProjectionYFlip = GL.GetGPUProjectionMatrix(projection, true);
                InvertOrthographic(ref deviceProjectionYFlip, ref view, out invViewProjection);
                splitData.cullingMatrix = projection * view;
                splitData.cullingNearPlane = nearPlane;
            }
        }

        public static void ExtractDirectionalLightData(VisibleLight visibleLight, Vector2 viewportSize, uint cascadeIndex, int cascadeCount, float[] cascadeRatios, float nearPlaneOffset, CullingResults cullResults, int lightIndex,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

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

        public static void ExtractRectangleAreaLightData(VisibleLight visibleLight, float forwardOffset, float areaLightShadowCone, float shadowNearPlane, Vector2 shapeSize, Vector2 viewportSize, float normalBiasMax, HDAreaShadowFilteringQuality filteringQuality,
            out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out Matrix4x4 deviceProjectionYFlip, out ShadowSplitData splitData)
        {
            Vector4 lightDir;
            float aspectRatio = shapeSize.x / shapeSize.y;
            float spotAngle = areaLightShadowCone;
            visibleLight.spotAngle = spotAngle;
            float guardAngle = CalcGuardAnglePerspective(visibleLight.spotAngle, viewportSize.x, 1, normalBiasMax, 180.0f - visibleLight.spotAngle);

            ExtractSpotLightMatrix(visibleLight, forwardOffset, visibleLight.spotAngle, shadowNearPlane, guardAngle, aspectRatio, out view, out projection, out deviceProjection, out deviceProjectionYFlip, out invViewProjection, out lightDir, out splitData);
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

        static Matrix4x4 ExtractSpotLightMatrix(VisibleLight vl, float forwardOffset, float spotAngle, float nearPlane, float guardAngle, float aspectRatio, out Matrix4x4 view, out Matrix4x4 proj, out Matrix4x4 deviceProj, out Matrix4x4 deviceProjYFlip, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
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
            deviceProj = GL.GetGPUProjectionMatrix(proj, false);
            deviceProjYFlip = GL.GetGPUProjectionMatrix(proj, true);
            InvertPerspective(ref deviceProj, ref view, out vpinverse);

            Matrix4x4 viewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(proj, view);
            SetSplitDataCullingPlanesFromViewProjMatrix(ref splitData, viewProj);

            Matrix4x4 deviceViewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(deviceProj, view);

            splitData.cullingMatrix = deviceViewProj;
            splitData.cullingNearPlane = nearPlane - forwardOffset;
            return deviceViewProj;
        }

        static Matrix4x4 ExtractPointLightMatrix(VisibleLight vl, uint faceIdx, float nearPlane, float guardAngle, out Matrix4x4 view, out Matrix4x4 proj, out Matrix4x4 deviceProj, out Matrix4x4 deviceProjYFlip, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
        {
            if (faceIdx > (uint)CubemapFace.NegativeZ)
                Debug.LogError("Tried to extract cubemap face " + faceIdx + ".");

            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);

            // get lightDir
            lightDir = vl.GetForward();
            // calculate the view matrices
            Vector3 lpos = vl.GetPosition();
            view = kCubemapFaces[faceIdx];
            Vector3 inverted_viewpos = kCubemapFaces[faceIdx].MultiplyPoint(-lpos);
            view.SetColumn(3, new Vector4(inverted_viewpos.x, inverted_viewpos.y, inverted_viewpos.z, 1.0f));

            float nearZ = Mathf.Max(nearPlane, k_MinShadowNearPlane);
            proj = Matrix4x4.Perspective(90.0f + guardAngle, 1.0f, nearZ, vl.range);
            // and the compound (deviceProj will potentially inverse-Z)
            deviceProj = GL.GetGPUProjectionMatrix(proj, false);
            deviceProjYFlip = GL.GetGPUProjectionMatrix(proj, true);
            InvertPerspective(ref deviceProj, ref view, out vpinverse);

            Matrix4x4 viewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(proj, view);
            SetSplitDataCullingPlanesFromViewProjMatrix(ref splitData, viewProj);

            Matrix4x4 deviceViewProj = CoreMatrixUtils.MultiplyPerspectiveMatrix(deviceProj, view);

            splitData.cullingMatrix = deviceViewProj;
            splitData.cullingNearPlane = nearZ;
            return deviceViewProj;
        }

        static float CalcGuardAnglePerspective(float angleInDeg, float resolution, float filterWidth, float normalBiasMax, float guardAngleMaxInDeg)
        {
            float angleInRad = angleInDeg * 0.5f * Mathf.Deg2Rad;
            float res = 2.0f / resolution;
            float texelSize = Mathf.Cos(angleInRad) * res;
            float beta = normalBiasMax * texelSize * 1.4142135623730950488016887242097f;
            float guardAngle = Mathf.Atan(beta);
            texelSize = Mathf.Tan(angleInRad + guardAngle) * res;
            guardAngle = Mathf.Atan((resolution + Mathf.Ceil(filterWidth)) * texelSize * 0.5f) * 2.0f * Mathf.Rad2Deg - angleInDeg;
            guardAngle *= 2.0f;

            return guardAngle < guardAngleMaxInDeg ? guardAngle : guardAngleMaxInDeg;
        }

        public static float GetSlopeBias(float baseBias, float normalizedSlopeBias)
        {
            return normalizedSlopeBias * baseBias;
        }

        static void SetSplitDataCullingPlanesFromViewProjMatrix(ref ShadowSplitData splitData, Matrix4x4 matrix)
        {
            GeometryUtility.CalculateFrustumPlanes(matrix, s_CachedPlanes);

            if (SystemInfo.usesReversedZBuffer)
            {
                var tmpPlane = s_CachedPlanes[2];
                s_CachedPlanes[2] = s_CachedPlanes[3];
                s_CachedPlanes[3] = tmpPlane;
            }
            splitData.cullingPlaneCount = 6;
            for (int i = 0; i < 6; i++)
                splitData.SetCullingPlane(i, s_CachedPlanes[i]);
        }
    }
}
