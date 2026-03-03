using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // This allows any model in a domain to be mapped to a provider.
    internal class ProviderModelAttribute : Attribute
    {
        public readonly string ProviderKey;
        public ProviderModelAttribute(string providerKey) { ProviderKey = providerKey; }
    }

    internal class ScriptedProviderAttribute : Attribute { }

    internal static class ProviderTypeCache
    {
        static Dictionary<string, Type> s_providerModels = null;
        static List<Type> s_scriptedProviders = null;

        private static void BuildCache()
        {
            if (s_providerModels == null && s_scriptedProviders == null)
            {
                s_providerModels = new();
                s_scriptedProviders = new();

                foreach(var type in TypeCache.GetTypesWithAttribute<ProviderModelAttribute>())
                {
                    var providerKey = type.GetCustomAttribute<ProviderModelAttribute>()?.ProviderKey ?? null;
                    if (providerKey != null)
                        if (!s_providerModels.TryAdd(providerKey, type))
                            Debug.LogError($"Attempted to register '{type.Name}' with provider key '{providerKey}', but key is already registered to '{s_providerModels[providerKey]}'.");
                }

                foreach (var type in TypeCache.GetTypesWithAttribute<ScriptedProviderAttribute>())
                    s_scriptedProviders.Add(type);
            }
        }

        internal static IEnumerable<Type> GetScriptedProviderTypes()
        {
            BuildCache();

            return s_scriptedProviders;
        }

        internal static bool TryCreateModel(string key, out object model)
        {
            BuildCache();

            if (s_providerModels.TryGetValue(key, out Type type))
            {
                model = Activator.CreateInstance(type, true);
                return true;
            }
            model = null;
            return false;
        }
    }
}