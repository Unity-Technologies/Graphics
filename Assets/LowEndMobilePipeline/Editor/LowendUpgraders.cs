using UnityEditor.Experimental.Rendering;

public struct UpgradeParams
{
    public float blendMode;
    public float specularSource;
    public float glosinessSource;
    public float reflectionSource;
}

public class LegacyBlinnPhongUpgrader : MaterialUpgrader
{
    public LegacyBlinnPhongUpgrader(string oldShaderName, UpgradeParams upgraderParams)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR");
        SetNewFloatProperty("_Mode", upgraderParams.blendMode);
        SetNewFloatProperty("_SpecSource", upgraderParams.specularSource);
        SetNewFloatProperty("_GlossinessSource", upgraderParams.glosinessSource);
        SetNewFloatProperty("_ReflectionSource", upgraderParams.reflectionSource);
    }
}

public class ParticlesMultiplyUpgrader : MaterialUpgrader
{
    public ParticlesMultiplyUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/Particles/Multiply");
    }
}

public class ParticlesAdditiveUpgrader : MaterialUpgrader
{
    public ParticlesAdditiveUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/Particles/Additive");
    }
}
