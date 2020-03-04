using System;
using UnityEditor.ShaderGraph;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    class DefaultPreviewTarget : ITargetImplementation
    {
        public Type targetType => typeof(PreviewTarget);
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
    }
}
