using System;

namespace UnityEditor.ShaderAnalysis
{
    [Flags]
    public enum BuildReportFeature
    {
        GenerateVariantKeywords = 1 << 0,
        StaticAnalysis = 1 << 1,
    }
}
