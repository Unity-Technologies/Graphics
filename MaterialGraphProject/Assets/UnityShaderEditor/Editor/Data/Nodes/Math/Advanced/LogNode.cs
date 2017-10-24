using System.Collections.Generic;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;
using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    public enum LogType
    {
        Log,
        Log2,
        Log10
    };

    [Title("Math/Advanced/Log")]
    public class LogNode : CodeFunctionNode
    {
        public LogNode()
        {
            name = "Log";
        }

        [SerializeField]
        private LogType m_LogType = LogType.Log;

        [EnumControl("")]
        public LogType logType
        {
            get { return m_LogType; }
            set
            {
                if (m_LogType == value)
                    return;

                m_LogType = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        string GetCurrentType()
        {
            return System.Enum.GetName(typeof(LogType), m_LogType);
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_{0}", GetCurrentType()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Log(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = log(In);
}
";
        }

        static string Unity_Log2(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = log2(In);
}
";
        }

        static string Unity_Log10(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = log10(In);
}
";
        }
    }
}
