#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class Provider2DCache<T> : IProvider2DCache where T : Provider2D
    {
        [SerializeField] private List<Provider2DKVPair> m_Cache = new List<Provider2DKVPair>();

        public IEnumerable<Provider2DKVPair> Cache
        {
            get
            {
                foreach (var kvp in m_Cache)
                {
                    // Only return if the Value (T) is not null
                    if (kvp.m_Value.m_Provider != null)
                    {
                        yield return kvp;
                    }
                }
            }
        }

        static int CacheContainsKeyAt(List<Provider2DKVPair> cache, Provider2DInfo key)
        {
            for(int i=0;i < cache.Count; i++)
            {
                Provider2DKVPair pair  = cache[i];
                if (IsProviderInfoEqual(key, pair.m_Key))
                    return i;
            }

            return -1;
        }

        internal static bool AreProviderCachesEqual(Provider2DCache<T> a, Provider2DCache<T> b, Action<string> onNotEqual)
        {
            if(a.m_Cache.Count != b.m_Cache.Count)     { onNotEqual("Cache sizes are Different Sizes"); return false; }

            for(int i = 0; i < a.m_Cache.Count; i++)
            { 
                if(!IsProviderInfoEqual(a.m_Cache[i].m_Key, b.m_Cache[i].m_Key)) { onNotEqual("Key info is different"); return false; }

                Provider2D aT = a.m_Cache[i].m_Value.m_Provider;
                Provider2D bT = b.m_Cache[i].m_Value.m_Provider;

                if (aT == null || bT == null) { onNotEqual("Value info is null"); return false; }
            }

            return true;
        }

        static bool IsProviderInfoEqual(Provider2DInfo a, Provider2DInfo b)
        {
            if(a.m_Component == null || b.m_Component == null)
                return a.m_TypeName == b.m_TypeName && a.m_Component == b.m_Component;
            else
                return a.m_TypeName == b.m_TypeName && a.m_Component.GetType().Name == b.m_Component.GetType().Name;
        }

        public void UpdateCache(GameObject gameObj)
        {
            var providerTypes = TypeCache.GetTypesDerivedFrom<T>();
            var activeProviders = new List<Provider2DKVPair>();

            foreach (Type providerType in providerTypes)
            {
                if (providerType.IsAbstract)
                    continue;

                T provider = Activator.CreateInstance(providerType) as T;

                Component[] components = gameObj.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (provider.Internal_IsRequiredComponentData(component))
                    {
                        Provider2DInfo info = new Provider2DInfo(providerType, component);
                        int keyIndex = CacheContainsKeyAt(m_Cache, info);
                        if (keyIndex >= 0)
                        {
                            activeProviders.Add(m_Cache[keyIndex]);
                        }
                        else
                        {
                            provider.OnAwake();
                            activeProviders.Add(new Provider2DKVPair(info, new Provider2DRef(provider)));
                        }
                    }
                }
            }

            // We need to clear any deleted providers from our cache without deleting data we have cached.
            m_Cache.Clear();
            for (int i = 0; i < activeProviders.Count; i++)
                m_Cache.Add(activeProviders[i]);

        }
    }
}
#endif
