using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    class CustomEditorSorter : IComparer<ShaderCustomEditor>
    {
        public int Compare(ShaderCustomEditor a, ShaderCustomEditor b)
        {
            int result = string.CompareOrdinal(a.RenderPipelineAssetType, b.RenderPipelineAssetType);
            if (result == 0)
                result = string.CompareOrdinal(a.ShaderGUI, b.ShaderGUI);
            return result;
        }
    }

    class DependencySorter : IComparer<ShaderDependency>
    {
        public int Compare(ShaderDependency a, ShaderDependency b)
        {
            int result = string.CompareOrdinal(a.DependencyName, b.DependencyName);
            if (result == 0)
                result = string.CompareOrdinal(a.ShaderName, b.ShaderName);
            return result;
        }
    }

    internal readonly struct ShaderInstance
    {
        readonly string m_Name;
        readonly string m_AdditionalShaderID;
        readonly List<TemplateInstance> m_TemplateInstances;
        readonly string m_FallbackShader;
        readonly List<ShaderCustomEditor> m_CustomEditors;
        readonly List<ShaderDependency> m_Dependencies;

        public string Name => m_Name;
        public bool IsPrimaryShader => string.IsNullOrEmpty(m_AdditionalShaderID);
        public IEnumerable<TemplateInstance> TemplateInstances => m_TemplateInstances?.AsReadOnly() ?? Enumerable.Empty<TemplateInstance>();
        public string FallbackShader => m_FallbackShader;
        public IEnumerable<ShaderCustomEditor> CustomEditors => m_CustomEditors?.AsReadOnly() ?? Enumerable.Empty<ShaderCustomEditor>();
        public IEnumerable<ShaderDependency> Dependencies => m_Dependencies?.AsReadOnly() ?? Enumerable.Empty<ShaderDependency>();

        public bool IsValid => !string.IsNullOrEmpty(m_Name);
        public static ShaderInstance Invalid => new ShaderInstance(null, null, null);

        internal ShaderInstance(string name, string additionalShaderID, List<TemplateInstance> templateInstances)
        {
            m_Name = name;
            m_AdditionalShaderID = additionalShaderID;
            m_TemplateInstances = templateInstances;

            // these are copied out from the set of template instances
            m_FallbackShader = null;
            m_CustomEditors = new List<ShaderCustomEditor>();
            m_Dependencies = new List<ShaderDependency>();
            foreach (var templateInstance in templateInstances)
            {
                if (!string.IsNullOrEmpty(templateInstance.Template.ShaderFallback))
                {
                    if (m_FallbackShader == null)
                        m_FallbackShader = templateInstance.Template.ShaderFallback;
                    else
                    {
                        if (m_FallbackShader != templateInstance.Template.ShaderFallback)
                        {
                            // ERROR : conflicting shader fallbacks defined...
                        }
                    }
                }

                foreach (var customEditor in templateInstance.Template.ShaderCustomEditors)
                {
                    if (!m_CustomEditors.Contains(customEditor))
                        m_CustomEditors.Add(customEditor);
                }

                foreach (var dependency in templateInstance.Template.ShaderDependencies)
                {
                    if (!m_Dependencies.Contains(dependency))
                        m_Dependencies.Add(dependency);
                }
            }
            m_CustomEditors.Sort(new CustomEditorSorter());
            m_Dependencies.Sort(new DependencySorter());
        }

        internal class Builder
        {
            ShaderContainer container;
            public string Name { get; set; }
            public string AdditionalShaderID { get; set; }
            public List<TemplateInstance> TemplateInstances { get; set; } = new List<TemplateInstance>();

            public Builder(ShaderContainer container, string name, string additionalShaderID)
            {
                this.container = container;
                Name = name;
                AdditionalShaderID = additionalShaderID;
            }

            public ShaderInstance Build()
            {
                return new ShaderInstance(Name, AdditionalShaderID, TemplateInstances);
            }
        }
    }
}
