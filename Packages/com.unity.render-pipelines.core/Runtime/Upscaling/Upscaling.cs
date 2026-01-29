#if ENABLE_UPSCALER_FRAMEWORK
#nullable enable
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public static class UpscalerRegistry
    {
        public static readonly Dictionary<Type, (Type? OptionsType, string ID)> s_RegisteredUpscalers = new();

        /// <summary>
        /// Registers an IUpscaler type without any custom options type.
        /// </summary>
        public static void Register<TUpscaler>(string id) where TUpscaler : IUpscaler, new()
        {
            s_RegisteredUpscalers[typeof(TUpscaler)] = (null, id);
        }

        /// <summary>
        /// Registers an IUpscaler type with its custom options type.
        /// </summary>
        public static void Register<TUpscaler, TOptions>(string id)
            where TUpscaler : IUpscaler
            where TOptions : UpscalerOptions
        {
            s_RegisteredUpscalers[typeof(TUpscaler)] = (typeof(TOptions), id);
        }
    }

    public class Upscaling
    {
        #region private

        // The integration type is internally used by the SRP systems, which contain embedded upscaling passes in an uber-pass.
        // The external upscaler integrations are assumed to be standalone render passes.
        private enum UpscalerIntegrationType
        {
            StandalonePass, // The upscaler is executed as a standalone Render Graph pass.
            EmbeddedPass // The upscaler is baked into a pipeline-specific uber-pass (e.g., URP's Post-process pass).
        }
        private struct UpscalerEntry
        {
            readonly public IUpscaler Instance { get; }
            readonly public UpscalerIntegrationType IntegrationType { get; }
            readonly public bool IsEmbedded { get { return IntegrationType == UpscalerIntegrationType.EmbeddedPass; } }

            public UpscalerEntry(IUpscaler instance, UpscalerIntegrationType integrationType)
            {
                Instance = instance;
                IntegrationType = integrationType;
            }
        }

        private List<UpscalerEntry> m_Upscalers = new List<UpscalerEntry>();
        private string[] m_UpscalerNamesCache;
        private int m_ActiveUpscalerIndex = -1;
        #endregion

        /// <summary>
        /// Returns the names of the upscalers registered to the upscaling system.
        /// </summary>
        public IReadOnlyList<string> upscalerNames => m_UpscalerNamesCache;

        /// <summary>
        /// Returns the active IUpscaler instance, null if none is selected.
        /// </summary>
        public IUpscaler? activeUpscaler => (m_ActiveUpscalerIndex >= 0) ? m_Upscalers[m_ActiveUpscalerIndex].Instance : null;

        /// <summary>
        /// Returns true if the active upscaler is embedded in an uber pass.
        /// </summary>
        public bool activeUpscalerIsEmbedded => (m_ActiveUpscalerIndex >= 0) && m_Upscalers[m_ActiveUpscalerIndex].IsEmbedded;

        /// <summary>
        /// Initializes the Upscaling system. with the given list of upscaler options per upscaler type.
        /// </summary>
        /// <param name="upscalerOptions">The list of options from the RP asset.</param>
        /// <param name="embeddedTypes">A set of types that the pipeline handles internally (e.g., Bilinear, Point in URP).</param>
        /// <param name="priorityOrder">
        ///   An ordered list of Types. Upscalers matching these types will appear first 
        ///   in the list, in the order provided. All others appear after, alphabetically.
        /// </param>
        public Upscaling(
            List<UpscalerOptions> upscalerOptions, 
            HashSet<Type>? embeddedTypes = null,
            Type[]? priorityOrder = null
        )
        {
            // 1. Instantiate the upscaler instances
            foreach (var kvp in UpscalerRegistry.s_RegisteredUpscalers)
            {
                Type upscalerType = kvp.Key;
                Type? optionsType = kvp.Value.OptionsType;

                // find any serialized options, if any provided by the package implementor
                int optionsIndex = upscalerOptions.FindIndex(o => o != null && o.GetType() == optionsType);
                bool optionsNotFound = optionsIndex == -1;
                UpscalerOptions? options = optionsNotFound ? null: upscalerOptions[optionsIndex];

                // construct upscaler
                IUpscaler upscaler = optionsType != null
                    ? (IUpscaler)Activator.CreateInstance(upscalerType, new object[] { options! })
                    : (IUpscaler)Activator.CreateInstance(upscalerType);

                if(options != null && string.IsNullOrEmpty(options.upscalerName))
                {
                    Debug.LogWarningFormat("[Upscaling] UpscalerOptions with empty upscalerName for {0}", upscaler.name);
                    options.upscalerName = upscaler.name;
                }

                bool isEmbedded = embeddedTypes != null && embeddedTypes.Contains(upscalerType);
                m_Upscalers.Add(new UpscalerEntry(upscaler, isEmbedded ? UpscalerIntegrationType.EmbeddedPass : UpscalerIntegrationType.StandalonePass));
            }

            // 2. Type-based sorting based on priorty order
            m_Upscalers.Sort((a, b) =>
            {
                Type typeA = a.Instance.GetType();
                Type typeB = b.Instance.GetType();

                int indexA = -1;
                int indexB = -1;

                if (priorityOrder != null)
                {
                    indexA = Array.IndexOf(priorityOrder, typeA);
                    indexB = Array.IndexOf(priorityOrder, typeB);
                }

                // Priority Sort: If both are in the priority list, respect that order.
                if (indexA != -1 && indexB != -1) return indexA.CompareTo(indexB);

                // Mixed Sort: Priority items always come before non-priority items.
                if (indexA != -1) return -1;
                if (indexB != -1) return 1;

                // Fallback Sort: If neither are in the list (external upscalers), sort Alphabetically.
                return string.Compare(a.Instance.name, b.Instance.name, StringComparison.OrdinalIgnoreCase);
            });

            // 3. Populate name cache
            m_UpscalerNamesCache = new string[m_Upscalers.Count];
            for (int i = 0; i < m_Upscalers.Count; i++)
            {
                string name = m_Upscalers[i].Instance.name;
                m_UpscalerNamesCache[i] = name;
            }
        }

        /// <summary>
        /// Sets the active upscaler by name, returns whether an upscaler with the given name was found.
        /// </summary>
        public bool SetActiveUpscaler(string name)
        {
            int index = Array.IndexOf(m_UpscalerNamesCache, name);
            if (index == -1)
            {
                m_ActiveUpscalerIndex = -1;
                return false;
            }

            m_ActiveUpscalerIndex = index;

            // TODO (Apoorva): We need to allow the IUpscaler itself to decide whether it can run. E.g.
            // DLSS might need a certain version of Windows, and a compatible GPU. We should add an
            // overrideable function to IUpscaler so that the active IUpscaler can return a bool
            // indicating support.
            return true;
        }

        /// <summary>
        /// Returns the index of the upscalerName. -1 is returned if upscalerName is not in the name cache.
        /// </summary>
        public int IndexOf(string upscalerName)
        {
            return Array.IndexOf(m_UpscalerNamesCache, upscalerName);
        }

        /// <summary>
        /// Returns null if no IUpscaler exists for given type
        /// </summary>
        public IUpscaler? GetIUpscalerOfType<T>() where T : IUpscaler
        {
            if(!UpscalerRegistry.s_RegisteredUpscalers.ContainsKey(typeof(T)))
                return null;
            foreach (UpscalerEntry entry in m_Upscalers)
                if (entry.Instance.GetType() == typeof(T))
                    return entry.Instance;
            Debug.LogErrorFormat($"Upscaler type {typeof(T)} not found");
            return null;
        }

        /// <summary>
        /// Returns null if no IUpscaler exists for given type
        /// </summary>
        public IUpscaler? GetIUpscalerOfType(Type T)
        {
            if (!UpscalerRegistry.s_RegisteredUpscalers.ContainsKey(T))
                return null;
            foreach (UpscalerEntry entry in m_Upscalers)
                if (entry.Instance.GetType() == T)
                    return entry.Instance;
            Debug.LogErrorFormat($"Upscaler type {T} not found");
            return null;
        }
    }
}
#endif
