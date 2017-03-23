using UnityEditor.Experimental.Rendering;
using UnityEngine;

public class LegacyBlinnPhongUpgrader : MaterialUpgrader
{
    public LegacyBlinnPhongUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR");
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

public class StandardUpgrader : MaterialUpgrader
{
    public StandardUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR");
        RenameFloat("_Glossiness", "_Shininess");
    }
}

public class TerrainUpgrader : MaterialUpgrader
{
    public TerrainUpgrader(string oldShaderName)
    {
        RenameShader(oldShaderName, "ScriptableRenderPipeline/LowEndMobile/NonPBR");
        SetFloat("_Shininess", 1.0f);
    }
}
