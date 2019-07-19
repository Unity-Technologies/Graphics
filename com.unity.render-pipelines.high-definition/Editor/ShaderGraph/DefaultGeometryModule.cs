using System.Collections.Generic;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [GeometryModule.DisplayName("Default")]
    class DefaultGeometryModule : IGeometryModule
    {
        [SerializeField]
        bool m_DOTSInstancing = false;

        public IEnumerable<VisualElement> CreateVisualElements()
        {
            yield return new Toggle()
            {
                name = "DOTS instancing",
                value = m_DOTSInstancing
            };
        }

        public void OnVisualElementValueChanged(VisualElement visualElement)
        {
            m_DOTSInstancing = (visualElement as Toggle).value;
        }

        public InstancingSettings GenerateInstancingSettings()
        {
            var settings = InstancingSettings.Default;

            if (m_DOTSInstancing)
                settings.Options |= InstancingOption.NoLightProbe | InstancingOption.NoLODFade;
            else
                settings.Options |= InstancingOption.RenderingLayer;

            return settings;
        }

        public LODFadeSettings GenerateLODFadeSettings() => LODFadeSettings.Default;

        public bool ForceVertex() => false;

        public void OverrideActiveFields(ICollection<string> activeFields)
        { }

        public void RegisterGlobalFunctions(FunctionRegistry functionRegistry)
        { }

        public void GenerateVertexProlog(ShaderStringBuilder sb)
        { }

        public void GeneratePixelProlog(ShaderStringBuilder sb)
        { }
    }
}
