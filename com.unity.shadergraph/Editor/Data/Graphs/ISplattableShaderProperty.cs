namespace UnityEditor.ShaderGraph
{
    public interface ISplattableShaderProperty
    {
        bool splat { get; set; }
    }

    static class SplattableShaderProperty
    {
        public static string PerSplatString(this ISplattableShaderProperty splatProp)
            => splatProp.splat ? "[PerSplat]" : string.Empty;
    }
}
