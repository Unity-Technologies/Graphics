using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal readonly struct ShaderInstance
    {
        readonly string m_Name;
        readonly List<TemplateInstance> m_TemplateInstances;
        readonly string m_FallbackShader;

        public string Name => m_Name;
        public IEnumerable<TemplateInstance> TemplateInstances => m_TemplateInstances;
        public string FallbackShader => m_FallbackShader;
        public bool IsValid => !string.IsNullOrEmpty(m_Name);
        public static ShaderInstance Invalid => new ShaderInstance(null, null, null);

        internal ShaderInstance(string name, List<TemplateInstance> templateInstances, string fallbackShader)
        {
            m_Name = name;
            m_TemplateInstances = templateInstances;
            m_FallbackShader = fallbackShader;
        }

        internal class Builder
        {
            ShaderContainer container;
            public string Name { get; set; }
            public List<TemplateInstance> TemplateInstances { get; set; } = new List<TemplateInstance>();
            public string FallbackShader { get; set; } = @"FallBack ""Hidden/Shader Graph/FallbackError""";

            public Builder(ShaderContainer container, string name)
            {
                this.container = container;
                Name = name;
            }

            public ShaderInstance Build()
            {
                return new ShaderInstance(Name, TemplateInstances, FallbackShader);
            }
        }
    }
}
