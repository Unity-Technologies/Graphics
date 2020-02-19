using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    [Serializable]
    public class ShaderBuildReport : ISerializationCallbackReceiver
    {
        class IndirectEnumerable<TType> : IEnumerable<TType>
        {
            List<TType> m_Source;
            Dictionary<int, int> m_Indices;

            public IndirectEnumerable(List<TType> source, Dictionary<int, int> indices)
            {
                m_Source = source;
                m_Indices = indices;
            }

            public IEnumerator<TType> GetEnumerator()
            {
                foreach (var index in m_Indices)
                {
                    if (index.Value < 0 || index.Value >= m_Source.Count)
                        continue;

                    yield return m_Source[index.Value];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Serializable]
        public class GPUProgram : ISerializationCallbackReceiver
        {
            [SerializeField]
            string m_Name;
            [SerializeField]
            string m_SourceCodeB64;
            [SerializeField]
            string[] m_Defines = new string[0];
            [SerializeField]
            DefineSet[] m_Multicompiles = new DefineSet[0];
            [SerializeField]
            DefineSet[] m_MulticompileCombinations = new DefineSet[0];
            [SerializeField]
            int m_CompileWarnings;
            [SerializeField]
            int m_CompileErrors;

            [NonSerialized]
            ShaderBuildReport m_Report;
            internal ShaderBuildReport report { get { return m_Report; } set { m_Report = value; } }

            string m_SourceCode;

            public DefineSet[] multicompileCombinations { get { return m_MulticompileCombinations; } }
            public DefineSet[] multicompiles { get { return m_Multicompiles; } }
            public string[] defines { get { return m_Defines; } }
            public string sourceCode { get { return m_SourceCode; } }
            public string name { get { return m_Name; } }
            public int compileErrors { get { return m_CompileErrors; } }
            public int compileWarnings { get { return m_CompileWarnings; } }

            public IEnumerable<CompileUnit> compileUnits { get { return report.GetCompileUnits(this); } }
            public IEnumerable<PerformanceUnit> performanceUnits { get { return report.GetPerformanceUnits(this); } }

            internal GPUProgram(string name, string sourceCode, string[] defines, DefineSet[] multicompiles, DefineSet[] multicompileCombinations)
            {
                m_Name = name;
                m_SourceCode = sourceCode;
                m_Defines = defines;
                m_Multicompiles = multicompiles;
                m_MulticompileCombinations = multicompileCombinations;
            }

            public CompileUnit AddCompileUnit(int multicompileIndex, string[] defines, LogItem[] warnings, LogItem[] errors, ShaderProfile profile, string entry)
            {
                m_CompileWarnings += warnings.Length;
                m_CompileErrors += errors.Length;

                return report.AddCompileUnit(this, multicompileIndex, defines, warnings, errors, profile, entry);
            }

            public PerformanceUnit AddPerformanceReport(int multicompileIndex, string rawReport, ProgramPerformanceMetrics parsedReport)
            {
                return report.AddPerformanceReport(this, multicompileIndex, rawReport, parsedReport);
            }

            public CompileUnit GetCompileUnit(int multicompileIndex)
            {
                return report.GetCompileUnit(this, multicompileIndex);
            }

            public PerformanceUnit GetPerformanceUnit(int multicompileIndex)
            {
                return report.GetPerformanceUnit(this, multicompileIndex);
            }

            public CompileUnit GetCompileUnitByDefines(string[] defines)
            {
                var hash = HashDefines(defines);
                foreach (var cu in compileUnits)
                {
                    var chash = HashDefines(cu.defines);
                    if (chash == hash)
                        return cu;
                }
                return null;
            }

            public void OnBeforeSerialize()
            {
                m_SourceCodeB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(m_SourceCode));
            }

            public void OnAfterDeserialize()
            {
                m_SourceCode = Encoding.UTF8.GetString(Convert.FromBase64String(m_SourceCodeB64));
            }
        }

        [Serializable]
        public class DefineSet
        {
            [SerializeField]
            string[] m_Defines = new string[0];
            public string[] defines { get { return m_Defines; } }

            public DefineSet(string[] defines)
            {
                m_Defines = defines;
            }

            public DefineSet(HashSet<string> defines)
            {
                m_Defines = defines.ToArray();
            }
        }

        [Serializable]
        public class CompileUnit
        {
            [SerializeField]
            int m_GPUProgramIndex;
            [SerializeField]
            int m_MultiCompileIndex;
            [SerializeField]
            string[] m_Defines = new string[0];
            [SerializeField]
            LogItem[] m_Warnings = new LogItem[0];
            [SerializeField]
            LogItem[] m_Errors = new LogItem[0];
            // TODO: This is specific for PS4
            // It should be stored in a different way
            [SerializeField]
            ShaderProfile m_Profile;
            [SerializeField]
            string m_Entry;

            [NonSerialized]
            ShaderBuildReport m_Owner;

            internal ShaderBuildReport owner { get { return m_Owner; } set { m_Owner = value; } }
            internal int programIndex { get { return m_GPUProgramIndex; } }
            internal int multicompileIndex { get { return m_MultiCompileIndex; } }

            public LogItem[] warnings { get { return m_Warnings; } }
            public LogItem[] errors { get { return m_Errors; } }
            public string[] defines { get { return m_Defines; } }
            public PerformanceUnit performanceUnit { get { return owner.GetPerformanceUnit(m_GPUProgramIndex, m_MultiCompileIndex); } }
            public GPUProgram program { get { return owner.m_Programs[m_GPUProgramIndex]; } }
            public ShaderProfile profile { get { return m_Profile; } }
            public string entry { get { return m_Entry; } }

            public string[] multicompileDefines
            {
                get
                {
                    if (m_GPUProgramIndex < 0 || m_GPUProgramIndex >= owner.m_Programs.Count)
                        return new string[0];
                    var program = owner.m_Programs[m_GPUProgramIndex];

                    if (m_MultiCompileIndex < 0 || m_MultiCompileIndex >= program.multicompileCombinations.Length)
                        return new string[0];

                    var multicompiles = program.multicompileCombinations[m_MultiCompileIndex];
                    return multicompiles.defines;
                }
            }

            internal CompileUnit(ShaderBuildReport owner, int programIndex, int multicompileIndex, string[] defines, LogItem[] warnings, LogItem[] errors, ShaderProfile profile, string entry)
            {
                m_GPUProgramIndex = programIndex;
                m_MultiCompileIndex = multicompileIndex;
                m_Defines = defines;
                m_Warnings = warnings;
                m_Errors = errors;
                m_Profile = profile;
                m_Entry = entry;

                this.owner = owner;
            }
        }

        [Serializable]
        public class PerformanceUnit : ISerializationCallbackReceiver
        {
            [SerializeField]
            int m_GPUProgramIndex;
            [SerializeField]
            int m_MultiCompileIndex;
            [SerializeField]
            string m_RawReportB64;
            [SerializeField]
            ProgramPerformanceMetrics m_ParsedReport;
            [NonSerialized]
            ShaderBuildReport m_Owner;

            internal ShaderBuildReport owner { get { return m_Owner; } set { m_Owner = value; } }
            internal int programIndex { get { return m_GPUProgramIndex; } }
            internal int multicompileIndex { get { return m_MultiCompileIndex; } }

            string m_RawReport;

            public ProgramPerformanceMetrics parsedReport { get { return m_ParsedReport; } }
            public string rawReport { get { return m_RawReport; } }
            public CompileUnit compileUnit { get { return owner.GetCompileUnit(m_GPUProgramIndex, m_MultiCompileIndex); } }
            public GPUProgram program { get { return owner.m_Programs[m_GPUProgramIndex]; } }

            internal PerformanceUnit(ShaderBuildReport owner, int programIndex, int multicompileIndex, string rawReport, ProgramPerformanceMetrics parsedReport)
            {
                this.owner = owner;

                m_GPUProgramIndex = programIndex;
                m_MultiCompileIndex = multicompileIndex;
                m_RawReport = rawReport;
                m_ParsedReport = parsedReport;
            }

            public void OnBeforeSerialize()
            {
                if (!string.IsNullOrEmpty(m_RawReport))
                    m_RawReportB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(m_RawReport));
            }

            public void OnAfterDeserialize()
            {
                if (!string.IsNullOrEmpty(m_RawReportB64))
                    m_RawReport = Encoding.UTF8.GetString(Convert.FromBase64String(m_RawReportB64));
            }
        }

        [Serializable]
        public struct LogItem
        {
            public string message;
            public string[] stacktrace;
        }


        [Serializable]
        public struct ProgramPerformanceMetrics
        {
            public int microCodeSize;
            public int VGPRCount;
            public int VGPRUsedCount;
            public int SGPRCount;
            public int SGPRUsedCount;
            public int UserSGPRCount;
            public int LDSSize;
            public int threadGroupWaves;
            public int SIMDOccupancyCount;
            public int SIMDOccupancyMax;
            public int CUOccupancyCount;
            public int CUOccupancyMax;

            public static ProgramPerformanceMetrics Diff(ProgramPerformanceMetrics l, ProgramPerformanceMetrics r)
            {
                return new ProgramPerformanceMetrics
                {
                    microCodeSize = l.microCodeSize - r.microCodeSize,
                    VGPRCount = l.VGPRCount - r.VGPRCount,
                    VGPRUsedCount = l.VGPRUsedCount - r.VGPRUsedCount,
                    SGPRCount = l.SGPRCount - r.SGPRCount,
                    SGPRUsedCount = l.SGPRUsedCount - r.SGPRUsedCount,
                    UserSGPRCount = l.UserSGPRCount - r.UserSGPRCount,
                    LDSSize = l.LDSSize - r.LDSSize,
                    threadGroupWaves = l.threadGroupWaves - r.threadGroupWaves,
                    SIMDOccupancyCount = l.SIMDOccupancyCount - r.SIMDOccupancyCount,
                    SIMDOccupancyMax = l.SIMDOccupancyMax - r.SIMDOccupancyMax,
                    CUOccupancyCount = l.CUOccupancyCount - r.CUOccupancyCount,
                    CUOccupancyMax = l.CUOccupancyMax - r.CUOccupancyMax
                };
            }
        }

        [SerializeField]
        List<GPUProgram> m_Programs = new List<GPUProgram>();
        [SerializeField]
        List<CompileUnit> m_CompileUnits = new List<CompileUnit>();
        [SerializeField]
        List<PerformanceUnit> m_PerformanceUnit = new List<PerformanceUnit>();

        [SerializeField]
        Dictionary<int, string> m_SkippedPasses = new Dictionary<int, string>();

        Dictionary<int, Dictionary<int, int>> m_ProgramCompileUnits = new Dictionary<int, Dictionary<int, int>>();
        Dictionary<int, Dictionary<int, int>> m_ProgramPerformanceUnits = new Dictionary<int, Dictionary<int, int>>();
        Dictionary<string, int> m_ProgramNameIndex = new Dictionary<string, int>();

        public IDictionary<int, string> skippedPasses { get { return m_SkippedPasses; } }
        public IList<GPUProgram> programs { get { return m_Programs; } }
        public IList<CompileUnit> compileUnits { get { return m_CompileUnits; } }
        public IList<PerformanceUnit> performanceUnits { get { return m_PerformanceUnit; } }

        public GPUProgram AddGPUProgram(string name, string sourceCode, string[] defines, DefineSet[] multicompiles, DefineSet[] multicompileCombinations)
        {
            var result = new GPUProgram(name, sourceCode, defines, multicompiles, multicompileCombinations);
            result.report = this;
            m_Programs.Add(result);
            m_ProgramNameIndex[name] = m_Programs.Count - 1;
            return result;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_Programs.Count; i++)
            {
                var program = m_Programs[i];
                program.report = this;
                m_ProgramNameIndex[program.name] = i;
            }

            for (var i = 0; i < m_PerformanceUnit.Count; i++)
            {
                var unit = m_PerformanceUnit[i];
                unit.owner = this;
                if (!m_ProgramCompileUnits.ContainsKey(unit.programIndex))
                    m_ProgramCompileUnits[unit.programIndex] = new Dictionary<int, int>();

                m_ProgramCompileUnits[unit.programIndex][unit.multicompileIndex] = i;
            }

            for (var i = 0; i < m_CompileUnits.Count; i++)
            {
                var unit = m_CompileUnits[i];
                unit.owner = this;
                if (!m_ProgramPerformanceUnits.ContainsKey(unit.programIndex))
                    m_ProgramPerformanceUnits[unit.programIndex] = new Dictionary<int, int>();

                m_ProgramPerformanceUnits[unit.programIndex][unit.multicompileIndex] = i;
            }
        }

        public void AddSkippedPass(int skippedPassIndex, string passName)
        {
            m_SkippedPasses[skippedPassIndex] = passName;
        }

        public GPUProgram GetProgramByName(string name)
        {
            int index;
            return m_ProgramNameIndex.TryGetValue(name, out index)
                ? m_Programs[index]
                : null;
        }

        CompileUnit AddCompileUnit(GPUProgram gpuProgram, int multicompileIndex, string[] defines, LogItem[] warnings, LogItem[] errors, ShaderProfile profile, string entry)
        {
            var programIndex = m_Programs.IndexOf(gpuProgram);

            var unit = new CompileUnit(this, programIndex, multicompileIndex, defines, warnings, errors, profile, entry);

            m_CompileUnits.Add(unit);

            if (!m_ProgramCompileUnits.ContainsKey(unit.programIndex))
                m_ProgramCompileUnits[unit.programIndex] = new Dictionary<int, int>();

            m_ProgramCompileUnits[unit.programIndex][unit.multicompileIndex] = m_CompileUnits.Count - 1;

            return unit;
        }

        PerformanceUnit AddPerformanceReport(GPUProgram gpuProgram, int multicompileIndex, string rawReport, ProgramPerformanceMetrics parsedReport)
        {
            var programIndex = m_Programs.IndexOf(gpuProgram);

            var unit = new PerformanceUnit(this, programIndex, multicompileIndex, rawReport, parsedReport);

            m_PerformanceUnit.Add(unit);

            if (!m_ProgramPerformanceUnits.ContainsKey(unit.programIndex))
                m_ProgramPerformanceUnits[unit.programIndex] = new Dictionary<int, int>();

            m_ProgramPerformanceUnits[unit.programIndex][unit.multicompileIndex] = m_PerformanceUnit.Count - 1;

            return unit;
        }

        TType GetUnit<TType>(Dictionary<int, Dictionary<int, int>> map, List<TType> source, int programIndex, int multicompileIndex)
            where TType : class
        {
            if (!map.ContainsKey(programIndex))
                return null;

            var indices = map[programIndex];
            int unitIndex;
            return indices.TryGetValue(multicompileIndex, out unitIndex)
                ? source[unitIndex]
                : null;
        }

        TType GetUnit<TType>(Dictionary<int, Dictionary<int, int>> map, List<TType> source, GPUProgram program, int multicompileIndex)
            where TType : class
        {
            var programIndex = m_Programs.IndexOf(program);

            return GetUnit(map, source, programIndex, multicompileIndex);
        }

        IEnumerable<TType> GetUnits<TType>(Dictionary<int, Dictionary<int, int>> map, List<TType> source, GPUProgram program)
            where TType : class
        {
            var programIndex = m_Programs.IndexOf(program);

            if (!map.ContainsKey(programIndex))
                return Enumerable.Empty<TType>();

            var indices = map[programIndex];
            return new IndirectEnumerable<TType>(source, indices);
        }

        PerformanceUnit GetPerformanceUnit(GPUProgram gpuProgram, int multicompileIndex)
        {
            return GetUnit(m_ProgramPerformanceUnits, m_PerformanceUnit, gpuProgram, multicompileIndex);
        }

        PerformanceUnit GetPerformanceUnit(int programIndex, int multicompileIndex)
        {
            return GetUnit(m_ProgramPerformanceUnits, m_PerformanceUnit, programIndex, multicompileIndex);
        }

        IEnumerable<PerformanceUnit> GetPerformanceUnits(GPUProgram gpuProgram)
        {
            return GetUnits(m_ProgramPerformanceUnits, m_PerformanceUnit, gpuProgram);
        }

        CompileUnit GetCompileUnit(GPUProgram gpuProgram, int multicompileIndex)
        {
            return GetUnit(m_ProgramCompileUnits, m_CompileUnits, gpuProgram, multicompileIndex);
        }

        CompileUnit GetCompileUnit(int programIndex, int multicompileIndex)
        {
            return GetUnit(m_ProgramCompileUnits, m_CompileUnits, programIndex, multicompileIndex);
        }

        IEnumerable<CompileUnit> GetCompileUnits(GPUProgram gpuProgram)
        {
            return GetUnits(m_ProgramCompileUnits, m_CompileUnits, gpuProgram);
        }

        static int HashDefines(string[] defines)
        {
            if (defines == null || defines.Length == 0)
                return 0;

            var hash = 0;
            for (var i = 0; i < defines.Length; i++)
                hash += defines[i].GetHashCode();

            return hash;
        }
    }

    public static class ShaderBuildReportExtensions
    {
        public static void OpenSourceCode(this ShaderBuildReport.GPUProgram program)
        {
            if (program != null && !string.IsNullOrEmpty(program.sourceCode))
            {
                var fileInfo = program.CreateTemporarySourceCodeFile();
                Application.OpenURL(fileInfo.FullName);
            }
        }

        public static FileInfo CreateTemporarySourceCodeFile(this ShaderBuildReport.CompileUnit cu, string tag = null)
        {
            var path = FileUtil.GetUniqueTempPathInProject();
            var extension = cu.profile == ShaderProfile.ComputeProgram
                ? "compute"
                : "shader";
            var program = cu.program;
            var fileInfo = new FileInfo(string.Format("{0}-{1}-{2:D3}{4}.{3}", path, program.name, cu.multicompileIndex, extension, tag ?? string.Empty));
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
            File.WriteAllText(fileInfo.FullName, program.sourceCode);

            return fileInfo;
        }

        public static FileInfo CreateTemporarySourceCodeFile(this ShaderBuildReport.GPUProgram program)
        {
            var path = FileUtil.GetUniqueTempPathInProject();
            var fileInfo = new FileInfo(string.Format("{0}-{1}.{2}", path, program.name, "hlsl"));
            if (!fileInfo.Directory.Exists)
                fileInfo.Directory.Create();
            File.WriteAllText(fileInfo.FullName, program.sourceCode);

            return fileInfo;
        }
    }
}
