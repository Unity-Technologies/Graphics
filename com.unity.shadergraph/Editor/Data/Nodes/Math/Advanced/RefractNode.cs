using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    enum RefractMode
    {
        CriticalAngle,
        Safe
    };

    [Title("Math", "Vector")]
    class RefractNode : CodeFunctionNode
    {
        [SerializeField]
        private RefractMode m_RefractMode = RefractMode.Safe;

        [EnumControl("Mode")]
        public RefractMode refractMode
        {
            get { return m_RefractMode; }
            set
            {
                if (m_RefractMode == value)
                    return;

                m_RefractMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        public RefractNode()
        {
            name = "Refract";
            synonyms = new string[] { "refract", "warp", "bend", "distort" };
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            if (m_RefractMode == RefractMode.CriticalAngle)
                return GetType().GetMethod("Unity_RefractCriticalAngle", BindingFlags.Static | BindingFlags.NonPublic);
            else
                return GetType().GetMethod("Unity_RefractSafe", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_RefractCriticalAngle(
            [Slot(0, Binding.None, 0, 0, 0, 1)] DynamicDimensionVector Incident,
            [Slot(1, Binding.None, 0, 1, 0, 1)] DynamicDimensionVector Normal,
            [Slot(2, Binding.None)] Vector1 Eta,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - Eta*Eta*(1.0 - cos0*cos0);
    Out = k >= 0.0 ? Eta*Incident - (Eta*cos0 + sqrt(k))*Normal : 0.0;
}
";
        }

        static string Unity_RefractSafe(
            [Slot(0, Binding.None, 0, 0, 0, 1)] DynamicDimensionVector Incident,
            [Slot(1, Binding.None, 0, 1, 0, 1)] DynamicDimensionVector Normal,
            [Slot(2, Binding.None)] Vector1 Eta,
            [Slot(3, Binding.None)] out DynamicDimensionVector Out)
        {
            return
@"
{
    $precision cos0 = dot(Incident, Normal);
    $precision k = 1.0 - Eta*Eta*(1.0 - cos0*cos0);
    Out = Eta*Incident - (Eta*cos0 + sqrt(max(k, 0.0)))*Normal;
}
";
        }
    }
}
