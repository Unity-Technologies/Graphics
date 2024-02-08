using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.HighDefinition
{
    // Extract data required to build the UI
    static class FrameSettingsExtractedDatas
    {
        //Most of the data are static, only a few fields need dynamic overrides
        //FieldStatic exists for every FrameSettings in a static way
        //DataLinked is an instance that is bound to a certain target
        //DataLinked.FieldDynamic is the dynamic part that need to be rebound each time
        //DataLinked.FieldDescriptor is an helper to access both static and dynamic part
        //TODO: In future version, find a less hardcoded way to bound the data

        struct FieldStatic
        {
            //All static from the attribute still remains on the attribute.
            //They are all extracted in FieldDescriptor
            public bool ignoreDependencies;
            public Action<object, object> callbackOnChange;
            public FrameSettingsField fieldDependentLabel;
        }

        public class DataLinked
        {
            // Use an enum to have appropriate UI enum field in the frame setting api
            // Do not use anywhere else
            enum ScalableLevel3ForFrameSettingsUIOnly
            {
                Low,
                Medium,
                High
            }

            struct FieldDynamic
            {
                public Func<bool> overrideable;
                public Func<object> overridedGetter;
                public Action<object> overridedSetter;
                public Func<object> overridedDefaultValue;
                public Func<bool> overridedMixedState;
                public Func<string> overridedLabel;
            }

            public struct FieldDescriptor
            {
                readonly DataLinked link;
                
                //static
                public readonly FrameSettingsField field;
                public bool ignoreDependencies => staticFields[field].ignoreDependencies;
                public FrameSettingsField fieldDependentLabel => staticFields[field].fieldDependentLabel;
                public FrameSettingsFieldAttribute.DisplayType displayType => attributes[field].type;
                public string tooltip => attributes[field].tooltip;
                public Type targetType => attributes[field].targetType;
                public int indentLevel => attributes[field].indentLevel;
                public FrameSettings? defaultValues
                {
                    get
                    {
                        if (link.defaultType.HasValue)
                            return GraphicsSettings.GetRenderPipelineSettings<RenderingPathFrameSettings>().GetDefaultFrameSettings(link.defaultType.Value);
                        return null;
                    }
                }
                public bool? enabledInDefault => defaultValues?.IsEnabled(field);
                public FrameSettingsField[] dependencies => attributes[field].dependencies;
                bool IsNegativeDependency(FrameSettingsField otherField) => attributes[field].IsNegativeDependency(otherField);
                public Action<object, object> callbackOnChange => staticFields[field].callbackOnChange;

                //dynamic
                FieldDynamic? fieldDynamic
                {
                    get
                    {
                        if (link.dynamicFields.ContainsKey(field))
                            return link.dynamicFields[field];
                        return null;
                    }
                }
                public UnityEngine.Object[] targetObjects => link.data.targetObjects;
                public Func<bool> overrideable => fieldDynamic?.overrideable;
                public Func<object> overridedGetter => fieldDynamic?.overridedGetter;
                public Action<object> overridedSetter => fieldDynamic?.overridedSetter;
                public Func<object> overridedDefaultValue => fieldDynamic?.overridedDefaultValue;
                
                //Compound static/dynamic
                public string displayedName => fieldDynamic?.overridedLabel?.Invoke() ?? attributes[field].displayedName;

                //data
                public bool hasMultipleDifferentValues => link.data.HasMultipleDifferentValues(field) || (fieldDynamic?.overridedMixedState?.Invoke() ?? false);
                public bool? enabled
                {
                    get => link.data.GetEnabled(field);
                    set {
                        if (value.HasValue) 
                            link.data.SetEnabled(field, value.Value);
                    }
                }
                public bool enabledUnchecked
                {
                    get => link.data.GetEnabledUnchecked(field);
                    set => link.data.SetEnabled(field, value);
                }
                
                public struct Override
                {
                    SerializedFrameSettings.Mask mask;
                    FieldDescriptor field;
                    internal Override(SerializedFrameSettings.Mask mask, FieldDescriptor field)
                    {
                        this.mask = mask;
                        this.field = field;
                    }
                    
                    public bool hasMultipleDifferentOverrides => mask.HasMultipleDifferentOverrides(field.field);
                    public bool? overrided
                    {
                        get => mask.GetOverrided(field.field);
                        set {
                            if (value.HasValue) 
                                mask.SetOverrided(field.field, value.Value);
                        }
                    }
                    public bool overridedUnchecked
                    {
                        get => mask.GetOverridedUnchecked(field.field);
                        set => mask.SetOverrided(field.field, value);
                    }
                    
                    public bool IsOverrideableWithDependencies()
                    {
                        bool EvaluateBoolWithOverride(Override o, bool negative)
                        {
                            bool value;
                            if (o.overrided ?? false)
                                value = o.field.enabled ?? false;
                            else
                                value = GraphicsSettings.GetRenderPipelineSettings<RenderingPathFrameSettings>().GetDefaultFrameSettings(o.field.link.defaultType.Value).IsEnabled(o.field.field);
                            return value ^ negative;
                        }

                        bool locallyOverrideable = field.overrideable?.Invoke() ?? true;
                        FrameSettingsField[] dependencies = field.dependencies;
                        if (dependencies == null || staticFields[field.field].ignoreDependencies || !locallyOverrideable)
                            return locallyOverrideable;

                        if (!field.link.defaultType.HasValue)
                            return true;

                        bool dependenciesOverrideable = true;
                        for (int index = dependencies.Length - 1; index >= 0 && dependenciesOverrideable; --index)
                        {
                            FrameSettingsField dependency = dependencies[index];
                            dependenciesOverrideable &= EvaluateBoolWithOverride(field.link.GetFieldDescriptor(dependency).GetOverrideInterface(mask), field.IsNegativeDependency(dependency));
                        }
                        return dependenciesOverrideable;
                    }
                }

                public Override GetOverrideInterface(SerializedFrameSettings.Mask mask) => new Override(mask, this);

                public DataLinked boundData => link;

                internal FieldDescriptor(FrameSettingsField field, DataLinked link)
                {
                    this.field = field;
                    this.link = link;
                }
            }

            Dictionary<FrameSettingsField, FieldDynamic> dynamicFields;

            SerializedFrameSettings.Data data;
            FrameSettingsRenderType? defaultType;
            HDRenderPipelineAsset hdrpAsset; //sadly required for scalability settings at the moment
            
            public SerializedFrameSettings.Data boundData => data;
            public FrameSettingsRenderType? boundDefaultType => defaultType;
            public HDRenderPipelineAsset boundRPAsset => hdrpAsset;

            internal DataLinked(SerializedFrameSettings.Data serializedData, FrameSettingsRenderType? defaultType, HDRenderPipelineAsset hdrpAsset)
            {
                data = serializedData;
                this.defaultType = defaultType;
                this.hdrpAsset = hdrpAsset;
                
                AddDynamicOverrides();
            }
            
            void AddDynamicOverrides()
            {
                dynamicFields = new();
                var @default = defaultType.HasValue ? GraphicsSettings.GetRenderPipelineSettings<RenderingPathFrameSettings>().GetDefaultFrameSettings(defaultType.Value) : (FrameSettings?)null;

                // Rendering
                dynamicFields[FrameSettingsField.MSAAMode] = new() {
                    overridedDefaultValue = () => @default?.msaaMode ?? MSAAMode.FromHDRPAsset,
                    overridedGetter = () => data.msaaMode.GetEnumValue<MSAAMode>(),
                    overridedSetter = v => data.msaaMode.SetEnumValue((MSAAMode)v),
                    overridedMixedState = () => data.msaaMode.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.LODBiasMode] = new() {
                    overridedDefaultValue = () => @default?.lodBiasMode ?? data.lodBiasMode.GetEnumValue<LODBiasMode>(),
                    overridedGetter = () => data.lodBiasMode.GetEnumValue<LODBiasMode>(),
                    overridedSetter = v => data.lodBiasMode.SetEnumValue((LODBiasMode)v),
                    overridedMixedState = () => data.lodBiasMode.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.LODBiasQualityLevel] = new() {
                    overridedDefaultValue = () => (ScalableLevel3ForFrameSettingsUIOnly)(@default?.lodBiasQualityLevel ?? data.lodBiasQualityLevel.intValue),
                    overridedGetter = () => (ScalableLevel3ForFrameSettingsUIOnly)data.lodBiasQualityLevel.intValue,
                    overridedSetter = v => data.lodBiasQualityLevel.intValue = (int)v,
                    overrideable = () => data.lodBiasMode.GetEnumValue<LODBiasMode>() != LODBiasMode.OverrideQualitySettings,
                    overridedMixedState = () => data.lodBiasQualityLevel.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.LODBias] = new() {
                    overridedDefaultValue = () => hdrpAsset?.currentPlatformRenderPipelineSettings.lodBias[data.lodBiasQualityLevel.intValue] ?? 0,
                    overridedGetter = () => data.lodBias.floatValue,
                    overridedSetter = v => data.lodBias.floatValue = (float)v,
                    overrideable = () => data.lodBiasMode.GetEnumValue<LODBiasMode>() != LODBiasMode.FromQualitySettings,
                    overridedLabel = () => data.lodBiasMode.GetEnumValue<LODBiasMode>() == LODBiasMode.ScaleQualitySettings ? "Scale Factor" : "LOD Bias",
                    overridedMixedState = () => data.lodBias.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.MaximumLODLevelMode] = new() {
                    overridedDefaultValue = () => @default?.maximumLODLevelMode ?? data.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>(),
                    overridedGetter = () => data.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>(),
                    overridedSetter = v => data.maximumLODLevelMode.SetEnumValue((MaximumLODLevelMode)v),
                    overridedMixedState = () => data.maximumLODLevelMode.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.MaximumLODLevelQualityLevel] = new() {
                    overridedDefaultValue = () => (ScalableLevel3ForFrameSettingsUIOnly) (@default?.maximumLODLevelQualityLevel ?? data.maximumLODLevelQualityLevel.intValue),
                    overridedGetter = () => (ScalableLevel3ForFrameSettingsUIOnly)data.maximumLODLevelQualityLevel.intValue,
                    overridedSetter = v => data.maximumLODLevelQualityLevel.intValue = (int)v,
                    overrideable = () => data.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() != MaximumLODLevelMode.OverrideQualitySettings,
                    overridedMixedState = () => data.maximumLODLevelQualityLevel.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.MaximumLODLevel] = new() {
                    overridedDefaultValue = () => hdrpAsset?.currentPlatformRenderPipelineSettings.maximumLODLevel[data.maximumLODLevelQualityLevel.intValue] ?? 0,
                    overridedGetter = () => data.maximumLODLevel.intValue,
                    overridedSetter = v => data.maximumLODLevel.intValue = (int)v,
                    overrideable = () => data.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() != MaximumLODLevelMode.FromQualitySettings,
                    overridedLabel = () => data.maximumLODLevelMode.GetEnumValue<MaximumLODLevelMode>() == MaximumLODLevelMode.OffsetQualitySettings ? "Offset Factor" : "Maximum LOD Level",
                    overridedMixedState = () => data.maximumLODLevel.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.MaterialQualityLevel] = new() {
                    overridedDefaultValue = () => @default?.materialQuality.Into() ?? MaterialQualityMode.Medium,
                    overridedGetter = () => ((MaterialQuality)data.materialQuality.intValue).Into(),
                    overridedSetter = v => data.materialQuality.intValue = (int)((MaterialQualityMode)v).Into(),
                    overridedMixedState = () => data.materialQuality.hasMultipleDifferentValues,
                };

                // Lighting
                dynamicFields[FrameSettingsField.SssQualityMode] = new() {
                    overridedDefaultValue = () => SssQualityMode.FromQualitySettings,
                    overridedGetter = () => data.sssQualityMode.GetEnumValue<SssQualityMode>(),
                    overridedSetter = v => data.sssQualityMode.SetEnumValue((SssQualityMode)v),
                    overrideable = () => data.GetEnabled(FrameSettingsField.SubsurfaceScattering) ?? false,
                    overridedMixedState = () => data.sssQualityMode.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.SssQualityLevel] = new() {
                    overridedDefaultValue = () => ScalableLevel3ForFrameSettingsUIOnly.Low,
                    overridedGetter = () => (ScalableLevel3ForFrameSettingsUIOnly)data.sssQualityLevel.intValue, // 3 levels
                    overridedSetter = v => data.sssQualityLevel.intValue = Math.Max(0, Math.Min((int)v, 2)), // Levels 0-2
                    overrideable = () => (data.GetEnabled(FrameSettingsField.SubsurfaceScattering) ?? false)
                        && (data.sssQualityMode.GetEnumValue<SssQualityMode>() == SssQualityMode.FromQualitySettings),
                    overridedMixedState = () => data.sssQualityLevel.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.SssCustomSampleBudget] = new() {
                    overridedDefaultValue = () => (int)DefaultSssSampleBudgetForQualityLevel.Low,
                    overridedGetter = () => data.sssCustomSampleBudget.intValue,
                    overridedSetter = v => data.sssCustomSampleBudget.intValue = Math.Max(1, Math.Min((int)v, (int)DefaultSssSampleBudgetForQualityLevel.Max)),
                    overrideable = () => (data.GetEnabled(FrameSettingsField.SubsurfaceScattering) ?? false)
                        && (data.sssQualityMode.GetEnumValue<SssQualityMode>() != SssQualityMode.FromQualitySettings),
                    overridedMixedState = () => data.sssCustomSampleBudget.hasMultipleDifferentValues,
                };
                dynamicFields[FrameSettingsField.SssCustomDownsampleSteps] = new() {
                    overridedDefaultValue = () => 0,
                    overridedGetter = () => data.sssDownsampleSteps.intValue,
                    overridedSetter = v => data.sssDownsampleSteps.intValue = Math.Max(0, Math.Min((int)v, (int)DefaultSssDownsampleSteps.Max)),
                    overrideable = () => (data.GetEnabled(FrameSettingsField.SubsurfaceScattering) ?? false)
                        && (data.sssQualityMode.GetEnumValue<SssQualityMode>() != SssQualityMode.FromQualitySettings),
                    overridedMixedState = () => data.sssDownsampleSteps.hasMultipleDifferentValues,
                };

                // AsyncCompute
                    //nothing here

                //LightLoop
                    //nothing here
            }

            public FieldDescriptor GetFieldDescriptor(FrameSettingsField field)
                => new(field, this);

            public IEnumerable<FieldDescriptor> GetDescriptorForGroup(int groupIndex)
            {
                if (!groups.ContainsKey(groupIndex))
                    throw new ArgumentOutOfRangeException(nameof(groupIndex), $"Value {groupIndex} is out of range");

                foreach (var field in groups[groupIndex])
                    yield return GetFieldDescriptor(field);
            }

            public void SetAllAllowedOverridesTo(SerializedFrameSettings.Mask mask, int groupIndex, bool value)
            {
                foreach (var field in GetDescriptorForGroup(groupIndex))
                {
                    var o = field.GetOverrideInterface(mask);
                    if (o.IsOverrideableWithDependencies())
                        o.overrided = value;
                }
            }
        }
        
        static readonly Dictionary<FrameSettingsField, FrameSettingsFieldAttribute> attributes;
        static readonly Dictionary<int, List<FrameSettingsField>> groups;
        static readonly Dictionary<FrameSettingsField, FieldStatic> staticFields;
        
        static FrameSettingsExtractedDatas()
        {
            attributes = new();
            groups = new();
            staticFields = new();
            staticFields = new();
            Dictionary<FrameSettingsField, string> frameSettingsEnumNameMap = FrameSettingsFieldAttribute.GetEnumNameMap();
            Type type = typeof(FrameSettingsField);
            foreach (FrameSettingsField enumVal in frameSettingsEnumNameMap.Keys)
            {
                var attr = type.GetField(frameSettingsEnumNameMap[enumVal]).GetCustomAttribute<FrameSettingsFieldAttribute>();
                if (attr == null)
                    continue;

                if (!groups.ContainsKey(attr.group))
                    groups[attr.group] = new();
                groups[attr.group].Add(enumVal);
                attributes[enumVal] = attr;
                staticFields[enumVal] = new() { fieldDependentLabel = FrameSettingsField.None };
            }
            AmmendStaticDependencies();

            for (int groupIndex = 0; groupIndex < groups.Count; ++groupIndex)
                groups[groupIndex].Sort((a, b) => attributes[a].orderInGroup != attributes[b].orderInGroup ? attributes[a].orderInGroup.CompareTo(attributes[b].orderInGroup) : a.CompareTo(b));
        }
        
        static void AmmendStaticDependencies()
        {
            void AmmendInfo(FrameSettingsField field, bool ignoreDependencies = false, Action<object, object> callbackOnChange = null, FrameSettingsField fieldDependentLabel = FrameSettingsField.None)
            {
                FieldStatic tmp = staticFields[field];
                tmp.ignoreDependencies = ignoreDependencies;
                tmp.callbackOnChange = callbackOnChange;
                tmp.fieldDependentLabel = fieldDependentLabel;
                staticFields[field] = tmp;
            }

            // Rendering
            AmmendInfo(FrameSettingsField.DepthPrepassWithDeferredRendering, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.ClearGBuffers, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.MSAAMode, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.ComputeThickness, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.DecalLayers, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.ObjectMotionVectors, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.TransparentsWriteMotionVector, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.LODBiasQualityLevel, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.LODBias, ignoreDependencies: true, fieldDependentLabel: FrameSettingsField.LODBiasMode);
            AmmendInfo(FrameSettingsField.MaximumLODLevelQualityLevel, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.MaximumLODLevel, ignoreDependencies: true, fieldDependentLabel: FrameSettingsField.MaximumLODLevelMode);
            AmmendInfo(FrameSettingsField.Decals, callbackOnChange: (oldVal, newVal) => VFX.HDRP.VFXHDRPSettingsUtility.RefreshVfxErrorsIfNeeded());
            AmmendInfo(FrameSettingsField.DecalLayers, callbackOnChange: (oldVal, newVal) => VFX.HDRP.VFXHDRPSettingsUtility.RefreshVfxErrorsIfNeeded());

            // Lighting
            AmmendInfo(FrameSettingsField.Volumetrics, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.ReprojectionForVolumetrics, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.TransparentSSR, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.SssQualityMode, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.SssQualityLevel, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.SssCustomSampleBudget, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.SssCustomDownsampleSteps, ignoreDependencies: true);

            // AsyncCompute
            AmmendInfo(FrameSettingsField.LightListAsync, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.SSRAsync, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.SSAOAsync, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.ContactShadowsAsync, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.VolumeVoxelizationsAsync, ignoreDependencies: true);

            //LightLoop
            AmmendInfo(FrameSettingsField.ComputeLightVariants, ignoreDependencies: true);
            AmmendInfo(FrameSettingsField.ComputeMaterialVariants, ignoreDependencies: true);
        }

        static internal FrameSettingsFieldAttribute GetFieldAttribute(FrameSettingsField field) => attributes[field];

        public static DataLinked CreateBoundInstance(SerializedFrameSettings.Data serializedData, FrameSettingsRenderType? defaultType, HDRenderPipelineAsset hdrpAsset)
            => new DataLinked(serializedData, defaultType, hdrpAsset);

        public static int GetGroupLength(int groupIndex)
        {
            if (!groups.ContainsKey(groupIndex))
                throw new ArgumentOutOfRangeException(nameof(groupIndex), $"Value {groupIndex} is out of range");
            return groups[groupIndex].Count;
        }

        public static bool Exists(int groupIndex)
            => groups.ContainsKey(groupIndex);
    }

}
