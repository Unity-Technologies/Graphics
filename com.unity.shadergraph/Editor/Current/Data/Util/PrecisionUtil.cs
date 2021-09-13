using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    internal static class PrecisionUtil
    {
        internal const string Token = "$precision";

        internal static string ToShaderString(this ConcretePrecision precision)
        {
            switch (precision)
            {
                case ConcretePrecision.Single:
                    return "float";
                case ConcretePrecision.Half:
                    return "half";
                default:
                    return "float";
            }
        }

        internal static string ToGenericString(this GraphPrecision precision)
        {
            switch (precision)
            {
                case GraphPrecision.Single:
                    return "float";
                case GraphPrecision.Half:
                    return "half";
                default:
                    return Token;
            }
        }

        internal static ConcretePrecision ToConcrete(this Precision precision, ConcretePrecision InheritPrecision, ConcretePrecision GraphPrecision)
        {
            switch (precision)
            {
                case Precision.Single:
                    return ConcretePrecision.Single;
                case Precision.Half:
                    return ConcretePrecision.Half;
                case Precision.Inherit:
                    return InheritPrecision;
                default:
                    return GraphPrecision;
            }
        }

        internal static GraphPrecision ToGraphPrecision(this Precision precision, GraphPrecision inheritPrecision)
        {
            switch (precision)
            {
                case Precision.Single:
                    return GraphPrecision.Single;
                case Precision.Half:
                    return GraphPrecision.Half;
                case Precision.Graph:
                    return GraphPrecision.Graph;
                default:
                    return inheritPrecision;
            }
        }

        internal static ConcretePrecision ToConcrete(this GraphPrecision precision, ConcretePrecision graphPrecision)
        {
            switch (precision)
            {
                case GraphPrecision.Single:
                    return ConcretePrecision.Single;
                case GraphPrecision.Half:
                    return ConcretePrecision.Half;
                default:
                    return graphPrecision;
            }
        }

        internal static GraphPrecision GraphFallback(this GraphPrecision precision, GraphPrecision graphFallback)
        {
            if (precision == GraphPrecision.Graph)
                return graphFallback;
            return precision;
        }
    }
}
