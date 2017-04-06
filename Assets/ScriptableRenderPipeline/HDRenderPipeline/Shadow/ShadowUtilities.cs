namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ShadowUtils
    {
        public static Matrix4x4 ExtractSpotLightMatrix(VisibleLight vl, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 lightDir, out ShadowSplitData splitData)
        {
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            // get lightDir
            lightDir = vl.light.transform.forward;
            // calculate view
            Matrix4x4 scaleMatrix = Matrix4x4.identity;
            scaleMatrix.m22 = -1.0f;
            view = scaleMatrix * vl.localToWorld.inverse;
            // following code is from SharedLightData::GetNearPlaneMinBound
            float percentageBound = 0.01f * vl.light.range;
            float fixedBound = 0.1f;
            float nearmin = fixedBound <= percentageBound ? fixedBound : percentageBound;
            // calculate projection
            float zfar = vl.range;
            float znear = vl.light.shadowNearPlane >= nearmin ? vl.light.shadowNearPlane : nearmin;
            float fov = vl.spotAngle;
            proj = Matrix4x4.Perspective(fov, 1.0f, znear, zfar);
            // and the compound
            return proj * view;
        }

        public static Matrix4x4 ExtractPointLightMatrix(VisibleLight vl, uint faceIdx, float fovBias, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 lightDir, out ShadowSplitData splitData, CullResults cullResults, int lightIndex)
        {
            Debug.Assert(faceIdx <= (uint)CubemapFace.NegativeZ, "Tried to extract cubemap face " + faceIdx + ".");

            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            // get lightDir
            lightDir = vl.light.transform.forward;
            // TODO: At some point this logic should be moved to C#, then the parameters cullResults and lightIndex can be removed as well
            cullResults.ComputePointShadowMatricesAndCullingPrimitives(lightIndex, (CubemapFace)faceIdx, fovBias, out view, out proj, out splitData);
            // and the compound
            return proj * view;
        }

        public static Matrix4x4 ExtractDirectionalLightMatrix(VisibleLight vl, uint cascadeIdx, int cascadeCount, Vector3 splitRatio, float nearPlaneOffset, uint width, uint height, out Matrix4x4 view, out Matrix4x4 proj, out Vector4 lightDir, out ShadowSplitData splitData, CullResults cullResults, int lightIndex)
        {
            Debug.Assert(width == height, "Currently the cascaded shadow mapping code requires square cascades.");
            splitData = new ShadowSplitData();
            splitData.cullingSphere.Set(0.0f, 0.0f, 0.0f, float.NegativeInfinity);
            splitData.cullingPlaneCount = 0;
            // get lightDir
            lightDir = vl.light.transform.forward;
            // TODO: At some point this logic should be moved to C#, then the parameters cullResults and lightIndex can be removed as well
            //       For directional lights shadow data is extracted from the cullResults, so that needs to be somehow provided here.
            //       Check ScriptableShadowsUtility.cpp ComputeDirectionalShadowMatricesAndCullingPrimitives(...) for details.
            cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(lightIndex, (int)cascadeIdx, cascadeCount, splitRatio, (int)width, nearPlaneOffset, out view, out proj, out splitData);
            // and the compound
            return proj * view;
        }

        public static bool MapLightType(LightArchetype la, LightType lt, out GPULightType gputype, out GPUShadowType shadowtype)
        {
            switch (la)
            {
                case LightArchetype.Punctual: return MapLightType(lt, out gputype, out shadowtype);
                case LightArchetype.Rectangle: gputype = GPULightType.Rectangle; shadowtype = GPUShadowType.Unknown; return true;
                case LightArchetype.Line: gputype = GPULightType.Line; shadowtype = GPUShadowType.Unknown; return true;
                default: gputype = GPULightType.Spot; shadowtype = GPUShadowType.Unknown; return false;                      // <- probably not what you want
            }
        }

        public static bool MapLightType(LightType lt,  out GPULightType gputype, out GPUShadowType shadowtype)
        {
            switch (lt)
            {
                case LightType.Spot: gputype = GPULightType.Spot;          shadowtype = GPUShadowType.Spot;        return true;
                case LightType.Directional: gputype = GPULightType.Directional;   shadowtype = GPUShadowType.Directional; return true;
                case LightType.Point: gputype = GPULightType.Point;         shadowtype = GPUShadowType.Point;       return true;
                default:
                case LightType.Area: gputype = GPULightType.Rectangle; shadowtype = GPUShadowType.Unknown; return false;        // area lights by themselves can't be mapped to any GPU type
            }
        }

        public static float Asfloat(uint val) { return System.BitConverter.ToSingle(System.BitConverter.GetBytes(val), 0); }
        public static int Asint(float val)    { return System.BitConverter.ToInt32(System.BitConverter.GetBytes(val), 0); }
        public static uint Asuint(float val)  { return System.BitConverter.ToUInt32(System.BitConverter.GetBytes(val), 0); }
    }
} // end of namespace UnityEngine.Experimental.ScriptableRenderLoop
