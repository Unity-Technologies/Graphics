using System.Collections.Generic;
using System.Linq;
using Data.Util;

namespace UnityEditor.ShaderGraph.Internal
{
    public class ConditionalShaderPass
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

        public bool TestActive(ActiveFields fields)
        {
            // Test FieldCondition against current active Fields
            bool TestFieldCondition(FieldCondition fieldCondition)
            {
                // Required active field is not active
                if(fieldCondition.condition == true && !fields.baseInstance.Contains(fieldCondition.field))
                    return false;

                // Required non-active field is active
                else if(fieldCondition.condition == false && fields.baseInstance.Contains(fieldCondition.field))
                    return false;

                return true;
            }

            // No FieldConditions is always true
            if(fieldConditions == null)
            {
                return true;
            }

            // One or more FieldConditions failed
            if(fieldConditions.Where(x => !TestFieldCondition(x)).Any())
            {
                return false;
            }

            // All FieldConditions passed
            return true;
        }
    }

    public struct ShaderPass
    {
        // Definition
        public string displayName;
        public string referenceName;
        public string lightMode;
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
        public ConditionalInclude[] preGraphIncludes;
        public ConditionalInclude[] postGraphIncludes;

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
