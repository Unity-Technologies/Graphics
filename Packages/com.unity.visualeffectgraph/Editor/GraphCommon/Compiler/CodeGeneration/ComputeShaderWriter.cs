using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    class ComputeShaderWriter : ShaderWriter
    {
        public override void Begin(string name)
        {
            base.Begin(name);

            Pragma("kernel CSMain");
            Pragma("only_renderers d3d11 glcore gles3 metal vulkan xboxone xboxone xboxseries playstation ps5 switch webgpu");
            //Pragma("enable_d3d11_debug_symbols");
            NewLine();
        }

        public override string End()
        {
            return base.End();
        }

        public void WriteMainFunction(uint x, uint y, uint z)
        {
            WriteLine($"[numthreads({x}, {y}, {z})]");
            WriteLine("void CSMain(uint3 groupId : SV_GroupID, uint3 groupThreadId : SV_GroupThreadID)");
        }
    }
}
