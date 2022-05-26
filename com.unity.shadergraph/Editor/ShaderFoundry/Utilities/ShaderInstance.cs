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
                            ErrorHandling.ReportError($"Conflicting shader fallbacks defined: {m_FallbackShader} != {templateInstance.Template.ShaderFallback}");
                        }
                    }
                }

                if (templateInstance.Template.CustomEditor.IsValid)
                    customEditors.Add(templateInstance.Template.CustomEditor);

                dependencies.AddRange(templateInstance.Template.ShaderDependencies);
            }

            // sort these lists to a deterministic order
            customEditors.Sort();
            dependencies.Sort();

            // filter out conflicting custom editors (relies on sort order)
            m_CustomEditors = new List<ShaderCustomEditor>();
            ShaderCustomEditor lastEditor = default;
            foreach (var customEditor in customEditors)
            {
                if (customEditor.RenderPipelineAssetClassName != lastEditor.RenderPipelineAssetClassName)
                    m_CustomEditors.Add(customEditor);
                else
                {
                    if (customEditor.CustomEditorClassName != lastEditor.CustomEditorClassName)
                        ErrorHandling.ReportError($"Conflicting custom editor defined for render pipeline '{customEditor.RenderPipelineAssetClassName}' : {customEditor.CustomEditorClassName} != {lastEditor.CustomEditorClassName}");
                }
                lastEditor = customEditor;
            }

            // filter out conflicting dependencies (relies on sort order)
            m_Dependencies = new List<ShaderDependency>();
            ShaderDependency lastDependency = default;
            foreach (var dependency in dependencies)
            {
                if (dependency.DependencyName != lastDependency.DependencyName)
                    m_Dependencies.Add(dependency);
                else
                {
                    if (dependency.ShaderName != lastDependency.ShaderName)
                        ErrorHandling.ReportError($"Conflicting dependency defined '{dependency.DependencyName}' : {dependency.ShaderName} != {lastDependency.ShaderName}");
                }
                lastDependency = dependency;
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
}
