using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDDefines
    {
        public static DefineCollection SceneSelection = new DefineCollection
        {
            { HDKeywords.Descriptors.SceneSelectionPass, 1 },
        };

        public static DefineCollection ShaderGraphRaytracingHigh = new DefineCollection
        {
            { RayTracingNode.GetRayTracingKeyword(), 0 },
        };

        public static DefineCollection Forward = new DefineCollection
        {
            { HDKeywords.Descriptors.HasLightloop, 1 },
            { HDKeywords.Descriptors.LightList, 1, new FieldCondition(Fields.SurfaceTransparent, true) },
            { RayTracingNode.GetRayTracingKeyword(), 0, new FieldCondition(Fields.SurfaceTransparent, true) },
        };

        public static DefineCollection TransparentDepthPrepass = new DefineCollection
        {
            { RayTracingNode.GetRayTracingKeyword(), 0 },
            { HDKeywords.Descriptors.TransparentDepthPrepass, 1 },
        };

        public static DefineCollection TransparentDepthPostpass = new DefineCollection
        {
            { RayTracingNode.GetRayTracingKeyword(), 0 },
            { HDKeywords.Descriptors.TransparentDepthPostpass, 1 },
        };

        public static DefineCollection DepthMotionVectors = new DefineCollection
        {
            { RayTracingNode.GetRayTracingKeyword(), 0 },
            { HDKeywords.Descriptors.WriteNormalBuffer, 1 },
        };

        public static DefineCollection Decals3RT = new DefineCollection
        {
            { HDKeywords.Descriptors.Decals3RT, 1 },
        };

        public static DefineCollection Decals4RT = new DefineCollection
        {
            { HDKeywords.Descriptors.Decals4RT, 1 },
        };

        public static DefineCollection HDLitRaytracingForwardIndirect = new DefineCollection
        {
            { HDKeywords.Descriptors.Shadow, 0 },
            { RayTracingNode.GetRayTracingKeyword(), 1 },
            { HDKeywords.Descriptors.HasLightloop, 1 },
        };

        public static DefineCollection HDLitRaytracingGBuffer = new DefineCollection
        {
            { HDKeywords.Descriptors.Shadow, 0 },
            { RayTracingNode.GetRayTracingKeyword(), 1 },
        };

        public static DefineCollection HDLitRaytracingVisibility = new DefineCollection
        {
            { RayTracingNode.GetRayTracingKeyword(), 1 },
        };
        public static DefineCollection HDLitRaytracingPathTracing = new DefineCollection
        {
            { HDKeywords.Descriptors.Shadow, 0 },
            { RayTracingNode.GetRayTracingKeyword(), 0 },
        };

        public static DefineCollection FabricRaytracingForwardIndirect = new DefineCollection
        {
            { HDKeywords.Descriptors.Shadow, 0 },
            { HDKeywords.Descriptors.HasLightloop, 1 },
        };

        public static DefineCollection FabricRaytracingGBuffer = new DefineCollection
        {
            { HDKeywords.Descriptors.Shadow, 0 },
        };
    }
}
