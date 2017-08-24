using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("UV/UV Rotator")]
    public class UVRotatorNode : CodeFunctionNode
    {
        public UVRotatorNode()
        {
            name = "UVRotator";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_UVRotator", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_UVRotator(
            [Slot(0, Binding.MeshUV0)] Vector4 uv,
            [Slot(1, Binding.None)] Vector1 rotation,
            [Slot(2, Binding.None)] out Vector4 result)
        {
            result = Vector2.zero;
            return
                @"
{
    //rotation matrix
    uv.xy -= 0.5;
    {precision} s = sin(rotation);
    {precision} c = cos(rotation);


    //center rotation matrix
    {precision}2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix*2 - 1;

    //multiply the UVs by the rotation matrix
    uv.xy = mul(uv.xy, rMatrix);
    uv.xy += 0.5;
    result = uv;
}";
        }
    }
}
