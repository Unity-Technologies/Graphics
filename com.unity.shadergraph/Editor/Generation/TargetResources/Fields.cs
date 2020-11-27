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
        // These are core Fields shared between URP and HDRP etc.
        public static FieldDescriptor GraphVertex =           new FieldDescriptor(kFeatures, "graphVertex", "FEATURES_GRAPH_VERTEX");
        public static FieldDescriptor GraphPixel =            new FieldDescriptor(kFeatures, "graphPixel", "FEATURES_GRAPH_PIXEL");
        public static FieldDescriptor AlphaClip =             new FieldDescriptor(string.Empty, "AlphaClip", "_AlphaClip 1");
        public static FieldDescriptor AlphaTest =             new FieldDescriptor(string.Empty, "AlphaTest", "_ALPHA_TEST 1");
        public static FieldDescriptor BlendAlpha =            new FieldDescriptor(kBlendMode, "Alpha", "_BLENDMODE_ALPHA 1");       // Universal, vfx: HDRP?
        public static FieldDescriptor DoubleSided =           new FieldDescriptor(string.Empty, "DoubleSided", "_DOUBLE_SIDED 1");  // Universal, duplicated in HD
        public static FieldDescriptor IsPreview =             new FieldDescriptor(string.Empty, "isPreview", "SHADERGRAPH_PREVIEW");
        public static FieldDescriptor LodCrossFade =          new FieldDescriptor(string.Empty, "LodCrossFade", "_LODCROSSFADE 1"); // HD only
        public static FieldDescriptor AlphaToMask =           new FieldDescriptor(string.Empty, "AlphaToMask", "_ALPHATOMASK_ON 1"); // HD only
        #endregion
    }
}
