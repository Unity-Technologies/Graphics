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
            public static BlockFieldDescriptor Position      = new BlockFieldDescriptor(VertexDescription.name, "Position", "VERTEXDESCRIPTION_POSITION",
                new ObjectSpacePositionControl(), ContextStage.Vertex, new ShaderGraphRequirements() { requiresPosition = NeededCoordinateSpace.Object });
            public static BlockFieldDescriptor Normal        = new BlockFieldDescriptor(VertexDescription.name, "Normal", "VERTEXDESCRIPTION_NORMAL",
                new ObjectSpacePositionControl(), ContextStage.Vertex, new ShaderGraphRequirements() { requiresNormal = NeededCoordinateSpace.Object });
            public static BlockFieldDescriptor Tangent       = new BlockFieldDescriptor(VertexDescription.name, "Tangent", "VERTEXDESCRIPTION_TANGENT",
                new ObjectSpacePositionControl(), ContextStage.Vertex, new ShaderGraphRequirements() { requiresTangent = NeededCoordinateSpace.Object });
        }

        [GenerateBlocks]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor BaseColor     = new BlockFieldDescriptor(SurfaceDescription.name, "BaseColor", "SURFACEDESCRIPTION_BASECOLOR",
                new ColorControl(UnityEngine.Color.grey, false), ContextStage.Fragment);
            public static BlockFieldDescriptor Normal        = new BlockFieldDescriptor(SurfaceDescription.name, "Normal", "SURFACEDESCRIPTION_NORMAL",
                new TangentSpaceNormalControl(), ContextStage.Fragment, new ShaderGraphRequirements() { requiresNormal = NeededCoordinateSpace.Tangent });
            public static BlockFieldDescriptor Metallic      = new BlockFieldDescriptor(SurfaceDescription.name, "Metallic", "SURFACEDESCRIPTION_METALLIC", 
                new FloatControl(0.0f), ContextStage.Fragment);
            public static BlockFieldDescriptor Specular      = new BlockFieldDescriptor(SurfaceDescription.name, "Specular", "SURFACEDESCRIPTION_SPECULAR",
                new ColorControl(UnityEngine.Color.grey, false), ContextStage.Fragment);
            public static BlockFieldDescriptor Smoothness    = new BlockFieldDescriptor(SurfaceDescription.name, "Smoothness", "SURFACEDESCRIPTION_SMOOTHNESS",
                new FloatControl(0.5f), ContextStage.Fragment);
            public static BlockFieldDescriptor Occlusion     = new BlockFieldDescriptor(SurfaceDescription.name, "Occlusion", "SURFACEDESCRIPTION_OCCLUSION",
                new FloatControl(1.0f), ContextStage.Fragment);
            public static BlockFieldDescriptor Emission      = new BlockFieldDescriptor(SurfaceDescription.name, "Emission", "SURFACEDESCRIPTION_EMISSION",
                new ColorControl(UnityEngine.Color.white, true), ContextStage.Fragment);
            public static BlockFieldDescriptor Alpha         = new BlockFieldDescriptor(SurfaceDescription.name, "Alpha", "SURFACEDESCRIPTION_ALPHA",
                new FloatControl(1.0f), ContextStage.Fragment);
            public static BlockFieldDescriptor ClipThreshold = new BlockFieldDescriptor(SurfaceDescription.name, "ClipThreshold", "SURFACEDESCRIPTION_CLIPTHRESHOLD",
                new FloatControl(0.5f), ContextStage.Fragment);
            public static BlockFieldDescriptor SpriteMask = new BlockFieldDescriptor(SurfaceDescription.name, "SpriteMask", "SURFACEDESCRIPTION_SPRITEMASK",
                new ColorRGBAControl(new Color(1, 1, 1, 1)), ContextStage.Fragment);
        }
    }
}