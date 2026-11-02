#if ENABLE_UPSCALER_FRAMEWORK
#nullable enable
using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Manages per-camera upscaler contexts. Handles context creation, caching, validation, and expiry.
    /// </summary>
    internal class UpscalerContextManager
    {
        private struct ContextKey : IEquatable<ContextKey>
        {
            public ulong cameraId;
            public string upscalerName;

            public ContextKey(ulong cameraId, string upscalerName)
            {
                this.cameraId = cameraId;
                this.upscalerName = upscalerName;
            }

            public bool Equals(ContextKey other)
            {
                return cameraId == other.cameraId &&
                       upscalerName == other.upscalerName;
            }

            public override bool Equals(object? obj) => obj is ContextKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(cameraId, upscalerName);
        }

        // Contexts unused for this many frames are automatically cleaned up.
        // 400 frames at 60 FPS ≈ 6.7 seconds. This threshold follows the pattern
        // established in HDRP's existing context management.
        private const int k_ContextExpiryFrames = 400;
        private readonly Dictionary<ContextKey, IUpscalerContext> m_Contexts = new();
        private readonly List<ContextKey> m_KeysToRemove = new(); // Reusable list for cleanup
        private readonly List<IUpscalerContext> m_InvalidatedContexts = new(); // Contexts pending cleanup

        /// <summary>
        /// Acquires a context for the specified camera and upscaler.
        /// Returns a cached context if valid, or creates a new one if missing or invalid.
        /// Also updates the context's last-used frame for expiry tracking.
        /// </summary>
        /// <param name="cameraId">Unique ID for the camera. In XR scenarios, this must be unique per view (encode eye information into the ID).</param>
        /// <param name="upscaler">The upscaler to acquire a context for.</param>
        /// <param name="options">The current upscaler options.</param>
        /// <param name="displayResolution">The target display resolution.</param>
        /// <returns>The context, or null for spatial upscalers that don't need context.</returns>
        public IUpscalerContext? AcquireContext(
            ulong cameraId,
            IUpscaler upscaler,
            UpscalerOptions options,
            Vector2Int displayResolution)
        {
            var key = new ContextKey(cameraId, upscaler.name);

            // Note: Time.frameCount may not advance in Editor when Game view is inactive.
            // This is acceptable because contexts are only acquired during active rendering,
            // and lastUsedFrame is updated on every AcquireContext call. If a more robust
            // solution is needed (e.g., for scene view upscaling), consider using
            // Time.realtimeSinceStartup or a pipeline-provided render counter.
            int currentFrame = Time.frameCount;

            if (m_Contexts.TryGetValue(key, out var existingContext))
            {
                // Check if context is still valid
                bool resolutionValid = existingContext.createdForDisplayResolution == displayResolution;
                bool optionsValid = existingContext.IsValidForOptions(options);

                if (resolutionValid && optionsValid)
                {
                    existingContext.lastUsedFrame = currentFrame;
                    return existingContext;
                }

                // Context is invalid, queue it for cleanup and remove from cache
                m_InvalidatedContexts.Add(existingContext);
                m_Contexts.Remove(key);
            }

            // Create new context
            var newContext = upscaler.CreateContext(options, displayResolution);
            if (newContext != null)
            {
                newContext.lastUsedFrame = currentFrame;
                m_Contexts[key] = newContext;
            }
            else if (upscaler.isTemporal)
            {
                // Temporal upscalers should always return a context. Null indicates
                // a creation failure (e.g., plugin not loaded, GPU not supported).
                Debug.LogWarning($"[UpscalerContextManager] Temporal upscaler '{upscaler.name}' returned null context. " +
                                 "Upscaling may not function correctly.");
            }

            return newContext;
        }

        /// <summary>
        /// Removes contexts that haven't been used for more than k_ContextExpiryFrames frames.
        /// Also cleans up any contexts that were invalidated (due to resolution or option changes).
        /// Should be called once per frame from the render pipeline.
        /// </summary>
        public void CleanupExpiredContexts(CommandBuffer cmd)
        {
            // Clean up invalidated contexts first
            foreach (var context in m_InvalidatedContexts)
            {
                context.Cleanup(cmd);
            }
            m_InvalidatedContexts.Clear();

            // Clean up expired contexts (see AcquireContext for Time.frameCount limitations)
            int currentFrame = Time.frameCount;
            m_KeysToRemove.Clear();

            foreach (var kvp in m_Contexts)
            {
                var context = kvp.Value;
                int framesSinceLastUse = currentFrame - context.lastUsedFrame;
                if (framesSinceLastUse > k_ContextExpiryFrames)
                {
                    context.Cleanup(cmd);
                    m_KeysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in m_KeysToRemove)
            {
                m_Contexts.Remove(key);
            }
        }

        /// <summary>
        /// Cleans up all contexts. Called when the upscaling system is disposed.
        /// </summary>
        public void Dispose(CommandBuffer cmd)
        {
            // Clean up any pending invalidated contexts
            foreach (var context in m_InvalidatedContexts)
            {
                context.Cleanup(cmd);
            }
            m_InvalidatedContexts.Clear();

            // Clean up all active contexts
            foreach (var kvp in m_Contexts)
            {
                kvp.Value.Cleanup(cmd);
            }
            m_Contexts.Clear();
        }
    }

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
        private readonly UpscalerContextManager m_ContextManager = new();
        // Pipeline-wide ("global") options per upscaler, keyed by upscaler name; GetGlobalOptions() returns these.
        // Absent for upscalers registered without an options type (spatial/embedded) — their options are null.
        private readonly Dictionary<string, UpscalerOptions> m_GlobalOptions = new();
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

                // construct upscaler (always parameterless; options are owned by the framework, not the instance)
                IUpscaler upscaler = (IUpscaler)Activator.CreateInstance(upscalerType);

                // Create a fresh options object when the asset provided none, so a quality-mode upscaler always has one.
                // Upscalers without an options type (spatial/embedded) keep no entry, leaving their options null.
                if (options == null && optionsType != null)
                {
                    options = (UpscalerOptions)ScriptableObject.CreateInstance(optionsType);
                    options.upscalerName = upscaler.name;
                }
                if (options != null && string.IsNullOrEmpty(options.upscalerName))
                {
                    Debug.LogWarningFormat("[Upscaling] UpscalerOptions with empty upscalerName for {0}", upscaler.name);
                    options.upscalerName = upscaler.name;
                }
                if (options != null)
                    m_GlobalOptions[upscaler.name] = options;

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
        /// Returns the registered IUpscaler instance with the given name, or null if none matches. Resolves the same
        /// name as <see cref="SetActiveUpscaler"/> (the registered upscaler name).
        /// </summary>
        public IUpscaler? GetIUpscalerByName(string upscalerName)
        {
            int index = IndexOf(upscalerName);
            return index >= 0 ? m_Upscalers[index].Instance : null;
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

        #region Options

        /// <summary>
        /// Returns the pipeline-wide ("global") options for the given upscaler — the live, user-configured options
        /// object the pipeline supplied (e.g. an RP-asset sub-asset), shared by all cameras — or <c>null</c> if the
        /// upscaler was registered without an options type (e.g. spatial/embedded upscalers).
        /// </summary>
        /// <param name="upscaler">The upscaler whose global options to fetch.</param>
        /// <returns>The pipeline-wide options, or null if the upscaler has no options type.</returns>
        /// <remarks>
        /// This is not a copy and not factory defaults: editing the returned object (inspector or C# API) is reflected
        /// here, because it is the same serialized instance. Options are owned by the framework, not the
        /// <see cref="IUpscaler"/> instance.
        /// <para>
        /// Per-camera options (future) layer on top of this: add an override-aware
        /// <c>ResolveOptions(IUpscaler, UpscalerOptions perCameraOverride)</c> returning
        /// <c>perCameraOverride ?? GetGlobalOptions(upscaler)</c>, and have the call sites pass the camera's serialized
        /// override (read from the pipeline-specific camera component — core cannot see it). That is a non-breaking
        /// addition and this method stays as the global fallback. Not added now because no per-camera override source
        /// exists yet (it would be a no-op).
        /// </para>
        /// </remarks>
        public UpscalerOptions? GetGlobalOptions(IUpscaler upscaler)
        {
            return upscaler != null && m_GlobalOptions.TryGetValue(upscaler.name, out var options) ? options : null;
        }

        #endregion

        #region Context Management

        /// <summary>
        /// Acquires an upscaler context for the specified camera.
        /// Returns a cached context if valid, or creates a new one if missing or invalid.
        /// Also updates the context's last-used frame for expiry tracking.
        /// </summary>
        /// <param name="cameraId">Unique ID for the camera. In XR scenarios, this must be unique per view (encode eye information into the ID).</param>
        /// <param name="upscaler">The upscaler to acquire a context for.</param>
        /// <param name="options">The current upscaler options.</param>
        /// <param name="displayResolution">The target display resolution.</param>
        /// <returns>The context, or null for spatial upscalers that don't need context.</returns>
        public IUpscalerContext? AcquireContext(
            ulong cameraId,
            IUpscaler upscaler,
            UpscalerOptions options,
            Vector2Int displayResolution)
        {
            return m_ContextManager.AcquireContext(cameraId, upscaler, options, displayResolution);
        }

        /// <summary>
        /// Cleans up contexts that haven't been used for a while (400 frames).
        /// Should be called once per frame from the render pipeline.
        /// </summary>
        /// <param name="cmd">The command buffer to record cleanup commands into.</param>
        public void CleanupExpiredContexts(CommandBuffer cmd)
        {
            m_ContextManager.CleanupExpiredContexts(cmd);
        }

        /// <summary>
        /// Cleans up all contexts. Call when the upscaling system is torn down (e.g. when the render
        /// pipeline is disposed)
        /// </summary>
        /// <param name="cmd">The command buffer to record cleanup commands into.</param>
        public void Dispose(CommandBuffer cmd)
        {
            m_ContextManager.Dispose(cmd);
        }

        #endregion
    }
}
#endif
