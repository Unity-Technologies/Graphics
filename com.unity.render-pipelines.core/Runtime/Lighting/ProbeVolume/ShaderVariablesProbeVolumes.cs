using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// TODO
    /// </summary>
    public enum APVConstantBufferRegister
    {
        GlobalRegister = 5
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)APVConstantBufferRegister.GlobalRegister)]
    internal unsafe struct ShaderVariablesProbeVolumes
    {
        public Matrix4x4 _WStoRS;

        public Vector3 _IndexDim;
        public float _NormalBias;

        public Vector3 _PoolDim;
        public float pad0;
    }
}
