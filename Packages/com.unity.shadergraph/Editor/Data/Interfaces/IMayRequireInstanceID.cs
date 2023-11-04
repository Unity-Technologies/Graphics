namespace UnityEditor.ShaderGraph
{
    interface IMayRequireInstanceID
    {
        bool RequiresInstanceID(ShaderStageCapability stageCapability = ShaderStageCapability.All);
    }
}
