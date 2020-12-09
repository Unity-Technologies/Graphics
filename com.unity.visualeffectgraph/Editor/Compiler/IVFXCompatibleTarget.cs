namespace UnityEditor.VFX
{
    interface IVFXCompatibleTarget
    {
        bool TryConfigureVFX(VFXContext context, VFXContextCompiledData contextData);
    }
}
