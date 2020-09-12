namespace UnityEditor.ShaderAnalysis
{
    public interface IReportDiffExport
    {
        void Export(ShaderBuildReportDiff diff, string filePath);
    }
}
