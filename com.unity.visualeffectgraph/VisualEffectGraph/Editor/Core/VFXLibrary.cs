using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = System.Object;

namespace UnityEditor.VFX
{
    // Attribute used to register VFX type to library
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class VFXInfoAttribute : Attribute
    {
        public VFXInfoAttribute(bool register = true)
        {
            this.autoRegister = register;
        }

        public bool autoRegister = true;
        public string category = "";
        public Type type = null; // Used by slots to map types to slot

        public static VFXInfoAttribute Get(Object obj)
        {
            return Get(obj.GetType());
        }

        public static VFXInfoAttribute Get(Type type)
        {
            var attribs = type.GetCustomAttributes(typeof(VFXInfoAttribute), false);
            return attribs.Length == 1 ? (VFXInfoAttribute)attribs[0] : null;
        }
    }

    class VFXModelDescriptor<T> where T : VFXModel
    {
        public VFXModelDescriptor(T template)
        {
            m_Template = template;
        }

        virtual public string name { get { return m_Template.name; } }
        public VFXInfoAttribute info { get { return VFXInfoAttribute.Get(m_Template); } }
        public Type modelType { get { return m_Template.GetType(); } }

        public bool AcceptParent(VFXModel parent, int index = -1)
        {
            return parent.AcceptChild(m_Template, index);
        }

        virtual public T CreateInstance()
        {
            return (T)ScriptableObject.CreateInstance(m_Template.GetType());
        }

        protected T m_Template;
    }

    class VFXModelDescriptorCustomSpawnerBlock : VFXModelDescriptor<VFXBlock>
    {
        public VFXModelDescriptorCustomSpawnerBlock(Type customType) : base(new VFXSpawnerCustomWrapper())
        {
            (m_Template as VFXSpawnerCustomWrapper).Init(customType);
        }

        public override VFXBlock CreateInstance()
        {
            var instance = base.CreateInstance();
            var vfxSpawnerInstance = instance as VFXSpawnerCustomWrapper;
            var vfxSpawnerTemplate = m_Template as VFXSpawnerCustomWrapper;
            vfxSpawnerInstance.Init(vfxSpawnerTemplate.customBehavior);
            return vfxSpawnerInstance;
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
            m_Template.Init(type);
            m_name = type.UserFriendlyName();
        }

        public override VFXParameter CreateInstance()
        {
            var instance = base.CreateInstance();
            instance.Init(m_Template.outputSlots[0].property.type);
            return instance;
        }
    }

    class VFXModelDescriptorBuiltInParameters : VFXModelDescriptor<VFXBuiltInParameter>
    {
        private string m_name;
        public override string name
        {
            get
            {
                return m_name;
            }
        }

        public VFXModelDescriptorBuiltInParameters(VFXExpressionOp op) : base(ScriptableObject.CreateInstance<VFXBuiltInParameter>())
        {
            m_name = op.ToString();
            m_Template.Init(op);
        }

        public override VFXBuiltInParameter CreateInstance()
        {
            var instance = base.CreateInstance();
            instance.Init(m_Template.outputSlots[0].GetExpression().Operation);
            return instance;
        }
    }

    class VFXModelDescriptorAttributeParameters<T> : VFXModelDescriptor<T> where T : VFXAttributeParameter
    {
        private string m_attributeName;
        public override string name
        {
            get
            {
                return m_attributeName;
            }
        }

        public VFXModelDescriptorAttributeParameters(string attributeName) : base(ScriptableObject.CreateInstance<T>())
        {
            m_attributeName = attributeName;
        }

        public override T CreateInstance()
        {
            var instance = base.CreateInstance();
            instance.SetSettingValue("attribute", m_attributeName);
            return instance;
        }
    }

    class VFXModelDescriptorCurrentAttributeParameters : VFXModelDescriptorAttributeParameters<VFXCurrentAttributeParameter>
    {
        public VFXModelDescriptorCurrentAttributeParameters(string attributeName) : base(attributeName)
        {
        }
    }

    class VFXModelDescriptorSourceAttributeParameters : VFXModelDescriptorAttributeParameters<VFXSourceAttributeParameter>
    {
        public VFXModelDescriptorSourceAttributeParameters(string attributeName) : base(attributeName)
        {
        }
    }

    static class VFXLibrary
    {
        public static IEnumerable<VFXModelDescriptor<VFXContext>> GetContexts()     { LoadIfNeeded(); return m_ContextDescs; }
        public static IEnumerable<VFXModelDescriptor<VFXBlock>> GetBlocks()         { LoadIfNeeded(); return m_BlockDescs; }
        public static IEnumerable<VFXModelDescriptor<VFXOperator>> GetOperators()   { LoadIfNeeded(); return m_OperatorDescs; }
        public static IEnumerable<VFXModelDescriptor<VFXSlot>> GetSlots()           { LoadSlotsIfNeeded(); return m_SlotDescs.Values; }
        public static IEnumerable<VFXModelDescriptorParameters> GetParameters()     { LoadIfNeeded(); return m_ParametersDescs; }
        public static IEnumerable<VFXModelDescriptorBuiltInParameters> GetBuiltInParameters() { LoadIfNeeded(); return m_BuiltInParametersDescs; }
        public static IEnumerable<VFXModelDescriptorCurrentAttributeParameters> GetCurrentAttributeParameters() { LoadIfNeeded(); return m_CurrentAttributeParametersDecs; }
        public static IEnumerable<VFXModelDescriptorSourceAttributeParameters> GetSourceAttributeParameters() { LoadIfNeeded(); return m_SourceAttributeParametersDecs; }

        public static VFXModelDescriptor<VFXSlot> GetSlot(System.Type type)
        {
            LoadSlotsIfNeeded();
            VFXModelDescriptor<VFXSlot> desc;
            m_SlotDescs.TryGetValue(type, out desc);
            return desc;
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
            lock (m_Lock)
            {
                LoadSlotsIfNeeded();
                m_ContextDescs = LoadModels<VFXContext>();
                m_BlockDescs = LoadModels<VFXBlock>();
                var customSpawners = FindConcreteSubclasses(typeof(VFXSpawnerFunction));
                foreach (var customSpawnerType in customSpawners)
                {
                    m_BlockDescs.Add(new VFXModelDescriptorCustomSpawnerBlock(customSpawnerType));
                }

                m_OperatorDescs = LoadModels<VFXOperator>();

                m_ParametersDescs = m_SlotDescs.Select(s =>
                    {
                        var desc = new VFXModelDescriptorParameters(s.Key);
                        return desc;
                    }).ToList();

                m_BuiltInParametersDescs = VFXBuiltInExpression.All.Select(e =>
                    {
                        var desc = new VFXModelDescriptorBuiltInParameters(e);
                        return desc;
                    }).ToList();

                m_CurrentAttributeParametersDecs = VFXAttribute.All.Select(a =>
                    {
                        var desc = new VFXModelDescriptorCurrentAttributeParameters(a);
                        return desc;
                    }).ToList();

                m_SourceAttributeParametersDecs = VFXAttribute.All.Select(a =>
                    {
                        var desc = new VFXModelDescriptorSourceAttributeParameters(a);
                        return desc;
                    }).ToList();

                m_Loaded = true;
            }
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
                    m_SlotLoaded = true;
                }
            }
        }

        private static List<VFXModelDescriptor<T>> LoadModels<T>() where T : VFXModel
        {
            var modelTypes = FindConcreteSubclasses(typeof(T), typeof(VFXInfoAttribute));
            var modelDescs = new List<VFXModelDescriptor<T>>();
            foreach (var modelType in modelTypes)
            {
                try
                {
                    T instance = (T)ScriptableObject.CreateInstance(modelType);
                    var modelDesc = new VFXModelDescriptor<T>(instance);
                    if (modelDesc.info.autoRegister)
                    {
                        modelDescs.Add(modelDesc);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading model from type " + modelType + ": " + e);
                }
            }

            return modelDescs.OrderBy(o => o.name).ToList();
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
            var vfxTypes = FindConcreteSubclasses(null, typeof(VFXTypeAttribute));
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

        private static IEnumerable<Type> FindConcreteSubclasses(Type objectType = null, Type attributeType = null)
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

        private static volatile List<VFXModelDescriptor<VFXContext>> m_ContextDescs;
        private static volatile List<VFXModelDescriptor<VFXOperator>> m_OperatorDescs;
        private static volatile List<VFXModelDescriptor<VFXBlock>> m_BlockDescs;
        private static volatile List<VFXModelDescriptorParameters> m_ParametersDescs;
        private static volatile List<VFXModelDescriptorBuiltInParameters> m_BuiltInParametersDescs;
        private static volatile List<VFXModelDescriptorCurrentAttributeParameters> m_CurrentAttributeParametersDecs;
        private static volatile List<VFXModelDescriptorSourceAttributeParameters> m_SourceAttributeParametersDecs;
        private static volatile Dictionary<Type, VFXModelDescriptor<VFXSlot>> m_SlotDescs;

        private static Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
        private static volatile bool m_SlotLoaded = false;
    }
}
