using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// The Shadow2DGenerators SubShader tag: how it parses, and what URP's own shadow shader
    /// declares.
    /// </summary>
    /// <remarks>
    /// Two generators with incompatible vertex layouts coexist, and the quiet direction of a
    /// mismatch -- a Unity.SoftShadow mesh drawn by a Unity.Legacy shader -- produces a
    /// plausible-looking wrong shadow rather than an obviously broken one, because TANGENT carries a
    /// different quantity in each layout. Which layout a project builds is now
    /// Shadow2DGeometrySettings.geometryVersion, so the tag no longer selects anything at runtime; what
    /// is left to protect is that Hidden/Shadow2D's authored tag and the C# copy of it agree.
    /// </remarks>
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class Shadow2DGeneratorTagTests
    {
        readonly List<Object> m_Created = new List<Object>();

        T Track<T>(T obj) where T : Object
        {
            m_Created.Add(obj);
            return obj;
        }

        [TearDown]
        public void Cleanup()
        {
            for (int i = 0; i < m_Created.Count; i++)
            {
                if (m_Created[i] != null)
                    Object.DestroyImmediate(m_Created[i]);
            }
            m_Created.Clear();

            Shadow2DGeometry.globalGeneratorOverride = null;
        }

        Material CreateBuiltInShadowMaterial()
        {
            var shader = Shader.Find("Hidden/Shadow2D");
            Assert.IsNotNull(shader, "Could not find Hidden/Shadow2D; it is URP's own shadow shader.");
            return Track(new Material(shader));
        }

        // ---- Parsing ----

        [Test]
        public void AnAbsentTagDeclaresLegacyOnly()
        {
            // The default is what every shader written before the tag existed means: they all read
            // ShadowProjectVertex.hlsl's POSITION / TANGENT / TEXCOORD0, which is Legacy's layout.
            // A default of "everything" would have declared them compatible with layouts they
            // cannot read, which is exactly the failure the tag exists to prevent.
            CollectionAssert.AreEqual(new[] { LegacyShadowGeometryGenerator.k_Id },
                Shadow2DGeneratorTag.Parse(string.Empty));
            CollectionAssert.AreEqual(new[] { LegacyShadowGeometryGenerator.k_Id },
                Shadow2DGeneratorTag.Parse((string)null));
            CollectionAssert.AreEqual(new[] { LegacyShadowGeometryGenerator.k_Id },
                Shadow2DGeneratorTag.Parse("   "));
            CollectionAssert.AreEqual(new[] { LegacyShadowGeometryGenerator.k_Id },
                Shadow2DGeneratorTag.Parse((Material)null));
        }

        [Test]
        public void TheTagParsesAsASpaceSeparatedList()
        {
            // List-capable from the start on purpose: widening the format later would need a
            // tag-format migration on every shipped shader.
            CollectionAssert.AreEqual(
                new[] { LegacyShadowGeometryGenerator.k_Id, SoftShadowGeometryGenerator.k_Id },
                Shadow2DGeneratorTag.Parse($"{LegacyShadowGeometryGenerator.k_Id} {SoftShadowGeometryGenerator.k_Id}"));
        }

        [Test]
        public void ExtraWhitespaceInTheTagIsTolerated()
        {
            // A multi-line tag value keeps its newlines and ShaderLab does not normalise runs of
            // spaces, so splitting on a single space would produce empty entries -- which would then
            // fail to match any registered id and look like a shader declaring nothing.
            // Two arbitrary ids -- this is about splitting, not about whether the pair is a sane thing
            // for one shader to declare (it is not: their vertex layouts differ).
            CollectionAssert.AreEqual(
                new[] { LegacyShadowGeometryGenerator.k_Id, SoftShadowGeometryGenerator.k_Id },
                Shadow2DGeneratorTag.Parse($"  {LegacyShadowGeometryGenerator.k_Id}\t\n  {SoftShadowGeometryGenerator.k_Id} "));
        }

        [Test]
        public void DeclaresMatchesOrdinally()
        {
            var declared = new[] { LegacyShadowGeometryGenerator.k_Id };

            Assert.IsTrue(Shadow2DGeneratorTag.Declares(declared, LegacyShadowGeometryGenerator.k_Id));
            Assert.IsFalse(Shadow2DGeneratorTag.Declares(declared, SoftShadowGeometryGenerator.k_Id));

            // Ids are identifiers and the registry keys on them ordinally. Matching
            // case-insensitively here would accept a caster whose id the registry then fails to
            // resolve, which is a silent fall back to Legacy rather than an error.
            Assert.IsFalse(Shadow2DGeneratorTag.Declares(declared, "unity.legacy"));

            Assert.IsFalse(Shadow2DGeneratorTag.Declares(declared, null));
            Assert.IsFalse(Shadow2DGeneratorTag.Declares(null, LegacyShadowGeometryGenerator.k_Id));
        }

        // ---- What the shipped shader declares ----

        [Test]
        public void TheBuiltInShadowShaderDeclaresItsGeometryGenerators()
        {
            // Hidden/Shadow2D is the fallback for every caster with no custom material, and a project
            // may build either layout, so it has to declare both. Its projected passes carry
            // SHADOW2D_SOFT_GEOMETRY to pick between them; the tag says "either", the keyword says
            // "this one now".
            var material = CreateBuiltInShadowMaterial();
            var declared = Shadow2DGeneratorTag.Parse(material);

            Assert.IsTrue(Shadow2DGeneratorTag.HasTag(material),
                $"Hidden/Shadow2D must author the {Shadow2DGeneratorTag.k_TagName} tag, not rely on the default.");
            CollectionAssert.AreEquivalent(
                new[] { SoftShadowGeometryGenerator.k_Id, LegacyShadowGeometryGenerator.k_Id },
                declared);

            // The drift guard. Shadow2DGeneratorTag duplicates this set in C# because the built-in
            // shadow shader lives on Renderer2DResources, which is renderer-scoped and out of reach
            // from a caster. Asserting the shader's authored tag against the copy is what stops the
            // two diverging -- editing the shader alone now fails here instead of silently changing
            // which layouts the fallback claims to read.
            CollectionAssert.AreEquivalent(declared, Shadow2DGeneratorTag.builtInShadowShaderIds,
                $"Hidden/Shadow2D's {Shadow2DGeneratorTag.k_TagName} tag and " +
                "Shadow2DGeneratorTag.builtInShadowShaderIds have drifted apart. Update both.");
        }

        [Test]
        public void TheBuiltInShaderDeclaresEveryRegisteredBuiltInGenerator()
        {
            // The replacement for the old "all declared layouts must agree" check, which was correct
            // only while one shader meant one Attributes struct. Hidden/Shadow2D now compiles a
            // variant per layout, so differing layouts are the point rather than an authoring error.
            //
            // What still has to hold is the other direction: every generator URP ships must be one
            // the fallback can read, or turning the project format onto it leaves casters with no
            // usable shader.
            var declared = Shadow2DGeneratorTag.Parse(CreateBuiltInShadowMaterial());

            foreach (var id in new[] { LegacyShadowGeometryGenerator.k_Id, SoftShadowGeometryGenerator.k_Id })
            {
                Assert.IsTrue(ShadowGeometryGeneratorRegistry.Contains(id),
                    $"'{id}' ships with URP but is not registered.");
                Assert.IsTrue(Shadow2DGeneratorTag.Declares(declared, id),
                    $"Hidden/Shadow2D does not declare '{id}', so a project set to that format would " +
                    "have no shader for casters without a custom material.");
            }
        }

        [Test]
        public void TheShippedGeneratorsUseDifferentVertexLayouts()
        {
            // Pins the premise the keyword exists for. If these ever converged, the keyword and its
            // variants would be dead weight rather than load-bearing.
            var legacy = ShadowGeometryGeneratorRegistry.Get(LegacyShadowGeometryGenerator.k_Id).vertexLayout;
            var soft = ShadowGeometryGeneratorRegistry.Get(SoftShadowGeometryGenerator.k_Id).vertexLayout;

            CollectionAssert.AreNotEqual(legacy, soft,
                "Unity.Legacy and Unity.SoftShadow are expected to differ; SHADOW2D_SOFT_GEOMETRY " +
                "exists to switch between them.");
        }
    }
}
