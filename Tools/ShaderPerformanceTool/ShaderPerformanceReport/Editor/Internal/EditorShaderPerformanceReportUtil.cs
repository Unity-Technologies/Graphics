using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.ShaderTools.Internal
{
    public static class EditorShaderPerformanceReportUtil
    {
        ////#pragma target 4.5
        static readonly Regex k_RegexShaderModel = new Regex(@"^[\s\r]*#pragma\s+target\s+([\d\.]+)\s+[\s\r]*$");
        static readonly Regex k_RegexMultiCompile = new Regex(@"^[\s\r]*(#pragma\s+multi_compile\s+(?:([\w_]+)\s+)+)[\s\r]*$");
        static readonly Regex k_RegexPragmaKernel = new Regex(@"#pragma kernel ([\w_]+)(?:\s+(\S+))*$");

        static Dictionary<string, Dictionary<BuildTarget, AssetMetadata>> s_AssetMetadatas = new Dictionary<string, Dictionary<BuildTarget, AssetMetadata>>();

        #region Folder Layout

        public static DirectoryInfo GetTemporaryDirectory(Object asset, BuildTarget target)
        {
            var guid = CalculateGUIDFor(asset);
            return GetTemporaryDirectory(guid, target);
        }

        public static DirectoryInfo GetTemporaryDirectory(string guid, BuildTarget target)
        {
            return new DirectoryInfo(string.Format("Temp/ShaderPerformanceReports/{0}/{1}", guid, target));
        }

        public static FileInfo GetTemporaryProgramSourceCodeFile(DirectoryInfo folder, int variantNumber)
        {
            return new FileInfo(Path.Combine(folder.FullName, string.Format("Variant{0:D3}.shader", variantNumber)));
        }

        public static FileInfo GetTemporaryProgramCompiledFile(FileInfo sourceFile, DirectoryInfo genDir, string multicompile)
        {
            return new FileInfo(Path.Combine(genDir.FullName, string.Format("{0}.{1}.sb", Path.GetFileNameWithoutExtension(sourceFile.Name), multicompile)));
        }

        public static FileInfo GetTemporaryDisassemblyFile(FileInfo binaryFile)
        {
            return new FileInfo(
                Path.Combine(
                    binaryFile.DirectoryName,
                    Path.GetFileNameWithoutExtension(binaryFile.Name) + ".disassemble.txt"
                )
            );
        }

        public static FileInfo GetTemporaryPerformanceReportFile(FileInfo binaryFile)
        {
            return new FileInfo(
                Path.Combine(
                    binaryFile.DirectoryName,
                    Path.GetFileNameWithoutExtension(binaryFile.Name) + ".perf.txt"
                )
            );
        }

        public static FileInfo GetTemporaryExcelReportFile(Object asset, BuildTarget target)
        {
            return new FileInfo(Path.Combine(
                GetTemporaryDirectory(asset, target).FullName,
                Path.GetFileNameWithoutExtension(asset.name) + ".perf.xlsx"
            ));
        }

        public static FileInfo GetTemporaryExcelDiffFile(string assetGUID, BuildTarget target)
        {
            return new FileInfo(Path.Combine(
                GetTemporaryDirectory(assetGUID, target).FullName,
                "Diff.xlsx"
            ));
        }

        public static FileInfo GetTemporaryCSVReportFile(Object asset, BuildTarget target)
        {
            return new FileInfo(Path.Combine(
                GetTemporaryDirectory(asset, target).FullName,
                Path.GetFileNameWithoutExtension(asset.name) + ".perf.csv"
            ));
        }

        public static string CalculateGUIDFor(Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var guid = string.IsNullOrEmpty(assetPath)
                ? asset.name
                : AssetDatabase.AssetPathToGUID(assetPath);
            return guid;
        }

        public static FileInfo GetWorkingAssetMetaDataBaseFile(DirectoryInfo sourceDir)
        {
            var fileInfo = new FileInfo(Path.Combine(sourceDir.FullName, "AssetMetaDataBase.asset"));
            return fileInfo;
        }
        #endregion

        #region Shader Code

        public static void ParseVariantMultiCompiles(string variantBody, List<HashSet<string>> multicompiles)
        {
            var lines = variantBody.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var m = k_RegexMultiCompile.Match(lines[i]);
                if (m.Success)
                {
                    var entry = new HashSet<string>();
                    multicompiles.Add(entry);

                    var captures = m.Groups[2].Captures;
                    for (var j = 0; j < captures.Count; j++)
                        entry.Add(captures[j].Value);
                }
            }
        }

        public static void ParseShaderModel(string sourceCode, ref string shaderModel)
        {
            var lines = sourceCode.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var m = k_RegexShaderModel.Match(lines[i]);
                if (m.Success)
                {
                    shaderModel = m.Groups[1].Value.Replace(".", string.Empty);
                    break;
                }
            }
        }

        public static IEnumerator BuildDefinesFromMultiCompiles(List<HashSet<string>> multicompiles, List<HashSet<string>> defines)
        {
            if (multicompiles.Count == 0)
            {
                defines.Add(new HashSet<string>());
                yield break;
            }

            var mc = new string[multicompiles.Count][];
            for (var i = 0; i < mc.Length; i++)
            {
                mc[i] = new string[multicompiles[i].Count];
                multicompiles[i].CopyTo(mc[i]);
            }

            // current multicompile index
            var indices = new int[multicompiles.Count];

            while (true)
            {
                // Add an entry with current indices
                var entry = new HashSet<string>();
                defines.Add(entry);

                for (var i = 0; i < indices.Length; i++)
                {
                    var token = mc[i][indices[i]];
                    if (token == "_")
                        continue;
                    entry.Add(token);
                }

                yield return defines.Count;

                var incrementIndex = indices.Length - 1;
                while (true)
                {
                    // increment last index
                    ++indices[incrementIndex];
                    if (indices[incrementIndex] >= multicompiles[incrementIndex].Count)
                    {
                        // We have done all multicompiles at this index
                        // So increment previous indices
                        indices[incrementIndex] = 0;
                        --incrementIndex;
                    }
                    else
                        break;

                    // Loop complete
                    if (incrementIndex < 0)
                        break;
                }

                // Loop complete
                if (incrementIndex < 0)
                    break;
            }
        }

        public static void ParseComputeShaderKernels(string computeBody, Dictionary<string, HashSet<string>> kernels)
        {
            var lines = computeBody.Split('\n', '\r');
            for (var i = 0; i < lines.Length; i++)
            {
                var m = k_RegexPragmaKernel.Match(lines[i]);
                if (m.Success)
                {
                    var defines = new HashSet<string>();
                    
                    for (var j = 0; j < m.Groups[2].Captures.Count; j++)
                        defines.Add(m.Groups[2].Captures[j].Value);

                    kernels.Add(m.Groups[1].Value, defines);
                }
            }
        }

        #endregion

        #region Asset Metadata
        static readonly DirectoryInfo k_WorkingAssetMetadataFolder = new DirectoryInfo("Library/ShaderPerformanceReports");

        public static AssetMetadata LoadAssetMetadatasFor(BuildTarget target, DirectoryInfo rootFolder = null)
        {
            rootFolder = rootFolder ?? k_WorkingAssetMetadataFolder;

            var rootHash = rootFolder.FullName.Replace("\\\\", "/");
            Dictionary<BuildTarget, AssetMetadata> metadatabase;
            if (!s_AssetMetadatas.TryGetValue(rootHash, out metadatabase))
            {
                metadatabase = new Dictionary<BuildTarget, AssetMetadata>();
                s_AssetMetadatas[rootHash] = metadatabase;
            }

            if (metadatabase.ContainsKey(target))
                return metadatabase[target];

            AssetMetadata result = null;
            var file = GetAssetMetadataFileFor(target, rootFolder);
            if (file.Exists)
            {
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(file.FullName);
                if (objs.Length > 0)
                {
                    result = objs[0] as AssetMetadata;
                    result.OnAfterDeserialize();
                }
            }
            if (result == null)
            {
                result = ScriptableObject.CreateInstance<AssetMetadata>();
                result.target = target;
            }

            metadatabase[target] = result;

            return result;
        }

        public static void SaveAssetMetadata(AssetMetadata metadatas, DirectoryInfo rootFolder = null)
        {
            Assert.IsNotNull(metadatas);

            rootFolder = rootFolder ?? k_WorkingAssetMetadataFolder;

            var file = GetAssetMetadataFileFor(metadatas.target, rootFolder);
            if (!file.Directory.Exists)
                file.Directory.Create();
            if (file.Exists)
                file.Delete();
            InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { metadatas }, file.FullName, true);
        }

        static FileInfo GetAssetMetadataFileFor(BuildTarget target, DirectoryInfo rootFolder)
        {
            return new FileInfo(Path.Combine(rootFolder.FullName, string.Format("AssetMetaData_{0}.asset", target)));
        }
        #endregion

        #region Perf utils

        struct PerfExportFormat
        {
            internal Type type;
            internal string name;
            internal Getter getter;
            internal DiffMethod diffMethod;
            internal double colWidth;
            internal ExcelHorizontalAlignment horizontalAlignment;

            public PerfExportFormat(string name, Type type, Getter getter, DiffMethod diffMethod, double colWidth, ExcelHorizontalAlignment horizontalAlignment)
            {
                this.name = name;
                this.getter = getter;
                this.colWidth = colWidth;
                this.horizontalAlignment = horizontalAlignment;
                this.type = type;
                this.diffMethod = diffMethod;
            }
        }

        delegate object Getter(
            ShaderBuildReport report, 
            ShaderBuildReport.GPUProgram po, 
            ShaderBuildReport.CompileUnit cu,
            ShaderBuildReport.PerformanceUnit pu,
            ShaderBuildReport.ProgramPerformanceMetrics p);

        delegate object DiffMethod(object l, object r);

        static readonly PerfExportFormat[] k_PerfsFormats =
        {
            new PerfExportFormat("Pass Name", typeof(string), (r, po, cu, pu, p) => po.name, DiffFirst, -1, ExcelHorizontalAlignment.Left),
            new PerfExportFormat("Multicompile index", typeof(int), (r, po, cu, pu, p) => cu.multicompileIndex, DiffNull, 13, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("All Defines", typeof(string), (r, po, cu, pu, p) => string.Join("\r\n", cu.defines), DiffFirst, -1, ExcelHorizontalAlignment.Left),
            new PerfExportFormat("Multicompile Defines", typeof(string), (r, po, cu, pu, p) => string.Join("\r\n", cu.multicompileDefines), DiffFirst, -1, ExcelHorizontalAlignment.Left),
            new PerfExportFormat("Micro Code Size (byte)", typeof(int), (r, po, cu, pu, p) => p.microCodeSize, DiffIntSub, 11, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("VGPR count", typeof(int), (r, po, cu, pu, p) => p.VGPRCount, DiffIntSub, -1, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("VGPR Used Count", typeof(int), (r, po, cu, pu, p) => p.VGPRUsedCount, DiffIntSub, -1, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("SGPR Count", typeof(int), (r, po, cu, pu, p) => p.SGPRCount, DiffIntSub, -1, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("SGPR Used Count", typeof(int), (r, po, cu, pu, p) => p.SGPRUsedCount, DiffIntSub, -1, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("User SGPR Count", typeof(int), (r, po, cu, pu, p) => p.UserSGPRCount, DiffIntSub, -1, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("LDS Size", typeof(int), (r, po, cu, pu, p) => p.LDSSize, DiffIntSub, -1, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("Thread Group Waves", typeof(int), (r, po, cu, pu, p) => p.threadGroupWaves, DiffIntSub, -1, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("SIMD Occupancy Count", typeof(int), (r, po, cu, pu, p) => p.SIMDOccupancyCount, DiffIntSub, 11, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("SIMD Occupancy Max", typeof(int), (r, po, cu, pu, p) => p.SIMDOccupancyMax, DiffIntSub, 11, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("SIMD Occupancy %", typeof(float), (r, po, cu, pu, p) => (float)p.SIMDOccupancyCount / (float)p.SIMDOccupancyMax, DiffFloatDiv, 11, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("CU Occupancy Count", typeof(int), (r, po, cu, pu, p) => p.CUOccupancyCount, DiffIntSub, 11, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("CU Occupancy Max", typeof(int), (r, po, cu, pu, p) => p.CUOccupancyMax, DiffIntSub, 11, ExcelHorizontalAlignment.Center),
            new PerfExportFormat("CU Occupancy %", typeof(float), (r, po, cu, pu, p) => (float)p.CUOccupancyCount / (float)p.CUOccupancyMax, DiffFloatDiv, 11, ExcelHorizontalAlignment.Center),
        };

        static object DiffNull(object l, object r) { return string.Empty; }
        static object DiffFirst(object l, object r) { return l; }
        static object DiffSecond(object l, object r) { return r; }
        static object DiffIntSub(object l, object r) { return (int)l - (int)r; }
        static object DiffFloatDiv(object l, object r) { return (float)l/(float)r; }

        public static void ExportPerfsToExcel(ShaderBuildReport report, FileInfo file)
        {
            var dir = file.Directory;
            if (!dir.Exists)
                dir.Create();

            if (file.Exists)
                file.Delete();

            using (var xls = new ExcelPackage(file))
            {
                var worksheet = xls.Workbook.Worksheets["Shader Performance"] ?? xls.Workbook.Worksheets.Add("Shader Performance");
                FillExcelWorksheetWithReport(worksheet, report);

                xls.Save();
            }
        }

        public static void ExportDiffPerfsToExcel(FileInfo file, ShaderBuildReportDiff diff)
        {
            var dir = file.Directory;
            if (!dir.Exists)
                dir.Create();

            if (file.Exists)
                file.Delete();

            using (var xls = new ExcelPackage(file))
            {
                var diffWorksheet = xls.Workbook.Worksheets["Diff"] ?? xls.Workbook.Worksheets.Add("Diff");
                var sourceWorksheet = xls.Workbook.Worksheets["Source"] ?? xls.Workbook.Worksheets.Add("Source");
                var refWorksheet = xls.Workbook.Worksheets["Reference"] ?? xls.Workbook.Worksheets.Add("Reference");
                FillExcelWorksheetWithReport(refWorksheet, diff.reference);
                FillExcelWorksheetWithReport(sourceWorksheet, diff.source);
                FillExcelWorksheetWithDiff(diffWorksheet, diff);

                xls.Save();
            }
        }

        public static void ExportPerfsToCSV(ShaderBuildReport report, FileInfo targetFile)
        {
            var dir = targetFile.Directory;
            if (!dir.Exists)
                dir.Create();

            if (targetFile.Exists)
                targetFile.Delete();

            using (var stream = new FileStream(targetFile.FullName, FileMode.Create, FileAccess.ReadWrite))
            using (var writer = new StreamWriter(stream))
            {
                // Write header

                for (var i = 0; i < k_PerfsFormats.Length; ++i)
                {
                    writer.Write('"');
                    writer.Write(k_PerfsFormats[i].name);
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

                        for (var k = 0; k < k_PerfsFormats.Length; k++)
                        {
                            var escape = k_PerfsFormats[k].type == typeof(string);

                            if (escape)
                                writer.Write('"');
                            writer.Write(k_PerfsFormats[k].getter(report, po, cu, pu, p));
                            if (escape)
                                writer.Write('"');
                            writer.Write(';');
                        }
                        writer.WriteLine();
                    }
                }
            }
        }

        static void FillExcelWorksheetWithDiff(ExcelWorksheet worksheet, ShaderBuildReportDiff data)
        {
            worksheet.Cells.Clear();
            worksheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Style header
            var headerRange = worksheet.Cells[1, 1, 1, k_PerfsFormats.Length];
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.Indexed = 21;
            headerRange.Style.Font.Color.Indexed = 1;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.WrapText = true;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Height = 75;

            for (var i = 0; i < k_PerfsFormats.Length; i++)
                worksheet.Cells[1, i + 1].Value = k_PerfsFormats[i].name;

            for (var i = 0; i < data.perfDiffs.Count; i++)
            {
                var row = i + 2;
                var diff = data.perfDiffs[i];

                var sr = data.source;
                var rr = data.reference;
                var spo = data.source.programs[diff.sourceProgramIndex];
                var rpo = data.reference.programs[diff.refProgramIndex];
                var scu = spo.GetCompileUnit(diff.sourceMultiCompileIndex);
                var rcu = rpo.GetCompileUnit(diff.refMultiCompileIndex);
                var spu = scu.performanceUnit;
                var rpu = rcu.performanceUnit;
                var sp = spu.parsedReport;
                var rp = rpu.parsedReport;

                for (var k = 0; k < k_PerfsFormats.Length; k++)
                {
                    var c = worksheet.Cells[row, k + 1];
                    var l = k_PerfsFormats[k].getter(sr, spo, scu, spu, sp);
                    var r = k_PerfsFormats[k].getter(rr, rpo, rcu, rpu, rp);
                    var d = k_PerfsFormats[k].diffMethod(l, r);
                    c.Value = d;
                    c.Style.HorizontalAlignment = k_PerfsFormats[k].horizontalAlignment;
                }

                worksheet.Row(row).Height = scu.defines.Length * 15;
            }

            var tableName = (worksheet.Name + " Values").Replace(" ", string.Empty);
            var table = worksheet.Tables[tableName];
            if (table != null)
                worksheet.Tables.Delete(tableName);

            var tableRange = worksheet.Cells[1, 1, data.perfDiffs.Count + 2, k_PerfsFormats.Length];
            worksheet.Tables.Add(tableRange, tableName);

            // Sizing
            worksheet.Cells.AutoFitColumns();
            worksheet.DefaultRowHeight = 22;

            for (var i = 0; i < k_PerfsFormats.Length; i++)
            {
                if (k_PerfsFormats[i].colWidth > 0)
                    worksheet.Column(i + 1).Width = k_PerfsFormats[i].colWidth;
            }
        }

        static void FillExcelWorksheetWithReport(ExcelWorksheet worksheet, ShaderBuildReport report)
        {
            worksheet.Cells.Clear();
            worksheet.Cells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            // Style header
            var headerRange = worksheet.Cells[1, 1, 1, k_PerfsFormats.Length];
            headerRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            headerRange.Style.Fill.BackgroundColor.Indexed = 21;
            headerRange.Style.Font.Color.Indexed = 1;
            headerRange.Style.Font.Bold = true;
            headerRange.Style.WrapText = true;
            headerRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            headerRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Row(1).Height = 75;

            for (var i = 0; i < k_PerfsFormats.Length; i++)
                worksheet.Cells[1, i + 1].Value = k_PerfsFormats[i].name;

            var row = 2;
            for (var i = 0; i < report.programs.Count; i++)
            {
                var po = report.programs[i];
                foreach (var cu in po.compileUnits)
                {
                    var pu = cu.performanceUnit;
                    if (pu == null)
                        continue;

                    var p = pu.parsedReport;

                    for (var k = 0; k < k_PerfsFormats.Length; k++)
                    {
                        var c = worksheet.Cells[row, k + 1];
                        c.Value = k_PerfsFormats[k].getter(report, po, cu, pu, p);
                        c.Style.HorizontalAlignment = k_PerfsFormats[k].horizontalAlignment;
                    }

                    worksheet.Row(row).Height = cu.defines.Length * 15;

                    ++row;
                }
            }

            var tableName = (worksheet.Name + " Values").Replace(" ", string.Empty);
            var table = worksheet.Tables[tableName];
            if (table != null)
                worksheet.Tables.Delete(tableName);

            worksheet.Tables.Add(worksheet.Cells[1, 1, row, k_PerfsFormats.Length], tableName);

            // Sizing
            worksheet.Cells.AutoFitColumns();
            worksheet.DefaultRowHeight = 22;

            for (var i = 0; i < k_PerfsFormats.Length; i++)
            {
                if (k_PerfsFormats[i].colWidth > 0)
                    worksheet.Column(i + 1).Width = k_PerfsFormats[i].colWidth;
            }
        }
        #endregion

        #region Basic Utils
        public static string Join(this HashSet<string> strings, string separator)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var s in strings)
            {
                if (!first)
                    builder.Append(separator);
                first = false;
                builder.Append(s);
            }

            return builder.ToString();
        }

        public static string Join(this List<string> strings, string separator)
        {
            if (strings == null)
                return string.Empty;

            var builder = new StringBuilder();
            var first = true;
            foreach (var s in strings)
            {
                if (!first)
                    builder.Append(separator);
                first = false;
                builder.Append(s);
            }

            return builder.ToString();
        }
        #endregion

        public class ShaderBuildReportDiff
        {
            public class PerfDiff
            {
                public string programName;
                public string[] defines;
                public string[] multicompiles;
                public int sourceProgramIndex;
                public int sourceMultiCompileIndex;
                public int refProgramIndex;
                public int refMultiCompileIndex;
                public ShaderBuildReport.ProgramPerformanceMetrics metrics;
            }

            public ShaderBuildReport source;
            public ShaderBuildReport reference;
            public List<PerfDiff> perfDiffs = new List<PerfDiff>();
        }

        public static ShaderBuildReportDiff DiffReports(ShaderBuildReport source, ShaderBuildReport reference)
        {
            var diff = new ShaderBuildReportDiff
            {
                source = source,
                reference = reference,
            };

            var spos = source.programs;
            for (var i = 0; i < spos.Count; i++)
            {
                var spo = spos[i];
                var rpo = reference.GetProgramByName(spo.name);

                if (rpo == null)
                    continue;

                foreach (var scu in spo.compileUnits)
                {
                    var rcu = rpo.GetCompileUnitByDefines(scu.defines);
                    if (rcu == null)
                        continue;

                    var spu = scu.performanceUnit;
                    var rpu = rcu.performanceUnit;

                    if (spu == null || rpu == null)
                        continue;

                    var perfdiff = new ShaderBuildReportDiff.PerfDiff
                    {
                        programName = spo.name,
                        defines = scu.defines,
                        multicompiles = scu.multicompileDefines,
                        metrics = ShaderBuildReport.ProgramPerformanceMetrics.Diff(spu.parsedReport, rpu.parsedReport),
                        sourceMultiCompileIndex = scu.multicompileIndex,
                        sourceProgramIndex = scu.programIndex,
                        refMultiCompileIndex = rcu.multicompileIndex,
                        refProgramIndex = rcu.programIndex
                    };
                    diff.perfDiffs.Add(perfdiff);
                }
            }

            return diff;
        }
    }
}