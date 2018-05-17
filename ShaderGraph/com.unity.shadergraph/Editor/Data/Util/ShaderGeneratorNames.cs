using System;

namespace UnityEditor.ShaderGraph {
    public static class ShaderGeneratorNames
    {
        private static string[] UV = {"uv0", "uv1", "uv2", "uv3"};
        public static int UVCount = 4;

        public const string ScreenPosition = "screenPosition";
        public const string VertexColor = "vertexColor";


        public static string GetUVName(this UVChannel channel)
        {
            return UV[(int)channel];
        }
    }
}