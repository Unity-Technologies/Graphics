using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.BuiltIn.ShaderGraph
{
    internal static class BuiltInFields
    {
        #region Tags
        public const string kFeatures = "features";
        public const string kSurfaceType = "SurfaceType";
        public const string kBlendMode = "BlendMode";
        #endregion

        #region Fields
        // TODO: figure which ones are actually URP only, leave those here and put others shared/core Fields in Fields.cs
        public static FieldDescriptor SurfaceOpaque = new FieldDescriptor(kSurfaceType, "Opaque", "_SURFACE_TYPE_OPAQUE 1");
        public static FieldDescriptor SurfaceTransparent = new FieldDescriptor(kSurfaceType, "Transparent", "_SURFACE_TYPE_TRANSPARENT 1");
        public static FieldDescriptor BlendAdd = new FieldDescriptor(kBlendMode, "Add", "_BLENDMODE_ADD 1");
        public static FieldDescriptor BlendPremultiply = new FieldDescriptor(kBlendMode, "Premultiply", "_ALPHAPREMULTIPLY_ON 1");
        public static FieldDescriptor BlendMultiply = new FieldDescriptor(kBlendMode, "Multiply", "_BLENDMODE_MULTIPLY 1");
        public static FieldDescriptor VelocityPrecomputed = new FieldDescriptor(string.Empty, "AddPrecomputedVelocity", "_ADD_PRECOMPUTED_VELOCITY");
        public static FieldDescriptor SpecularSetup = new FieldDescriptor(string.Empty, "SpecularSetup", "_SPECULAR_SETUP");
        public static FieldDescriptor Normal = new FieldDescriptor(string.Empty, "Normal", "_NORMALMAP 1");
        public static FieldDescriptor NormalDropOffTS = new FieldDescriptor(string.Empty, "NormalDropOffTS", "_NORMAL_DROPOFF_TS 1");
        public static FieldDescriptor NormalDropOffOS = new FieldDescriptor(string.Empty, "NormalDropOffOS", "_NORMAL_DROPOFF_OS 1");
        public static FieldDescriptor NormalDropOffWS = new FieldDescriptor(string.Empty, "NormalDropOffWS", "_NORMAL_DROPOFF_WS 1");
        #endregion

        // A predicate is field that has a matching template command, for example: $<name> <content>
        // It is only used to enable/disable <content> in the tempalate
        #region Predicates
        //public static FieldDescriptor PredicateClearCoat =    new FieldDescriptor(string.Empty, "ClearCoat", "_CLEARCOAT 1");
        #endregion
    }
}
