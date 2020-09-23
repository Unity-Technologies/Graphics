using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    internal static class PrecisionUtil
    {
        internal const string Token = "$precision";

        internal static string ToShaderString(this ConcretePrecision precision)
        {
            switch(precision)
            {
                case ConcretePrecision.Single:
                    return "float";
                case ConcretePrecision.Half:
                    return "half";
                default:
                    return "float";
            }
        }

        internal static ConcretePrecision ToConcrete(this Precision precision)
        {
            switch(precision)
            {
                case Precision.Single:
                    return ConcretePrecision.Single;
                case Precision.Half:
                    return ConcretePrecision.Half;
                default:
                    return ConcretePrecision.Single;
            }
        }
    }
}
