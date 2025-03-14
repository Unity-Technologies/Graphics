using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    interface IExpressionProvider
    {
        VFXNamedExpression GetExpression(List<VFXNamedExpression> slotExpressions);
    }

    struct ExpressionFromSlot : IExpressionProvider
    {
        string name;
        public static explicit operator ExpressionFromSlot(string _name) => new() { name = _name };
        public VFXNamedExpression GetExpression(List<VFXNamedExpression> slotExpressions)
        {
            var name = this.name;
            return slotExpressions.Find(o => o.name == name);
        }
    }

    struct ShaderKeywordExpressionFromSlot : IExpressionProvider
    {
        string slotName;
        string keywordName;
        uint keywordValue;

        public ShaderKeywordExpressionFromSlot(string _slotName, string _keywordName, uint _keywordValue = uint.MaxValue)
        {
            slotName = _slotName;
            keywordName = _keywordName;
            keywordValue = _keywordValue;
        }

        public VFXNamedExpression GetExpression(List<VFXNamedExpression> slotExpressions)
        {
            var name = this.slotName;
            var refExpression = slotExpressions.Find(o => o.name == name);

            var keywordPrefixed = VFXSGInputs.kKeywordPrefix + keywordName;
            if (refExpression.exp.valueType == VFXValueType.Boolean)
            {
                return new VFXNamedExpression(refExpression.exp, keywordPrefixed);
            }
            else
            {
                if (refExpression.exp.valueType != VFXValueType.Uint32)
                    throw new InvalidOperationException("Unexpected input slot from keyword: " + slotName);

                var compare = new VFXExpressionCondition(VFXValueType.Uint32, VFXCondition.Equal, refExpression.exp, VFXValue.Constant(keywordValue));
                return new VFXNamedExpression(compare, keywordPrefixed);
            }
        }
    }

    struct ExpressionConstant : IExpressionProvider
    {
        VFXNamedExpression namedExpression;

        public static explicit operator ExpressionConstant(VFXNamedExpression _namedExpression) => new() { namedExpression = _namedExpression };

        public VFXNamedExpression GetExpression(List<VFXNamedExpression> _)
        {
            return namedExpression;
        }
    }

    struct TraitDescription
    {
        public Dictionary<string, (VFXSetting setting, VFXSettingAttribute attribute)> settings;
        public List<VFXPropertyWithValue> properties;
        public HashSet<string> hiddenSettings;
        public List<IExpressionProvider> propertiesGpuExpressions;
        public List<IExpressionProvider> propertiesCpuExpressions;
        public List<string> additionalDefines;
        public VFXTaskType taskType;
        public string name;

        public bool? hasShadowCasting;
        public int? sortingPriority;
        public bool supportMotionVectorPerVertex;
        public uint motionVectorPerVertexCount;
        public VFXAbstractRenderedOutput.BlendMode blendMode;
        public VFXOutputUpdate.Features features;
        public bool hasAlphaClipping;
        public List<(string key, VFXErrorType type, string description)> errors;
    }

    [Serializable]
    abstract class ParticleTrait
    {
        public virtual TraitDescription GetDescription(VFXAbstractComposedParticleOutput parent)
        {
            var traitDescription = new TraitDescription()
            {
                settings = new(),
                properties = new(),
                hiddenSettings = new(),
                propertiesGpuExpressions = new(),
                propertiesCpuExpressions = new(),
                additionalDefines = new(),
                taskType = VFXTaskType.None,
                errors = new()
            };

            foreach (var field in VFXModel.GetFields(GetType()))
            {
                var vfxSetting = new VFXSetting(field.field, this);
                traitDescription.settings.Add(vfxSetting.name, (vfxSetting, field.attribute));
            }
            return traitDescription;
        }

        public virtual void GetImportDependentAssets(HashSet<int> dependencies)
        {
        }

        public virtual bool CanBeCompiled()
        {
            return true;
        }

        protected static IEnumerable<VFXPropertyWithValue> PropertiesFromType(Type type)
        {
            return VFXSlotContainerModel<VFXContext, VFXBlock>.PropertiesFromType(type);
        }

        public virtual void InitInspectorGUI(VFXAbstractComposedParticleOutput parent)
        {
        }

        public virtual void DoInspectorGUI(VFXAbstractComposedParticleOutput parent, out VFXModel.InvalidationCause? invalidationCause)
        {
            invalidationCause = null;
        }

        public virtual void ReleaseInspectorGUI(VFXAbstractComposedParticleOutput parent)
        {
        }
    }

    [Serializable]
    abstract class ParticleTopology : ParticleTrait
    {
    }

    [Serializable]
    abstract class ParticleShading : ParticleTrait
    {
        public virtual void SetupMaterial(VFXAbstractComposedParticleOutput parent, Material material)
        {
        }
    }

    [CustomEditor(typeof(VFXAbstractComposedParticleOutput), true)]
    [CanEditMultipleObjects]
    class VFXComposedParticleOutputEditor : VFXAbstractParticleOutputEditor
    {
        protected sealed override void OnEnable()
        {
            base.OnEnable();
            foreach (var context in targets)
            {
                var composedParticleOutput = (VFXAbstractComposedParticleOutput)context;
                composedParticleOutput.InitInspectorGUI();
            }
        }

        internal class Content
        {
            public static GUIContent particlesOptionHeader { get; } = EditorGUIUtility.TrTextContent("Particles Options");
        }

        private bool m_ShowParticleOptions = true;
        public sealed override void OnInspectorGUI()
        {
            PrepareContextEditorGUI();
            DisplayName();

            foreach (var context in targets)
            {
                var composedParticleOutput = (VFXAbstractComposedParticleOutput)context;
                composedParticleOutput.DoInspectorGUI();
            }

            m_ShowParticleOptions = CoreEditorUtils.DrawHeaderFoldout(Content.particlesOptionHeader, m_ShowParticleOptions);
            if (m_ShowParticleOptions)
                DoDefaultContextEditorGUI();

            DisplayWarnings();
            DisplaySummary();
        }

        public void OnDisable()
        {
            foreach (var context in targets)
            {
                var composedParticleOutput = (VFXAbstractComposedParticleOutput)context;
                composedParticleOutput.ReleaseInspectorGUI();
            }
        }
    }

    abstract class VFXAbstractComposedParticleOutput : VFXAbstractParticleOutput, IVFXMultiMeshOutput, IVFXShaderGraphOutput
    {
        protected VFXAbstractComposedParticleOutput(bool strip) : base(strip)
        {
            m_Shading = new ParticleShadingShaderGraph();
        }

        public sealed override string name => GetOrRefreshDecription().name ?? "NULL";

        public sealed override string codeGeneratorTemplate => null;
        public sealed override bool doesGenerateShader => true;

        public sealed override VFXTaskType taskType
        {
            get
            {
                var desc = GetOrRefreshDecription();
                return desc.taskType;
            }
        }

        public sealed override IEnumerable<string> additionalDefines
        {
            get
            {
                foreach (var d in base.additionalDefines)
                    yield return d;

                var desc = GetOrRefreshDecription();
                foreach (var d in desc.additionalDefines)
                    yield return d;
            }
        }

        [SerializeReference, VFXSetting] protected ParticleTopology m_Topology;
        [SerializeReference, VFXSetting] protected ParticleShading m_Shading;

        private TraitDescription m_CacheComposedTraitDescription = new()
        {
            settings = new(),
            properties = new(),
            hiddenSettings = new(),
            propertiesGpuExpressions = new(),
            propertiesCpuExpressions = new(),
            additionalDefines = new(),
            errors = new (),
            taskType = VFXTaskType.None
        };

        static int ComputeErrorHashCode(List<(string key, VFXErrorType type, string description)> errors)
        {
            int hashCode = 0;
            foreach (var error in errors)
            {
                hashCode = HashCode.Combine(hashCode, error.key.GetHashCode());
            }
            return hashCode;
        }

        private TraitDescription GetOrRefreshDecription(bool refreshError = true)
        {
            if (m_CacheComposedTraitDescription.taskType == VFXTaskType.None && m_Topology != null && m_Shading != null)
            {
                var topologyDescription = m_Topology.GetDescription(this);
                var shadingDescription = m_Shading.GetDescription(this);

                m_CacheComposedTraitDescription.settings.Clear();
                foreach (var entry in topologyDescription.settings)
                    m_CacheComposedTraitDescription.settings.Add(entry.Key, entry.Value);

                foreach (var entry in shadingDescription.settings)
                    m_CacheComposedTraitDescription.settings.Add(entry.Key, entry.Value);

                m_CacheComposedTraitDescription.properties.Clear();
                m_CacheComposedTraitDescription.properties.AddRange(topologyDescription.properties);
                m_CacheComposedTraitDescription.properties.AddRange(shadingDescription.properties);

                m_CacheComposedTraitDescription.hiddenSettings.Clear();
                m_CacheComposedTraitDescription.hiddenSettings.Add(nameof(m_Topology));
                m_CacheComposedTraitDescription.hiddenSettings.Add(nameof(m_Shading));
                m_CacheComposedTraitDescription.hiddenSettings.UnionWith(topologyDescription.hiddenSettings);
                m_CacheComposedTraitDescription.hiddenSettings.UnionWith(shadingDescription.hiddenSettings);

                m_CacheComposedTraitDescription.propertiesGpuExpressions.Clear();
                m_CacheComposedTraitDescription.propertiesGpuExpressions.AddRange(topologyDescription.propertiesGpuExpressions);
                m_CacheComposedTraitDescription.propertiesGpuExpressions.AddRange(shadingDescription.propertiesGpuExpressions);

                m_CacheComposedTraitDescription.propertiesCpuExpressions.Clear();
                m_CacheComposedTraitDescription.propertiesCpuExpressions.AddRange(topologyDescription.propertiesCpuExpressions);
                m_CacheComposedTraitDescription.propertiesCpuExpressions.AddRange(shadingDescription.propertiesCpuExpressions);

                m_CacheComposedTraitDescription.additionalDefines.Clear();
                m_CacheComposedTraitDescription.additionalDefines.AddRange(topologyDescription.additionalDefines);
                m_CacheComposedTraitDescription.additionalDefines.AddRange(shadingDescription.additionalDefines);

                m_CacheComposedTraitDescription.motionVectorPerVertexCount = topologyDescription.motionVectorPerVertexCount;
                m_CacheComposedTraitDescription.supportMotionVectorPerVertex = topologyDescription.supportMotionVectorPerVertex && shadingDescription.supportMotionVectorPerVertex;
                m_CacheComposedTraitDescription.hasShadowCasting = shadingDescription.hasShadowCasting;
                m_CacheComposedTraitDescription.sortingPriority = shadingDescription.sortingPriority;
                m_CacheComposedTraitDescription.blendMode = shadingDescription.blendMode;
                m_CacheComposedTraitDescription.hasAlphaClipping = shadingDescription.hasAlphaClipping;

                m_CacheComposedTraitDescription.taskType = topologyDescription.taskType | shadingDescription.taskType;
                m_CacheComposedTraitDescription.features = topologyDescription.features | shadingDescription.features;
                m_CacheComposedTraitDescription.name = $"Output " +
                                                       $"{(ownedType == VFXDataType.ParticleStrip ? "ParticleStrip" : "Particle")}".AppendLabel("ShaderGraph") +
                                                       $"\n{topologyDescription.name}" +
                                                       (string.IsNullOrEmpty(shadingDescription.name) ? string.Empty : $" - {shadingDescription.name}");


                var oldHashCode = ComputeErrorHashCode(m_CacheComposedTraitDescription.errors);

                m_CacheComposedTraitDescription.errors.Clear();
                m_CacheComposedTraitDescription.errors.AddRange(topologyDescription.errors);
                m_CacheComposedTraitDescription.errors.AddRange(shadingDescription.errors);

                var newHashCode = ComputeErrorHashCode(m_CacheComposedTraitDescription.errors);
                if (refreshError && oldHashCode != newHashCode)
                    RefreshErrors();

                if (m_CacheComposedTraitDescription.taskType == VFXTaskType.None)
                    throw new InvalidOperationException();
            }

            return m_CacheComposedTraitDescription;
        }

        void MarkCacheAsDirty()
        {
            //Avoid usage of independent boolean to be consistent with domain reload behavior (avoid restoring state)
            m_CacheComposedTraitDescription.taskType = VFXTaskType.None;
        }

        internal static string GetName(VFXDataType dataType,string topologyDescription, string shadingDescription)
        {
            return $"Output " +
                $"{(dataType == VFXDataType.ParticleStrip ? "ParticleStrip" : "Particle")} ShaderGraph" +
                $"\n{topologyDescription}" +
                (string.IsNullOrEmpty(shadingDescription) ? string.Empty : $" - {shadingDescription}");
        }

        public void InitInspectorGUI()
        {
            m_Shading?.InitInspectorGUI(this);
            m_Topology?.InitInspectorGUI(this);
        }

        public void DoInspectorGUI()
        {
            InvalidationCause? invalidationShading = null;
            InvalidationCause? invalidationTopology = null;

            m_Shading?.DoInspectorGUI(this, out invalidationShading);
            m_Topology?.DoInspectorGUI(this, out invalidationTopology);

            if (invalidationShading != null)
                Invalidate(this, (InvalidationCause)invalidationShading);
            if (invalidationTopology != null)
                Invalidate(this, (InvalidationCause)invalidationTopology);
        }

        public void ReleaseInspectorGUI()
        {
            m_Shading?.ReleaseInspectorGUI(this);
            m_Topology?.ReleaseInspectorGUI(this);
        }

        protected internal sealed override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            if (cause == InvalidationCause.kStructureChanged || cause == InvalidationCause.kSettingChanged)
            {
                MarkCacheAsDirty();
            }
            base.Invalidate(model, cause);
        }

        internal override void GenerateErrors(VFXErrorReporter report)
        {
            base.GenerateErrors(report);
            if (SerializationUtility.HasManagedReferencesWithMissingTypes(this))
            {
                report.RegisterError("AnyMissingRef", VFXErrorType.Error, "Missing Assembly reference(s).", this);
            }
            else
            {
                var desc = GetOrRefreshDecription(false);
                foreach (var error in desc.errors)
                    report.RegisterError(error.key, error.type, error.description, this);
            }
        }

        public sealed override bool CanBeCompiled()
        {
            if (m_Topology == null || m_Shading == null)
                return false;
            return base.CanBeCompiled() && m_Topology.CanBeCompiled() && m_Shading.CanBeCompiled();
        }

        public sealed override void GetImportDependentAssets(HashSet<int> dependencies)
        {
            base.GetImportDependentAssets(dependencies);
            m_Topology?.GetImportDependentAssets(dependencies);
            m_Shading?.GetImportDependentAssets(dependencies);
        }

        public sealed override void OnUnknownChange()
        {
            MarkCacheAsDirty();
            base.OnUnknownChange();
        }

        public sealed override void CheckGraphBeforeImport()
        {
            if (m_Topology != null && m_Shading != null)
            {
                MarkCacheAsDirty();
                base.CheckGraphBeforeImport();
                if (!VFXGraph.explicitCompile)
                {
                    ResyncSlots(true);
                }
            }
        }

        public sealed override IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags)
        {
            var desc = GetOrRefreshDecription();
            var baseSettings = base.GetSettings(listHidden, flags);
            foreach (var setting in baseSettings)
                if (!desc.hiddenSettings.Contains(setting.name))
                    yield return setting;

            foreach (var setting in desc.settings)
            {
                var vfxSetting = setting.Value;
                if (ShouldSettingBeListed(vfxSetting.setting.field, vfxSetting.attribute, listHidden, flags, desc.hiddenSettings))
                    yield return vfxSetting.setting;
            }
        }

        public sealed override VFXSetting GetSetting(string name)
        {
            var currentSetting = base.GetSetting(name);
            if (currentSetting.valid)
                return currentSetting;

            if (GetOrRefreshDecription().settings.TryGetValue(name, out var entry))
                return entry.setting;

            return new VFXSetting(null, null);
        }

        protected sealed override IEnumerable<string> untransferableSettings
        {
            get
            {
                foreach (var setting in base.untransferableSettings)
                    yield return setting;
                yield return "primitiveType";
            }
        }

        protected sealed override IEnumerable<VFXPropertyWithValue> inputProperties
        {
            get
            {
                var properties = base.inputProperties;
                foreach (var property in properties)
                    yield return property;

                var desc = GetOrRefreshDecription();
                foreach (var property in desc.properties)
                    yield return property;
            }
        }

        public sealed override bool hasShadowCasting => GetOrRefreshDecription().hasShadowCasting ?? base.hasShadowCasting;

        public sealed override bool implementsMotionVector => true;

        public sealed override int GetMaterialSortingPriority()
        {
            return GetOrRefreshDecription().sortingPriority ?? 0;
        }

        public sealed override bool SupportsMotionVectorPerVertex(out uint vertsCount)
        {
            var desc = GetOrRefreshDecription();
            vertsCount = desc.motionVectorPerVertexCount;
            return desc.supportMotionVectorPerVertex;
        }

        public sealed override bool isBlendModeOpaque => GetOrRefreshDecription().blendMode == BlendMode.Opaque;

        public sealed override bool HasSorting()
        {
            var desc = GetOrRefreshDecription();
            if (sort == SortActivationMode.On)
                return true;
            if (sort == SortActivationMode.Auto && (desc.blendMode == BlendMode.Alpha || desc.blendMode == BlendMode.AlphaPremultiplied))
                return true;
            return false;
        }

        protected sealed override bool hasExposure => false;
        public sealed override bool exposeAlphaThreshold => false;

        public sealed override VFXOutputUpdate.Features outputUpdateFeatures
        {
            get
            {
                var desc = GetOrRefreshDecription();
                var features = base.outputUpdateFeatures | desc.features;
                if (HasSorting() && VFXOutputUpdate.HasFeature(features, VFXOutputUpdate.Features.IndirectDraw) || needsOwnSort)
                {
                    if (VFXSortingUtility.IsPerCamera(sortMode))
                        features |= VFXOutputUpdate.Features.CameraSort;
                    else
                        features |= VFXOutputUpdate.Features.Sort;
                }

                return features;
            }
        }

        public sealed override bool usesMaterialVariantInEditMode => CanBeCompiled();

        public sealed override void SetupMaterial(Material material)
        {
            m_Shading?.SetupMaterial(this, material);
        }

        protected sealed override IEnumerable<VFXNamedExpression> CollectGPUExpressions(IEnumerable<VFXNamedExpression> slotExpressions)
        {
            //Directly implemented in GetExpressionMapper, parent CollectGPUExpressions is only relevant for built in
            throw new NotImplementedException();
        }

        public override VFXExpressionMapper GetExpressionMapper(VFXDeviceTarget target)
        {
            VFXExpressionMapper mapper = null;
            var desc = GetOrRefreshDecription();
            var expressionsSlots = new List<VFXNamedExpression>(GetExpressionsFromSlots(this));

            switch (target)
            {
                case VFXDeviceTarget.CPU:
                {
                    mapper = new();
                    foreach (var expression in desc.propertiesCpuExpressions)
                        mapper.AddExpression(expression.GetExpression(expressionsSlots), -1);
                    break;
                }
                case VFXDeviceTarget.GPU:
                {
                    mapper = VFXExpressionMapper.FromBlocks(activeFlattenedChildrenWithImplicit);
                    foreach (var expression in desc.propertiesGpuExpressions)
                        mapper.AddExpression(expression.GetExpression(expressionsSlots), -1);
                    if (generateMotionVector)
                        mapper.AddExpression(VFXBuiltInExpression.FrameIndex, "currentFrameIndex", -1);
                    break;
                }
            }

            return mapper;
        }

        public override IEnumerable<VFXAttributeInfo> attributes
        {
            get
            {
                yield return new VFXAttributeInfo(VFXAttribute.Alive, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Position, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AxisZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.AngleZ, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.PivotZ, VFXAttributeMode.Read);

                yield return new VFXAttributeInfo(VFXAttribute.Size, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleX, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleY, VFXAttributeMode.Read);
                yield return new VFXAttributeInfo(VFXAttribute.ScaleZ, VFXAttributeMode.Read);
            }
        }

        public uint meshCount
        {
            get
            {
                if (m_Topology is IVFXMultiMeshOutput topologyMesh)
                    return topologyMesh.meshCount;
                return 0;
            }
        }

        public ShaderGraphVfxAsset GetShaderGraph()
        {
            if (m_Shading is IVFXShaderGraphOutput shadingShaderGraph)
                return shadingShaderGraph.GetShaderGraph();
            return null;
        }
    }
}
