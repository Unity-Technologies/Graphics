using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // TODO remove every occurrence of ShadowSplitData in function parameters when we'll have scriptable culling
    public static class HDShadowUtils
    {
        public static readonly float k_MinShadowNearPlane = 0.0001f;

        public static float Asfloat(uint val) { unsafe { return *((float*)&val); } }
        public static float Asfloat(int val)  { unsafe { return *((float*)&val); } }
        public static int Asint(float val)    { unsafe { return *((int*)&val); } }
        public static uint Asuint(float val)  { unsafe { return *((uint*)&val); } }

        static Plane[] s_CachedPlanes = new Plane[6];

        static float GetPunctualFilterWidthInTexels(HDCamera camera, LightType lightType)
        {
            var hdAsset = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset);

            if (hdAsset == null)
                return 1;

            // Currently only PCF 3x3 is used for deferred rendering so if we're in deferred return 3
            if (camera.frameSettings.litShaderMode == LitShaderMode.Deferred)
                return 3;

            switch (hdAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.shadowQuality)
            {
                // Warning: these values have to match the algorithms used for shadow filtering (in HDShadowAlgorithm.hlsl)
                case HDShadowQuality.Low:
                    return 3; // PCF 3x3
                case HDShadowQuality.Medium:
                    return 5; // PCF 5x5
                default:
                    return 1; // Any non PCF algorithms
            }
        }

        public static void ExtractPointLightData(HDCamera camera, LightType lightType, VisibleLight visibleLight, Vector2 viewportSize, float nearPlane, float normalBiasMax, uint faceIndex, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            float guardAngle = CalcGuardAnglePerspective(90.0f, viewportSize.x, GetPunctualFilterWidthInTexels(camera, lightType), normalBiasMax, 79.0f);
            ExtractPointLightMatrix(visibleLight, faceIndex, nearPlane, guardAngle, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData);
        }

        // TODO: box spot and pyramid spots with non 1 aspect ratios shadow are incorrectly culled, see when scriptable culling will be here
        public static void ExtractSpotLightData(HDCamera camera, LightType lightType, SpotLightShape shape, float nearPlane, float aspectRatio, float shapeWidth, float shapeHeight, VisibleLight visibleLight, Vector2 viewportSize, float normalBiasMax, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4 lightDir;

            // There is no aspect ratio for non pyramid spot lights
            if (shape != SpotLightShape.Pyramid)
                aspectRatio = 1.0f;

            float guardAngle = CalcGuardAnglePerspective(visibleLight.light.spotAngle, viewportSize.x, GetPunctualFilterWidthInTexels(camera, lightType), normalBiasMax, 180.0f - visibleLight.light.spotAngle);
            ExtractSpotLightMatrix(visibleLight, nearPlane, guardAngle, aspectRatio, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData);

            if (shape == SpotLightShape.Box)
            {
                projection = ExtractBoxLightProjectionMatrix(visibleLight.range, shapeWidth, shapeHeight, nearPlane);
                deviceProjection = GL.GetGPUProjectionMatrix(projection, false);
                projection = GL.GetGPUProjectionMatrix(projection, true);
                InvertOrthographic(ref projection, ref view, out invViewProjection);
            }
        }

        public static void ExtractDirectionalLightData(VisibleLight visibleLight, Vector2 viewportSize, uint cascadeIndex, int cascadeCount, float[] cascadeRatios, float nearPlaneOffset, CullingResults cullResults, int lightIndex, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            Vector4     lightDir;

            Debug.Assert((uint)viewportSize.x == (uint)viewportSize.y, "Currently the cascaded shadow mapping code requires square cascades.");
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;

#if UNITY_2019_2_OR_NEWER
            // This used to be fixed to .6f, but is now configureable.
            splitData.shadowCascadeBlendCullingFactor = .6f;
#endif

            // get lightDir
            lightDir = visibleLight.light.transform.forward;
            // TODO: At some point this logic should be moved to C#, then the parameters cullResults and lightIndex can be removed as well
            //       For directional lights shadow data is extracted from the cullResults, so that needs to be somehow provided here.
            //       Check ScriptableShadowsUtility.cpp ComputeDirectionalShadowMatricesAndCullingPrimitives(...) for details.
            Vector3 ratios = new Vector3();
            for (int i = 0, cnt = cascadeRatios.Length < 3 ? cascadeRatios.Length : 3; i < cnt; i++)
                ratios[i] = cascadeRatios[i];
            cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, (int)cascadeIndex, cascadeCount, ratios, (int)viewportSize.x, nearPlaneOffset, out view, out projection, out splitData);
            // and the compound (deviceProjection will potentially inverse-Z)
            deviceProjection = GL.GetGPUProjectionMatrix(projection, false);
            projection = GL.GetGPUProjectionMatrix(projection, true);
            InvertOrthographic(ref deviceProjection, ref view, out invViewProjection);
        }

        // Currently area light shadows are not supported
        public static void ExtractAreaLightData(HDCamera camera, VisibleLight visibleLight, LightTypeExtent lightTypeExtent, Vector3 shadowPosition, float areaLightShadowCone, float shadowNearPlane, Vector2 shapeSize, Vector2 viewportSize, float normalBiasMax, out Matrix4x4 view, out Matrix4x4 invViewProjection, out Matrix4x4 projection, out Matrix4x4 deviceProjection, out ShadowSplitData splitData)
        {
            if (lightTypeExtent != LightTypeExtent.Rectangle)
            {
                view = Matrix4x4.identity;
                invViewProjection = Matrix4x4.identity;
                deviceProjection = Matrix4x4.identity;
                projection = Matrix4x4.identity;
                splitData = default(ShadowSplitData);
            }
            else
            {

                Vector4 lightDir;
                float aspectRatio = shapeSize.x / shapeSize.y;
                float spotAngle = areaLightShadowCone;
                visibleLight.spotAngle = spotAngle;
                float guardAngle = CalcGuardAnglePerspective(visibleLight.spotAngle, viewportSize.x, GetPunctualFilterWidthInTexels(camera, LightType.Rectangle), normalBiasMax, 180.0f - visibleLight.spotAngle);

                ExtractSpotLightMatrix(visibleLight, shadowNearPlane, guardAngle, aspectRatio, out view, out projection, out deviceProjection, out invViewProjection, out lightDir, out splitData);
            }
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

            vpinv = invview * invproj;
        }

        public static Matrix4x4 ExtractSpotLightProjectionMatrix(float range, float spotAngle, float nearPlane, float aspectRatio, float guardAngle)
        {
            float fov = spotAngle + guardAngle;
            float nearZ = Mathf.Max(nearPlane, k_MinShadowNearPlane);
            return Matrix4x4.Perspective(fov, aspectRatio, nearZ, range);
        }

        public static Matrix4x4 ExtractBoxLightProjectionMatrix(float range, float width, float height, float nearPlane)
        {
            float nearZ = Mathf.Max(nearPlane, k_MinShadowNearPlane);
            return Matrix4x4.Ortho(-width / 2, width / 2, -height / 2, height / 2, nearZ, range);
        }

        static Matrix4x4 ExtractSpotLightMatrix(VisibleLight vl, float nearPlane, float guardAngle, float aspectRatio, out Matrix4x4 view, out Matrix4x4 proj, out Matrix4x4 deviceProj, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
        {
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            // get lightDir
            lightDir = vl.light.transform.forward;
            // calculate view
            Matrix4x4 scaleMatrix = Matrix4x4.identity;
            scaleMatrix.m22 = -1.0f;
            view = scaleMatrix * vl.localToWorldMatrix.inverse;
            // calculate projection
            proj = ExtractSpotLightProjectionMatrix(vl.range, vl.spotAngle, nearPlane, aspectRatio, guardAngle);
            // and the compound (deviceProj will potentially inverse-Z)
            deviceProj = GL.GetGPUProjectionMatrix(proj, false);
            proj = GL.GetGPUProjectionMatrix(proj, true);
            InvertPerspective(ref deviceProj, ref view, out vpinverse);
            return deviceProj * view;
        }

        static Matrix4x4 ExtractPointLightMatrix(VisibleLight vl, uint faceIdx, float nearPlane, float guardAngle, out Matrix4x4 view, out Matrix4x4 proj, out Matrix4x4 deviceProj, out Matrix4x4 vpinverse, out Vector4 lightDir, out ShadowSplitData splitData)
        {
            if (faceIdx > (uint)CubemapFace.NegativeZ)
                Debug.LogError("Tried to extract cubemap face " + faceIdx + ".");

            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);

            // get lightDir
            lightDir = vl.light.transform.forward;
            // calculate the view matrices
            Vector3 lpos = vl.light.transform.position;
            view = kCubemapFaces[faceIdx];
            Vector3 inverted_viewpos = kCubemapFaces[faceIdx].MultiplyPoint(-lpos);
            view.SetColumn(3, new Vector4(inverted_viewpos.x, inverted_viewpos.y, inverted_viewpos.z, 1.0f));

            float nearZ = Mathf.Max(nearPlane, k_MinShadowNearPlane);
            proj = Matrix4x4.Perspective(90.0f + guardAngle, 1.0f, nearZ, vl.range);
            // and the compound (deviceProj will potentially inverse-Z)
            deviceProj = GL.GetGPUProjectionMatrix(proj, false);
            proj = GL.GetGPUProjectionMatrix(proj, true);
            InvertPerspective(ref deviceProj, ref view, out vpinverse);

            GeometryUtility.CalculateFrustumPlanes(proj * view, s_CachedPlanes);
            splitData.cullingPlaneCount = 6;
            for (int i = 0; i < 6; i++)
                splitData.SetCullingPlane(i, s_CachedPlanes[i]);

            return deviceProj * view;
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
    }
}
