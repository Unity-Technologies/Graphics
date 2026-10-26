using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The <c>Shadow2DGenerators</c> SubShader tag: which shadow geometry generators a shadow
    /// shader's vertex programs are able to read.
    /// </summary>
    /// <remarks>
    /// A generator's <see cref="ShadowGeometryGenerator.id"/> implies its vertex layout -- changing
    /// the layout requires a new id rather than a version bump -- so naming ids is enough for a
    /// shader to declare which meshes it understands.
    ///
    /// The mismatch this exists to prevent is not loud. A shader reading a semantic the mesh does
    /// not carry gets zero, and <c>TANGENT</c> means something entirely different in each of the
    /// two layouts URP ships: <c>Unity.Legacy</c> reads <c>tangent.x</c> as a projection type and
    /// <c>tangent.zw</c> as an edge endpoint, while <c>Unity.SoftShadow</c> puts a role index and a
    /// fan parameter there. Pairing the wrong two therefore produces a plausible-looking wrong
    /// shadow rather than an obviously broken one.
    ///
    /// The value is a space-separated list, so one shader can support several generators, which is
    /// what lets <c>Hidden/Shadow2D</c> declare both and pick between them with a keyword. It was
    /// list-capable from the start deliberately: widening the format later would have needed a
    /// tag-format migration on every shipped shader.
    ///
    /// Which layout a project actually builds is <c>Shadow2DGeometrySettings.geometryVersion</c>, so the
    /// tag no longer selects anything at runtime -- it records what a shader can read, and exists to
    /// pin that against the C# copy in <see cref="builtInShadowShaderIds"/> and to keep a
    /// hand-written shader honest about which format it was authored for.
    ///
    /// Read with <see cref="Material.GetTag(string, bool, string)"/>, which sees only the
    /// <b>active</b> SubShader. ShaderGraph emits several, so a SubTarget adding this tag has to add
    /// it to every <c>SubShaderDescriptor</c>.
    /// </remarks>
    internal static class Shadow2DGeneratorTag
    {
        /// <summary>The SubShader tag name.</summary>
        internal const string k_TagName = "Shadow2DGenerators";

        // Missing tag => Unity.Legacy alone. Chosen so that every shader written before the tag
        // existed keeps meaning exactly what it meant: those all read ShadowProjectVertex.hlsl's
        // POSITION / TANGENT / TEXCOORD0, which is the Legacy layout. A default of "everything"
        // would instead have silently declared them compatible with layouts they cannot read.
        static readonly string[] s_DefaultIds = { LegacyShadowGeometryGenerator.k_Id };

        // ShaderLab collapses runs of whitespace inside a tag value inconsistently enough that
        // splitting on a single space is not safe; tabs and newlines survive a multi-line value.
        static readonly char[] s_Separators = { ' ', '\t', '\r', '\n' };

        // What Hidden/Shadow2D declares. A caster with no material is drawn by that shader, so this
        // is the set the project's format has to be in -- and it is both formats, because that
        // shader is the fallback for every caster and the project may build either layout. It picks
        // between them with SHADOW2D_SOFT_GEOMETRY, which ShadowRendering bakes onto the cached
        // material from Shadow2DGeometrySettings.geometryVersion.
        //
        // Duplicated from the shader rather than read from it because the built-in shadow shader
        // lives on Renderer2DResources, which is renderer-scoped and deliberately out of reach here
        // -- a caster must resolve its generator identically no matter which renderer draws it, the
        // same reason Shadow2DGeometrySettings does not live on Renderer2DData either.
        //
        // Shadow2DGeneratorTagTests.TheBuiltInShadowShaderDeclaresItsGeometryGenerators asserts the
        // shader's authored tag against this array, so the two cannot drift apart silently. Order
        // matches the tag's.
        static readonly string[] s_BuiltInShadowShaderIds =
        {
            SoftShadowGeometryGenerator.k_Id,
            LegacyShadowGeometryGenerator.k_Id,
        };

        /// <summary>
        /// The generators <c>Hidden/Shadow2D</c> can read, i.e. the ones valid on a caster with no
        /// custom material.
        /// </summary>
        internal static string[] builtInShadowShaderIds => s_BuiltInShadowShaderIds;

        /// <summary>
        /// The generator ids <paramref name="material"/>'s active SubShader declares. Never empty
        /// and never null: a material with no tag declares <see cref="LegacyShadowGeometryGenerator.k_Id"/>.
        /// </summary>
        /// <remarks>
        /// Allocates -- <c>Material.GetTag</c> builds a string and the split builds an array -- so a
        /// caller on a per-frame path would have to cache the result and re-parse only when the
        /// material or its shader changes. Nothing on the draw path calls this: which layout a
        /// project builds is a setting, so there is no per-draw compatibility question left to ask.
        /// </remarks>
        internal static string[] Parse(Material material)
        {
            if (material == null)
                return s_DefaultIds;

            // searchFallbacks: false -- a fallback shader's idea of the vertex layout says nothing
            // about this one's, and the passes URP draws come from this shader.
            return Parse(material.GetTag(k_TagName, false, string.Empty));
        }

        /// <summary>Parses a raw tag value. Exposed for tests and for shader-level callers.</summary>
        internal static string[] Parse(string tagValue)
        {
            if (string.IsNullOrWhiteSpace(tagValue))
                return s_DefaultIds;

            var ids = tagValue.Split(s_Separators, StringSplitOptions.RemoveEmptyEntries);
            return ids.Length > 0 ? ids : s_DefaultIds;
        }

        /// <summary>Whether the tag was authored at all, as opposed to defaulted.</summary>
        /// <remarks>
        /// Only for diagnostics that want to say "declares nothing" differently from "declares
        /// Legacy". Compatibility itself must never branch on this: an absent tag has a defined
        /// meaning, and treating it as unknown would make every pre-tag shader a special case.
        /// </remarks>
        internal static bool HasTag(Material material)
        {
            return material != null && !string.IsNullOrWhiteSpace(material.GetTag(k_TagName, false, string.Empty));
        }

        /// <summary>Whether <paramref name="declaredIds"/> names <paramref name="generatorId"/>.</summary>
        internal static bool Declares(string[] declaredIds, string generatorId)
        {
            if (declaredIds == null || string.IsNullOrEmpty(generatorId))
                return false;

            for (int i = 0; i < declaredIds.Length; i++)
            {
                // Ordinal: ids are identifiers, not display text, and the registry keys on them
                // ordinally too. A case-insensitive match here would accept a caster whose id the
                // registry then fails to resolve.
                if (string.Equals(declaredIds[i], generatorId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
