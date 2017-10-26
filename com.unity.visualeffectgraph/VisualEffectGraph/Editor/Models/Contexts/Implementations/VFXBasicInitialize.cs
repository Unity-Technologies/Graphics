using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicInitialize : VFXContext
    {
        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kSpawnEvent, VFXDataType.kParticle) {}
        public override string name { get { return "Initialize"; } }
        public override string codeGeneratorTemplate { get { return "VFXShaders/VFXInit"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.kInitialize; } }

        public class InputProperties
        {
            public AABox bounds = new AABox() { size = Vector3.one };
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            // GPU
            if (target == VFXDeviceTarget.GPU)
                return VFXExpressionMapper.FromBlocks(activeChildrenWithImplicit);

            // CPU
            var cpuMapper = new VFXExpressionMapper();
            cpuMapper.AddExpressionFromSlotContainer(this, -1);
            return cpuMapper;
        }
    }
}
