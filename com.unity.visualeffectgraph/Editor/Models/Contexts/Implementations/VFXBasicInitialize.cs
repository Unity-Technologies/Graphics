using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.VFX.UI;
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

        private bool hasDynamicSourceCount => GetData() != null ? ((VFXDataParticle)GetData()).hasDynamicSourceCount : false;

        public override IEnumerable<string> additionalDefines
        {
            get
            {
                if (hasGPUSpawner)
                    yield return "VFX_USE_SPAWNER_FROM_GPU";

                if (hasDynamicSourceCount)
                    yield return "VFX_USE_DYNAMIC_SOURCE_COUNT";

                if (ownedType == VFXDataType.ParticleStrip)
                    yield return "HAS_STRIPS";

                yield return "VFX_STATIC_SOURCE_COUNT (" + GetData().staticSourceCount + ")";
            }
        }

        public class InputPropertiesBounds
        {
            [Tooltip(
                "The culling bounds of this system. The Visual Effect is only visible if the bounding box specified here is visible to the camera.")]
            public AABox bounds = new AABox() {size = Vector3.one};
        }

        public class InputPropertiesPadding
        {
            [Tooltip(
                "Some additional padding to add the culling bounds set above. It can be helpful when using recorded bounds.")]
            public Vector3 boundsPadding = Vector3.zero;
        }

        public class StripInputProperties
        {
            public uint stripIndex = 0;
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kConnectionChanged)
            {
                if (model == this)
                    ResyncSlots(false); // To add/remove stripIndex
                RefreshErrors(GetGraph());
            }

            base.OnInvalidate(model, cause);
        }

        protected override void GenerateErrors(VFXInvalidateErrorReporter manager)
        {
            VFXSetting capacitySetting = GetSetting("capacity");
            if ((uint)capacitySetting.value > 1000000)
                manager.RegisterError("CapacityOver1M", VFXErrorType.PerfWarning, "Systems with large capacities can be slow to simulate");
            var data = GetData() as VFXDataParticle;
            if (data != null && data.boundsSettingMode == BoundsSettingMode.Recorded
                && CanBeCompiled())
            {
                if (VFXViewWindow.currentWindow?.graphView?.attachedComponent == null ||
                    !BoardPreferenceHelper.IsVisible(BoardPreferenceHelper.Board.componentBoard, false))
                {
                    manager.RegisterError("NeedsRecording", VFXErrorType.Warning,
                        "In order to record the bounds, the current graph needs to be attached to a scene instance via the Target Game Object panel");
                }

                try
                {
                    var boundsSlot = inputSlots.First(s => s.name == "bounds");
                    if (boundsSlot.AllChildrenWithLink().Any())
                    {
                        manager.RegisterError("OverriddenRecording", VFXErrorType.Warning,
                            "This system bounds will not be recorded because they are set from operators.");
                    }
                }
                catch { /* do nothing*/ }
            }
        }

        protected override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var particleData = GetData() as VFXDataParticle;

                var prop = Enumerable.Empty<VFXPropertyWithValue>();
                if (particleData)
                {
                    if (particleData.boundsSettingMode == BoundsSettingMode.Manual)
                    {
                        prop = prop.Concat(PropertiesFromType("InputPropertiesBounds"));
                    }
                    if (particleData.boundsSettingMode == BoundsSettingMode.Recorded)
                    {
                        prop = prop.Concat(PropertiesFromType("InputPropertiesBounds"));
                        prop = prop.Concat(PropertiesFromType("InputPropertiesPadding"));
                    }
                    if (particleData.boundsSettingMode == BoundsSettingMode.Automatic)
                    {
                        prop = prop.Concat(PropertiesFromType("InputPropertiesPadding"));
                    }
                }

                if (ownedType == VFXDataType.ParticleStrip && !hasGPUSpawner)
                    prop = prop.Concat(PropertiesFromType("StripInputProperties"));
                return prop;
            }
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                foreach (var attribute in base.attributes)
                    yield return attribute;
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
            var particleData = GetData() as VFXDataParticle;
            bool isRecordedBounds = particleData && particleData.boundsSettingMode == BoundsSettingMode.Recorded;
            // GPU
            if (target == VFXDeviceTarget.GPU)
            {
                var gpuMapper = VFXExpressionMapper.FromBlocks(activeFlattenedChildrenWithImplicit);
                if (ownedType == VFXDataType.ParticleStrip && !hasGPUSpawner)
                    gpuMapper.AddExpressionsFromSlot(inputSlots[(isRecordedBounds ? 2 : 1)], -1); // strip index
                return gpuMapper;
            }

            // CPU
            var cpuMapper = new VFXExpressionMapper();
            if (particleData)
            {
                switch (particleData.boundsSettingMode)
                {
                    case BoundsSettingMode.Manual:
                        cpuMapper.AddExpressionsFromSlot(inputSlots[0], -1); // bounds
                        break;
                    case BoundsSettingMode.Recorded:
                        cpuMapper.AddExpressionsFromSlot(inputSlots[0], -1); // bounds
                        cpuMapper.AddExpressionsFromSlot(inputSlots[1], -1); //bounds padding
                        break;
                    case BoundsSettingMode.Automatic:
                        cpuMapper.AddExpressionsFromSlot(inputSlots[0], -1); //bounds padding
                        break;
                }
            }

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
