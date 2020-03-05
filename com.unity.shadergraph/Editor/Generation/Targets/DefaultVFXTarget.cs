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

        public bool IsValid(IMasterNode masterNode)
        {
            return masterNode is VfxMasterNode;
        }
        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return (currentPipeline != null);
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
        }

        public List<BlockFieldDescriptor> GetSupportedBlocks(IMasterNode masterNode)
        {
            var supportedBlocks = new List<BlockFieldDescriptor>();

            // Always supported Blocks
            supportedBlocks.Add(BlockFields.SurfaceDescription.BaseColor);
            supportedBlocks.Add(BlockFields.SurfaceDescription.Alpha);

            // Lit Blocks
            if(masterNode is VfxMasterNode vfxMasterNode)
            {
                // Alpha Blocks
                if(vfxMasterNode.alphaTest.isOn)
                {
                    supportedBlocks.Add(BlockFields.SurfaceDescription.ClipThreshold);
                }

                if(vfxMasterNode.lit.isOn)
                {
                    supportedBlocks.Add(BlockFields.SurfaceDescription.Metallic);
                    supportedBlocks.Add(BlockFields.SurfaceDescription.Smoothness);
                    supportedBlocks.Add(BlockFields.SurfaceDescription.Normal);
                    supportedBlocks.Add(BlockFields.SurfaceDescription.Emission);
                }
            }

            return supportedBlocks;
        }
    }
}
