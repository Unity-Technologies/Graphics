namespace UnityEditor.ShaderAnalysis
{
    public struct ReportDiffExporterIndex
    {
        int m_Value;

        public static explicit operator int(in ReportDiffExporterIndex v) => v.m_Value;
        public static explicit operator ReportDiffExporterIndex(in int v) => new ReportDiffExporterIndex { m_Value = v };
    }
}
