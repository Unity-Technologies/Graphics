namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal static class Fields
    {
#region Tags
        public const string kFeatures = "features";
        public const string kSurfaceType = "SurfaceType";
        public const string kBlendMode = "BlendMode";
#endregion

#region Fields
        public static FieldDescriptor GraphVertex =           new FieldDescriptor(kFeatures, "graphVertex", "FEATURES_GRAPH_VERTEX");
        public static FieldDescriptor GraphPixel =            new FieldDescriptor(kFeatures, "graphPixel", "FEATURES_GRAPH_PIXEL");
        public static FieldDescriptor AlphaClip =             new FieldDescriptor(string.Empty, "AlphaClip", "_AlphaClip 1");
        public static FieldDescriptor AlphaTest =             new FieldDescriptor(string.Empty, "AlphaTest", "_ALPHA_TEST 1");
        public static FieldDescriptor SurfaceOpaque =         new FieldDescriptor(kSurfaceType, "Opaque", "_SURFACE_TYPE_OPAQUE 1");
        public static FieldDescriptor SurfaceTransparent =    new FieldDescriptor(kSurfaceType, "Transparent", "_SURFACE_TYPE_TRANSPARENT 1");
        public static FieldDescriptor BlendAdd =              new FieldDescriptor(kBlendMode, "Add", "_BLENDMODE_ADD 1");
        public static FieldDescriptor BlendAlpha =            new FieldDescriptor(kBlendMode, "Alpha", "_BLENDMODE_ALPHA 1");
        public static FieldDescriptor BlendPremultiply =      new FieldDescriptor(kBlendMode, "Premultiply", "_ALPHAPREMULTIPLY_ON 1");
        public static FieldDescriptor BlendMultiply =         new FieldDescriptor(kBlendMode, "Multiply", "_BLENDMODE_MULTIPLY 1");
        public static FieldDescriptor VelocityPrecomputed =   new FieldDescriptor(string.Empty, "AddPrecomputedVelocity", "_ADD_PRECOMPUTED_VELOCITY");
        public static FieldDescriptor DoubleSided =           new FieldDescriptor(string.Empty, "DoubleSided", "_DOUBLE_SIDED 1");
        public static FieldDescriptor SpecularSetup =         new FieldDescriptor(string.Empty, "SpecularSetup", "_SPECULAR_SETUP");
        public static FieldDescriptor Normal =                new FieldDescriptor(string.Empty, "Normal", "_NORMALMAP 1");
        public static FieldDescriptor NormalDropOffTS =       new FieldDescriptor(string.Empty, "NormalDropOffTS", "_NORMAL_DROPOFF_TS 1");
        public static FieldDescriptor NormalDropOffOS =       new FieldDescriptor(string.Empty, "NormalDropOffOS", "_NORMAL_DROPOFF_OS 1");
        public static FieldDescriptor NormalDropOffWS =       new FieldDescriptor(string.Empty, "NormalDropOffWS", "_NORMAL_DROPOFF_WS 1");
        public static FieldDescriptor IsPreview =             new FieldDescriptor(string.Empty, "isPreview", "SHADERGRAPH_PREVIEW");
        public static FieldDescriptor LodCrossFade =          new FieldDescriptor(string.Empty, "LodCrossFade", "_LODCROSSFADE 1");
        public static FieldDescriptor AlphaToMask =           new FieldDescriptor(string.Empty, "AlphaToMask", "_ALPHATOMASK_ON 1");
#endregion
    }
}
