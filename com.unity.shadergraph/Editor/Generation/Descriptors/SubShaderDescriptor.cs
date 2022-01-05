using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal struct SubShaderDescriptor
    {
        public string pipelineTag;
        public string customTags;
        public string renderType;
        public string renderQueue;
        public bool generatesPreview;
        public PassCollection passes;
        public List<string> usePassList;

        // if set, this subshader is intended to be placed not in the main shader result, but in an additional shader
        // the string is used as a postfix on the shader name (i.e. "shaderName-additionalShaderID")
        public string additionalShaderID;
    }
}
