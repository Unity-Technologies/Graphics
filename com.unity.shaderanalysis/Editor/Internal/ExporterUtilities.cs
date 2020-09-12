using System;
using System.IO;
using System.Linq;

namespace UnityEditor.ShaderAnalysis.Internal
{
    public static class ExporterUtilities
    {
        struct ReportExporterInfo
        {
            public string name;
            public string extension;
        }

        struct ReportDiffExporterInfo
        {
            public string name;
            public string extension;
        }

        static (ReportExporterInfo info, IReportExport exporter)[] s_ReportExporters;
        static (ReportDiffExporterInfo info, IReportDiffExport exporter)[] s_ReportDiffExporters;
        static string[] s_ReportExporterNames;
        static string[] s_ReportDiffExporterNames;

        static ExporterUtilities()
        {
            s_ReportExporters = TypeCache.GetTypesWithAttribute<ReportExportAttribute>()
                .Where(t => typeof(IReportExport).IsAssignableFrom(t))
                .Select(t =>
                {
                    var attr = (ReportExportAttribute)t.GetCustomAttributes(typeof(ReportExportAttribute), false)[0];
                    var instance = (IReportExport)Activator.CreateInstance(t);
                    return (new ReportExporterInfo { name = attr.name, extension = attr.extension }, instance);
                }).ToArray();

            s_ReportDiffExporters = TypeCache.GetTypesWithAttribute<ReportDiffExportAttribute>()
                .Where(t => typeof(IReportDiffExport).IsAssignableFrom(t))
                .Select(t =>
                {
                    var attr = (ReportDiffExportAttribute)t.GetCustomAttributes(typeof(ReportDiffExportAttribute), false)[0];
                    var instance = (IReportDiffExport)Activator.CreateInstance(t);
                    return (new ReportDiffExporterInfo { name = attr.name, extension = attr.extension }, instance);
                }).ToArray();

            s_ReportExporterNames = s_ReportExporters.Select(s => s.info.name).ToArray();
            s_ReportDiffExporterNames = s_ReportDiffExporters.Select(s => s.info.name).ToArray();
        }

        public static int ReportExporterCount => s_ReportExporters.Length;
        public static int ReportDiffExporterCount => s_ReportDiffExporters.Length;
        public static string[] ReportExporterNames => s_ReportExporterNames;
        public static string[] ReportDiffExporterNames => s_ReportDiffExporterNames;

        public static bool GetExporterIndex(string exporter, out ReportExporterIndex index)
        {
            for (int i = 0; i < s_ReportExporters.Length; i++)
            {
                if (s_ReportExporters[i].info.name == exporter)
                {
                    index = (ReportExporterIndex)i;
                    return true;
                }
            }

            index = default;
            return false;
        }

        public static bool GetDiffExporterIndex(string exporter, out ReportDiffExporterIndex index)
        {
            for (int i = 0; i < s_ReportDiffExporters.Length; i++)
            {
                if (s_ReportDiffExporters[i].info.name == exporter)
                {
                    index = (ReportDiffExporterIndex)i;
                    return true;
                }
            }

            index = default;
            return false;
        }

        public static string ChangeExtensionFor(ReportExporterIndex index, string file)
        {
            var extension = s_ReportExporters[(int)index].info.extension;
            return Path.ChangeExtension(file, extension);
        }

        public static string ChangeExtensionFor(ReportDiffExporterIndex index, string file)
        {
            var extension = s_ReportDiffExporters[(int)index].info.extension;
            return Path.ChangeExtension(file, extension);
        }

        public static void Export(ReportExporterIndex exporterIndex, ShaderBuildReport report, string filePath)
        {
            var exporter = s_ReportExporters[(int)exporterIndex].exporter;
            exporter.Export(report, filePath);
        }

        public static void ExportDiff(ReportDiffExporterIndex exporterIndex, ShaderBuildReportDiff report, string filePath)
        {
            var exporter = s_ReportDiffExporters[(int)exporterIndex].exporter;
            exporter.Export(report, filePath);
        }

        public static bool IsValid(ReportExporterIndex reportExporterIndex)
            => (int)reportExporterIndex >= 0 && (int)reportExporterIndex < s_ReportExporters.Length;

        public static bool IsValid(ReportDiffExporterIndex reportExporterIndex)
            => (int)reportExporterIndex >= 0 && (int)reportExporterIndex < s_ReportDiffExporters.Length;
    }
}
