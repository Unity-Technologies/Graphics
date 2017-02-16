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

    static class VFXLibrary
    {
        public static VFXContextDesc GetContext(string id)      { LoadIfNeeded(); return m_ContextDescs[id]; }
        public static IEnumerable<VFXContextDesc> GetContexts() { LoadIfNeeded(); return m_ContextDescs.Values; }

        public static VFXBlockDesc GetBlock(string id)          { LoadIfNeeded(); return m_BlockDescs[id]; }
        public static IEnumerable<VFXBlockDesc> GetBlocks()     { LoadIfNeeded(); return m_BlockDescs.Values; }

        public static VFXOperatorDesc GetOperator(string id) { LoadIfNeeded(); return m_OperatorDescs[id]; }
        public static IEnumerable<VFXOperatorDesc> GetOperators() { LoadIfNeeded(); return m_OperatorDescs.Values; }

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
                m_ContextDescs = LoadDescs<VFXContextDesc>();
                m_BlockDescs = LoadDescs<VFXBlockDesc>();
                m_OperatorDescs = LoadDescs<VFXOperatorDesc>();
                m_Loaded = true;
            }
        }

        private static Dictionary<string, T> LoadDescs<T>()
        {
            var contextDescTypes = FindConcreteSubclasses<T>();
            var contextDescs = new Dictionary<string, T>();
            foreach (var contextDesc in contextDescTypes)
            {
                try
                {
                    var instance = (T)contextDesc.Assembly.CreateInstance(contextDesc.FullName);
                    contextDescs.Add(contextDesc.FullName, instance);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading context desc from type " + contextDesc.FullName + ": " + e.Message);
                }
            }

            return contextDescs;
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

        private static volatile Dictionary<string, VFXOperatorDesc> m_OperatorDescs;
        private static volatile Dictionary<string, VFXContextDesc> m_ContextDescs;
        private static volatile Dictionary<string, VFXBlockDesc> m_BlockDescs;

        private static Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
    }
}
