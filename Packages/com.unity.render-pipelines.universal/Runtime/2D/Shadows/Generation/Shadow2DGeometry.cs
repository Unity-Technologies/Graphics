namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Process-wide controls for 2D shadow geometry generation.
    /// </summary>
    internal static class Shadow2DGeometry
    {
        static string s_GlobalGeneratorOverride;

        /// <summary>
        /// Forces every shadow caster onto one generator regardless of what it has serialized.
        /// Null clears the override. Intended for A/B'ing a new generator against the existing
        /// one across a whole project without touching a single component.
        /// </summary>
        /// <remarks>
        /// Geometry produced while an override is active is never written to disk -- see the
        /// guard in <see cref="ShadowMesh2D.OnBeforeSerialize"/>. Without it, saving a scene or a
        /// prefab while overridden would bake the other generator's output into the asset and
        /// leave a large spurious override on every prefab instance.
        /// </remarks>
        internal static string globalGeneratorOverride
        {
            get => s_GlobalGeneratorOverride;
            set => s_GlobalGeneratorOverride = string.IsNullOrEmpty(value) ? null : value;
        }

        // Cached Shadow2DGeometrySettings, so per-caster and per-light queries skip the settings lookup
        // (UUM-154097). Cleared on SubsystemRegistration, by the geometryVersion setter, and in the
        // editor after each rendered context. Players cannot change the setting, so one read holds.
        static bool s_HasCachedSettings;
        static Shadow2DGeometrySettings.GeometryVersion s_CachedVersion;
        static string s_CachedFormat;

#if UNITY_EDITOR
        static bool s_SubscribedToEndContextRendering;
#endif

        /// <summary>Drops the cached settings so the next query reads them again.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void InvalidateCache()
        {
            s_HasCachedSettings = false;
        }

        static bool TryCacheSettings()
        {
            if (s_HasCachedSettings)
                return true;

            // A miss is not cached: URP may simply not be the active pipeline yet, and caching the
            // fallback would pin Legacy until something else invalidated.
            if (!GraphicsSettings.TryGetRenderPipelineSettings<Shadow2DGeometrySettings>(out var settings))
                return false;

            s_CachedVersion = settings.geometryVersion;
            s_CachedFormat = settings.activeFormat;
            s_HasCachedSettings = true;

#if UNITY_EDITOR
            // Subscribed lazily, on the first fill, so the refresh exists wherever the cache does. A
            // domain reload clears both this flag and the event, so they cannot drift apart.
            if (!s_SubscribedToEndContextRendering)
            {
                RenderPipelineManager.endContextRendering += OnEndContextRendering;
                s_SubscribedToEndContextRendering = true;
            }
#endif
            return true;
        }

#if UNITY_EDITOR
        // After rendering rather than before: ShadowCaster2D.Update and RenderShadows then read the same
        // value within a frame. Refreshing before rendering would let a frame draw casters still holding
        // one layout's geometry with the shader variant for the other.
        static void OnEndContextRendering(ScriptableRenderContext context, System.Collections.Generic.List<Camera> cameras)
        {
            InvalidateCache();
        }
#endif

        /// <summary>
        /// Which generation of shadow geometry this project builds, from
        /// <c>Shadow2DGeometrySettings</c>.
        /// </summary>
        /// <remarks>
        /// Falls back to Legacy when there are no URP graphics settings to read -- a project with no
        /// URP asset has no shadow rendering either, so the value only has to be defined, not right.
        /// Must be read from the main thread: reading graphics settings is forbidden from a
        /// serialization callback, which is why <c>ShadowMesh2D</c> defers its generator check rather
        /// than resolving one in <c>OnAfterDeserialize</c>.
        /// </remarks>
        internal static Shadow2DGeometrySettings.GeometryVersion geometryVersion =>
            TryCacheSettings() ? s_CachedVersion : Shadow2DGeometrySettings.GeometryVersion.Legacy;

        /// <summary>The generator every caster in this project builds with.</summary>
        internal static string projectFormat =>
            TryCacheSettings() ? s_CachedFormat : LegacyShadowGeometryGenerator.k_Id;

        /// <summary>
        /// Whether this project builds enhanced shadow geometry: the penumbra-carrying layout on
        /// every caster, the matching variant of the built-in shadow shader, ShaderGraph shadow
        /// authoring, and custom shadow materials on a Light2D.
        /// </summary>
        /// <remarks>
        /// Follows <see cref="ResolveId"/>, so a global override moves this with it; otherwise the shader
        /// variant and the caster geometry could disagree. Without an override it reads the cached version.
        /// </remarks>
        internal static bool enhancedGeometryEnabled =>
            s_GlobalGeneratorOverride != null
                ? string.Equals(s_GlobalGeneratorOverride, SoftShadowGeometryGenerator.k_Id, System.StringComparison.Ordinal)
                : geometryVersion >= Shadow2DGeometrySettings.GeometryVersion.Enhanced;

        /// <summary>Resolves the generator id a caster should actually build with.</summary>
        /// <remarks>
        /// The caster's own serialized id is no longer consulted: the format is a project decision, so
        /// a caster carrying a stale id from before the setting existed resolves to the project's
        /// format like every other. The parameter is kept so call sites read unchanged and so the
        /// serialized id stays available for diagnostics.
        /// </remarks>
        internal static string ResolveId(string serializedId)
        {
            return s_GlobalGeneratorOverride ?? projectFormat;
        }
    }
}
