using System;

namespace UnityEditor.ShaderAnalysis
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ReportDiffExportAttribute : Attribute
    {
        public readonly string name;
        public readonly string extension;

        public ReportDiffExportAttribute(string name, string extension)
        {
            this.name = name;
            this.extension = extension;
        }
    }
}
