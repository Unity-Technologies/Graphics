using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The set of shadow geometry generators this project knows about, keyed by
    /// <see cref="ShadowGeometryGenerator.id"/>.
    /// </summary>
    /// <remarks>
    /// Built-in generators are always present so the system works even with no settings asset.
    /// Everything else comes from <see cref="Shadow2DGeometrySettings"/>, which the editor
    /// populates by reflection on domain reload and which is deserialized -- not reflected --
    /// at runtime.
    ///
    /// The key is the id alone. Version is deliberately NOT part of it: bumping a generator's
    /// version must keep existing casters resolving to that same generator, otherwise every
    /// version bump would silently drop content onto the fallback.
    /// </remarks>
    internal static class ShadowGeometryGeneratorRegistry
    {
        /// <summary>Resolved when a caster references an id this build does not have.</summary>
        internal const string k_DefaultGeneratorId = LegacyShadowGeometryGenerator.k_Id;

        // Always available, independent of settings. A missing or empty settings object must not
        // be able to take shadows down.
        static ShadowGeometryGenerator[] CreateBuiltIns()
        {
            return new ShadowGeometryGenerator[]
            {
                new LegacyShadowGeometryGenerator(),
                new SoftShadowGeometryGenerator(),
            };
        }

        static Dictionary<string, ShadowGeometryGenerator> s_Generators;
        static List<ShadowGeometryGenerator> s_Ordered;
        static readonly HashSet<string> s_WarnedUnknownIds = new HashSet<string>();

        // Statics survive "Enter Play Mode without Domain Reload", so the cache is rebuilt
        // explicitly rather than relying on field initializers re-running.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void Invalidate()
        {
            s_Generators = null;
            s_Ordered = null;
            s_WarnedUnknownIds.Clear();
        }

        static void EnsureBuilt()
        {
            if (s_Generators != null)
                return;

            s_Generators = new Dictionary<string, ShadowGeometryGenerator>(StringComparer.Ordinal);
            s_Ordered = new List<ShadowGeometryGenerator>();

            foreach (var generator in CreateBuiltIns())
                TryRegister(generator, isBuiltIn: true);

            // Discovered generators, including third-party ones. Absent in a project with no URP
            // settings (tests, a bare domain reload) -- built-ins alone are enough there.
            if (GraphicsSettings.TryGetRenderPipelineSettings<Shadow2DGeometrySettings>(out var settings))
            {
                var discovered = settings.generators;
                for (int i = 0; i < discovered.Count; i++)
                    TryRegister(discovered[i], isBuiltIn: false);
            }
        }

        static void TryRegister(ShadowGeometryGenerator generator, bool isBuiltIn)
        {
            if (generator == null)
                return;

            string id = generator.id;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[Shadow2D] Shadow geometry generator '{generator.GetType().FullName}' declares no id and was not registered.");
                return;
            }

            if (s_Generators.TryGetValue(id, out var existing))
            {
                // Deterministic: first registration wins, so a third-party generator can never
                // displace a built-in by reusing its id.
                if (existing.GetType() != generator.GetType())
                {
                    Debug.LogError($"[Shadow2D] Duplicate shadow geometry generator id '{id}'. Keeping '{existing.GetType().FullName}', ignoring '{generator.GetType().FullName}'. Ids must be unique; prefix yours with a vendor name.");
                }
                return;
            }

            s_Generators.Add(id, generator);
            s_Ordered.Add(generator);
        }

        /// <summary>Registered generators, built-ins first, in registration order.</summary>
        internal static IReadOnlyList<ShadowGeometryGenerator> generators
        {
            get
            {
                EnsureBuilt();
                return s_Ordered;
            }
        }

        internal static int generatorCount
        {
            get
            {
                EnsureBuilt();
                return s_Ordered.Count;
            }
        }

        /// <summary>
        /// Resolves <paramref name="id"/>, falling back to <see cref="k_DefaultGeneratorId"/>
        /// when a caster references a generator this build does not have. Never returns null and
        /// never throws -- content referencing a removed generator must still render.
        /// </summary>
        internal static ShadowGeometryGenerator Get(string id)
        {
            EnsureBuilt();

            if (!string.IsNullOrEmpty(id) && s_Generators.TryGetValue(id, out var generator))
                return generator;

            if (!string.IsNullOrEmpty(id) && s_WarnedUnknownIds.Add(id))
                Debug.LogWarning($"[Shadow2D] Unknown shadow geometry generator id '{id}'. Falling back to '{k_DefaultGeneratorId}'.");

            return s_Generators[k_DefaultGeneratorId];
        }

        /// <summary>Whether <paramref name="id"/> resolves without falling back.</summary>
        internal static bool Contains(string id)
        {
            EnsureBuilt();
            return !string.IsNullOrEmpty(id) && s_Generators.ContainsKey(id);
        }
    }
}
