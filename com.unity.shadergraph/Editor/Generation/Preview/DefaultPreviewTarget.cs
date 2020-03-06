using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditor.ShaderGraph.Drawing;

namespace UnityEditor.ShaderGraph
{
    class DefaultPreviewTarget : ITargetImplementation
    {
        public Type targetType => typeof(PreviewTarget);

        // TODO: How do we handle these special cases?
        // TODO: PreviewTarget does not require a dataType
        // TODO: but must return some Type of TargetImplementationData here...
        public Type dataType => typeof(DefaultPreviewTargetData);

        public string displayName => null;
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool IsValid(IMasterNode masterNode)
        {
            return false;
        }
        public bool IsPipelineCompatible(RenderPipelineAsset currentPipeline)
        {
            return (currentPipeline != null);
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath("7464b9fcde08e5645a16b9b8ae1e573c"); // PreviewTarget
            context.AddAssetDependencyPath("17beeb3de0d148c4091315e2775a46e3"); // DefaultPreviewTarget

            context.SetupSubShader(PreviewTargetResources.PreviewSubShader);
        }

        public List<BlockFieldDescriptor> GetSupportedBlocks(TargetImplementationData data)
        {
            return null;
        }

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks, TargetImplementationData data)
        {
            return null;
        }
    }

    internal class DefaultPreviewTargetData : TargetImplementationData
    {
        internal override void GetProperties(PropertySheet propertySheet, InspectorView inspectorView)
        {
        }
    }
}
