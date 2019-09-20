using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph.Internal
{
    class DecalTarget : ITarget
    {
        public string displayName => "Decal";
        public string passTemplatePath => string.Empty;
        public string sharedTemplateDirectory => string.Empty;

        public bool Validate(RenderPipelineAsset pipelineAsset)
        {
            return false;
        }

        public bool TryGetSubShader(IMasterNode masterNode, out ISubShader subShader)
        {
            subShader = null;
            return false;
        }
    }
}
