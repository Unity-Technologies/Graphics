namespace UnityEditor.ShaderGraph
{
    interface IMayRequireUITK
    {
        bool RequiresUITK(ShaderStageCapability stageCapability = ShaderStageCapability.Fragment);
    }

}
