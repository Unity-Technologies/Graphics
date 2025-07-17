
namespace UnityEngine.Rendering.RadeonRays
{
    internal static class Common
    {
        public static uint CeilDivide(uint val, uint div)
        {
            return (val + div - 1) / div;
        }

        public static void EnableKeyword(CommandBuffer cmd, ComputeShader shader, string keyword, bool enable)
        {
            if (enable)
            {
                cmd.EnableKeyword(shader, new LocalKeyword(shader, keyword));
            }
            else
            {
                cmd.DisableKeyword(shader, new LocalKeyword(shader, keyword));
            }
        }
    }
}
