namespace UnityEditor.ShaderGraph
{
    internal static class PrecisionUtil
    {
        internal const string Token = "$precision";

        internal static string ToShaderString(this ConcretePrecision precision)
        {
            switch(precision)
            {
                case ConcretePrecision.Float:
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
                case Precision.Float:
                    return ConcretePrecision.Float;
                case Precision.Half:
                    return ConcretePrecision.Half;
                default:
                    return ConcretePrecision.Float;
            }
        }
    }
}
