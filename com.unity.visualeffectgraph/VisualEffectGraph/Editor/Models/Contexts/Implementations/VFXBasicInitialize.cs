using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.VFX
{
    [VFXInfo]
    class VFXBasicInitialize : VFXContext
    {
        [VFXSetting]
        private uint capacity = 0; // not serialized here but in VFXDataParticle

        public VFXBasicInitialize() : base(VFXContextType.kInit, VFXDataType.kSpawnEvent, VFXDataType.kParticle) {}
        public override string name { get { return "Initialize"; } }
        public override string codeGeneratorTemplate { get { return "VFXEditor/Shaders/VFXInit"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.Initialize; } }

        public override void OnEnable()
        {
            base.OnEnable();
            capacity = ((VFXDataParticle)GetData()).capacity;
        }

        protected override void OnInvalidate(VFXModel model, VFXModel.InvalidationCause cause)
        {
            if (model == this && cause == VFXModel.InvalidationCause.kSettingChanged)
                ((VFXDataParticle)GetData()).capacity = capacity;

            base.OnInvalidate(model, cause);
        }

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
