using System;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGeneratorNames
    {
        private static string[] UV = { "uv0", "uv1", "uv2", "uv3" };
        public static int UVCount = 4;

        public const string ScreenPosition = "ScreenPosition";
        public const string VertexColor = "VertexColor";
        public const string FaceSign = "FaceSign";
        public const string TimeParameters = "TimeParameters";
        public const string BoneWeights = "BoneWeights";
        public const string BoneIndices = "BoneIndices";
        public const string VertexID = "VertexID";

        public static string GetUVName(this UVChannel channel)
        {
            return UV[(int)channel];
        }
    }
}
