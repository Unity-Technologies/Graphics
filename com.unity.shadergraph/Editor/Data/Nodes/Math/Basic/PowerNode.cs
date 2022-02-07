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
        bool m_AbsA = true;

        [ToggleControl("Absolute A")]
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
            if (m_AbsA)
            {
                powerFunction = GetType().GetMethod("Unity_Power_Absolute", BindingFlags.Static | BindingFlags.NonPublic);
            }
            else
            {
                powerFunction = GetType().GetMethod("Unity_Power", BindingFlags.Static | BindingFlags.NonPublic);

            }
            return powerFunction;
            //return GetType().GetMethod("Unity_Power_Absolute", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Power(
            [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
            [Slot(1, Binding.None, 2, 2, 2, 2)] DynamicDimensionVector B,
            [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = pow(A, B);
}
";
        }

        static string Unity_Power_Absolute(
    [Slot(0, Binding.None, 0, 0, 0, 0)] DynamicDimensionVector A,
    [Slot(1, Binding.None, 2, 2, 2, 2)] DynamicDimensionVector B,
    [Slot(2, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    Out = pow(abs(A), B);
}
";
        }
    }
}
