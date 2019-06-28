using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    enum LogBase
    {
        BaseE,
        Base2,
        Base10
    };

    [Title("Math", "Advanced", "Log")]
    class LogNode : CodeFunctionNode
    {
        public LogNode()
        {
            name = "Log";
        }


        [SerializeField]
        private LogBase m_LogBase = LogBase.BaseE;

        [EnumControl("Base")]
        public LogBase logBase
        {
            get { return m_LogBase; }
            set
            {
                if (m_LogBase == value)
                    return;

                m_LogBase = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_LogBase)
            {
                case LogBase.Base2:
                    return GetType().GetMethod("Unity_Log2", BindingFlags.Static | BindingFlags.NonPublic);
                case LogBase.Base10:
                    return GetType().GetMethod("Unity_Log10", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_Log", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        [HlslCodeGen]
        static void Unity_Log(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = log(In);
        }

        [HlslCodeGen]
        static void Unity_Log2(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = log2(In);
        }

        [HlslCodeGen]
        static void Unity_Log10(
            [Slot(0, Binding.None, 1, 1, 1, 1)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = log10(In);
        }
    }
}
