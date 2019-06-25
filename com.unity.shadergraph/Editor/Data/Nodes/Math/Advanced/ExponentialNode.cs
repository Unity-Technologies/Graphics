using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Hlsl;
using static UnityEditor.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    enum ExponentialBase
    {
        BaseE,
        Base2
    };

    [Title("Math", "Advanced", "Exponential")]
    class ExponentialNode : CodeFunctionNode
    {
        public ExponentialNode()
        {
            name = "Exponential";
        }

        [SerializeField]
        private ExponentialBase m_ExponentialBase = ExponentialBase.BaseE;

        [EnumControl("Base")]
        public ExponentialBase exponentialBase
        {
            get { return m_ExponentialBase; }
            set
            {
                if (m_ExponentialBase == value)
                    return;

                m_ExponentialBase = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_ExponentialBase)
            {
                case ExponentialBase.Base2:
                    return GetType().GetMethod("Unity_Exponential2", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_Exponential", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        [HlslCodeGen]
        static void Unity_Exponential(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = exp(In);
        }

        [HlslCodeGen]
        static void Unity_Exponential2(
            [Slot(0, Binding.None)] [AnyDimension] Float4 In,
            [Slot(1, Binding.None)] [AnyDimension] out Float4 Out)
        {
            Out = exp2(In);
        }
    }
}
