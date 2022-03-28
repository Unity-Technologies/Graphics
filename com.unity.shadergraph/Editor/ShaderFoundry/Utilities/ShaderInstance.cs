using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
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

            // these are copied out of the Templates in the template instances
            m_FallbackShader = null;
            var customEditors = new List<ShaderCustomEditor>();
            var dependencies = new List<ShaderDependency>();
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
                            // TODO @ SHADERS ERROR - conflicting shader fallbacks defined...
                        }
                    }
                }

                customEditors.AddRange(templateInstance.Template.ShaderCustomEditors);
                dependencies.AddRange(templateInstance.Template.ShaderDependencies);
            }

            // sort these lists to a deterministic order
            customEditors.Sort();
            dependencies.Sort(new DependencySorter());

            // filter out conflicting custom editors (relies on sort order)
            m_CustomEditors = new List<ShaderCustomEditor>();
            string lastRenderPipelineAssetClassName = null;
            foreach (var customEditor in customEditors)
            {
                if (customEditor.RenderPipelineAssetClassName != lastRenderPipelineAssetClassName)
                    m_CustomEditors.Add(customEditor);
                else
                {
                    // TODO @ SHADERS ERROR potentially conflicting custom editors defined... (if the CustomEditorClassNames are different)
                }
                lastRenderPipelineAssetClassName = customEditor.RenderPipelineAssetClassName;
            }

            // filter out conflicting dependencies (relies on sort order)
            m_Dependencies = new List<ShaderDependency>();
            string lastDependencyName = null;
            foreach (var dependency in dependencies)
            {
                if (dependency.DependencyName != lastDependencyName)
                    m_Dependencies.Add(dependency);
                else
                {
                    // TODO @ SHADERS ERROR potentially conflicting dependencies defined... (if the ShaderNames are different)
                }
                lastDependencyName = dependency.DependencyName;
            }
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

    // we must declare this sorter class, instead of making ShaderDependency implement IComparable, because
    // declaring a CompareTo function in ShaderDependency crashes the bindings generation somehow...
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
}
