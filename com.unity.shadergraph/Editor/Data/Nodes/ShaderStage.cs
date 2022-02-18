using System;

namespace UnityEditor.ShaderGraph
{
    [Flags]
    enum ShaderStageCapability
    {
        None = 0,
        Vertex = 1 << 0,
        Fragment = 1 << 1,
        All = Vertex | Fragment
    }

    enum ShaderStage
    {
        Vertex = 1 << 0,
        Fragment = 1 << 1
    }

    static class ShaderStageExtensions
    {
        /// <summary>
        /// Tries to convert a ShaderStageCapability into a ShaderStage. The conversion is only successful if the given ShaderStageCapability <paramref name="capability"/> refers to exactly 1 shader stage.
        /// </summary>
        /// <param name="capability">The ShaderStageCapability to convert.</param>
        /// <param name="stage">If <paramref name="capability"/> refers to exactly 1 shader stage, this parameter will contain the equivalent ShaderStage of that. Otherwise the value is undefined.</param>
        /// <returns>True is <paramref name="capability"/> holds exactly 1 shader stage.</returns>
        public static bool TryGetShaderStage(this ShaderStageCapability capability, out ShaderStage stage)
        {
            switch (capability)
            {
                case ShaderStageCapability.Vertex:
                    stage = ShaderStage.Vertex;
                    return true;
                case ShaderStageCapability.Fragment:
                    stage = ShaderStage.Fragment;
                    return true;
                default:
                    stage = ShaderStage.Fragment;
                    return false;
            }
        }

        public static ShaderStageCapability GetShaderStageCapability(this ShaderStage stage)
        {
            switch (stage)
            {
                case ShaderStage.Vertex:
                    return ShaderStageCapability.Vertex;
                case ShaderStage.Fragment:
                    return ShaderStageCapability.Fragment;
                default:
                    return ShaderStageCapability.Fragment;
            }
        }
    }
}
