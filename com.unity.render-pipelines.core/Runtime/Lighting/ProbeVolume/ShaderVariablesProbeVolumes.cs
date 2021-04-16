using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// TODO
    /// </summary>
    public enum APVConstantBufferRegister
    {
        GlobalRegister = 5
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)APVConstantBufferRegister.GlobalRegister)]
    public unsafe struct ShaderVariablesProbeVolumes
    {
    }
}
