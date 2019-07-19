using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    public class GeometryModuleDisplayNameAttribute : Attribute
    {
        public readonly string DisplayName;

        public GeometryModuleDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
    }

    public interface IGeometryModule
    {
        IEnumerable<VisualElement> CreateVisualElements();
        void OnVisualElementValueChanged(VisualElement visualElement);
        InstancingSettings GenerateInstancingSettings();
        LODFadeSettings GenerateLODFadeSettings();
        void OverrideActiveFields(ICollection<string> activeFields);
        string GenerateVertexProlog();
        string GeneratePixelProlog();
    }
}
