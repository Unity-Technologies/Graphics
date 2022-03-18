using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal readonly struct ShaderInstance
    {
        readonly string m_Name;
        readonly string m_AdditionalShaderID;
        readonly List<TemplateInstance> m_TemplateInstances;
        readonly string m_FallbackShader;

        public string Name => m_Name;
        public bool IsPrimaryShader => string.IsNullOrEmpty(m_AdditionalShaderID);
        public IEnumerable<TemplateInstance> TemplateInstances => m_TemplateInstances;
        public string FallbackShader => m_FallbackShader;
        public bool IsValid => !string.IsNullOrEmpty(m_Name);
        public static ShaderInstance Invalid => new ShaderInstance(null, null, null, null);

        internal ShaderInstance(string name, string additionalShaderID, List<TemplateInstance> templateInstances, string fallbackShader)
        {
            m_Name = name;
            m_AdditionalShaderID = additionalShaderID;
            m_TemplateInstances = templateInstances;
            m_FallbackShader = fallbackShader;
        }

        internal class Builder
        {
            ShaderContainer container;
            public string Name { get; set; }
            public string AdditionalShaderID { get; set; }
            public List<TemplateInstance> TemplateInstances { get; set; } = new List<TemplateInstance>();
            public string FallbackShader { get; set; } = @"FallBack ""Hidden/Shader Graph/FallbackError""";

            public Builder(ShaderContainer container, string name, string additionalShaderID)
            {
                this.container = container;
                Name = name;
                AdditionalShaderID = additionalShaderID;
            }

            public ShaderInstance Build()
            {
                return new ShaderInstance(Name, AdditionalShaderID, TemplateInstances, FallbackShader);
            }
        }
    }
}
