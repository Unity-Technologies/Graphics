using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    interface ITarget
    {
        string displayName { get; }
        string passTemplatePath { get; }
        string sharedTemplateDirectory { get; }

        bool Validate(RenderPipelineAsset pipelineAsset);
        bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader);
    }
}
