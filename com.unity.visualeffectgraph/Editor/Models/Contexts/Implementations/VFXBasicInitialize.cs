using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    class InitializeVariantProvider : VariantProvider
    {
        protected override sealed Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, object[]>
                {
                    { "dataType", Enum.GetValues(typeof(VFXDataParticle.DataType)).Cast<object>().ToArray() }
                };
            }
        }
    }

    [VFXInfo(variantProvider = typeof(InitializeVariantProvider))]
    class VFXBasicInitialize : VFXContext
    {
        public VFXBasicInitialize() : base(VFXContextType.Init, VFXDataType.SpawnEvent, VFXDataType.None) {}
        public override string name { get { return "Initialize " + ObjectNames.NicifyVariableName(ownedType.ToString()); } }
        public override string codeGeneratorTemplate { get { return VisualEffectGraphPackageInfo.assetPackagePath + "/Shaders/VFXInit"; } }
        public override bool codeGeneratorCompute { get { return true; } }
        public override VFXTaskType taskType { get { return VFXTaskType.Initialize; } }
        public override VFXDataType outputType { get { return GetData() == null ? VFXDataType.Particle : GetData().type; } }


        private bool hasGPUSpawner => inputContexts.Any(o => o.contextType == VFXContextType.SpawnerGPU);

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (hasGPUSpawner)
                    yield return "VFX_USE_SPAWNER_FROM_GPU";

                if (ownedType == VFXDataType.ParticleStrip)
                    yield return "HAS_STRIPS";
            }
        }

        public class InputProperties
        {
            [Tooltip("The culling bounds of this system. The Visual Effect is only visible if the bounding box specified here is visible to the camera.")]
            public AABox bounds = new AABox() { size = Vector3.one };
        }

        public class StripInputProperties
        {
            public uint stripIndex = 0;
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (model == this && cause == InvalidationCause.kConnectionChanged)
                ResyncSlots(false); // To add/remove stripIndex

            base.OnInvalidate(model, cause);
        }

        protected override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            VFXSetting capacitySetting = GetSetting("capacity");
            if ((uint)capacitySetting.value > 1000000)
                manager.RegisterError("CapacityOver1M", VFXErrorType.PerfWarning, "Systems with large capacities can be slow to simulate");
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var prop = base.inputProperties;
                if (ownedType == VFXDataType.ParticleStrip && !hasGPUSpawner)
                    prop = prop.Concat(PropertiesFromType("StripInputProperties"));
                return prop;
            }
        }

        public sealed override VFXCoordinateSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            if (slot.name == "bounds")
                return VFXCoordinateSpace.Local;
            return base.GetOutputSpaceFromSlot(slot);
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            // GPU
            if (target == VFXDeviceTarget.GPU)
            {
                var gpuMapper = VFXExpressionMapper.FromBlocks(activeFlattenedChildrenWithImplicit);
                if (ownedType == VFXDataType.ParticleStrip && !hasGPUSpawner)
                    gpuMapper.AddExpressionsFromSlot(inputSlots[1], -1); // strip index
                return gpuMapper;
            }

            // CPU
            var cpuMapper = new VFXExpressionMapper();
            cpuMapper.AddExpressionsFromSlot(inputSlots[0], -1); // bounds
            return cpuMapper;
        }

        public override VFXSetting GetSetting(string name)
        {
            return GetData().GetSetting(name); // Just a bridge on data
        }

        public override IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags)
        {
            return GetData().GetSettings(listHidden, flags); // Just a bridge on data
        }
    }
}
