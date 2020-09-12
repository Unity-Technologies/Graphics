using System.IO;

namespace UnityEditor.ShaderAnalysis.Internal
{
    [ReportExport("CSV", "csv")]
    class CSVReportExport : IReportExport
    {
        public void Export(ShaderBuildReport report, string filePath)
        {
            var targetFile = new FileInfo(filePath);

            var dir = targetFile.Directory;
            if (!dir.Exists)
                dir.Create();

            if (targetFile.Exists)
                targetFile.Delete();

            using (var stream = new FileStream(targetFile.FullName, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new StreamWriter(stream))
            {
                // Write header

                for (var i = 0; i < ExportFormat.Columns.Length; ++i)
                {
                    writer.Write('"');
                    writer.Write(ExportFormat.Columns[i].name);
                    writer.Write('"');
                    writer.Write(';');
                }
                writer.WriteLine();

                for (var i = 0; i < report.programs.Count; i++)
                {
                    var po = report.programs[i];
                    foreach (var cu in po.compileUnits)
                    {
                        var pu = cu.performanceUnit;
                        if (pu == null)
                            continue;

                        var p = pu.parsedReport;

                        for (var k = 0; k < ExportFormat.Columns.Length; k++)
                        {
                            var escape = ExportFormat.Columns[k].type == typeof(string);

                            if (escape)
                                writer.Write('"');
                            writer.Write(ExportFormat.Columns[k].getter(report, po, cu, pu, p));
                            if (escape)
                                writer.Write('"');
                            writer.Write(';');
                        }
                        writer.WriteLine();
                    }
                }
            }
        }
    }
}
