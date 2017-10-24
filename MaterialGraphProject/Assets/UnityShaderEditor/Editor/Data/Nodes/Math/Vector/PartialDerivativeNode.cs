using System.Reflection;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Title("Math/Vector/Partial Derivative")]
    public class PartialDerivativeNode : CodeFunctionNode
    {
        public enum Precision
        {
            Coarse,
            Default,
            Fine
        };

        public enum Coordinate
        {
            X,
            Y
        };

        public PartialDerivativeNode()
        {
            name = "Partial Derivative";
        }

        [SerializeField]
        private Precision m_Precision = Precision.Default;

        [EnumControl("Precision")]
        public Precision _precision
        {
            get { return m_Precision; }
            set
            {
                if (m_Precision == value)
                    return;

                m_Precision = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        string GetCurrentPrecision()
        {
            return System.Enum.GetName(typeof(Precision), m_Precision);
        }

        [SerializeField]
        private Coordinate m_Coordinate = Coordinate.X;

        [EnumControl("Respect Coordinate")]
        public Coordinate coordinate
        {
            get { return m_Coordinate; }
            set
            {
                if (m_Coordinate == value)
                    return;

                m_Coordinate = value;
                if (onModified != null)
                {
                    onModified(this, ModificationScope.Graph);
                }
            }
        }

        string GetCurrentCoordinate()
        {
            return System.Enum.GetName(typeof(Coordinate), m_Coordinate);
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod(string.Format("Unity_DD{0}_{1}", GetCurrentCoordinate(), GetCurrentPrecision()),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_DDX_Default(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = ddx(In);
}
";
        }

        static string Unity_DDX_Coarse(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = ddx_coarse(In);
}
";
        }

        static string Unity_DDX_Fine(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = ddx_fine(In);
}
";
        }

        static string Unity_DDY_Default(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = ddy(In);
}
";
        }

        static string Unity_DDY_Coarse(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = ddy_coarse(In);
}
";
        }

        static string Unity_DDY_Fine(
            [Slot(0, Binding.None)] Vector1 In,
            [Slot(1, Binding.None)] out Vector1 Out)
        {
            return
                @"
{
    Out = ddy_fine(In);
}
";
        }
    }
}
