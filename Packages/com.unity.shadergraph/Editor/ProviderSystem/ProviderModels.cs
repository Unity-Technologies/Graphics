using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [AttributeUsage(AttributeTargets.Class)]
    sealed class ProviderModelAttribute : Attribute
    {
        internal readonly string ProviderKey;
        internal ProviderModelAttribute(string providerKey) { ProviderKey = providerKey; }
    }

    static class ProviderModels
    {
        static Dictionary<string, Type> s_modelsByKey;

        internal static bool TryCreate(ProviderInfo info, out object model)
        {
            if (s_modelsByKey == null)
            {
                s_modelsByKey = new();
                foreach (var type in TypeCache.GetTypesWithAttribute<ProviderModelAttribute>())
                {
                    var key = type.GetCustomAttribute<ProviderModelAttribute>().ProviderKey;
                    if (string.IsNullOrEmpty(key))
                        Debug.LogError($"'{type.Name}' is marked as a provider model but has no provider key.");
                    else if (!s_modelsByKey.TryAdd(key, type))
                        Debug.LogError($"Attempted to register '{type.Name}' with provider key '{key}', but key is already registered to '{s_modelsByKey[key]}'.");
                }
            }

            if (info.ProviderKey != null && s_modelsByKey.TryGetValue(info.ProviderKey, out var modelType))
            {
                model = Activator.CreateInstance(modelType, true);
                return true;
            }
            model = null;
            return false;
        }
    }
}
