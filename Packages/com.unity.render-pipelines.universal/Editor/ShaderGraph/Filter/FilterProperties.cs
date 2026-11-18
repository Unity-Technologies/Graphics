using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // System-bound properties for a filter pass. _MainTex is fed by UIRFilterHelper.ApplyFilterPass at
    // draw time, never by the graph author (hidden = true). Reuses Canvas's own _MainTex rather than
    // declare a second copy that could silently drift.
    static class FilterProperties
    {
        public static readonly Texture2DShaderProperty MainTex = CanvasProperties.MainTex;
    }

    // Declares the filter parameters a second time, per instance, in the INSTANCING_ON variant, where UI Toolkit draws the
    // pass of many elements together. The graph code keeps reading the parameters by name: the defines redirect each name
    // to the instance, as URP's hand-written shaders do for DOTS instancing. Shader Graph's own Hybrid Per Instance
    // declaration can't be used: it is a setting of each property, which a filter graph doesn't get by default.
    sealed class FilterInstancedParametersProperty : AbstractShaderProperty
    {
        const string k_BufferName = "UIEFilterParameters";

        readonly List<AbstractShaderProperty> m_GraphProperties;

        public FilterInstancedParametersProperty(IEnumerable<AbstractShaderProperty> graphProperties)
        {
            m_GraphProperties = new List<AbstractShaderProperty>(graphProperties);
            displayName = k_BufferName;
            overrideReferenceName = k_BufferName;
            generatePropertyBlock = false;
            overrideHLSLDeclaration = true;
            hlslDeclarationOverride = HLSLDeclaration.Global;
        }

        public override PropertyType propertyType => PropertyType.Float;
        internal override ConcreteSlotValueType concreteShaderValueType => ConcreteSlotValueType.Vector1;
        internal override bool isExposable => false;
        internal override bool isRenamable => false;

        internal override void ForeachHLSLProperty(GenerationMode mode, Action<HLSLProperty> action)
        {
            var parameters = new List<(string type, string name)>();
            foreach (var property in m_GraphProperties)
            {
                // Another declaration (Hybrid Per Instance, Global) keeps Shader Graph's own access macro, which a define would break
                if (!property.isExposed || property.ResolveHLSLDeclaration(mode) != HLSLDeclaration.UnityPerMaterial)
                    continue;

                var type = GetInstancedType(property);
                if (type != null)
                    parameters.Add((type, property.referenceName));
            }

            if (parameters.Count == 0)
                return;

            var declaration = new HLSLProperty(HLSLType._CUSTOM, k_BufferName, HLSLDeclaration.Global);
            declaration.customDeclaration = builder =>
            {
                builder.AppendLine("#if defined(UNITY_INSTANCING_ENABLED)");
                builder.AppendLine($"UNITY_INSTANCING_BUFFER_START({k_BufferName})");
                foreach (var (type, name) in parameters)
                    builder.AppendLine($"    UNITY_DEFINE_INSTANCED_PROP({type}, {name})");
                builder.AppendLine($"UNITY_INSTANCING_BUFFER_END({k_BufferName})");
                foreach (var (_, name) in parameters)
                    builder.AppendLine($"#define {name} UNITY_ACCESS_INSTANCED_PROP({k_BufferName}, {name})");
                builder.AppendLine("#endif");
            };
            action(declaration);
        }

        // Every number a writeParametersCallback can write per element. Textures are set once per draw.
        static string GetInstancedType(AbstractShaderProperty property) => property switch
        {
            Vector1ShaderProperty or BooleanShaderProperty => "float",
            Vector2ShaderProperty => "float2",
            Vector3ShaderProperty => "float3",
            Vector4ShaderProperty or ColorShaderProperty => "float4",
            _ => null,
        };

        internal override string GetPropertyAsArgumentString(string precisionString) => string.Empty;
        internal override AbstractMaterialNode ToConcreteNode() => null;
        internal override PreviewProperty GetPreviewMaterialProperty() => default;
        internal override ShaderInput Copy() => new FilterInstancedParametersProperty(m_GraphProperties);
    }
}
