namespace UnityEditor.ShaderGraph
{
    public interface ISplattableShaderProperty
    {
        bool splat { get; set; }
    }

    static class SplatUtils
    {
        public static string PerSplatString(this ISplattableShaderProperty splatProp)
            => splatProp.splat ? "[PerSplat]" : string.Empty;

        public static bool IsSplattingShaderProperty(this AbstractShaderProperty shaderProperty)
            => shaderProperty is ISplattableShaderProperty splatProp && splatProp.splat;

        public static bool IsSplattingPropertyNode(this AbstractMaterialNode node)
            => node is PropertyNode propNode && propNode.shaderProperty.IsSplattingShaderProperty();

        public static bool IsNonSplattingPropertyNode(this AbstractMaterialNode node)
            => node is PropertyNode propNode && !propNode.shaderProperty.IsSplattingShaderProperty();
    }
}
