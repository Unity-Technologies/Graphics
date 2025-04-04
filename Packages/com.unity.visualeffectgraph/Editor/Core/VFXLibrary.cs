using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using UnityEngine.Profiling;

using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace UnityEditor.VFX
{
    class Variant
    {
        public string name { get; }
        public string category { get; set; }
        public string[] synonyms { get; set; }
        public Type modelType { get; }
        public Func<VariantProvider> variantProvider { get; }
        public KeyValuePair<string, object>[] settings { get; }
        public bool supportFavorite { get; }

        public Variant(string name, string category, Type modelType, KeyValuePair<string, object>[] kvp, Func<VariantProvider> variantProvider = null, string[] synonyms = null, bool supportFavorite = true)
        {
            this.name = name;
            this.category = category;
            this.modelType = modelType;
            this.settings = kvp;
            this.supportFavorite = supportFavorite;
            this.variantProvider = variantProvider ?? (() => null);
            this.synonyms = synonyms ?? Array.Empty<string>();
        }

        public virtual string GetDocumentationLink()
        {
            DocumentationUtils.TryGetHelpURL(modelType, out var url);
            return url;
        }

        public virtual VFXModel CreateInstance()
        {
            var instance = (VFXModel)ScriptableObject.CreateInstance(modelType);
            if (settings?.Length > 0)
            {
                instance.SetSettingValues(settings);
            }

            return instance;
        }

        public virtual string GetUniqueIdentifier() => $"{category}/{name}";
    }

    abstract class VariantProvider
    {
        public abstract IEnumerable<Variant> GetVariants();
    };

    // Attribute used to register VFX type to library
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    class VFXInfoAttribute : ExcludeFromPresetAttribute
    {
        public bool autoRegister { get; } = true;
        public string name { get; set; }
        public string category { get; set; }
        public Type type { get; set; } = null;
        public Type variantProvider { get; set; }
        public bool experimental { get; set; }
        public string[] synonyms { get; set; }

        public static VFXInfoAttribute Get(Type type)
        {
            var attribs = type.GetCustomAttributes(typeof(VFXInfoAttribute), false);
            if (attribs.Length == 1 && attribs[0] is VFXInfoAttribute attribute)
            {
                attribute.name ??= ObjectNames.NicifyVariableName((attribute.type ?? type).UserFriendlyName());
                return attribute;
            }

            return null;
        }
    }

    interface IVFXModelDescriptor
    {
        string name { get; }
        string category { get; }
        Type modelType { get; }
        VFXModel unTypedModel { get; }
        public string[] synonyms { get; }
        Variant variant { get; }
        IVFXModelDescriptor[] subVariantDescriptors { get; }
        VFXModel CreateInstance();
    }

    class VFXModelDescriptor<T> : IVFXModelDescriptor where T: VFXModel
    {
        private Lazy<T> m_CachedModel;
        private Lazy<IVFXModelDescriptor[]> m_SubvariantDescriptors;

        public VFXModelDescriptor(Variant variant, VFXInfoAttribute infoAttribute)
        {
            this.variant = variant;
            this.infoAttribute = infoAttribute;

            this.synonyms = this.variant.synonyms?.Length > 0 ? this.variant.synonyms : Array.Empty<string>();
            if (this.infoAttribute?.synonyms?.Length > 0)
            {
                this.synonyms = this.synonyms.Union(this.infoAttribute.synonyms).ToArray();
            }
            m_CachedModel = new Lazy<T>(() => (T)this.variant.CreateInstance());
            m_SubvariantDescriptors = new Lazy<IVFXModelDescriptor[]>(() => GetSubVariantDescriptors()?.ToArray() ?? Array.Empty<IVFXModelDescriptor>());
        }

        public string name => variant.name;
        public string category => variant.category;
        public string[] synonyms { get; }
        public Type modelType => variant.modelType;
        public Variant variant { get; }
        public IVFXModelDescriptor[] subVariantDescriptors => m_SubvariantDescriptors.Value;
        public VFXInfoAttribute infoAttribute { get; }
        public T model => m_CachedModel.Value;
        public VFXModel unTypedModel => m_CachedModel.Value;

        protected internal bool isCachedModelCreated => m_CachedModel.IsValueCreated;

        public bool HasSettingValue(object value)
        {
            return variant.settings.Any(x => x.Value.Equals(value));
        }

        VFXModel IVFXModelDescriptor.CreateInstance()
        {
            return variant.CreateInstance();
        }

        public T CreateInstance()
        {
            return (T)variant.CreateInstance();
        }

        private IEnumerable<IVFXModelDescriptor> GetSubVariantDescriptors()
        {
            return this.variant.variantProvider()?.GetVariants().Select(x => new VFXModelDescriptor<T>(x, this.infoAttribute));
        }
    }

    class VFXModelDescriptorParameters : VFXModelDescriptor<VFXParameter>
    {
        internal class ParameterVariant : Variant
        {
            public ParameterVariant(VFXInfoAttribute infoAttribute)
                : this(infoAttribute.name, null, infoAttribute.type)
            {
            }

            public ParameterVariant(string name, string category, Type type) : base(name, category, type, null, null, null, false)
            {
            }

            public override VFXModel CreateInstance()
            {
                var parameter = ScriptableObject.CreateInstance<VFXParameter>();
                parameter.Init(modelType);

                return parameter;
            }
        }

        public VFXModelDescriptorParameters(VFXInfoAttribute infoAttribute) : base(new ParameterVariant(infoAttribute), infoAttribute)
        {
        }
    }

    static class VFXLibrary
    {
        public static IEnumerable<VFXModelDescriptor<VFXContext>> GetContexts() { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_ContextDescs : m_ContextDescs.Where(o => !o.infoAttribute.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXBlock>> GetBlocks() { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_BlockDescs : m_BlockDescs.Where(o => !o.infoAttribute.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXOperator>> GetOperators() { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_OperatorDescs : m_OperatorDescs.Where(o => !o.infoAttribute.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXSlot>> GetSlots() { LoadSlotsIfNeeded(); return m_SlotDescs.Values; }
        public static IEnumerable<Type> GetSlotsType() { LoadSlotsIfNeeded(); return m_SlotDescs.Keys; }

        public static IEnumerable<Type> GetGraphicsBufferType()
        {
            foreach (var type in VFXLibrary.GetSlotsType())
            {
                if (VFXExpression.IsUniform(VFXExpression.GetVFXValueTypeFromType(type)))
                    yield return type;
                else
                {
                    var typeAttribute = VFXLibrary.GetAttributeFromSlotType(type);
                    if (typeAttribute != null && typeAttribute.usages.HasFlag(VFXTypeAttribute.Usage.GraphicsBuffer))
                        yield return type;
                }
            }
        }

        public static bool IsSpaceableSlotType(Type type) { LoadSlotsIfNeeded(); return m_SlotSpaceable.Contains(type); }
        public static VFXTypeAttribute GetAttributeFromSlotType(Type type)
        {
            LoadSlotsIfNeeded();
            m_SlotAttribute.TryGetValue(type, out var attribute);
            return attribute;
        }

        public static IEnumerable<VFXModelDescriptorParameters> GetParameters() { LoadIfNeeded(); return m_ParametersDescs; }

        public static VFXModelDescriptor<VFXSlot> GetSlot(System.Type type)
        {
            if (type == null)
                return null;

            LoadSlotsIfNeeded();
            VFXModelDescriptor<VFXSlot> desc;
            m_SlotDescs.TryGetValue(type, out desc);
            return desc;
        }

        public static void ClearLibrary()
        {
            lock (m_Lock)
            {
                if (m_Loaded)
                {
                    if (VFXViewPreference.advancedLogs)
                        Debug.Log("Clear VFX Library");

                    Clear(m_ContextDescs);
                    Clear(m_BlockDescs);
                    Clear(m_OperatorDescs);
                    Clear(m_SlotDescs.Values);
                    Clear(m_ContextDescs);
                    Clear(m_ParametersDescs);
                    m_Loaded = false;
                }
            }
        }

        private static void Clear<T>(IEnumerable<VFXModelDescriptor<T>> descriptors) where T : VFXModel
        {
            var dependencies = new HashSet<ScriptableObject>();
            foreach (var model in descriptors)
            {
                if (model.isCachedModelCreated)
                {
                    model.model.CollectDependencies(dependencies);
                    dependencies.Add(model.model);
                }
            }
            foreach (var obj in dependencies)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        private static void LoadIfNeeded()
        {
            if (m_Loaded)
                return;

            lock (m_Lock)
            {
                if (!m_Loaded)
                    Load();
            }
        }

        public static void Load()
        {
            if (VFXViewPreference.advancedLogs)
                Debug.Log("Load VFX Library");


            Profiler.BeginSample("VFXLibrary.Load");
            try
            {
                LoadSlotsIfNeeded();

                lock (m_Lock)
                {
                    if (m_Sentinel != null)
                        UnityEngine.Object.DestroyImmediate(m_Sentinel);
                    m_Sentinel = ScriptableObject.CreateInstance<LibrarySentinel>();

                    m_ContextDescs = LoadModels<VFXContext>("Context");
                    m_BlockDescs = LoadModels<VFXBlock>();
                    m_OperatorDescs = LoadModels<VFXOperator>("Operator");
                    m_ParametersDescs = m_SlotDescs
                        .Select(s => new VFXModelDescriptorParameters(s.Value.infoAttribute))
                        .ToArray();
                    m_Loaded = true;
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static bool IsSpaceable(Type type, Type attributeType)
        {
            if (type.IsDefined(typeof(VFXSpaceAttribute)))
            {
                return true;
            }

            return type
                .GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Any(x => x.IsDefined(attributeType) || IsSpaceable(x.FieldType, attributeType));
        }

        private static void LoadSlotsIfNeeded()
        {
            if (m_SlotLoaded)
                return;

            lock (m_Lock)
            {
                if (!m_SlotLoaded)
                {
                    var spaceAttributeType = typeof(VFXSpaceAttribute);
                    var vfxTypeAttributeType = typeof(VFXTypeAttribute);

                    m_SlotDescs = LoadSlots();
                    m_SlotSpaceable = new HashSet<Type>();
                    m_SlotAttribute = new Dictionary<Type, VFXTypeAttribute>();

                    foreach (var slotDescType in m_SlotDescs.Keys)
                    {
                        if (IsSpaceable(slotDescType, spaceAttributeType))
                        {
                            m_SlotSpaceable.Add(slotDescType);
                        }

                        var attribute = slotDescType.GetCustomAttributes(vfxTypeAttributeType, true).FirstOrDefault() as VFXTypeAttribute;
                        m_SlotAttribute.Add(slotDescType, attribute);
                    }

                    m_SlotLoaded = true;
                }
            }
        }

        private static List<VFXModelDescriptor<T>> LoadModels<T>(string categoryPrefix = null) where T : VFXModel
        {
            var modelTypes = FindConcreteSubclasses(typeof(T), typeof(VFXInfoAttribute));

            var modelDescs = new List<VFXModelDescriptor<T>>();
            var error = new StringBuilder();

            foreach (var modelType in modelTypes)
            {
                try
                {
                    var infoAttribute = VFXInfoAttribute.Get(modelType);
                    if (infoAttribute.autoRegister)
                    {
                        if (infoAttribute.variantProvider != null)
                        {
                            var provider = Activator.CreateInstance(infoAttribute.variantProvider) as VariantProvider;
                            foreach (var variant in provider.GetVariants())
                            {
                                if (!string.IsNullOrEmpty(categoryPrefix))
                                {
                                    variant.category = string.IsNullOrEmpty(variant.category) ? $"{categoryPrefix}" : $"{categoryPrefix}/{variant.category}";
                                }
                                modelDescs.Add(new VFXModelDescriptor<T>(variant, infoAttribute));
                            }
                        }
                        else
                        {
                            var category = string.IsNullOrEmpty(categoryPrefix)
                                ? infoAttribute.category
                                : string.IsNullOrEmpty(infoAttribute.category) ? $"{categoryPrefix}" : $"{categoryPrefix}/{infoAttribute.category}";
                            modelDescs.Add(new VFXModelDescriptor<T>(new Variant(infoAttribute.name ?? modelType.ToString(), category, modelType, null), infoAttribute));
                        }
                    }
                }
                catch (Exception e)
                {
                    error.AppendFormat("Error while loading model from type " + modelType + ": " + e);
                    error.AppendLine();
                }
            }

            if (error.Length != 0)
            {
                Debug.LogError(error);
            }

            return modelDescs;
        }

        class LibrarySentinel : ScriptableObject
        {
            void OnDisable()
            {
                VFXLibrary.ClearLibrary();
            }
        }

        public struct VFXFieldType
        {
            public VFXValueType valueType;
            public Type type;
            public string name;
        }

        public static IEnumerable<VFXFieldType> GetFieldFromType(Type type)
        {
            var bindingsFlag = BindingFlags.Public | BindingFlags.Instance;
            foreach (var field in type.GetFields(bindingsFlag))
            {
                yield return new VFXFieldType()
                {
                    valueType = VFXExpression.GetVFXValueTypeFromType(field.FieldType),
                    type = field.FieldType,
                    name = field.Name
                };
            }
        }

        private static bool CheckBlittablePublic(Type type)
        {
            if (type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Any())
                return false;

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (VFXExpression.GetVFXValueTypeFromType(field.FieldType) == VFXValueType.None
                    && !CheckBlittablePublic(field.FieldType))
                    return false;

            return true;
        }

        private static VFXTypeAttribute ValidateVFXType(Type type, StringBuilder errors, Dictionary<Type, VFXTypeAttribute> alreadyProcessedType)
        {
            if (alreadyProcessedType.TryGetValue(type, out var typeAttribute))
                return typeAttribute;

            if (type.GetCustomAttributes(typeof(VFXTypeAttribute), true).FirstOrDefault() is not VFXTypeAttribute attribute)
            {
                alreadyProcessedType.Add(type, null);
                errors.AppendFormat("The type {0} doesn't use the expected [VFXType] attribute.\n", type);
                return null;
            }

            alreadyProcessedType.Add(type, attribute);

            var hasGraphicsBufferFlag = attribute.usages.HasFlag(VFXTypeAttribute.Usage.GraphicsBuffer);
            if (hasGraphicsBufferFlag && !Unity.Collections.LowLevel.Unsafe.UnsafeUtility.IsBlittable(type))
            {
                errors.AppendFormat("The type {0} is using GraphicsBuffer flag but isn't blittable.\n", type);
                return null;
            }

            foreach (var field in GetFieldFromType(type))
            {
                if (field.valueType == VFXValueType.None)
                {
                    var innerType = field.type;
                    if (ValidateVFXType(innerType, errors, alreadyProcessedType) == null)
                    {
                        errors.AppendFormat("The field '{0}' ({1}) in type '{2}' isn't valid.\n", field.name, field.type, type);
                        return null;
                    }
                }
            }

            if (hasGraphicsBufferFlag && !CheckBlittablePublic(type))
            {
                errors.AppendFormat("The type {0} is using GraphicsBuffer flag but isn't fully public.\n", type);
                return null;
            }

            return attribute;
        }

        private static Dictionary<Type, VFXTypeAttribute> LoadAndValidateVFXType()
        {
            var vfxTypes = FindConcreteSubclasses(null, typeof(VFXTypeAttribute));
            var errors = new StringBuilder();
            var processedTypes = new Dictionary<Type, VFXTypeAttribute>();
            foreach (var type in vfxTypes)
            {
                ValidateVFXType(type, errors, processedTypes);
            }

            if (errors.Length != 0)
                Debug.LogErrorFormat("Error while processing VFXType\n{0}", errors.ToString());
            return processedTypes;
        }

        private static Dictionary<Type, VFXModelDescriptor<VFXSlot>> LoadSlots()
        {
            // First find concrete slots
            var slotTypes = FindConcreteSubclasses(typeof(VFXSlot), typeof(VFXInfoAttribute));
            var dictionary = new Dictionary<Type, VFXModelDescriptor<VFXSlot>>();
            foreach (var slotType in slotTypes)
            {
                try
                {
                    var infoAttribute = VFXInfoAttribute.Get(slotType);
                    if (infoAttribute.type != null)
                    {
                        if (dictionary.ContainsKey(infoAttribute.type))
                            throw new Exception(infoAttribute.type + " was already bound to a slot type");
                        dictionary[infoAttribute.type] = new VFXModelDescriptor<VFXSlot>(new Variant(infoAttribute.name, null, slotType, null), infoAttribute);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading slot from type " + slotType + ": " + e);
                }
            }

            // Then find types that needs a generic slot
            var vfxTypes = LoadAndValidateVFXType();
            foreach (var kvp in vfxTypes)
            {
                if (!dictionary.ContainsKey(kvp.Key)) // If a slot was not already explicitly declared
                {
                    var infoAttribute = new VFXInfoAttribute { type = kvp.Key, name = kvp.Value.name ?? kvp.Key.Name };
                    dictionary[kvp.Key] = new VFXModelDescriptor<VFXSlot>(new Variant(infoAttribute.name, null, typeof(VFXSlot), null), infoAttribute);
                }
            }

            return dictionary;
        }

        public static IEnumerable<Type> FindConcreteSubclasses(Type objectType = null, Type attributeType = null)
        {
            if (objectType == null && attributeType == null)
            {
                throw new ArgumentException("objectType and attributeType cannot both be null");
            }

            var unfilteredTypes = objectType != null
                ? TypeCache.GetTypesDerivedFrom(objectType)
                : TypeCache.GetTypesWithAttribute(attributeType);

            foreach (var type in unfilteredTypes) {
                // We still need to check for the attribute here, even if we are already only operating on types with that attribute:
                //  - we want to ensure there is only a single attribute
                //  - we want to only get types that have this attribute themselves, and the type cache also returns those that have it on a base class
                if (!type.IsAbstract && (attributeType == null || type.GetCustomAttributes(attributeType, false).Length == 1)) {
                    yield return type;
                }
            }
        }

        [NonSerialized]
        private static Dictionary<string, VFXSRPBinder> srpBinders = null;

        private static void LoadSRPBindersIfNeeded()
        {
            if (srpBinders != null)
                return;

            srpBinders = new Dictionary<string, VFXSRPBinder>();

            foreach (var binderType in FindConcreteSubclasses(typeof(VFXSRPBinder)))
            {
                try
                {
                    VFXSRPBinder binder = (VFXSRPBinder)Activator.CreateInstance(binderType);
                    string SRPAssetTypeStr = binder.SRPAssetTypeStr;

                    if (srpBinders.ContainsKey(SRPAssetTypeStr))
                        throw new Exception(string.Format("The SRP of asset type {0} is already registered ({1})", SRPAssetTypeStr, srpBinders[SRPAssetTypeStr].GetType()));
                    srpBinders[SRPAssetTypeStr] = binder;

                    if (VFXViewPreference.advancedLogs)
                        Debug.Log(string.Format("Register {0} for VFX", SRPAssetTypeStr));
                }
                catch (Exception e)
                {
                    Debug.LogError(string.Format("Exception while registering VFXSRPBinder {0}: {1} - {2}", binderType, e, e.StackTrace));
                }
            }
        }

        private static bool unsupportedSRPWarningIssued = false;

        private static void LogUnsupportedSRP(VFXSRPBinder binder, bool forceLog)
        {
            if (binder == null && (forceLog || !unsupportedSRPWarningIssued))
            {
                Debug.LogWarning("The Visual Effect Graph is supported in the High Definition Render Pipeline (HDRP) and the Universal Render Pipeline (URP). Please assign your chosen Render Pipeline Asset in the Graphics Settings to use it.");
                unsupportedSRPWarningIssued = true;
            }
        }

        public static void LogUnsupportedSRP(bool forceLog = true)
        {
            bool logIssued = unsupportedSRPWarningIssued;
            var binder = currentSRPBinder;

            if (logIssued || !unsupportedSRPWarningIssued) // Don't reissue warning if inner currentSRPBinder call has already logged it
                LogUnsupportedSRP(binder, forceLog);
        }

        public static VFXSRPBinder currentSRPBinder
        {
            get
            {
                LoadSRPBindersIfNeeded();

                VFXSRPBinder binder = null;
                var currentSRP = GraphicsSettings.currentRenderPipeline;
                if (currentSRP != null)
                    srpBinders.TryGetValue(currentSRP.GetType().Name, out binder);

                LogUnsupportedSRP(binder, false);

                return binder;
            }
        }

        [InitializeOnLoadMethod]
        private static void RegisterSRPChangeCallback()
        {
            if(!AssetDatabase.IsAssetImportWorkerProcess())
                RenderPipelineManager.activeRenderPipelineTypeChanged += SRPChanged;
        }

        private static void SRPChanged()
        {
            Profiler.BeginSample("VFX.SRPChanged");
            try
            {
                unsupportedSRPWarningIssued = false;
                var allModels = Resources.FindObjectsOfTypeAll<VFXModel>();
                foreach (var model in allModels)
                    model.OnSRPChanged();

                VFXAssetManager.Build();
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static LibrarySentinel m_Sentinel = null;

        private static volatile List<VFXModelDescriptor<VFXContext>> m_ContextDescs;
        private static volatile List<VFXModelDescriptor<VFXOperator>> m_OperatorDescs;
        private static volatile List<VFXModelDescriptor<VFXBlock>> m_BlockDescs;
        private static volatile VFXModelDescriptorParameters[] m_ParametersDescs;
        private static volatile Dictionary<Type, VFXModelDescriptor<VFXSlot>> m_SlotDescs;
        private static volatile HashSet<Type> m_SlotSpaceable;
        private static volatile Dictionary<Type, VFXTypeAttribute> m_SlotAttribute;

        private static readonly Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
        private static volatile bool m_SlotLoaded = false;
    }
}
