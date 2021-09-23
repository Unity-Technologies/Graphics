using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Rendering;
using Object = System.Object;
using System.Reflection;

namespace UnityEditor.VFX
{
    abstract class VariantProvider
    {
        protected virtual Dictionary<string, object[]> variants
        {
            get
            {
                return new Dictionary<string, Object[]>();
            }
        }

        public virtual IEnumerable<IEnumerable<KeyValuePair<string, object>>> ComputeVariants()
        {
            //Default behavior : Cartesian product
            IEnumerable<IEnumerable<object>> empty = new[] { Enumerable.Empty<object>() };
            var arrVariants = variants.Select(o => o.Value as IEnumerable<Object>);
            var combinations = arrVariants.Aggregate(empty, (x, y) => x.SelectMany(accSeq => y.Select(item => accSeq.Concat(new[] { item }))));
            foreach (var combination in combinations)
            {
                var variant = combination.Select((o, i) => new KeyValuePair<string, object>(variants.ElementAt(i).Key, o));
                yield return variant;
            }
        }
    };

    // Attribute used to register VFX type to library
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    class VFXInfoAttribute : ExcludeFromPresetAttribute
    {
        public VFXInfoAttribute()
        {
            this.autoRegister = true;
            this.category = "";
            this.type = null;
        }

        public bool autoRegister
        {
            get;
            set;
        }
        public string category
        {
            get;
            set;
        }
        public Type type
        {
            get;
            set;
        }

        public Type variantProvider
        {
            get;
            set;
        }

        public bool experimental
        {
            get;
            set;
        }

        public static VFXInfoAttribute Get(Type type)
        {
            var attribs = type.GetCustomAttributes(typeof(VFXInfoAttribute), false);
            return attribs.Length == 1 ? (VFXInfoAttribute)attribs[0] : null;
        }
    }

    class VFXModelDescriptor
    {
        protected VFXModelDescriptor(VFXModel template, IEnumerable<KeyValuePair<string, Object>> variants = null)
        {
            m_Template = template;
            m_Variants = variants == null ? Enumerable.Empty<KeyValuePair<string, object>>() : variants;
            ApplyVariant(m_Template);
        }

        public bool AcceptParent(VFXModel parent, int index = -1)
        {
            return parent.AcceptChild(m_Template, index);
        }

        protected void ApplyVariant(VFXModel model)
        {
            model.SetSettingValues(m_Variants);
        }

        private IEnumerable<KeyValuePair<string, object>> m_Variants;
        protected VFXModel m_Template;

        virtual public string name { get { return m_Template.libraryName; } }
        public VFXInfoAttribute info { get { return VFXInfoAttribute.Get(m_Template.GetType()); } }
        public Type modelType { get { return m_Template.GetType(); } }
        public VFXModel model
        {
            get { return m_Template; }
        }
    }

    class VFXModelDescriptor<T> : VFXModelDescriptor where T : VFXModel
    {
        public VFXModelDescriptor(T template, IEnumerable<KeyValuePair<string, Object>> variants = null) : base(template, variants)
        {
        }

        virtual public T CreateInstance()
        {
            var instance = (T)ScriptableObject.CreateInstance(m_Template.GetType());
            ApplyVariant(instance);
            return instance;
        }

        public new T model
        {
            get { return (T)m_Template; }
        }
    }

    class VFXModelDescriptorParameters : VFXModelDescriptor<VFXParameter>
    {
        private string m_name;
        public override string name
        {
            get
            {
                return m_name;
            }
        }

        public VFXModelDescriptorParameters(Type type) : base(ScriptableObject.CreateInstance<VFXParameter>())
        {
            model.Init(type);
            m_name = type.UserFriendlyName();
        }

        public override VFXParameter CreateInstance()
        {
            var instance = base.CreateInstance();
            instance.Init(model.outputSlots[0].property.type);
            return instance;
        }
    }

    static class VFXLibrary
    {
        public static IEnumerable<VFXModelDescriptor<VFXContext>> GetContexts() { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_ContextDescs : m_ContextDescs.Where(o => !o.info.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXBlock>> GetBlocks() { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_BlockDescs : m_BlockDescs.Where(o => !o.info.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXOperator>> GetOperators() { LoadIfNeeded(); return VFXViewPreference.displayExperimentalOperator ? m_OperatorDescs : m_OperatorDescs.Where(o => !o.info.experimental); }
        public static IEnumerable<VFXModelDescriptor<VFXSlot>> GetSlots() { LoadSlotsIfNeeded(); return m_SlotDescs.Values; }
        public static IEnumerable<Type> GetSlotsType() { LoadSlotsIfNeeded(); return m_SlotDescs.Keys; }
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
                    Clear(m_ParametersDescs.Cast<VFXModelDescriptor<VFXParameter>>());
                    m_Loaded = false;
                }
            }
        }

        static void Clear<T>(IEnumerable<VFXModelDescriptor<T>> descriptors) where T : VFXModel
        {
            HashSet<ScriptableObject> dependencies = new HashSet<ScriptableObject>();
            foreach (var model in descriptors)
            {
                model.model.CollectDependencies(dependencies);
                dependencies.Add(model.model);
            }
            foreach (var obj in dependencies)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        public static void LoadIfNeeded()
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

            LoadSlotsIfNeeded();

            lock (m_Lock)
            {
                if (m_Sentinel != null)
                    ScriptableObject.DestroyImmediate(m_Sentinel);
                m_Sentinel = ScriptableObject.CreateInstance<LibrarySentinel>();
                m_ContextDescs = LoadModels<VFXContext>();
                m_BlockDescs = LoadModels<VFXBlock>();
                m_OperatorDescs = LoadModels<VFXOperator>();
                m_ParametersDescs = m_SlotDescs.Select(s =>
                {
                    var desc = new VFXModelDescriptorParameters(s.Key);
                    return desc;
                }).ToList();

                m_Loaded = true;
            }
        }

        private static bool IsSpaceable(Type type)
        {
            var spaceAttributeOnType = type.GetCustomAttributes(typeof(VFXSpaceAttribute), true).FirstOrDefault();
            if (spaceAttributeOnType != null)
            {
                return true;
            }

            var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).ToArray();
            foreach (var field in fields)
            {
                var spaceAttributeOnField = field.GetCustomAttributes(typeof(VFXSpaceAttribute), true).FirstOrDefault();
                if (spaceAttributeOnField != null || IsSpaceable(field.FieldType))
                {
                    return true;
                }
            }
            return false;
        }

        private static void LoadSlotsIfNeeded()
        {
            if (m_SlotLoaded)
                return;

            lock (m_Lock)
            {
                if (!m_SlotLoaded)
                {
                    m_SlotDescs = LoadSlots();
                    m_SlotSpaceable = new HashSet<Type>();
                    foreach (var slotDescType in m_SlotDescs.Keys)
                    {
                        if (IsSpaceable(slotDescType))
                        {
                            m_SlotSpaceable.Add(slotDescType);
                        }
                    }

                    m_SlotAttribute = new Dictionary<Type, VFXTypeAttribute>();
                    foreach (var slotDescType in m_SlotDescs.Keys)
                    {
                        var attribute = slotDescType.GetCustomAttributes(typeof(VFXTypeAttribute), true).FirstOrDefault() as VFXTypeAttribute;
                        m_SlotAttribute.Add(slotDescType, attribute);
                    }

                    m_SlotLoaded = true;
                }
            }
        }

        private static List<VFXModelDescriptor<T>> LoadModels<T>() where T : VFXModel
        {
            var modelTypes = FindConcreteSubclasses(typeof(T), typeof(VFXInfoAttribute));
            var modelDescs = new List<VFXModelDescriptor<T>>();
            var nameAlreadyAdded = new HashSet<string>();
            var error = new StringBuilder();

            foreach (var modelType in modelTypes)
            {
                try
                {
                    T instance = (T)ScriptableObject.CreateInstance(modelType);
                    var modelDesc = new VFXModelDescriptor<T>(instance);
                    if (modelDesc.info.autoRegister)
                    {
                        if (modelDesc.info.variantProvider != null)
                        {
                            var provider = Activator.CreateInstance(modelDesc.info.variantProvider) as VariantProvider;
                            foreach (var variant in provider.ComputeVariants())
                            {
                                var variantArray = variant.ToArray();
                                var currentVariant = new VFXModelDescriptor<T>((T)ScriptableObject.CreateInstance(modelType), variant);
                                if (!nameAlreadyAdded.Contains(currentVariant.name))
                                {
                                    modelDescs.Add(currentVariant);
                                    nameAlreadyAdded.Add(currentVariant.name);
                                }
                                else
                                {
                                    error.AppendFormat("Trying to add twice : {0}", currentVariant.name);
                                    error.AppendLine();
                                }
                            }
                            nameAlreadyAdded.Clear();
                        }
                        else
                        {
                            modelDescs.Add(modelDesc);
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

            return modelDescs.OrderBy(o => o.name).ToList();
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

        public static IEnumerable<VFXFieldType> GetFieldFromType(Type type, bool enumeratePrivate = true)
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

        private static bool ValidateVFXType(Type type, StringBuilder errors, Dictionary<Type, bool> alreadyProcessedType)
        {
            if (alreadyProcessedType.TryGetValue(type, out var result))
                return result;

            alreadyProcessedType.Add(type, false);
            var attribute = type.GetCustomAttributes(typeof(VFXTypeAttribute), true).FirstOrDefault() as VFXTypeAttribute;
            if (attribute == null)
            {
                errors.AppendFormat("The type {0} doesn't use the expected [VFXType] attribute.\n", type);
                return false;
            }

            var hasGraphicsBufferFlag = attribute.usages.HasFlag(VFXTypeAttribute.Usage.GraphicsBuffer);
            if (hasGraphicsBufferFlag && !Unity.Collections.LowLevel.Unsafe.UnsafeUtility.IsBlittable(type))
            {
                errors.AppendFormat("The type {0} is using GraphicsBuffer flag but isn't blittable.\n", type);
                return false;
            }

            foreach (var field in GetFieldFromType(type))
            {
                if (field.valueType == VFXValueType.None)
                {
                    var innerType = field.type;
                    if (!ValidateVFXType(innerType, errors, alreadyProcessedType))
                    {
                        errors.AppendFormat("The field '{0}' ({1}) in type '{2}' isn't valid.\n", field.name, field.type, type);
                        return false;
                    }
                }
            }

            if (hasGraphicsBufferFlag && !CheckBlittablePublic(type))
            {
                errors.AppendFormat("The type {0} is using GraphicsBuffer flag but isn't fully public.\n", type);
                return false;
            }

            alreadyProcessedType.Remove(type);
            alreadyProcessedType.Add(type, true);
            return true;
        }

        private static bool ValidateVFXType(Type type, StringBuilder errors)
        {
            var processedType = new Dictionary<Type, bool>();
            return ValidateVFXType(type, errors, processedType);
        }

        private static Type[] LoadAndValidateVFXType()
        {
            var vfxTypes = FindConcreteSubclasses(null, typeof(VFXTypeAttribute));
            var errors = new StringBuilder();
            var validTypes = new List<Type>();
            foreach (var type in vfxTypes)
            {
                if (ValidateVFXType(type, errors))
                    validTypes.Add(type);
            }

            if (errors.Length != 0)
                Debug.LogErrorFormat("Error while processing VFXType\n{0}", errors.ToString());
            return validTypes.ToArray();
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
                    Type boundType = VFXInfoAttribute.Get(slotType).type; // Not null as it was filtered before
                    if (boundType != null)
                    {
                        if (dictionary.ContainsKey(boundType))
                            throw new Exception(boundType + " was already bound to a slot type");

                        VFXSlot instance = (VFXSlot)ScriptableObject.CreateInstance(slotType);
                        dictionary[boundType] = new VFXModelDescriptor<VFXSlot>(instance);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading slot from type " + slotType + ": " + e);
                }
            }

            // Then find types that needs a generic slot
            var vfxTypes = LoadAndValidateVFXType();
            foreach (var type in vfxTypes)
            {
                if (!dictionary.ContainsKey(type)) // If a slot was not already explicitly declared
                {
                    VFXSlot instance = ScriptableObject.CreateInstance<VFXSlot>();
                    dictionary[type] = new VFXModelDescriptor<VFXSlot>(instance);
                }
            }

            return dictionary;
        }

        public static IEnumerable<Type> FindConcreteSubclasses(Type objectType = null, Type attributeType = null)
        {
            List<Type> types = new List<Type>();
            foreach (var domainAssembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assemblyTypes = null;
                try
                {
                    assemblyTypes = domainAssembly.GetTypes();
                }
                catch (Exception)
                {
                    if (VFXViewPreference.advancedLogs)
                        Debug.Log("Cannot access assembly: " + domainAssembly);
                    assemblyTypes = null;
                }
                if (assemblyTypes != null)
                    foreach (var assemblyType in assemblyTypes)
                        if ((objectType == null || assemblyType.IsSubclassOf(objectType)) && !assemblyType.IsAbstract)
                            types.Add(assemblyType);
            }
            return types.Where(type => attributeType == null || type.GetCustomAttributes(attributeType, false).Length == 1);
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
                var currentSRP = QualitySettings.renderPipeline ?? GraphicsSettings.currentRenderPipeline;
                if (currentSRP != null)
                    srpBinders.TryGetValue(currentSRP.GetType().Name, out binder);

                LogUnsupportedSRP(binder, false);

                return binder;
            }
        }

        [InitializeOnLoadMethod]
        private static void RegisterSRPChangeCallback()
        {
            RenderPipelineManager.activeRenderPipelineTypeChanged += SRPChanged;
        }

        public delegate void OnSRPChangedEvent();
        public static event OnSRPChangedEvent OnSRPChanged;

        private static void SRPChanged()
        {
            unsupportedSRPWarningIssued = false;
            OnSRPChanged?.Invoke();
            VFXAssetManager.Build();
        }

        private static LibrarySentinel m_Sentinel = null;

        private static volatile List<VFXModelDescriptor<VFXContext>> m_ContextDescs;
        private static volatile List<VFXModelDescriptor<VFXOperator>> m_OperatorDescs;
        private static volatile List<VFXModelDescriptor<VFXBlock>> m_BlockDescs;
        private static volatile List<VFXModelDescriptorParameters> m_ParametersDescs;
        private static volatile Dictionary<Type, VFXModelDescriptor<VFXSlot>> m_SlotDescs;
        private static volatile HashSet<Type> m_SlotSpaceable;
        private static volatile Dictionary<Type, VFXTypeAttribute> m_SlotAttribute;

        private static Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
        private static volatile bool m_SlotLoaded = false;
    }
}
