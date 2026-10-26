using Unity.GraphCommon.LowLevel.Editor;

namespace UnityEditor.VFX
{
    /// <summary>
    /// A concrete VFX buffer, the lowered form of the logical data descriptions.
    /// </summary>
    interface IVfxBufferData : IDataDescription { }

    class VfxGpuBufferData : IVfxBufferData
    {
        public VFXGPUBufferDesc Desc { get; }

        public VfxGpuBufferData(VFXGPUBufferDesc desc) => Desc = desc;
    }

    class VfxCpuBufferData : IVfxBufferData
    {
        public VFXCPUBufferDesc Desc { get; }

        public VfxCpuBufferData(VFXCPUBufferDesc desc) => Desc = desc;
    }
}
