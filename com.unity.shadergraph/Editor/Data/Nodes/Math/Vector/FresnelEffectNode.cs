using UnityEngine.ShaderGraph.Hlsl;
using static UnityEngine.ShaderGraph.Hlsl.Intrinsics;

namespace UnityEditor.ShaderGraph
{
    [Title("Math", "Vector", "Fresnel Effect")]
    class FresnelNode : CodeFunctionNode
    {
        public FresnelNode()
        {
            name = "Fresnel Effect";
        }

        public override PreviewMode previewMode
        {
            get { return PreviewMode.Preview3D; }
        }

        [HlslCodeGen]
        static void Unity_FresnelEffect(
            [Slot(0, Binding.WorldSpaceNormal)] Float3 Normal,
            [Slot(1, Binding.WorldSpaceViewDirection)] Float3 ViewDir,
            [Slot(2, Binding.None, 1, 1, 1, 1)] Float Power,
            [Slot(3, Binding.None)] out Float Out)
        {
            Out = pow((1.0 - saturate(dot(normalize(Normal), normalize(ViewDir)))), Power);
        }
    }
}
