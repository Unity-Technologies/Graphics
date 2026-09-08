using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The shadow geometry generators available to this project, baked at editor time.
    /// </summary>
    /// <remarks>
    /// Discovery is reflective and editor-only (<c>Shadow2DGeometryDiscovery</c>); the result is
    /// persisted here through <c>[SerializeReference]</c>, which records each element's concrete
    /// type and reconstructs it on load. Nothing reflects at runtime.
    ///
    /// This lives in graphics settings rather than on Renderer2DData because shadow meshes are
    /// generated from <c>ShadowCaster2D.Update</c> and provider callbacks, outside any render
    /// pass -- there is no renderer in scope. A project can also have several Renderer2DData
    /// assets, and a caster must resolve its generator identically regardless of which renderer
    /// eventually draws it.
    /// </remarks>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "2D Shadow Geometry", Order = 1010)]
    class Shadow2DGeometrySettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        private Version m_Version;

        public int version => (int)m_Version;
        #endregion

        // The generator list has to reach the player: without it, only built-ins resolve and any
        // caster using a third-party generator silently falls back.
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        // Hidden because it is a cache, not a setting: Shadow2DGeometryDiscovery rewrites it from a
        // TypeCache sweep after every domain reload, so anything edited here is discarded on the next
        // one. It was the only thing in this category before Geometry Version existed, which is why
        // it went unnoticed; now that the category has a control a user is meant to change, an
        // adjacent list that silently resets would read as one too.
        [SerializeReference][HideInInspector]
        List<ShadowGeometryGenerator> m_Generators = new List<ShadowGeometryGenerator>();

        internal List<ShadowGeometryGenerator> generators => m_Generators;

        /// <summary>
        /// Which generation of 2D shadow geometry this project builds.
        /// </summary>
        /// <remarks>
        /// One number decides three things that must never disagree: which generator builds every
        /// caster's mesh, which vertex layout the built-in shadow shader is compiled for, and whether
        /// a Light2D may carry a custom shadow material at all. Keeping them on one axis is the point
        /// -- a mesh from one generation drawn by a shader from another produces a plausible wrong
        /// shadow rather than an error, because absent semantics read as zero and TANGENT means
        /// something different in each layout.
        ///
        /// Ordered, so "new enough for X" is a comparison rather than a set membership test, and a
        /// future generation is the next number rather than another branch everywhere.
        /// </remarks>
        internal enum GeometryVersion
        {
            /// <summary>
            /// The original shadow geometry and vertex layout. What every shader written before this
            /// setting existed reads.
            /// </summary>
            /// <remarks>
            /// Zero deliberately. A project that predates this setting has no value stored, which
            /// deserializes to zero, so it lands on the generation its content was authored against
            /// with no migration step to get wrong. Being wrong in the other direction is the quiet
            /// failure, so here the safe answer is also the free one.
            /// </remarks>
            [InspectorName("Legacy")]
            Legacy = 0,

            /// <summary>
            /// Shadow geometry that carries an explicit penumbra -- bands, fins, corner joins and
            /// concave fillets -- on the <c>Unity.SoftShadow</c> layout. Required for ShaderGraph
            /// shadow shaders and for custom shadow materials on a Light2D, both of which read that
            /// layout.
            /// </summary>
            /// <remarks>
            /// Named for the generation rather than for softness. Both generations honour
            /// <c>Light2D.shadowSoftness</c> -- <c>_SoftShadowAngle</c> is consumed by the Legacy
            /// projection too -- so calling this one "soft" would claim as new something the older
            /// generation already does.
            ///
            /// The generator behind it is still spelled <c>Unity.SoftShadow</c>. That is a name for
            /// the algorithm, which does build a penumbra, rather than for what the user is choosing
            /// between; the two are free to differ because <see cref="activeFormat"/> is the only
            /// thing that maps one to the other.
            /// </remarks>
            [InspectorName("Enhanced")]
            Enhanced = 1,
        }

        [SerializeField]
        [Tooltip("Which generation of 2D shadow geometry this project builds.\n\n" +
                 "Enhanced builds an explicit penumbra and is required for ShaderGraph shadow " +
                 "shaders and for custom shadow Materials on a Light 2D. Changing this rebuilds " +
                 "every Shadow Caster 2D's geometry and can change how existing shadows look.")]
        GeometryVersion m_GeometryVersion = GeometryVersion.Legacy;

        internal GeometryVersion geometryVersion
        {
            get => m_GeometryVersion;
            set => m_GeometryVersion = value;
        }

        /// <summary>The generator id <see cref="geometryVersion"/> selects.</summary>
        /// <remarks>
        /// The mapping lives here rather than on the enum so a generation is free to change which
        /// generator backs it -- the version is the contract, the id is an implementation detail.
        /// An unrecognised value maps to Legacy rather than throwing: a project saved by a newer URP
        /// and opened in an older one is a downgrade, and rendering the older generation is a better
        /// answer than not rendering.
        /// </remarks>
        internal string activeFormat => m_GeometryVersion >= GeometryVersion.Enhanced
            ? SoftShadowGeometryGenerator.k_Id
            : LegacyShadowGeometryGenerator.k_Id;

        /// <summary>
        /// Whether enhanced shadow geometry -- and with it ShaderGraph shadow authoring and custom
        /// shadow materials on a Light2D -- is available.
        /// </summary>
        /// <remarks>
        /// The user-facing question is which generation the project is on, not which vertex layout;
        /// the layout is the consequence. Callers should read this rather than comparing generator
        /// ids or the version directly, so both stay implementation details.
        /// </remarks>
        internal bool enhancedGeometryEnabled => m_GeometryVersion >= GeometryVersion.Enhanced;
    }
}
