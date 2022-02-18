using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal enum ShaderModel
    {
        Target20,
        Target25,
        Target30,
        Target35,
        Target40,
        Target45,
        Target46,
        Target50
    }

    [GenerationAPI]
    internal static class ShaderModelExtensions
    {
        public static string ToShaderString(this ShaderModel shaderModel)
        {
            switch (shaderModel)
            {
                case ShaderModel.Target20:
                    return "2.0";
                case ShaderModel.Target25:
                    return "2.5";
                case ShaderModel.Target30:
                    return "3.0";
                case ShaderModel.Target35:
                    return "3.5";
                case ShaderModel.Target40:
                    return "4.0";
                case ShaderModel.Target45:
                    return "4.5";
                case ShaderModel.Target46:
                    return "4.6";
                case ShaderModel.Target50:
                    return "5.0";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
