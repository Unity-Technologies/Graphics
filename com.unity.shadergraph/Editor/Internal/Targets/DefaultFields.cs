namespace UnityEditor.ShaderGraph.Internal
{
    public static class DefaultFields
    {
        public static class GraphFeatures
        {
            public static Field Vertex =        new Field("GRAPH_VERTEX");
            public static Field Pixel =         new Field("GRAPH_PIXEL");
        }

        public class ShaderFeatures
        {
            public static Field AlphaTest =     new Field("ALPHA_TEST");
        }

        public class SurfaceType
        {
            public static Field Opaque =        new Field("SURFACE_OPAQUE");
            public static Field Transparent =   new Field("SURFACE_TRANSPARENT");
        }

        public class BlendMode
        {
            public static Field Alpha =         new Field("BLEND_ALPHA");
            public static Field Add =           new Field("BLEND_ADD");
            public static Field Premultiply =   new Field("BLEND_PREMULTIPLY");
            public static Field Multiply =      new Field("BLEND_MULTIPLY");
        }

        public class Velocity
        {
            public static Field Precomputed =   new Field("VELOCITY_PRECOMPUTED");
        }
    }
}
