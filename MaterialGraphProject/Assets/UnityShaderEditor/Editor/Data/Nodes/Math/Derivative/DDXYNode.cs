using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph
{
    [Title("Math/Derivative/DDXY")]
    public class DDXYNode : CodeFunctionNode
    {
        public DDXYNode()
        {
            name = "DDXY";
        }

        [SerializeField]
        private PartialDerivativePrecision m_PartialDerivativePrecision = PartialDerivativePrecision.Default;

        [EnumControl("Precision")]
        public PartialDerivativePrecision partialDerivativePrecision
        {
            get { return m_PartialDerivativePrecision; }
            set
            {
                if (m_PartialDerivativePrecision == value)
                    return;

                m_PartialDerivativePrecision = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        string GetCurrentPrecision()
        {
            return System.Enum.GetName(typeof(PartialDerivativePrecision), m_PartialDerivativePrecision);
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_DDXY_{0}", GetCurrentPrecision()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDXY_Default(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = abs(ddx(In) + ddy(In));
}
";
        }

        static string Unity_DDXY_Coarse(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = abs(ddx_coarse(In) + ddy_coarse(In));
}
";
        }

        static string Unity_DDXY_Fine(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = abs(ddx_fine(In) + ddy_fine(In));
}
";
        }
    }
}
