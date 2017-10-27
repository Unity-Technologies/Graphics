using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    public enum ExponentialType
    {
        Exponential,
        Exponential2
    };

    [Title("Math/Advanced/Exponential")]
    public class ExponentialNode : CodeFunctionNode
    {
        public ExponentialNode()
        {
            name = "Exponential";
        }

        [SerializeField]
        private ExponentialType m_ExponentialType = ExponentialType.Exponential;

        [EnumControl("")]
        public ExponentialType exponentialType
        {
            get { return m_ExponentialType; }
            set
            {
                if (m_ExponentialType == value)
                    return;

                m_ExponentialType = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        string GetCurrentType()
        {
            return System.Enum.GetName(typeof(ExponentialType), m_ExponentialType);
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_{0}", GetCurrentType()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Exponential(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = exp(In);
}
";
        }

        static string Unity_Exponential2(
    [Slot(0, Binding.None)] DynamicDimensionVector In,
    [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = exp2(In);
}
";
        }
    }
}
