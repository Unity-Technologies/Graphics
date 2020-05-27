using System;

namespace UnityEditor.ShaderAnalysis
{
    public static class ExportFormat
    {
        public static readonly Column[] Columns =
        {
            new Column("Pass Name", typeof(string), (r, po, cu, pu, p) => po.name, DiffFirst, -1),
            new Column("Multicompile index", typeof(int), (r, po, cu, pu, p) => cu.multicompileIndex, DiffNull, 13),
            new Column("All Defines", typeof(string), (r, po, cu, pu, p) => string.Join("\r\n", cu.defines), DiffFirst, -1),
            new Column("Multicompile Defines", typeof(string), (r, po, cu, pu, p) => string.Join("\r\n", cu.multicompileDefines), DiffFirst, -1),
            new Column("Micro Code Size (byte)", typeof(int), (r, po, cu, pu, p) => p.microCodeSize, DiffIntSub, 11),
            new Column("VGPR count", typeof(int), (r, po, cu, pu, p) => p.VGPRCount, DiffIntSub, -1),
            new Column("VGPR Used Count", typeof(int), (r, po, cu, pu, p) => p.VGPRUsedCount, DiffIntSub, -1),
            new Column("SGPR Count", typeof(int), (r, po, cu, pu, p) => p.SGPRCount, DiffIntSub, -1),
            new Column("SGPR Used Count", typeof(int), (r, po, cu, pu, p) => p.SGPRUsedCount, DiffIntSub, -1),
            new Column("User SGPR Count", typeof(int), (r, po, cu, pu, p) => p.UserSGPRCount, DiffIntSub, -1),
            new Column("LDS Size", typeof(int), (r, po, cu, pu, p) => p.LDSSize, DiffIntSub, -1),
            new Column("Thread Group Waves", typeof(int), (r, po, cu, pu, p) => p.threadGroupWaves, DiffIntSub, -1),
            new Column("SIMD Occupancy Count", typeof(int), (r, po, cu, pu, p) => p.SIMDOccupancyCount, DiffIntSub, 11),
            new Column("SIMD Occupancy Max", typeof(int), (r, po, cu, pu, p) => p.SIMDOccupancyMax, DiffIntSub, 11),
            new Column("SIMD Occupancy %", typeof(float), (r, po, cu, pu, p) => (float)p.SIMDOccupancyCount / (float)p.SIMDOccupancyMax, DiffFloatDiv, 11),
            new Column("CU Occupancy Count", typeof(int), (r, po, cu, pu, p) => p.CUOccupancyCount, DiffIntSub, 11),
            new Column("CU Occupancy Max", typeof(int), (r, po, cu, pu, p) => p.CUOccupancyMax, DiffIntSub, 11),
            new Column("CU Occupancy %", typeof(float), (r, po, cu, pu, p) => (float)p.CUOccupancyCount / (float)p.CUOccupancyMax, DiffFloatDiv, 11)
        };

        static object DiffNull(object l, object r) { return string.Empty; }
        static object DiffFirst(object l, object r) { return l; }
        static object DiffSecond(object l, object r) { return r; }
        static object DiffIntSub(object l, object r) { return (int)l - (int)r; }
        static object DiffFloatDiv(object l, object r) { return (float)l/(float)r; }

        public delegate object Getter(
            ShaderBuildReport report,
            ShaderBuildReport.GPUProgram po,
            ShaderBuildReport.CompileUnit cu,
            ShaderBuildReport.PerformanceUnit pu,
            ShaderBuildReport.ProgramPerformanceMetrics p);

        public delegate object DiffMethod(object l, object r);

        public struct Column
        {
            public Type type;
            public string name;
            public Getter getter;
            public DiffMethod diffMethod;
            public double colWidth;

            public Column(string name, Type type, Getter getter, DiffMethod diffMethod, double colWidth)
            {
                this.name = name;
                this.getter = getter;
                this.colWidth = colWidth;
                this.type = type;
                this.diffMethod = diffMethod;
            }
        }
    }
}
