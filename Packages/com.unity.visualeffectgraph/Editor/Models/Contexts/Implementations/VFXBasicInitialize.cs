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
        public override IEnumerable<Variant> GetVariants()
        {
            foreach (var dataType in Enum.GetValues(typeof(VFXDataParticle.DataType)))
            {
                yield return new Variant(
                    "Initialize " + ObjectNames.NicifyVariableName(dataType.ToString()),
                    VFXLibraryStringHelper.Separator("Common", 0),
                    typeof(VFXBasicInitialize),
                    new[] {new KeyValuePair<string, object>("dataType", dataType)}
                );
            }
        }
    }

    [VFXHelpURL("Context-Initialize")]
    [VFXInfo(variantProvider = typeof(InitializeVariantProvider))]
    class VFXBasicInitialize : VFXContext
    {
        public VFXBasicInitialize() : base(VFXContextType.Init, VFXDataType.SpawnEvent, VFXDataType.None) { }
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
            public AABox bounds = new AABox() { size = Vector3.one };
        }

        public class InputPropertiesPadding
        {
            [Range(Single.MinValue * 0.5f, Single.MaxValue * 0.5f), /*Avoids overflow when converting from size to extents*/
             Tooltip(
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
                RefreshErrors();
            }

            base.OnInvalidate(model, cause);
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            VFXSetting capacitySetting = GetSetting("capacity");

            if ((uint)capacitySetting.value > UnityEngine.VFX.VFXManager.maxCapacity)
                report.RegisterError("CapacityOverMaximum", VFXErrorType.Error, "Systems capacity is greater than maximum capacity. This system will be skipped during rendering.\nYou can modify this limit in ProjectSettings/VFX.", this);
            else if ((uint)capacitySetting.value > 1000000)
                report.RegisterError("CapacityOver1M", VFXErrorType.PerfWarning, "Systems with large capacities can be slow to simulate", this);
            var data = GetData() as VFXDataParticle;
            if (data != null && CanBeCompiled())
            {
                if (data.boundsMode == BoundsSettingMode.Recorded)
                {
                    if (VFXViewWindow.GetWindow(GetGraph(), false, false)?.graphView?.attachedComponent == null ||
                        !BoardPreferenceHelper.IsVisible(BoardPreferenceHelper.Board.componentBoard, false))
                    {
                        report.RegisterError("NeedsRecording", VFXErrorType.Warning,
                            "In order to record the bounds, the current graph needs to be attached to a scene instance via the Target Game Object panel", this);
                    }
                    var boundsSlot = inputSlots.FirstOrDefault(s => s.name == nameof(InputPropertiesBounds.bounds));
                    if (boundsSlot != null && boundsSlot.HasLink(true))
                    {
                        report.RegisterError("OverriddenRecording", VFXErrorType.Warning,
                            "This system bounds will not be recorded because they are set from operators.", this);
                    }
                }

                if (data.boundsMode == BoundsSettingMode.Automatic)
                {
                    report.RegisterError("CullingFlagAlwaysSimulate", VFXErrorType.Warning,
                        "Setting the system Bounds Mode to Automatic will switch the culling flags of the Visual Effect asset" +
                        " to 'Always recompute bounds and simulate'.", this);
                }

                if (data.hasTooManyContext)
                {
                    report.RegisterError("TooManyContexts", VFXErrorType.Error, $"Too many contexts within the same system, maximum is {VFXData.kMaxContexts}", this);
                }

                if (data.hasStrip)
                {
                    bool hasDynamicStripIndex = inputSlots.Any(inputSlot => inputSlot.name == "stripIndex" && inputSlot.HasLink() && inputSlot.LinkedSlots.First().GetExpression().Is(VFXExpression.Flags.PerElement));
                    if (hasDynamicStripIndex)
                    {
                        bool hasParticleCountInStripAttribute = data.GetAttributesForContext(this).Any(attribute => attribute.attrib.Equals(VFXAttribute.ParticleCountInStrip));
                        if (hasParticleCountInStripAttribute)
                        {
                            report.RegisterError("WrongParticleCountInStrip", VFXErrorType.Warning,
                                "Using \"Get Particle Count In Strip\" or \"Get Ratio Over Strip\" in this context will only return the correct value if \"Strip Index\" is constant for all particles.", this);
                        }
                    }
                }
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
                    if (particleData.boundsMode == BoundsSettingMode.Manual)
                    {
                        prop = prop.Concat(PropertiesFromType("InputPropertiesBounds"));
                    }
                    if (particleData.boundsMode == BoundsSettingMode.Recorded)
                    {
                        prop = prop.Concat(PropertiesFromType("InputPropertiesBounds"));
                        prop = prop.Concat(PropertiesFromType("InputPropertiesPadding"));
                    }
                    if (particleData.boundsMode == BoundsSettingMode.Automatic)
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

        public sealed override VFXSpace GetOutputSpaceFromSlot(VFXSlot slot)
        {
            if (slot.name == nameof(InputPropertiesBounds.bounds))
                return VFXSpace.Local;
            return base.GetOutputSpaceFromSlot(slot);
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            var particleData = GetData() as VFXDataParticle;
            bool isRecordedBounds = particleData && particleData.boundsMode == BoundsSettingMode.Recorded;
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
                switch (particleData.boundsMode)
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

        protected override IEnumerable<VFXBlock> implicitPreBlock
        {
            get
            {
                var data = GetData();
                if (hasGPUSpawner)
                {
                    // Force "alive" attribute when a system can spawn particles from GPU, because we are updating the entire capacity
                    var block = GetOrCreateImplicitBlock<Block.SetAttribute>(data);
                    block.SetSettingValue(nameof(Block.SetAttribute.attribute), VFXAttribute.Alive.name);
                    yield return block;
                }
            }
        }
    }
}
