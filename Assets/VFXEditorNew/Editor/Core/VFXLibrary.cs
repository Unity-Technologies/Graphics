using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            this.register = register;
        }

        public bool register = true;
        public string category = "";

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

        public string name { get { return m_Template.name; } }
        public VFXInfoAttribute info { get { return VFXInfoAttribute.Get(m_Template); }}

        public bool AcceptParent(VFXModel parent, int index = -1)
        {
            return parent.AcceptChild(m_Template, index);
        }

        public T CreateInstance()
        {
            return (T)System.Activator.CreateInstance(m_Template.GetType());
        }

        private T m_Template;
    }

    static class VFXLibrary
    {
        public static VFXContextDesc GetContext(string id)      { LoadIfNeeded(); return m_ContextDescs[id]; }
        public static IEnumerable<VFXContextDesc> GetContexts() { LoadIfNeeded(); return m_ContextDescs.Values; }

        public static IEnumerable<VFXModelDescriptor<VFXBlock>> GetBlocks() { LoadIfNeeded(); return m_BlockDescs; }

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
            lock(m_Lock)
            {
                LoadContextDescs();
                m_BlockDescs = LoadModels<VFXBlock>();
                m_Loaded = true;
            }
        }

        private static void LoadContextDescs()
        {
            // Search for derived type of VFXContextDesc in assemblies
            var contextDescTypes = FindConcreteSubclasses<VFXContextDesc>();
            var contextDescs = new Dictionary<string, VFXContextDesc>();
            foreach (var contextDesc in contextDescTypes)
            {
                try
                {
                    VFXContextDesc instance = (VFXContextDesc)contextDesc.Assembly.CreateInstance(contextDesc.FullName);
                    contextDescs.Add(contextDesc.FullName, instance);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading context desc from type " + contextDesc.FullName + ": " + e.Message);
                }
            }

            m_ContextDescs = contextDescs; // atomic set
        }

        private static List<VFXModelDescriptor<T>> LoadModels<T>() where T : VFXModel
        {
            var modelTypes = FindConcreteSubclasses<T>();
            var modelDescs = new List<VFXModelDescriptor<T>>();
            foreach (var modelType in modelTypes)
            {
                try
                {
                    T instance = (T)System.Activator.CreateInstance(modelType);
                    modelDescs.Add(new VFXModelDescriptor<T>(instance));
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading model from type " + modelType + ": " + e.Message);
                }
            }

            return modelDescs;
        }

        private static IEnumerable<Type> FindConcreteSubclasses<T>()
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
                        if (assemblyType.IsSubclassOf(typeof(T)) && !assemblyType.IsAbstract)
                            types.Add(assemblyType);
            }
            return types.Where(type => type.GetCustomAttributes(typeof(VFXInfoAttribute), false).Length == 1);
        }

        private static volatile Dictionary<string, VFXContextDesc> m_ContextDescs;
        private static volatile List<VFXModelDescriptor<VFXBlock>> m_BlockDescs;

        private static Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
    }
}
