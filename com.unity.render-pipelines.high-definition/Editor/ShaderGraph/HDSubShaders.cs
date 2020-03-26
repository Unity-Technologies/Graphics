using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDSubShaders
    {
        public static SubShaderDescriptor Eye = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.Eye.ShadowCaster },
                { HDPasses.Eye.META },
                { HDPasses.Eye.SceneSelection },
                { HDPasses.Eye.DepthForwardOnly },
                { HDPasses.Eye.MotionVectors },
                { HDPasses.Eye.ForwardOnly },
            },
            // customEditorOverride = "Rendering.HighDefinition.EyeGUI",
        };

        public static SubShaderDescriptor Fabric = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.Fabric.ShadowCaster },
                { HDPasses.Fabric.META },
                { HDPasses.Fabric.SceneSelection },
                { HDPasses.Fabric.DepthForwardOnly },
                { HDPasses.Fabric.MotionVectors },
                { HDPasses.Fabric.FabricForwardOnly },
            },
            // customEditorOverride = "Rendering.HighDefinition.FabricGUI",
        };
        public static SubShaderDescriptor Hair = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.Hair.ShadowCaster },
                { HDPasses.Hair.META },
                { HDPasses.Hair.SceneSelection },
                { HDPasses.Hair.DepthForwardOnly },
                { HDPasses.Hair.MotionVectors },
                { HDPasses.Hair.TransparentBackface, new FieldCondition(HDFields.TransparentBackFace, true) },
                { HDPasses.Hair.TransparentDepthPrepass, new FieldCondition(HDFields.TransparentDepthPrePass, true) },
                { HDPasses.Hair.ForwardOnly },
                { HDPasses.Hair.TransparentDepthPostpass, new FieldCondition(HDFields.TransparentDepthPostPass, true) },
            },
            // customEditorOverride = "Rendering.HighDefinition.HairGUI",
        };

        public static SubShaderDescriptor StackLit = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.StackLit.ShadowCaster },
                { HDPasses.StackLit.META },
                { HDPasses.StackLit.SceneSelection },
                { HDPasses.StackLit.DepthForwardOnly },
                { HDPasses.StackLit.MotionVectors },
                { HDPasses.StackLit.Distortion, new FieldCondition(HDFields.TransparentDistortion, true) },
                { HDPasses.StackLit.ForwardOnly },
            },
            // customEditorOverride = "Rendering.HighDefinition.StackLitGUI",
        };

        public static SubShaderDescriptor FabricRaytracing = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = false,
            passes = new PassCollection
            {
                { HDPasses.FabricRaytracing.Indirect, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.FabricRaytracing.Visibility, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.FabricRaytracing.Forward, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.FabricRaytracing.GBuffer, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.FabricRaytracing.SubSurface, new FieldCondition(Fields.IsPreview, false) },
            },
        };
    }
}