using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data.Util;

namespace UnityEditor.ShaderGraph.Internal
{
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
        public RenderStateCollection renderStates;
        public PragmaCollection pragmas;
        public DefineCollection defines;
        public KeywordCollection keywords;
        public IncludeCollection includes;

        // Custom Template
        public string passTemplatePath;
        public string sharedTemplateDirectory;

        // Methods
        public bool Equals(ShaderPass other)
        {
            return referenceName == other.referenceName;
        }
    }

    public class ShaderPassCollection : IEnumerable<ConditionalShaderPass>
    {
        private readonly List<ConditionalShaderPass> m_ShaderPasses;

        public ShaderPassCollection()
        {
            m_ShaderPasses = new List<ConditionalShaderPass>();
        }

        public void Add(ShaderPass pass)
        {
            m_ShaderPasses.Add(new ConditionalShaderPass(pass, null));
        }

        public void Add(ShaderPass pass, FieldCondition fieldCondition)
        {
            m_ShaderPasses.Add(new ConditionalShaderPass(pass, new FieldCondition[]{ fieldCondition }));
        }

        public void Add(ShaderPass pass, FieldCondition[] fieldConditions)
        {
            m_ShaderPasses.Add(new ConditionalShaderPass(pass, fieldConditions));
        }

        public IEnumerator<ConditionalShaderPass> GetEnumerator()
        {
            return m_ShaderPasses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class ConditionalShaderPass
    {
        public ShaderPass shaderPass { get; }
        public FieldCondition[] fieldConditions { get; }

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
}
