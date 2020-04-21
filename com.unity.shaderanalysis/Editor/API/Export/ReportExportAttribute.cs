using System;

namespace UnityEditor.ShaderAnalysis
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ReportExportAttribute : Attribute
    {
        public readonly string name;
        public readonly string extension;

        public ReportExportAttribute(string name, string extension)
        {
            this.name = name;
            this.extension = extension;
        }
    }
}
