using System;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Stateless filter that defers to ShaderGraph's keyword tables.
    // Defined here rather than in com.unity.graph-authoring so it can reference NodeUtils,
    // and serialized via [SerializeReference] in ExpressionProvider.
    [Serializable]
    sealed class ShaderGraphKeywordFilter : ExpressionProvider.IKeywordFilter
    {
        public bool IsReserved(string name)
            => NodeUtils.IsShaderLabKeyWord(name) || NodeUtils.IsShaderGraphKeyWord(name) || NodeUtils.IsHLSLKeyword(name);
    }

    [Serializable]
    [ProviderModel(ExpressionProvider.kExpressionProviderKey)]
    internal class ExpressionNode : ProviderNode
    {
        ExpressionProvider TypedProvider => Provider as ExpressionProvider;

        string hlslFunctionName => $"ExpressionNode_{this.objectId}";

        static readonly ShaderGraphKeywordFilter s_keywordFilter = new();

        enum SupportedTypes { Vector1, Vector2, Vector3, Vector4 }

        [EnumControl("Type")]
        SupportedTypes SelectedType {
            get => FromTypeName(TypedProvider.ShaderType);
            set
            {
                TypedProvider.UpdateExpression(hlslFunctionName, Expression, ToTypeName(value), s_keywordFilter);
                Refresh();
            }
        }

        internal override bool requiresGeneration => true;

        [TextControl(null, true)]
        internal string Expression
        {
            get => TypedProvider.Expression;
            set
            {
                if (value == null) // Text control can misbehave in undo redo scenarios; we'll need to visit that separately.
                    return;
                TypedProvider.UpdateExpression(hlslFunctionName, value, TypedProvider.ShaderType, s_keywordFilter);
                Refresh();
            }
        }

        public ExpressionNode() { }

        public override void UpdateNodeAfterDeserialization()
        {
            // Ensure the keyword filter is in place before base calls Refresh() → Provider.Reload().
            // Handles fresh nodes and graphs saved before [SerializeReference] was introduced.
            TypedProvider?.UpdateExpression(hlslFunctionName, TypedProvider.Expression, TypedProvider.ShaderType, s_keywordFilter);
            base.UpdateNodeAfterDeserialization();
        }

        static SupportedTypes FromTypeName(string stype)
        {
            switch (stype)
            {
                default:
                case "float": return SupportedTypes.Vector1;
                case "float2": return SupportedTypes.Vector2;
                case "float3": return SupportedTypes.Vector3;
                case "float4": return SupportedTypes.Vector4;
            }
        }

        static string ToTypeName(SupportedTypes etype)
        {
            switch (etype)
            {
                default:
                case SupportedTypes.Vector1: return "float";
                case SupportedTypes.Vector2: return "float2";
                case SupportedTypes.Vector3: return "float3";
                case SupportedTypes.Vector4: return "float4";
            }
        }
    }
}
