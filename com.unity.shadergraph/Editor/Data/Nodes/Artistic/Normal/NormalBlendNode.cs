using System.Reflection;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;

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

        public override string documentationURL
        {
            get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Normal-Blend-Node"; }
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

        static string Unity_NormalBlend(
            [Slot(0, Binding.None, 0, 0, 1, 0)] Vector3 A,
            [Slot(1, Binding.None, 0, 0, 1, 0)] Vector3 B,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;

            return @"
{
    Out = normalize({precision}3(A.rg + B.rg, A.b * B.b));
}
";
        }

        static string Unity_NormalBlend_Reoriented(
            [Slot(0, Binding.None, 0, 0, 1, 0)] Vector3 A,
            [Slot(1, Binding.None, 0, 0, 1, 0)] Vector3 B,
            [Slot(2, Binding.None)] out Vector3 Out)
        {
            Out = Vector3.one;
            return
                @"
{
	{precision}3 t = A.xyz + {precision}3(0.0, 0.0, 1.0);
	{precision}3 u = B.xyz * {precision}3(-1.0, -1.0, 1.0);
	Out = (t / t.z) * dot(t, u) - u;
}
";
        }
    }
}
