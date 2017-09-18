using System.Reflection;

namespace UnityEngine.MaterialGraph
{
    [Title("Utility/Heightmap To Normalmap")]
    public class HeightToNormalNode : CodeFunctionNode
    {
        public HeightToNormalNode()
        {
            name = "HeightToNormal";
        }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("Unity_HeightToNormal", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string Unity_HeightToNormal(
            [Slot(0, Binding.None)] Texture2D heightmap,
            [Slot(1, Binding.MeshUV0)] Vector1 texCoord,
            [Slot(2, Binding.None, 0.005f, 0, 0, 0)] Vector1 texOffset,
            [Slot(3, Binding.None, 8f, 0, 0, 0)] Vector1 strength,
            [Slot(4, Binding.None)] out Vector1 normal)
        {
            return
                @"
{
    float2 offsetU = float2(texCoord.x + texOffset, texCoord.y);
    float2 offsetV = float2(texCoord.x, texCoord.y + texOffset);

    float normalSample = 0;
    float uSample = 0;
    float vSample = 0;

    #ifdef UNITY_COMPILER_HLSL
    normalSample = heightmap.Sample(my_linear_repeat_sampler, texCoord).r;
    uSample = heightmap.Sample(my_linear_repeat_sampler, offsetU).r;
    vSample = heightmap.Sample(my_linear_repeat_sampler, offsetV).r;
    #endif

    float uMinusNormal = uSample - normalSample;
    float vMinusNormal = vSample - normalSample;

    uMinusNormal = uMinusNormal * strength;
    vMinusNormal = vMinusNormal * strength;

    float3 va = float3(1, 0, uMinusNormal);
    float3 vb = float3(0, 1, vMinusNormal);

    normals = cross(va, vb);
}
";
        }
    }
}
