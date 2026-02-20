using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal class ProviderLibrary
    {
        static ProviderLibrary s_instance;
        internal static ProviderLibrary Instance
        {
            get
            {
                if (s_instance is null)
                {
                    s_instance = new();
                    s_instance.PopulateFromFiles();
                    s_instance.PopulateFromScripts();
                }
                return s_instance;
            }
        }

        Dictionary<string, IProvider> providers = new();
        Dictionary<GUID, HashSet<string>> lookup = new();

        internal IEnumerable<IProvider> AllProviders() => providers.Values;

        internal IEnumerable<IProvider<T>> AllProvidersByType<T>() where T : IShaderObject
        {
            foreach(var provider in providers.Values)
            {
                if (provider is IProvider<T> typedProvider)
                    yield return typedProvider;
            }
        }

        internal void Clear(GUID assetID)
        {
            if (!lookup.TryGetValue(assetID, out var providerNames))
                return;

            foreach (var providerName in providerNames)
                providers.Remove(providerName);

            lookup.Remove(assetID);
        }

        internal bool TryAdd(IProvider provider)
        {
            if (!providers.TryAdd(provider.ProviderKey, provider))
                return false;

            if (!lookup.TryAdd(provider.AssetID, new() { provider.ProviderKey }))
                lookup[provider.AssetID].Add(provider.ProviderKey);

            return true;
        }

        internal bool TryFind(string name, out IProvider provider)
            => providers.TryGetValue(name, out provider);

        internal void PopulateFromFiles()
        {
            var results = AssetDatabase.FindAssetGUIDs("t: ShaderInclude");

            foreach (var result in results)
            {
                AnalyzeFile(result);
            }
        }

        internal void PopulateFromScripts()
        {
            foreach (var type in ProviderTypeCache.GetScriptedProviderTypes())
            {
                var provider = Activator.CreateInstance(type, true) as IProvider;
                if (provider != null)
                    TryAdd(provider);
            }
        }

        internal bool AnalyzeFile(GUID assetID)
        {
            Clear(assetID);

            var shaderInclude = AssetDatabase.LoadAssetByGUID<ShaderInclude>(assetID);
            if (shaderInclude == null)
                return false;

            var reflection = shaderInclude.Reflection;
            if (reflection == null)
                return false;

            foreach (var func in reflection.ReflectedFunctions)
            {
                var candidateProvider = new ReflectedFunctionProvider(assetID, func);
                if (!TryAdd(candidateProvider))
                {
                    TryFind(candidateProvider.ProviderKey, out var provider);

                    string path = AssetDatabase.GUIDToAssetPath(assetID);
                    string foundPath = AssetDatabase.GUIDToAssetPath(provider.AssetID);
                    Debug.LogError($"Attempted to register '{candidateProvider.ProviderKey}' found in '{path}', but was already registered from '{foundPath}'.", shaderInclude);
                }
            }
            return true;
        }
    }
}
