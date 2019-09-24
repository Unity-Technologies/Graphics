namespace UnityEditor.ShaderGraph.Internal
{
    public static class DefaultFields
    {
#region Tags
        const string kFeatures = "features";
        const string kSurfaceType = "SurfaceType";
        const string kBlendMode = "BlendMode";
#endregion

#region Fields
        public static FieldDescriptor GraphVertex =           new FieldDescriptor(kFeatures, "graphVertex", "FEATURES_GRAPH_VERTEX");
        public static FieldDescriptor GraphPixel =            new FieldDescriptor(kFeatures, "graphPixel", "FEATURES_GRAPH_PIXEL");
        public static FieldDescriptor AlphaClip =             new FieldDescriptor(string.Empty, "AlphaClip", "_AlphaClip 1");
        public static FieldDescriptor AlphaTest =             new FieldDescriptor(string.Empty, "AlphaTest", "ALPHA_TEST");
        public static FieldDescriptor SurfaceOpaque =         new FieldDescriptor(kSurfaceType, "Opaque", "_SURFACE_TYPE_OPAQUE 1");
        public static FieldDescriptor SurfaceTransparent =    new FieldDescriptor(kSurfaceType, "Transparent", "_SURFACE_TYPE_TRANSPARENT 1");
        public static FieldDescriptor BlendAdd =              new FieldDescriptor(kBlendMode, "Add", "_BLENDMODE_ADD 1");
        public static FieldDescriptor BlendAlpha =            new FieldDescriptor(kBlendMode, "Alpha", "_BLENDMODE_ALPHA 1");
        public static FieldDescriptor BlendPremultiply =      new FieldDescriptor(kBlendMode, "Premultiply", "_ALPHAPREMULTIPLY_ON 1");
        public static FieldDescriptor BlendMultiply =         new FieldDescriptor(kBlendMode, "Multiply", "_BLENDMODE_MULTIPLY 1");
        public static FieldDescriptor VelocityPrecomputed =   new FieldDescriptor(string.Empty, "AddPrecomputedVelocity", "VELOCITY_PRECOMPUTED");
        public static FieldDescriptor SpecularSetup =         new FieldDescriptor(string.Empty, "SpecularSetup", "_SPECULAR_SETUP");
        public static FieldDescriptor Normal =                new FieldDescriptor(string.Empty, "Normal", "_NORMALMAP 1");
#endregion
    }
}
