using System;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    static class StencilExtensions
    {
        public static string CompFuncToShaderLabString(CompareFunction compareFunction)
        {
            switch (compareFunction)
            {
                case CompareFunction.Never: return "Never";
                case CompareFunction.Less: return "Less";
                case CompareFunction.Equal: return "Equal";
                case CompareFunction.LessEqual: return "LEqual";
                case CompareFunction.Greater: return "Greater";
                case CompareFunction.NotEqual: return "NotEqual";
                case CompareFunction.GreaterEqual: return "GEqual";
                case CompareFunction.Always: return "Always";
                default:
                    throw new ArgumentOutOfRangeException(nameof(compareFunction), compareFunction, null);
            }
        }

        public static string StencilOpToShaderLabString(StencilOp stencilOp)
        {
            switch (stencilOp)
            {
                case StencilOp.Keep: return "Keep";
                case StencilOp.Zero: return "Zero";
                case StencilOp.Replace: return "Replace";
                case StencilOp.IncrementSaturate: return "IncrSat";
                case StencilOp.DecrementSaturate: return "DecrSat";
                case StencilOp.Invert: return "Invert";
                case StencilOp.IncrementWrap: return "IncrWrap";
                case StencilOp.DecrementWrap: return "DecrWrap";
                default:
                    throw new ArgumentOutOfRangeException(nameof(stencilOp), stencilOp, null);
            }
        }

        public static string ToShaderString(this StencilDescriptor descriptor)
        {
            ShaderStringBuilder builder = new ShaderStringBuilder();
            builder.AppendLine("Stencil");
            using (builder.BlockScope())
            {
                string compOverride = string.IsNullOrEmpty(descriptor.Comp) ? (string.IsNullOrEmpty(descriptor.CompBack) ? null : descriptor.CompBack) : (string.IsNullOrEmpty(descriptor.CompBack) ? descriptor.Comp : null);
                string passOverride = string.IsNullOrEmpty(descriptor.Pass) ? (string.IsNullOrEmpty(descriptor.PassBack) ? null : descriptor.PassBack) : (string.IsNullOrEmpty(descriptor.PassBack) ? descriptor.Pass : null);
                string failOverride = string.IsNullOrEmpty(descriptor.Fail) ? (string.IsNullOrEmpty(descriptor.FailBack) ? null : descriptor.FailBack) : (string.IsNullOrEmpty(descriptor.FailBack) ? descriptor.Fail : null);
                string zFailOverride = string.IsNullOrEmpty(descriptor.ZFail) ? (string.IsNullOrEmpty(descriptor.ZFailBack) ? null : descriptor.ZFailBack) : (string.IsNullOrEmpty(descriptor.ZFailBack) ? descriptor.ZFail : null);

                if (!string.IsNullOrEmpty(descriptor.Ref))
                    builder.AppendLine($"Ref {descriptor.Ref}");

                if (!string.IsNullOrEmpty(descriptor.ReadMask))
                    builder.AppendLine($"ReadMask {descriptor.ReadMask}");

                if (!string.IsNullOrEmpty(descriptor.WriteMask))
                    builder.AppendLine($"WriteMask {descriptor.WriteMask}");

                if (compOverride != null)
                    builder.AppendLine($"Comp {compOverride}");
                else
                {
                    if (!string.IsNullOrEmpty(descriptor.Comp))
                        builder.AppendLine($"CompFront {descriptor.Comp}");

                    if (!string.IsNullOrEmpty(descriptor.CompBack))
                        builder.AppendLine($"CompBack {descriptor.CompBack}");
                }

                if (passOverride != null)
                    builder.AppendLine($"Pass {passOverride}");
                else
                {
                    if (!string.IsNullOrEmpty(descriptor.Pass))
                        builder.AppendLine($"PassFront {descriptor.Pass}");

                    if (!string.IsNullOrEmpty(descriptor.PassBack))
                        builder.AppendLine($"PassBack {descriptor.PassBack}");
                }

                if (failOverride != null)
                    builder.AppendLine($"Fail {failOverride}");
                else
                {
                    if (!string.IsNullOrEmpty(descriptor.Fail))
                        builder.AppendLine($"FailFront {descriptor.Fail}");

                    if (!string.IsNullOrEmpty(descriptor.FailBack))
                        builder.AppendLine($"FailBack {descriptor.FailBack}");
                }

                if (zFailOverride != null)
                    builder.AppendLine($"ZFail {zFailOverride}");
                else
                {
                    if (!string.IsNullOrEmpty(descriptor.ZFail))
                        builder.AppendLine($"ZFailFront {descriptor.ZFail}");

                    if (!string.IsNullOrEmpty(descriptor.ZFailBack))
                        builder.AppendLine($"ZFailBack {descriptor.ZFailBack}");
                }
            }
            return builder.ToCodeBlock();
        }
    }
}
