using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    internal static class BlockFields
    {
        [GenerateBlocks]
        public struct VertexDescription
        {
            public static string name = "VertexDescription";
            public static BlockFieldDescriptor Position = new BlockFieldDescriptor(VertexDescription.name, "Position", "VERTEXDESCRIPTION_POSITION",
                new PositionControl(CoordinateSpace.Object), ShaderStage.Vertex);
            public static BlockFieldDescriptor Normal = new BlockFieldDescriptor(VertexDescription.name, "Normal", "VERTEXDESCRIPTION_NORMAL",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Vertex);
            public static BlockFieldDescriptor Tangent = new BlockFieldDescriptor(VertexDescription.name, "Tangent", "VERTEXDESCRIPTION_TANGENT",
                new TangentControl(CoordinateSpace.Object), ShaderStage.Vertex);
        }

        [GenerateBlocks]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor BaseColor = new BlockFieldDescriptor(SurfaceDescription.name, "BaseColor", "Base Color", "SURFACEDESCRIPTION_BASECOLOR",
                new ColorControl(UnityEngine.Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor NormalTS = new BlockFieldDescriptor(SurfaceDescription.name, "NormalTS", "Normal (Tangent Space)", "SURFACEDESCRIPTION_NORMALTS",
                new NormalControl(CoordinateSpace.Tangent), ShaderStage.Fragment);
            public static BlockFieldDescriptor NormalOS = new BlockFieldDescriptor(SurfaceDescription.name, "NormalOS", "Normal (Object Space)", "SURFACEDESCRIPTION_NORMALOS",
                new NormalControl(CoordinateSpace.Object), ShaderStage.Fragment);
            public static BlockFieldDescriptor NormalWS = new BlockFieldDescriptor(SurfaceDescription.name, "NormalWS", "Normal (World Space)", "SURFACEDESCRIPTION_NORMALWS",
                new NormalControl(CoordinateSpace.World), ShaderStage.Fragment);
            public static BlockFieldDescriptor Metallic = new BlockFieldDescriptor(SurfaceDescription.name, "Metallic", "SURFACEDESCRIPTION_METALLIC",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Specular = new BlockFieldDescriptor(SurfaceDescription.name, "Specular", "Specular Color", "SURFACEDESCRIPTION_SPECULAR",
                new ColorControl(UnityEngine.Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor Smoothness = new BlockFieldDescriptor(SurfaceDescription.name, "Smoothness", "SURFACEDESCRIPTION_SMOOTHNESS",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Occlusion = new BlockFieldDescriptor(SurfaceDescription.name, "Occlusion", "Ambient Occlusion", "SURFACEDESCRIPTION_OCCLUSION",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor Emission = new BlockFieldDescriptor(SurfaceDescription.name, "Emission", "SURFACEDESCRIPTION_EMISSION",
                new ColorControl(UnityEngine.Color.black, true), ShaderStage.Fragment);
            public static BlockFieldDescriptor Alpha = new BlockFieldDescriptor(SurfaceDescription.name, "Alpha", "SURFACEDESCRIPTION_ALPHA",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor AlphaClipThreshold = new BlockFieldDescriptor(SurfaceDescription.name, "AlphaClipThreshold", "Alpha Clip Threshold", "SURFACEDESCRIPTION_ALPHACLIPTHRESHOLD",
                new FloatControl(0.5f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatMask = new BlockFieldDescriptor(SurfaceDescription.name, "CoatMask", "Coat Mask", "SURFACEDESCRIPTION_COATMASK",
                new FloatControl(0.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor CoatSmoothness = new BlockFieldDescriptor(SurfaceDescription.name, "CoatSmoothness", "Coat Smoothness", "SURFACEDESCRIPTION_COATSMOOTHNESS",
                new FloatControl(1.0f), ShaderStage.Fragment);
            public static BlockFieldDescriptor MapRightTopBack = new BlockFieldDescriptor(SurfaceDescription.name, "RightTopBack", "Right Top Back", "SURFACEDESCRIPTION_MAP_RTBK",
                new ColorControl(UnityEngine.Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor MapLeftBottomFront = new BlockFieldDescriptor(SurfaceDescription.name, "LeftBottomFront", "Left Bottom Front", "SURFACEDESCRIPTION_MAP_LBTF",
                new ColorControl(UnityEngine.Color.grey, false), ShaderStage.Fragment);
            public static BlockFieldDescriptor AbsorptionStrength = new BlockFieldDescriptor(SurfaceDescription.name, "AbsorptionStrength", "Color Absorption Strength", "SURFACEDESCRIPTION_COLOR_ABSORPTION_STRENGTH",
                new FloatControl(0.5f), ShaderStage.Fragment);
        }

        [GenerateBlocks]
        public struct SurfaceDescriptionLegacy
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor SpriteColor = new BlockFieldDescriptor(SurfaceDescription.name, "SpriteColor", "SURFACEDESCRIPTION_SPRITECOLOR",
                new ColorRGBAControl(UnityEngine.Color.white), ShaderStage.Fragment, isHidden: true);
        }
    }
}
