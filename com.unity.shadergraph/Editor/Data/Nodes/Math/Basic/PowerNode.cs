using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Basic", "Power")]
    class PowerNode : CodeFunctionNode
    {
        public PowerNode()
        {
            name = "Power";
        }
        [SerializeField]
        bool m_AbsA = false;

        [ToggleControl("Unsign Base")]
        public ToggleData absA
        {
            get { return new ToggleData(m_AbsA); }
            set
            {
                if (m_AbsA == value.isOn)
                    return;
                m_AbsA = value.isOn;
                Dirty(ModificationScope.Graph);

            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            MethodInfo powerFunction;
            if (!m_AbsA)
            {
                powerFunction = GetType().GetMethod("Unity_Power_Absolute", BindingFlags.Static | BindingFlags.NonPublic);
            }
            else
            {
                powerFunction = GetType().GetMethod("Unity_Power", BindingFlags.Static | BindingFlags.NonPublic);

            }
            return powerFunction;
        }

        static string Unity_Power(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector Base,
            [Slot(1, Binding.None, 2, 2, 2, 2)] DynamicDimensionVector Exp,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = pow(Base, Exp);
}
";
        }

        static string Unity_Power_Absolute(
    [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector Base,
    [Slot(1, Binding.None, 2, 2, 2, 2)] DynamicDimensionVector Exp,
    [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = pow(abs(Base), Exp);
}
";
        }
    }
}
