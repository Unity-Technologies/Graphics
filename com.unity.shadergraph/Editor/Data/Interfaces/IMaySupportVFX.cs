namespace UnityEditor.ShaderGraph
{
    public interface IMaySupportVFX
    {
        bool SupportsVFX();
        bool CanSupportVFX();
    }

    static class MaySupportVFXExtensions
    {
        public static bool SupportsVFX(this Target target)
        {
            var vfxTarget = target as IMaySupportVFX;
            return vfxTarget != null && vfxTarget.SupportsVFX();
        }

        public static bool CanSupportVFX(this Target target)
        {
            var vfxTarget = target as IMaySupportVFX;
            return vfxTarget != null && vfxTarget.CanSupportVFX();
        }
    }
}
