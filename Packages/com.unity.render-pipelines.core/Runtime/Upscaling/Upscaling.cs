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
            public Type upscalerType; // Unique at runtime, cheaper than comparing strings.

            public ContextKey(ulong cameraId, Type upscalerType)
            {
                this.cameraId = cameraId;
                this.upscalerType = upscalerType;
            }

            public bool Equals(ContextKey other)
            {
                return cameraId == other.cameraId &&
                       upscalerType == other.upscalerType;
            }

            public override bool Equals(object? obj) => obj is ContextKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(cameraId, upscalerType);
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
            var key = new ContextKey(cameraId, upscaler.GetType());

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
        /// <summary>
        /// The registered upscalers, keyed by upscaler ID.
        /// </summary>
        public static readonly Dictionary<string, (Type UpscalerType, Type? OptionsType, string DisplayName)> s_RegisteredUpscalers = new();

        /// <summary>
        /// Registers an IUpscaler type without any custom options type.
        /// </summary>
        /// <param name="upscalerId">Stable identifier for the upscaler, serialized by render pipeline assets.</param>
        /// <param name="displayName">Name shown for the upscaler in the editor.</param>
        public static void Register<TUpscaler>(string upscalerId, string displayName) where TUpscaler : IUpscaler, new()
        {
            Register(typeof(TUpscaler), null, upscalerId, displayName);
        }

        /// <summary>
        /// Registers an IUpscaler type with its custom options type.
        /// </summary>
        /// <param name="upscalerId">Stable identifier for the upscaler, serialized by render pipeline assets.</param>
        /// <param name="displayName">Name shown for the upscaler in the editor.</param>
        public static void Register<TUpscaler, TOptions>(string upscalerId, string displayName)
            where TUpscaler : IUpscaler
            where TOptions : UpscalerOptions
        {
            Register(typeof(TUpscaler), typeof(TOptions), upscalerId, displayName);
        }

        static void Register(Type upscalerType, Type? optionsType, string upscalerId, string displayName)
        {
            if (string.IsNullOrEmpty(upscalerId))
            {
                Debug.LogError($"{upscalerType.FullName} was registered without an upscaler ID.");
                return;
            }

            // Upscalers re-register on domain reload and again on runtime init, so a repeat is expected.
            if (s_RegisteredUpscalers.TryGetValue(upscalerId, out var registered))
            {
                // Single Id registered under multiple types is an error.
                if (registered.UpscalerType != upscalerType)
                {
                    Debug.LogError($"{upscalerType.AssemblyQualifiedName} cannot use the upscaler ID '{upscalerId}' because {registered.UpscalerType.AssemblyQualifiedName} already does.");
                    return;
                }
            }

            s_RegisteredUpscalers[upscalerId] = (upscalerType, optionsType, displayName);
#if UNITY_EDITOR
            UpscalerSupportedBuildTargetAttribute.ValidateDeclarations(upscalerType);
#endif
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
        private string[] m_UpscalerIdsCache;
        private int m_ActiveUpscalerIndex = -1;
        private readonly UpscalerContextManager m_ContextManager = new();
        // Pipeline-wide ("global") options per upscaler, keyed by upscaler type; GetGlobalOptions() returns these.
        // Absent for upscalers registered without an options type (spatial/embedded) — their options are null.
        private readonly Dictionary<Type, UpscalerOptions> m_GlobalOptions = new();
        #endregion

        /// <summary>
        /// Returns the IDs of the upscalers registered to the upscaling system.
        /// </summary>
        public IReadOnlyList<string> upscalerIds => m_UpscalerIdsCache;

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
                string upscalerId = kvp.Key;
                Type upscalerType = kvp.Value.UpscalerType;
                Type? optionsType = kvp.Value.OptionsType;

                // find any serialized options, if any provided by the package implementor
                int optionsIndex = upscalerOptions.FindIndex(o => o != null && o.GetType() == optionsType);
                bool optionsNotFound = optionsIndex == -1;
                UpscalerOptions? options = optionsNotFound ? null: upscalerOptions[optionsIndex];

                // construct upscaler (always parameterless; options are owned by the framework, not the instance)
                IUpscaler upscaler = (IUpscaler)Activator.CreateInstance(upscalerType);

                // The registered ID is what assets serialize, so an upscaler disagreeing with it would never resolve.
                if (upscaler.upscalerId != upscalerId)
                    Debug.LogError($"{upscalerType.FullName} was registered as '{upscalerId}' but reports the upscaler ID '{upscaler.upscalerId}'.");

                // Create a fresh options object when the asset provided none, so a quality-mode upscaler always has one.
                // Upscalers without an options type (spatial/embedded) keep no entry, leaving their options null.
                if (options == null && optionsType != null)
                    options = (UpscalerOptions)ScriptableObject.CreateInstance(optionsType);

                if (options != null)
                {
                    options.upscalerId = upscalerId;
                    m_GlobalOptions[upscalerType] = options;
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
                return string.Compare(a.Instance.upscalerId, b.Instance.upscalerId, StringComparison.OrdinalIgnoreCase);
            });

            // 3. Populate ID cache
            m_UpscalerIdsCache = new string[m_Upscalers.Count];
            for (int i = 0; i < m_Upscalers.Count; i++)
                m_UpscalerIdsCache[i] = m_Upscalers[i].Instance.upscalerId;
        }

        /// <summary>
        /// Sets the active upscaler by ID, returns whether an upscaler with the given ID was registered. The
        /// upscaler is activated whether or not it runs on this device, which is left for the caller to decide.
        /// </summary>
        public bool SetActiveUpscaler(string upscalerId)
        {
            int index = Array.IndexOf(m_UpscalerIdsCache, upscalerId);
            if (index == -1)
            {
                m_ActiveUpscalerIndex = -1;
                return false;
            }

            m_ActiveUpscalerIndex = index;
            return true;
        }

        /// <summary>
        /// Activates the first upscaler in the list that runs on this device.
        /// </summary>
        /// <param name="upscalerIds">Upscaler IDs, from highest to lowest priority.</param>
        public void SetActiveUpscalerToFirstSupported(IReadOnlyList<string>? upscalerIds)
        {
            m_ActiveUpscalerIndex = -1;
            if (upscalerIds == null)
                return;

            for (int i = 0; i < upscalerIds.Count; ++i)
            {
                int index = Array.IndexOf(m_UpscalerIdsCache, upscalerIds[i]);
                if (index == -1 || !m_Upscalers[index].Instance.isSupportedOnDevice)
                    continue;

                m_ActiveUpscalerIndex = index;
                return;
            }
        }

        /// <summary>
        /// Returns the index of the upscalerId. -1 is returned if upscalerId is not in the ID cache.
        /// </summary>
        public int IndexOf(string upscalerId)
        {
            return Array.IndexOf(m_UpscalerIdsCache, upscalerId);
        }

        /// <summary>
        /// Returns the registered IUpscaler instance with the given ID, or null if none matches. Resolves the same
        /// ID as <see cref="SetActiveUpscaler"/>.
        /// </summary>
        public IUpscaler? GetIUpscalerById(string upscalerId)
        {
            int index = IndexOf(upscalerId);
            return index >= 0 ? m_Upscalers[index].Instance : null;
        }

        /// <summary>
        /// Returns null if no IUpscaler exists for given type
        /// </summary>
        public IUpscaler? GetIUpscalerOfType<T>() where T : IUpscaler
        {
            return GetIUpscalerOfType(typeof(T));
        }

        /// <summary>
        /// Returns null if no IUpscaler exists for given type
        /// </summary>
        public IUpscaler? GetIUpscalerOfType(Type T)
        {
            foreach (UpscalerEntry entry in m_Upscalers)
                if (entry.Instance.GetType() == T)
                    return entry.Instance;

            Debug.LogError($"Upscaler type {T.FullName} not found");
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
            return upscaler != null && m_GlobalOptions.TryGetValue(upscaler.GetType(), out var options) ? options : null;
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
