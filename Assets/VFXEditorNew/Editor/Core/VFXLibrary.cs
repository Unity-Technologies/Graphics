using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace UnityEditor.VFX
{
    static class VFXLibrary
    {
        public static VFXContextDesc GetContext(string id)      { LoadIfNeeded(); return m_ContextDescs[id]; }
        public static IEnumerable<VFXContextDesc> GetContexts() { LoadIfNeeded(); return m_ContextDescs.Values; }

        public static VFXBlockDesc GetBlock(string id)          { LoadIfNeeded(); return m_BlockDescs[id]; }
        public static IEnumerable<VFXBlockDesc> GetBlocks()     { LoadIfNeeded(); return m_BlockDescs.Values; }

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
                LoadBlockDescs();
                m_Loaded = true;
            }
        }

        private static void LoadContextDescs()
        {
            // Search for derived type of VFXBlockType in assemblies
            var contextDescTypes = FindConcreteSubclasses<VFXContextDesc,VFXContextAttribute>();
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

        private static void LoadBlockDescs()
        {
            // Search for derived type of VFXBlockType in assemblies
            var blockDescTypes = FindConcreteSubclasses<VFXBlockDesc,VFXBlockAttribute>();
            var blockDescs = new Dictionary<string, VFXBlockDesc>();
            foreach (var blockDesc in blockDescTypes)
            {
                try
                {
                    VFXBlockDesc instance = (VFXBlockDesc)blockDesc.Assembly.CreateInstance(blockDesc.FullName);
                    blockDescs.Add(blockDesc.FullName, instance);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while loading context desc from type " + blockDesc.FullName + ": " + e.Message);
                }
            }

            m_BlockDescs = blockDescs; // atomic set
        }

        private static IEnumerable<Type> FindConcreteSubclasses<T,U>()
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
            return types.Where(type => type.GetCustomAttributes(typeof(U), false).Length == 1);
        }

        private static volatile Dictionary<string, VFXContextDesc> m_ContextDescs;
        private static volatile Dictionary<string, VFXBlockDesc> m_BlockDescs;

        private static Object m_Lock = new Object();
        private static volatile bool m_Loaded = false;
    }
}
