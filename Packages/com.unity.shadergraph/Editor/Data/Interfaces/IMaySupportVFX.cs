namespace UnityEditor.ShaderGraph
{
    public interface IMaySupportVFX
    {
        bool SupportsVFX();
        bool CanSupportVFX();
    }
    static class MaySupportVFXExtensions
    {
        public static bool SupportsVFX(this Target target) =>  target is IMaySupportVFX vfxTarget && vfxTarget.SupportsVFX();
        public static bool CanSupportVFX(this Target target) =>  target is IMaySupportVFX vfxTarget && vfxTarget.CanSupportVFX();
    }
}
