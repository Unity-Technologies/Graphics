using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    class DefaultPreviewTargetImplementation : ITargetImplementation
    {
        public Type targetType => typeof(PreviewTarget);
        public string displayName => null;
        public string passTemplatePath => GenerationUtils.GetDefaultTemplatePath("PassMesh.template");
        public string sharedTemplateDirectory => GenerationUtils.GetDefaultSharedTemplateDirectory();

        public bool IsValid(IMasterNode masterNode)
        {
            return false;
        }

        public void SetupTarget(ref TargetSetupContext context)
        {
            context.SetupSubShader(PreviewTarget.SubShaders.Preview);
        }
    }
}
