#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Finds every <c>ShadowGeometryGenerator</c> in the project and bakes the list into
    /// <c>Shadow2DGeometrySettings</c>, so the runtime never has to reflect.
    /// </summary>
    /// <remarks>
    /// Mirrors what Unity does for graphics settings themselves in
    /// <c>RenderPipelineGraphicsSettingsManager.PopulateRenderPipelineGraphicsSettings</c>:
    /// TypeCache to find types, Activator to instantiate, and write back only when the set
    /// actually changed so the settings asset is not re-dirtied on every reload.
    /// </remarks>
    [InitializeOnLoad]
    static class Shadow2DGeometryDiscovery
    {
        static Shadow2DGeometryDiscovery()
        {
            // Runs after every domain reload, which is when new or removed generator types
            // become visible. Deferred because graphics settings may not be ready yet during
            // static construction.
            EditorApplication.delayCall += () => Refresh();
        }

        /// <summary>
        /// Rebuilds the baked generator list. Returns true if the settings asset was changed.
        /// </summary>
        internal static bool Refresh()
        {
            if (!GraphicsSettings.TryGetRenderPipelineSettings<Shadow2DGeometrySettings>(out var settings))
                return false;   // no URP graphics settings in this project

            List<ShadowGeometryGenerator> current = settings.generators;

            // Existing instances are reused rather than recreated so any serialized state on a
            // generator survives, and so an unchanged project produces no asset diff.
            var byType = new Dictionary<Type, ShadowGeometryGenerator>();
            for (int i = 0; i < current.Count; i++)
            {
                if (current[i] != null)
                    byType[current[i].GetType()] = current[i];
            }

            var rebuilt = new List<ShadowGeometryGenerator>();
            var seenIds = new Dictionary<string, Type>(StringComparer.Ordinal);

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ShadowGeometryGenerator>())
            {
                if (type.IsAbstract)
                    continue;

                // Hard-deprecated generators are dropped entirely; a plain [Obsolete] warning
                // keeps resolving so existing content is not broken by a deprecation.
                var obsolete = type.GetCustomAttribute<ObsoleteAttribute>();
                if (obsolete != null && obsolete.IsError)
                    continue;

                if (!byType.TryGetValue(type, out var generator))
                {
                    try
                    {
                        generator = (ShadowGeometryGenerator)Activator.CreateInstance(type, nonPublic: true);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Shadow2D] Could not instantiate shadow geometry generator '{type.FullName}'. It needs a parameterless constructor.");
                        Debug.LogException(ex);
                        continue;
                    }
                }

                string id = generator.id;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogError($"[Shadow2D] Shadow geometry generator '{type.FullName}' declares no id and was skipped.");
                    continue;
                }

                if (seenIds.TryGetValue(id, out var owner))
                {
                    Debug.LogError($"[Shadow2D] Duplicate shadow geometry generator id '{id}' on '{type.FullName}' and '{owner.FullName}'. Ids must be unique; prefix yours with a vendor name.");
                    continue;
                }

                seenIds.Add(id, type);
                rebuilt.Add(generator);
            }

            if (SameSet(current, rebuilt))
                return false;

            current.Clear();
            current.AddRange(rebuilt);

            // The registry caches its dictionary; it has to see the new list.
            ShadowGeometryGeneratorRegistry.Invalidate();

            EditorUtility.SetDirty(GraphicsSettings.currentRenderPipelineGlobalSettings);
            return true;
        }

        static bool SameSet(List<ShadowGeometryGenerator> a, List<ShadowGeometryGenerator> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (!ReferenceEquals(a[i], b[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Re-runs discovery immediately before a player build, so a build cannot ship a list
        /// that went stale because scripts changed without a reload having refreshed it.
        /// </summary>
        class BuildPreprocessor : IPreprocessBuildWithReport
        {
            public int callbackOrder => 0;

            public void OnPreprocessBuild(BuildReport report)
            {
                if (Refresh())
                    AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
