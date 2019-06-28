using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    enum NormalBlendMode
    {
        Default,
        Reoriented
    }

    [FormerName("UnityEditor.ShaderGraph.BlendNormalRNM")]
    [Title("Artistic", "Normal", "Normal Blend")]
    class NormalBlendNode : CodeFunctionNode
    {
        public NormalBlendNode()
        {
            name = "Normal Blend";
        }


        [SerializeField]
        private NormalBlendMode m_BlendMode = NormalBlendMode.Default;

        [EnumControl("Mode")]
        public NormalBlendMode blendMode
        {
            get { return m_BlendMode; }
            set
            {
                if (m_BlendMode == value)
                    return;

                m_BlendMode = value;
                Dirty(ModificationScope.Graph);
            }
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            switch (m_BlendMode)
            {
                case NormalBlendMode.Reoriented:
                    return GetType().GetMethod("Unity_NormalBlend_Reoriented", BindingFlags.Static | BindingFlags.NonPublic);
                default:
                    return GetType().GetMethod("Unity_NormalBlend", BindingFlags.Static | BindingFlags.NonPublic);
            }
        }

        [HlslCodeGen]
        static void Unity_NormalBlend(
            [Slot(0, Binding.None, 0, 0, 1, 0)] Float3 A,
            [Slot(1, Binding.None, 0, 0, 1, 0)] Float3 B,
            [Slot(2, Binding.None)] out Float3 Out)
        {
            Out = normalize(Float3(A.xy + B.xy, A.z * B.z));
        }

        [HlslCodeGen]
        static void Unity_NormalBlend_Reoriented(
            [Slot(0, Binding.None, 0, 0, 1, 0)] Float3 A,
            [Slot(1, Binding.None, 0, 0, 1, 0)] Float3 B,
            [Slot(2, Binding.None)] out Float3 Out)
        {
            var t = A + Float3(0.0, 0.0, 1.0);
	        var u = B * Float3(-1.0, -1.0, 1.0);
            Out = (t / t.z) * dot(t, u) - u;
        }
    }
}
