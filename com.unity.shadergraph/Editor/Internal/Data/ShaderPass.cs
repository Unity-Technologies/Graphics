using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    public struct ShaderPass
    {
        // Definition
        public string displayName;
        public string referenceName;
        public string lightMode;
        public string passInclude;
        public string varyingsInclude;
        public bool useInPreview;

        // Port mask
        public List<int> vertexPorts;
        public List<int> pixelPorts;

        // Required fields
        public List<string> requiredAttributes;
        public List<string> requiredVaryings;

        // Conditional State
        public ConditionalRenderState[] renderStates;
        public ConditionalPragma[] pragmas;
        public IEnumerable<string> defines;
        public IEnumerable<KeywordDescriptor> keywords;
        public ConditionalInclude[] includes;
        public IEnumerable<string> defaultDotsInstancingOptions;

        // Custom Template
        public string passTemplatePath;
        public string sharedTemplateDirectory;

        // Methods
        public bool Equals(ShaderPass other)
        {
            return referenceName == other.referenceName;
        }
    }
}
