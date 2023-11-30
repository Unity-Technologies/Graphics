using System;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class ShaderGeneratorNames
    {
        private static string[] UV = { "uv0", "uv1", "uv2", "uv3" };
        public static int UVCount = 4;

        public const string ScreenPosition = "ScreenPosition";
        public const string NDCPosition = "NDCPosition";        // normalized device coordinates, [0,1] across view, origin in lower left
        public const string PixelPosition = "PixelPosition";    // pixel coordinates
        public const string VertexColor = "VertexColor";
        public const string FaceSign = "FaceSign";
        public const string TimeParameters = "TimeParameters";
        public const string BoneWeights = "BoneWeights";
        public const string BoneIndices = "BoneIndices";
        public const string VertexID = "VertexID";
        public const string InstanceID = "InstanceID";

        public static string GetUVName(this UVChannel channel)
        {
            return UV[(int)channel];
        }
    }
}
