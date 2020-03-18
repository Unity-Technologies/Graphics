using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph
{
    class DefaultPreviewTarget : ITargetImplementation
    {
        public Type targetType => typeof(PreviewTarget);
        public string displayName => null;
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();
        public string renderTypeTag => null;
        public string renderQueueTag => null;

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.AddAssetDependencyPath("7464b9fcde08e5645a16b9b8ae1e573c"); // PreviewTarget
            context.AddAssetDependencyPath("17beeb3de0d148c4091315e2775a46e3"); // DefaultPreviewTarget

            context.AddSubShader(PreviewTargetResources.PreviewSubShader);
        }

        public void SetActiveBlocks(ref List<BlockFieldDescriptor> activeBlocks)
        {
        }

        public ConditionalField[] GetConditionalFields(PassDescriptor pass, List<BlockFieldDescriptor> blocks)
        {
            return null;
        }

        public VisualElement GetSettings(Action onChange)
        {
            return null;
        }
    }
}
