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
                ("World Space", WorldSpace_Normal),
                ("Object Space", ObjectSpace_Normal),
                ("View Space", ViewSpace_Normal),
                ("Tangent Space", TangentSpace_Normal)
            };
            public static readonly List<(string, object)> Tangents = new()
            {
                ("World Space", WorldSpace_Tangent),
                ("Object Space", ObjectSpace_Tangent),
                ("View Space", ViewSpace_Tangent),
                ("Tangent Space", TangentSpace_Tangent)
            };
            public static readonly List<(string, object)> Bitangents = new()
            {
                ("World Space", WorldSpace_Bitangent),
                ("Object Space", ObjectSpace_Bitangent),
                ("View Space", ViewSpace_Bitangent),
                ("Tangent Space", TangentSpace_Bitangent)
            };
            public static readonly List<(string, object)> ViewDirections = new()
            {
                ("World Space", WorldSpace_ViewDirection),
                ("Object Space", ObjectSpace_ViewDirection),
                ("View Space", ViewSpace_ViewDirection),
                ("Tangent Space", TangentSpace_ViewDirection)
            };
            public static readonly List<(string, object)> Positions = new()
            {
                ("World Space", WorldSpace_Position),
                ("Object Space", ObjectSpace_Position),
                ("View Space", ViewSpace_Position),
                ("Tangent Space", TangentSpace_Position)
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
                ("M", M),
                ("V", V),
                ("P", P),
                ("VP", VP),
                ("I_M", I_M),
                ("I_V", I_V),
                ("I_P", I_P),
                ("I_VP", I_VP)
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
        public static readonly ReferenceValueDescriptor WorldSpace_Normal = new("");
        public static readonly ReferenceValueDescriptor WorldSpace_Tangent = new("");
        public static readonly ReferenceValueDescriptor WorldSpace_Bitangent = new("");
        public static readonly ReferenceValueDescriptor WorldSpace_ViewDirection = new("");
        public static readonly ReferenceValueDescriptor WorldSpace_Position = new("");
        public static readonly ReferenceValueDescriptor WorldSpace_CameraPosition = new("");

        public static readonly ReferenceValueDescriptor ObjectSpace = new("");
        public static readonly ReferenceValueDescriptor ObjectSpace_Normal = new("");
        public static readonly ReferenceValueDescriptor ObjectSpace_Tangent = new("");
        public static readonly ReferenceValueDescriptor ObjectSpace_Bitangent = new("");
        public static readonly ReferenceValueDescriptor ObjectSpace_ViewDirection = new("");
        public static readonly ReferenceValueDescriptor ObjectSpace_Position = new("");

        public static readonly ReferenceValueDescriptor ViewSpace = new("");
        public static readonly ReferenceValueDescriptor ViewSpace_Normal = new("");
        public static readonly ReferenceValueDescriptor ViewSpace_Tangent = new("");
        public static readonly ReferenceValueDescriptor ViewSpace_Bitangent = new("");
        public static readonly ReferenceValueDescriptor ViewSpace_ViewDirection = new("");
        public static readonly ReferenceValueDescriptor ViewSpace_Position = new("");

        public static readonly ReferenceValueDescriptor TangentSpace = new("");
        public static readonly ReferenceValueDescriptor TangentSpace_Normal = new("");
        public static readonly ReferenceValueDescriptor TangentSpace_Tangent = new("");
        public static readonly ReferenceValueDescriptor TangentSpace_Bitangent = new("");
        public static readonly ReferenceValueDescriptor TangentSpace_ViewDirection = new("");
        public static readonly ReferenceValueDescriptor TangentSpace_Position = new("");

        public static readonly ReferenceValueDescriptor UV0 = new("");
        public static readonly ReferenceValueDescriptor UV1 = new("");
        public static readonly ReferenceValueDescriptor UV2 = new("");
        public static readonly ReferenceValueDescriptor UV3 = new("");
        public static readonly ReferenceValueDescriptor UV4 = new("");
        public static readonly ReferenceValueDescriptor UV5 = new("");
        public static readonly ReferenceValueDescriptor UV6 = new("");
        public static readonly ReferenceValueDescriptor UV7 = new("");

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

        public static readonly ReferenceValueDescriptor Vertext_Color = new("");
        public static readonly ReferenceValueDescriptor FaceSign = new("");
        public static readonly ReferenceValueDescriptor TimeParameters = new("");
        public static readonly ReferenceValueDescriptor VertextID = new("");
        public static readonly ReferenceValueDescriptor BoneIndices = new("");
        public static readonly ReferenceValueDescriptor BoneWeights1 = new("");

        public static readonly ReferenceValueDescriptor ScreenPosition = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Default = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Raw = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Center = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Tiled = new("");
        public static readonly ReferenceValueDescriptor ScreenPosition_Pixel = new("");
    }
}
