namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal static class Fields
    {
        #region Tags
        public const string kFeatures = "features";
        public const string kSurfaceType = "SurfaceType";
        public const string kBlendMode = "BlendMode";
        public const string kTransforms = "Transforms";
        #endregion

        #region Fields
        // These are core Fields shared between URP and HDRP etc.
        public static FieldDescriptor GraphVertex = new FieldDescriptor(kFeatures, "graphVertex", "FEATURES_GRAPH_VERTEX");
        public static FieldDescriptor GraphPixel = new FieldDescriptor(kFeatures, "graphPixel", "FEATURES_GRAPH_PIXEL");
        public static FieldDescriptor GraphColorInterp = new FieldDescriptor(kFeatures, "graphColorInterp", "FEATURES_GRAPH_COLOR_INTERP");
        public static FieldDescriptor AlphaTest = new FieldDescriptor(string.Empty, "AlphaTest", "_ALPHA_TEST 1");          // HDRP: surface & decal subtargets
        public static FieldDescriptor BlendAlpha = new FieldDescriptor(kBlendMode, "Alpha", "_BLENDMODE_ALPHA 1");           // URP: only sprite targets, vfx: HDRP?
        public static FieldDescriptor DoubleSided = new FieldDescriptor(string.Empty, "DoubleSided", "_DOUBLE_SIDED 1");      // URP: only sprite targets, duplicated in HD
        public static FieldDescriptor IsPreview = new FieldDescriptor(string.Empty, "isPreview", "SHADERGRAPH_PREVIEW");
        public static FieldDescriptor LodCrossFade = new FieldDescriptor(string.Empty, "LodCrossFade", "_LODCROSSFADE 1");     // HD only
        public static FieldDescriptor AlphaToMask = new FieldDescriptor(string.Empty, "AlphaToMask", "_ALPHATOMASK_ON 1");    // HD only

        public static FieldDescriptor GraphVFX = new FieldDescriptor(kFeatures, "graphVFX", "FEATURES_GRAPH_VFX");
        public static FieldDescriptor ObjectToWorld = new FieldDescriptor(kTransforms, "ObjectToWorld", "_OBJECT_TO_WORLD");
        public static FieldDescriptor WorldToObject = new FieldDescriptor(kTransforms, "WorldToObject", "_WORLD_TO_OBJECT");
        #endregion
    }
}
