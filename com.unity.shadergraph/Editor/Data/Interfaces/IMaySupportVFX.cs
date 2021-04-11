namespace UnityEditor.ShaderGraph
{
    public interface IMaySupportVFX
    {
        bool SupportsVFX();
    }

    static class MaySupportVFXExtensions
    {
        public static bool SupportsVFX(this Target target)
        {
            var vfxTarget = target as IMaySupportVFX;
            return vfxTarget != null && vfxTarget.SupportsVFX();
        }
    }
}
