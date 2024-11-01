using System;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class PassDescriptorExtension
    {
        public static bool IsDepthOrMV(this PassDescriptor pass)
        {
            return pass.lightMode == HDShaderPassNames.s_DepthForwardOnlyStr
                || pass.lightMode == HDShaderPassNames.s_DepthOnlyStr
                || pass.lightMode == HDShaderPassNames.s_MotionVectorsStr;
        }

        public static bool IsMotionVector(this PassDescriptor pass) =>
            pass.lightMode == HDShaderPassNames.s_MotionVectorsStr;

        public static bool IsShadow(this PassDescriptor pass)
            => pass.lightMode == HDShaderPassNames.s_ShadowCasterStr;

        public static bool IsLightingOrMaterial(this PassDescriptor pass)
        {
            if (pass.lightMode == null)
                return false;

            return pass.IsForward()
                || pass.lightMode == HDShaderPassNames.s_GBufferStr
                // DXR passes without visibility, prepass or ray tracing
                || (pass.lightMode.Contains("DXR") && pass.lightMode != HDShaderPassNames.s_RayTracingVisibilityStr);
        }

        public static bool IsForward(this PassDescriptor pass)
        {
            return pass.lightMode == HDShaderPassNames.s_ForwardOnlyStr
                || pass.lightMode == HDShaderPassNames.s_ForwardStr
                || pass.lightMode == HDShaderPassNames.s_LineRenderingOffscreenShading
                || pass.lightMode == HDShaderPassNames.s_TransparentBackfaceStr;
        }

        public static bool RequiresTransparentSurfaceTypeKeyword(this PassDescriptor pass)
        {
            return pass.IsForward()
                   || pass.lightMode == HDShaderPassNames.s_TransparentDepthPrepassStr
                   || pass.lightMode == HDShaderPassNames.s_TransparentDepthPostpassStr
                   || pass.lightMode == HDShaderPassNames.s_GBufferStr
                   || pass.lightMode == HDShaderPassNames.s_MetaStr
                   || pass.lightMode == HDShaderPassNames.s_MotionVectorsStr
                   || pass.lightMode == HDShaderPassNames.s_DistortionVectorsStr
                   || pass.lightMode == HDShaderPassNames.s_RayTracingVisibilityStr
                   || pass.lightMode == HDShaderPassNames.s_RayTracingIndirectStr
                   || pass.lightMode == HDShaderPassNames.s_RayTracingForwardStr
                   || pass.lightMode == HDShaderPassNames.s_PathTracingDXRStr;
        }

        public static bool RequiresTransparentMVKeyword(this PassDescriptor pass)
        {
            return pass.IsMotionVector()
                   || pass.IsForward()
                   || pass.lightMode == HDShaderPassNames.s_TransparentDepthPrepassStr
                   || pass.lightMode == HDShaderPassNames.s_TransparentDepthPostpassStr;
        }

        public static bool RequiresFogOnTransparentKeyword(this PassDescriptor pass)
        {
            return pass.IsForward()
                   || pass.lightMode == HDShaderPassNames.s_MetaStr;
        }

        public static bool NeedsDebugDisplay(this PassDescriptor pass)
        {
            return IsLightingOrMaterial(pass);
        }

        public static bool IsRaytracing(this PassDescriptor pass)
        {
            if (pass.pragmas == null)
                return false;

            foreach (var pragma in pass.pragmas)
            {
                if (pragma.value == "#pragma raytracing surface_shader")
                    return true;
            }

            return false;
        }

        public static bool IsPathTracing(this PassDescriptor pass)
        {
            return (pass.displayName == HDShaderPassNames.s_PathTracingDXRStr);
        }
        public static bool IsRayTracing(this PassDescriptor pass)
        {
            return (pass.displayName == HDShaderPassNames.s_RayTracingIndirectStr) || (pass.displayName == HDShaderPassNames.s_RayTracingGBufferStr);
        }

        // This function allow to know if a pass is used in context of raytracing rendering even if the pass is not a rayrtacing pass itself (like with RaytracingPrepass)
        public static bool IsRelatedToRaytracing(this PassDescriptor pass)
        {
            return pass.lightMode.Contains("DXR")
                || pass.lightMode == HDShaderPassNames.s_RayTracingVisibilityStr
                || pass.lightMode == HDShaderPassNames.s_RayTracingPrepassStr;
        }

        public static bool IsTessellation(this PassDescriptor pass)
        {
            foreach (var pragma in pass.pragmas)
            {
                if (pragma.value == "#pragma hull Hull")
                    return true;
            }

            return false;
        }
    }
}
