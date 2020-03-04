using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class HDSubShaders
    {
        public static SubShaderDescriptor Unlit = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            renderTypeOverride = HDRenderTypeTags.HDUnlitShader.ToString(),
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.Unlit.ShadowCaster },
                { HDPasses.Unlit.META },
                { HDPasses.Unlit.SceneSelection },
                { HDPasses.Unlit.DepthForwardOnly, new FieldCondition(Fields.SurfaceOpaque, true) },
                { HDPasses.Unlit.MotionVectors, new FieldCondition(Fields.SurfaceOpaque, true) },
                { HDPasses.Unlit.ForwardOnly },
            },
        };

        public static SubShaderDescriptor PBR = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            renderTypeOverride = HDRenderTypeTags.HDLitShader.ToString(),
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.PBR.ShadowCaster },
                { HDPasses.PBR.META },
                { HDPasses.PBR.SceneSelection },
                { HDPasses.PBR.DepthOnly, new FieldCondition(Fields.SurfaceOpaque, true) },
                { HDPasses.PBR.GBuffer, new FieldCondition(Fields.SurfaceOpaque, true) },
                { HDPasses.PBR.MotionVectors, new FieldCondition(Fields.SurfaceOpaque, true) },
                { HDPasses.PBR.Forward },
            },
        };

        public static SubShaderDescriptor HDUnlit = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.HDUnlit.ShadowCaster },
                { HDPasses.HDUnlit.META },
                { HDPasses.HDUnlit.SceneSelection },
                { HDPasses.HDUnlit.DepthForwardOnly },
                { HDPasses.HDUnlit.MotionVectors },
                { HDPasses.HDUnlit.Distortion, new FieldCondition(HDFields.TransparentDistortion, true) },
                { HDPasses.HDUnlit.ForwardOnly },
            },
            customEditorOverride = @"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDUnlitGUI""",
        };

        public static SubShaderDescriptor HDLit = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.HDLit.ShadowCaster },
                { HDPasses.HDLit.META },
                { HDPasses.HDLit.SceneSelection },
                { HDPasses.HDLit.DepthOnly },
                { HDPasses.HDLit.GBuffer },
                { HDPasses.HDLit.MotionVectors },
                { HDPasses.HDLit.DistortionVectors, new FieldCondition(HDFields.TransparentDistortion, true) },
                { HDPasses.HDLit.TransparentBackface, new FieldCondition(HDFields.TransparentBackFace, true) },
                { HDPasses.HDLit.TransparentDepthPrepass, new FieldCondition(HDFields.TransparentDepthPrePass, true) },
                { HDPasses.HDLit.Forward },
                { HDPasses.HDLit.TransparentDepthPostpass, new FieldCondition(HDFields.TransparentDepthPostPass, true) },
            },
            customEditorOverride = @"CustomEditor ""UnityEditor.Rendering.HighDefinition.HDLitGUI""",
        };

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
            customEditorOverride = @"CustomEditor ""UnityEditor.Rendering.HighDefinition.EyeGUI""",
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
            customEditorOverride = @"CustomEditor ""UnityEditor.Rendering.HighDefinition.FabricGUI""",
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
            customEditorOverride = @"CustomEditor ""UnityEditor.Rendering.HighDefinition.HairGUI""",
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
            customEditorOverride = @"CustomEditor ""UnityEditor.Rendering.HighDefinition.StackLitGUI""",
        };

        public static SubShaderDescriptor Decal = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = true,
            passes = new PassCollection
            {
                { HDPasses.Decal.Projector3RT, new FieldCondition(HDFields.DecalDefault, true) },
                { HDPasses.Decal.Projector4RT, new FieldCondition(HDFields.DecalDefault, true) },
                { HDPasses.Decal.ProjectorEmissive, new FieldCondition(HDFields.AffectsEmission, true) },
                { HDPasses.Decal.Mesh3RT, new FieldCondition(HDFields.DecalDefault, true) },
                { HDPasses.Decal.Mesh4RT, new FieldCondition(HDFields.DecalDefault, true) },
                { HDPasses.Decal.MeshEmissive, new FieldCondition(HDFields.AffectsEmission, true) },
                { HDPasses.Decal.Preview, new FieldCondition(Fields.IsPreview, true) },
            },
            customEditorOverride = @"CustomEditor ""UnityEditor.Rendering.HighDefinition.DecalGUI""",
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

        public static SubShaderDescriptor HDLitRaytracing = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = false,
            passes = new PassCollection
            {
                { HDPasses.HDLitRaytracing.Indirect, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDLitRaytracing.Visibility, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDLitRaytracing.Forward, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDLitRaytracing.GBuffer, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDLitRaytracing.SubSurface, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDLitRaytracing.PathTracing, new FieldCondition(Fields.IsPreview, false) },
            },
        };

        public static SubShaderDescriptor HDUnlitRaytracing = new SubShaderDescriptor()
        {
            pipelineTag = HDRenderPipeline.k_ShaderTagName,
            generatesPreview = false,
            passes = new PassCollection
            {
                { HDPasses.HDUnlitRaytracing.Indirect, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDUnlitRaytracing.Visibility, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDUnlitRaytracing.Forward, new FieldCondition(Fields.IsPreview, false) },
                { HDPasses.HDUnlitRaytracing.GBuffer, new FieldCondition(Fields.IsPreview, false) },
            },
        };
    }
}
