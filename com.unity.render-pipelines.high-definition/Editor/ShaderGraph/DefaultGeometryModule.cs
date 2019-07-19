using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    [GeometryModuleDisplayName("Default")]
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
                settings.Options |= InstancingOption.NoRenderingLayer;

            return settings;
        }

        public LODFadeSettings GenerateLODFadeSettings()
        {
            return LODFadeSettings.Default;
        }

        public void OverrideActiveFields(ICollection<string> activeFields)
        { }

        public string GenerateVertexProlog() => string.Empty;

        public string GeneratePixelProlog() => string.Empty;
    }
}
