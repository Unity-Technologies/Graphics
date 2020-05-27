namespace UnityEditor.ShaderAnalysis
{
    public interface IReportExport
    {
        void Export(ShaderBuildReport report, string filePath);
    }
}
