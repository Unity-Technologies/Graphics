using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.Graphing;

/*namespace UnityEditor.ShaderGraph
{
    [Title("OLD", "Transform")]
    public class UVTransform : CodeFunctionNode
    {
        [SerializeField]
        private RotationUnit m_constant = RotationUnit.Radians;

        [EnumControl("")]
        public RotationUnit constant
        {
            get { return m_constant; }
            set
            {
                if (m_constant == value)
                    return;

                m_constant = value;
                Dirty(ModificationScope.Graph);
            }
        }
        public UVTransform()
        {
            name = "Transform";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_ScaleOffsetRotate", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_ScaleOffsetRotate(
            [Slot(0, Binding.MeshUV0)] Vector2 uv,
            [Slot(1, Binding.None, 1f, 1f, 1f, 1f)] Vector2 scale,
            [Slot(2, Binding.None)] Vector2 offset,
            [Slot(3, Binding.None)] Vector1 rotation,
            [Slot(4, Binding.None)] out Vector2 result)
        {
            result = Vector2.zero;
            return
                @"
{


    uv -= offset;

    {precision} s = sin(rotation);
    {precision} c = cos(rotation);


    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    uv.xy = mul(uv.xy, rMatrix);
    uv += offset;
    float4 xform = float4(scale, offset + offset - offset * scale);
    result = uv * xform.xy + xform.zw;
}
";
        }
    }
}*/
