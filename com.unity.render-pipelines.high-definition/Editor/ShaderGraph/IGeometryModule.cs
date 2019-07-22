using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.ShaderGraph;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.HighDefinition
{
    public static class GeometryModule
    {
        public class DisplayNameAttribute : Attribute
        {
            public readonly string DisplayName;

            public DisplayNameAttribute(string displayName)
            {
                DisplayName = displayName;
            }
        }

        public static string DisplayName(this IGeometryModule geometryModule)
            => DisplayName(geometryModule.GetType());

        public static string DisplayName(Type geometryModuleType)
            => geometryModuleType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? geometryModuleType.Name;
    }

    public interface IGeometryModule
    {
        // UI
        IEnumerable<VisualElement> CreateVisualElements();
        void OnVisualElementValueChanged(VisualElement visualElement);

        InstancingSettings GenerateInstancingSettings();
        LODFadeSettings GenerateLODFadeSettings();
        bool ForceVertex();
        void OverrideActiveFields(ICollection<string> activeFields);
        void RegisterGlobalFunctions(FunctionRegistry functionRegistry);
        void GenerateVertexProlog(ShaderStringBuilder sb, string inputStructName);
        void GeneratePixelProlog(ShaderStringBuilder sb, string inputStructName);
    }
}
