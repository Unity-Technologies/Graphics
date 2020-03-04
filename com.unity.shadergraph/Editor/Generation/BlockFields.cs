using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    public static class BlockFields
    {
        [GenerateBlocks]
        public struct VertexDescription
        {
            public static string name = "VertexDescription";
            public static BlockFieldDescriptor Position      = new BlockFieldDescriptor(VertexDescription.name, "Position", 
                new ObjectSpacePositionControl(), ContextStage.Vertex);
            public static BlockFieldDescriptor Normal        = new BlockFieldDescriptor(VertexDescription.name, "Normal", 
                new ObjectSpaceNormalControl(), ContextStage.Vertex);
            public static BlockFieldDescriptor Tangent       = new BlockFieldDescriptor(VertexDescription.name, "Tangent", 
                new ObjectSpaceTangentControl(), ContextStage.Vertex);
        }

        [GenerateBlocks]
        public struct SurfaceDescription
        {
            public static string name = "SurfaceDescription";
            public static BlockFieldDescriptor Color         = new BlockFieldDescriptor(SurfaceDescription.name, "Color", 
                new ColorControl(UnityEngine.Color.grey, false), ContextStage.Fragment);
            public static BlockFieldDescriptor Normal        = new BlockFieldDescriptor(SurfaceDescription.name, "Normal", 
                new TangentSpaceNormalControl(), ContextStage.Fragment);
            public static BlockFieldDescriptor Metallic      = new BlockFieldDescriptor(SurfaceDescription.name, "Metallic", 
                new FloatControl(0.0f), ContextStage.Fragment);
            public static BlockFieldDescriptor Specular      = new BlockFieldDescriptor(SurfaceDescription.name, "Specular", 
                new ColorControl(UnityEngine.Color.grey, false), ContextStage.Fragment);
            public static BlockFieldDescriptor Smoothness    = new BlockFieldDescriptor(SurfaceDescription.name, "Smoothness", 
                new FloatControl(0.5f), ContextStage.Fragment);
            public static BlockFieldDescriptor Occlusion     = new BlockFieldDescriptor(SurfaceDescription.name, "Occlusion", 
                new FloatControl(1.0f), ContextStage.Fragment);
            public static BlockFieldDescriptor Emission      = new BlockFieldDescriptor(SurfaceDescription.name, "Emission", 
                new ColorControl(UnityEngine.Color.white, true), ContextStage.Fragment);
            public static BlockFieldDescriptor Alpha         = new BlockFieldDescriptor(SurfaceDescription.name, "Alpha", 
                new FloatControl(1.0f), ContextStage.Fragment);
            public static BlockFieldDescriptor ClipThreshold = new BlockFieldDescriptor(SurfaceDescription.name, "ClipThreshold", 
                new FloatControl(0.5f), ContextStage.Fragment);
        }
    }
}
