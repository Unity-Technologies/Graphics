using UnityEditor.ShaderGraph;

namespace UnityEditor.VFX
{
    interface IRequireVFXContext
    {
        void ConfigureContextData(VFXContext context, VFXTaskCompiledData data);
    }

    static class RequireVFXContextExtensions
    {
        public static bool TryConfigureContextData(this Target target, VFXContext context, VFXTaskCompiledData data)
        {
            if (!(target is IRequireVFXContext vfxTarget))
                return false;

            vfxTarget.ConfigureContextData(context, data);

            return true;
        }
    }
}
