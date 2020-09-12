using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderAnalysis
{
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
}
