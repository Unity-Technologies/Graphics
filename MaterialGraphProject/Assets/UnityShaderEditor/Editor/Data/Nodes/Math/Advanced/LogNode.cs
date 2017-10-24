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

        static Dictionary<LogType, string> m_LogTypes = new Dictionary<LogType, string>
        {
            {LogType.Log, "" },
            {LogType.Log2, "2" },
            {LogType.Log10, "10" },
        };

        [SerializeField]
        private static LogType m_LogType = LogType.Log;

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

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Log", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Log(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = log"+ m_LogTypes[m_LogType] + @"(In);
}
";
        }
    }
}
