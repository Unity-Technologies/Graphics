using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal readonly struct ShaderDescriptor
    {
        readonly string m_Name;
        readonly List<TemplateDescriptor> m_TemplateDescriptors;
        readonly string m_FallbackShader;

        public string Name => m_Name;
        public IEnumerable<TemplateDescriptor> TemplateDescriptors => m_TemplateDescriptors;
        public string FallbackShader => m_FallbackShader;

        internal ShaderDescriptor(string name, List<TemplateDescriptor> templateDescriptors, string fallbackShader)
        {
            m_Name = name;
            m_TemplateDescriptors = templateDescriptors;
            m_FallbackShader = fallbackShader;
        }

        internal class Builder
        {
            public string Name { get; set; }
            public List<TemplateDescriptor> TemplateDescriptors { get; set; } = new List<TemplateDescriptor>();
            public string FallbackShader { get; set; } = @"FallBack ""Hidden/Shader Graph/FallbackError""";

            public Builder(string name)
            {
                Name = name;
            }

            public ShaderDescriptor Build(ShaderContainer container)
            {
                return new ShaderDescriptor(Name, TemplateDescriptors, FallbackShader);
            }
        }
    }
}
