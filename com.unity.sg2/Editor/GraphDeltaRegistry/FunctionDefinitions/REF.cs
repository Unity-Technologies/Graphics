using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class REF
    {
        public class OptionList
        {
            public static readonly List<(string, object)> Spaces = new()
            {
                ("World Space", WorldSpace),
                ("Object Space", ObjectSpace),
                ("View Space", ViewSpace),
                ("Tangent Space", TangentSpace)
            };
            public static readonly List<(string, object)> Normals = new()
            {
                ("World", WorldSpace_Normal),
                ("Object", ObjectSpace_Normal),
                ("View", ViewSpace_Normal),
                ("Tangent", TangentSpace_Normal)
            };
            public static readonly List<(string, object)> Tangents = new()
            {
                ("World", WorldSpace_Tangent),
                ("Object", ObjectSpace_Tangent),
                ("View", ViewSpace_Tangent),
                ("Tangent", TangentSpace_Tangent)
            };
            public static readonly List<(string, object)> Bitangents = new()
            {
                ("World", WorldSpace_Bitangent),
                ("Object", ObjectSpace_Bitangent),
                ("View", ViewSpace_Bitangent),
                ("Tangent", TangentSpace_Bitangent)
            };
            public static readonly List<(string, object)> ViewDirections = new()
            {
                ("World", WorldSpace_ViewDirection),
                ("Object", ObjectSpace_ViewDirection),
                ("View", ViewSpace_ViewDirection),
                ("Tangent", TangentSpace_ViewDirection)
            };
            public static readonly List<(string, object)> Positions = new()
            {
                ("World", WorldSpace_Position),
                ("Object", ObjectSpace_Position),
                ("View", ViewSpace_Position),
                ("Tangent", TangentSpace_Position)
            };
            public static readonly List<(string, object)> UVs = new()
            {
                ("UV0", UV0),
                ("UV1", UV1),
                ("UV2", UV2),
                ("UV3", UV3),
                ("UV4", UV4),
                ("UV5", UV5),
                ("UV6", UV6),
                ("UV7", UV7)
            };
            public static readonly List<(string, object)> Matrices = new()
            {
                ("Model", M),
                ("Inverse Model", I_M),
                ("View", V),
                ("Inverse View", I_V),
                ("Projection", P),
                ("Inverse Projection", I_P),
                ("View Projection", VP),
                ("Inverse View Projection", I_VP)
            };
            public static readonly List<(string, object)> Ambients = new()
            {
                ("Equator", Ambient_Equator),
                ("Ground", Ambient_Ground),
                ("Sky", Ambient_Sky),
            };
            public static readonly List<(string, object)> Params = new()
            {
                ("Projection", ProjectionParams),
                ("Screen", ScreenParams),
                ("ZBuffer", ZBufferParams),
                ("Ortho", OrthoParams),
            };
            public static readonly List<(string, object)> ScreenPositions = new()
            {
                ("Default", ScreenPosition_Default),
                ("Raw", ScreenPosition_Raw),
                ("Center", ScreenPosition_Center),
                ("Tiled", ScreenPosition_Tiled),
                ("Pixel", ScreenPosition_Pixel)
            };
        }

        public static readonly ReferenceValueDescriptor WorldSpace = new("");
        public static readonly ReferenceValueDescriptor WorldSpace_Normal = new("WorldSpaceNormal");
        public static readonly ReferenceValueDescriptor WorldSpace_Tangent = new("WorldSpaceTangent");
        public static readonly ReferenceValueDescriptor WorldSpace_Bitangent = new("WorldSpaceBiTangent");
        public static readonly ReferenceValueDescriptor WorldSpace_ViewDirection = new("WorldSpaceViewDirection");
        public static readonly ReferenceValueDescriptor WorldSpace_Position = new("WorldSpacePosition");
        public static readonly ReferenceValueDescriptor WorldSpace_CameraPosition = new("");

        public static readonly ReferenceValueDescriptor ObjectSpace = new("");
        public static readonly ReferenceValueDescriptor ObjectSpace_Normal = new("ObjectSpaceNormal");
        public static readonly ReferenceValueDescriptor ObjectSpace_Tangent = new("ObjectSpaceTangent");
        public static readonly ReferenceValueDescriptor ObjectSpace_Bitangent = new("ObjectSpaceBiTangent");
        public static readonly ReferenceValueDescriptor ObjectSpace_ViewDirection = new("ObjectSpaceViewDirection");
        public static readonly ReferenceValueDescriptor ObjectSpace_Position = new("ObjectSpacePosition");

        public static readonly ReferenceValueDescriptor ViewSpace = new("");
        public static readonly ReferenceValueDescriptor ViewSpace_Normal = new("ViewSpaceNormal");
        public static readonly ReferenceValueDescriptor ViewSpace_Tangent = new("ViewSpaceTangent");
        public static readonly ReferenceValueDescriptor ViewSpace_Bitangent = new("ViewSpaceBiTangent");
        public static readonly ReferenceValueDescriptor ViewSpace_ViewDirection = new("ViewSpaceViewDirection");
        public static readonly ReferenceValueDescriptor ViewSpace_Position = new("ViewSpacePosition");

        public static readonly ReferenceValueDescriptor TangentSpace = new("");
        public static readonly ReferenceValueDescriptor TangentSpace_Normal = new("TangentSpaceNormal");
        public static readonly ReferenceValueDescriptor TangentSpace_Tangent = new("TangentSpaceTangent");
        public static readonly ReferenceValueDescriptor TangentSpace_Bitangent = new("TangentSpaceBiTangent");
        public static readonly ReferenceValueDescriptor TangentSpace_ViewDirection = new("TangentSpaceViewDirection");
        public static readonly ReferenceValueDescriptor TangentSpace_Position = new("TangentSpacePosition");

        public static readonly ReferenceValueDescriptor UV0 = new("uv0");
        public static readonly ReferenceValueDescriptor UV1 = new("uv1");
        public static readonly ReferenceValueDescriptor UV2 = new("uv2");
        public static readonly ReferenceValueDescriptor UV3 = new("uv3");
        public static readonly ReferenceValueDescriptor UV4 = new("uv4");
        public static readonly ReferenceValueDescriptor UV5 = new("uv5");
        public static readonly ReferenceValueDescriptor UV6 = new("uv6");
        public static readonly ReferenceValueDescriptor UV7 = new("uv7");

        public static readonly ReferenceValueDescriptor M = new("");  // model
        public static readonly ReferenceValueDescriptor I_M = new("");
        public static readonly ReferenceValueDescriptor V = new("");  // view
        public static readonly ReferenceValueDescriptor I_V = new("");
        public static readonly ReferenceValueDescriptor P = new("");  // projection
        public static readonly ReferenceValueDescriptor I_P = new("");
        public static readonly ReferenceValueDescriptor VP = new("");  // view projection
        public static readonly ReferenceValueDescriptor I_VP = new("");

        public static readonly ReferenceValueDescriptor ProjectionParams = new("");
        public static readonly ReferenceValueDescriptor ScreenParams = new("");
        public static readonly ReferenceValueDescriptor ZBufferParams = new("");
        public static readonly ReferenceValueDescriptor OrthoParams = new("");

        public static readonly ReferenceValueDescriptor Linear01Depth = new("");
        public static readonly ReferenceValueDescriptor LinearEyeDepth = new("");

        public static readonly ReferenceValueDescriptor InstanceID = new("");
        public static readonly ReferenceValueDescriptor StereoEyeIndex = new("");

        public static readonly ReferenceValueDescriptor Ambient_Equator = new("");
        public static readonly ReferenceValueDescriptor Ambient_Ground = new("");
        public static readonly ReferenceValueDescriptor Ambient_Sky = new("");

        public static readonly ReferenceValueDescriptor Object_Position = new("");

        public static readonly ReferenceValueDescriptor SampleScene_Depth = new("");
        public static readonly ReferenceValueDescriptor SampleScene_Color = new("");

        public static readonly ReferenceValueDescriptor Main_Light_Direction = new("");

        public static readonly ReferenceValueDescriptor Reflection_Probe = new("");

        public static readonly ReferenceValueDescriptor Fog = new("");

        public static readonly ReferenceValueDescriptor Vertex_Color = new("VertexColor");
        public static readonly ReferenceValueDescriptor FaceSign = new("FaceSign");
        public static readonly ReferenceValueDescriptor TimeParameters = new("TimeParameters");
        public static readonly ReferenceValueDescriptor VertexID = new("VertexID");
        public static readonly ReferenceValueDescriptor BoneIndices = new("BoneIndices");
        public static readonly ReferenceValueDescriptor BoneWeights1 = new("BoneWeights");

        public static readonly ReferenceValueDescriptor ScreenPosition = new("ScreenPosition");
        public static readonly ReferenceValueDescriptor ScreenPosition_Default = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Raw = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Center = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Tiled = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Pixel = new("");
    }
}
