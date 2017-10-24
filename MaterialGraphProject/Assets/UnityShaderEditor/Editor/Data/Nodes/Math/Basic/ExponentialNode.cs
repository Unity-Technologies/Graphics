using System.Collections.Generic;
using UnityEditor.MaterialGraph.Drawing.Controls;
using UnityEngine.Graphing;
using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    public enum ExponentialType
    {
        Exp,
        Exp2
    };

    [Title("Math/Basic/Exponential")]
    public class ExponentialNode : CodeFunctionNode
    {
        public ExponentialNode()
        {
            name = "Exponential";
        }

        static Dictionary<ExponentialType, string> m_ExponentialTypes = new Dictionary<ExponentialType, string>
        {
            {ExponentialType.Exp, "" },
            {ExponentialType.Exp2, "2" },
        };

        [SerializeField]
        private static ExponentialType m_ExponentialType = ExponentialType.Exp;

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

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_Exponential", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_Exponential(
            [Slot(0, Binding.None)] DynamicDimensionVector In,
            [Slot(1, Binding.None)] out DynamicDimensionVector Out)
        {
            return
                @"
{
    Out = exp"+m_ExponentialTypes[m_ExponentialType] +@"(In);
}
";
        }
    }
}
