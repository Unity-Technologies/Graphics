using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    static class UniversalBlockMasks
    {
        public static class Vertex
        {
            public static BlockFieldDescriptor[] Default = new BlockFieldDescriptor[]
            {
                BlockFields.VertexDescription.Position,
                BlockFields.VertexDescription.Normal,
                BlockFields.VertexDescription.Tangent,
            };
        }

        public static class Pixel
        {
            public static BlockFieldDescriptor[] LitForward = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Normal,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Metallic,
                BlockFields.SurfaceDescription.Specular,
                BlockFields.SurfaceDescription.Smoothness,
                BlockFields.SurfaceDescription.Occlusion,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.ClipThreshold,
            };

            public static BlockFieldDescriptor[] LitAlphaOnly = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.ClipThreshold,
            };

            public static BlockFieldDescriptor[] LitMeta = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Emission,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.ClipThreshold,
            };
            
            public static BlockFieldDescriptor[] Unlit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Alpha,
                BlockFields.SurfaceDescription.ClipThreshold,
            };

            public static BlockFieldDescriptor[] SpriteLit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.SpriteMask,
            };

            public static BlockFieldDescriptor[] SpriteNormal = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
                BlockFields.SurfaceDescription.Normal,
            };

            public static BlockFieldDescriptor[] SpriteUnlit = new BlockFieldDescriptor[]
            {
                BlockFields.SurfaceDescription.BaseColor,
            };
        }
    }
}
