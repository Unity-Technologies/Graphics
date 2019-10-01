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
        public int[] vertexPorts;
        public int[] pixelPorts;

        // Fields
        public IEnumerable<StructDescriptor> structs;
        public List<IField> requiredFields;
        public List<FieldDependency[]> fieldDependencies;        

        // Conditional State
        public ConditionalRenderState[] renderStates;
        public ConditionalPragma[] pragmas;
        public ConditionalDefine[] defines;
        public ConditionalKeyword[] keywords;
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
