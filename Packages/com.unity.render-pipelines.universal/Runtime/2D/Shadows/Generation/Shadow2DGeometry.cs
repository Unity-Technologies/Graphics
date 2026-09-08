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
        internal static Shadow2DGeometrySettings.GeometryVersion geometryVersion
        {
            get
            {
                if (GraphicsSettings.TryGetRenderPipelineSettings<Shadow2DGeometrySettings>(out var settings))
                    return settings.geometryVersion;

                return Shadow2DGeometrySettings.GeometryVersion.Legacy;
            }
        }

        /// <summary>The generator every caster in this project builds with.</summary>
        internal static string projectFormat
        {
            get
            {
                if (GraphicsSettings.TryGetRenderPipelineSettings<Shadow2DGeometrySettings>(out var settings))
                    return settings.activeFormat;

                return LegacyShadowGeometryGenerator.k_Id;
            }
        }

        /// <summary>
        /// Whether this project builds enhanced shadow geometry: the penumbra-carrying layout on
        /// every caster, the matching variant of the built-in shadow shader, ShaderGraph shadow
        /// authoring, and custom shadow materials on a Light2D.
        /// </summary>
        /// <remarks>
        /// Resolved through <see cref="ResolveId"/> rather than read straight off the version, so a
        /// global override moves this with it. It has to: answering "no" while the override builds
        /// the enhanced layout would compile the shader for one layout and hand it the other.
        /// </remarks>
        internal static bool enhancedGeometryEnabled =>
            string.Equals(ResolveId(null), SoftShadowGeometryGenerator.k_Id, System.StringComparison.Ordinal);

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
