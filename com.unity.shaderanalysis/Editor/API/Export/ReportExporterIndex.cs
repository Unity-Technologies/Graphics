namespace UnityEditor.ShaderAnalysis
{
    public struct ReportExporterIndex
    {
        int m_Value;

        public static explicit operator int(in ReportExporterIndex v) => v.m_Value;
        public static explicit operator ReportExporterIndex(in int v) => new ReportExporterIndex { m_Value = v };
    }
}
