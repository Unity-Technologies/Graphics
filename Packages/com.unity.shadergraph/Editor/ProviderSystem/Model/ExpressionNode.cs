
using System;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [Serializable]
    [ProviderModel(ExpressionProvider.kExpressionProviderKey)]
    internal class ExpressionNode : ProviderNode
    {
        ExpressionProvider TypedProvider => Provider as ExpressionProvider;

        string hlslFunctionName => $"ExpressionNode_{this.objectId}";

        enum SupportedTypes { Vector1, Vector2, Vector3, Vector4 }

        [EnumControl("Type")]
        SupportedTypes SelectedType {
            get => FromTypeName(TypedProvider.ShaderType);
            set
            {
                string typeName = ToTypeName(value);
                TypedProvider.UpdateExpression(hlslFunctionName, Expression, typeName);
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
                TypedProvider.UpdateExpression(hlslFunctionName, value, TypedProvider.ShaderType);
                Refresh();
            }
        }

        public ExpressionNode() { }

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
