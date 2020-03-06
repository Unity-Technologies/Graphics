using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    class DefaultVFXTarget : ITargetImplementation
    {
        public Type targetType => typeof(VFXTarget);
        public Type dataType => typeof(DefaultVFXTargetData);
        public string displayName => "Default";
        public string passTemplatePath => null;
        public string sharedTemplateDirectory => null;

        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return (currentPipeline != null);
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
        }

        public List<BlockFieldDescriptor> GetSupportedBlocks(TargetImplementationData data)
        {
            if(!(data is DefaultVFXTargetData vfxData))
                return null;

            var supportedBlocks = new List<BlockFieldDescriptor>();

            // Always supported Blocks
            supportedBlocks.Add(BlockFields.SurfaceDescription.BaseColor);
            supportedBlocks.Add(BlockFields.SurfaceDescription.Alpha);

            // Alpha Blocks
            if(vfxData.alphaTest)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.ClipThreshold);
            }

            // Lit Blocks
            if(vfxData.lit)
            {
                supportedBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Smoothness);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Normal);
                supportedBlocks.Add(BlockFields.SurfaceDescription.Emission);
            }

            return supportedBlocks;
        }

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks, TargetImplementationData data)
        {
            return null;
        }
    }
}
