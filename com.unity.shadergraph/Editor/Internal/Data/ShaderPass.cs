using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    public class ConditionalShaderPass : IConditionalShaderPass
    {
        public ShaderPass shaderPass { get; }
        public FieldCondition[] fieldConditions { get; }
        public ConditionalShaderPass(ShaderPass shaderPass)
        {
            this.shaderPass = shaderPass;
            this.fieldConditions = null;
        }

        public ConditionalShaderPass(ShaderPass shaderPass, FieldCondition fieldCondition)
        {
            this.shaderPass = shaderPass;
            this.fieldConditions = new FieldCondition[] { fieldCondition };
        }

        public ConditionalShaderPass(ShaderPass shaderPass, FieldCondition[] fieldConditions)
        {
            this.shaderPass = shaderPass;
            this.fieldConditions = fieldConditions;
        }
    }
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
        public StructDescriptor[] structs;
        public IField[] requiredFields;
        public FieldDependency[] fieldDependencies;

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
