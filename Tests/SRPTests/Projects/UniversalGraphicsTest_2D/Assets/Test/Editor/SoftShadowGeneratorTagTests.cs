using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// The Shadow2DGenerators tag against the real fixture shaders.
    /// </summary>
    /// <remarks>
    /// The package's own Shadow2DGeneratorTagTests cover parsing and what Hidden/Shadow2D declares;
    /// this sweeps the fixture shaders in this project, which is where a hand-written shadow shader
    /// authoring the tag by hand actually exists.
    ///
    /// The mismatch worth catching is quiet. A Unity.SoftShadow mesh drawn by a Legacy shader reads
    /// TANGENT.x as a projection type when it actually holds a role index, so it produces a
    /// wrong-looking shadow rather than a missing one, and nothing in the Scene view says so. Which
    /// layout a project builds is Shadow2DGeometrySettings.geometryVersion, so the tag no longer selects
    /// anything at runtime -- what it still has to do is describe each shader honestly.
    /// </remarks>
    [TestFixture]
    [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
    class SoftShadowGeneratorTagTests
    {
        const string k_FixtureDir = "Assets/Scenes/140_Shadow_Materials";
        const string k_SoftShadowMaterialPath = k_FixtureDir + "/TestShadow2D-SoftShadow.mat";

        readonly List<Object> m_Created = new List<Object>();

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

        static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsNotNull(material, $"Could not load {path}.");
            return material;
        }

        [Test]
        public void TheSoftShadowShaderDeclaresOnlyTheSoftShadowGenerator()
        {
            // Only. Listing Unity.Legacy as well would be the silent mismatch itself: the projected
            // passes read TEXCOORD1 and TEXCOORD2, which a Legacy mesh does not carry, so next,
            // prev2, next2 and windingSign would all arrive as zero -- and windingSign == 0
            // degenerates every facing test and the concavity test.
            var material = LoadMaterial(k_SoftShadowMaterialPath);

            Assert.IsTrue(Shadow2DGeneratorTag.HasTag(material),
                $"The soft shadow shader must author the {Shadow2DGeneratorTag.k_TagName} tag; the " +
                "default is Unity.Legacy, which is the one layout it cannot read.");
            CollectionAssert.AreEqual(new[] { SoftShadowGeometryGenerator.k_Id },
                Shadow2DGeneratorTag.Parse(material));
        }

        [Test]
        public void EveryFixtureShaderInThisFolderDeclaresTheTag()
        {
            // Swept rather than listed for the same reason EveryFixtureShaderInThisFolderCompiles is:
            // a fixture added later without a tag would silently default to Unity.Legacy, and if it
            // is a soft shadow fixture that default is wrong in the quiet direction.
            string[] guids = AssetDatabase.FindAssets("t:Shader", new[] { k_FixtureDir });
            Assert.GreaterOrEqual(guids.Length, 3,
                $"Expected at least 3 shaders under {k_FixtureDir}, found {guids.Length}.");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Assert.IsNotNull(shader, $"Could not load a Shader at {path}.");

                var probe = new Material(shader);
                try
                {
                    Assert.IsTrue(Shadow2DGeneratorTag.HasTag(probe),
                        $"{path} does not author the {Shadow2DGeneratorTag.k_TagName} tag.");

                    var declared = Shadow2DGeneratorTag.Parse(probe);

                    // One shader has one Attributes struct, so declaring two generators whose vertex
                    // layouts differ guarantees one of them is read wrong. The cross-check is only
                    // possible now that two incompatible layouts exist.
                    VertexAttributeDescriptor[] reference = null;
                    string referenceId = null;
                    bool anyRegistered = false;
                    for (int i = 0; i < declared.Length; i++)
                    {
                        if (!ShadowGeometryGeneratorRegistry.Contains(declared[i]))
                            continue;

                        anyRegistered = true;
                        var layout = ShadowGeometryGeneratorRegistry.Get(declared[i]).vertexLayout;
                        if (reference == null)
                        {
                            reference = layout;
                            referenceId = declared[i];
                            continue;
                        }

                        CollectionAssert.AreEqual(reference, layout,
                            $"{path} declares both '{referenceId}' and '{declared[i]}', whose vertex " +
                            "layouts differ; one of them is guaranteed to be read wrong.");
                    }

                    Assert.IsTrue(anyRegistered,
                        $"{path} declares no generator this project has registered: {string.Join(", ", declared)}.");
                }
                finally
                {
                    Object.DestroyImmediate(probe);
                }
            }
        }

        [Test]
        public void TheSoftShadowFixtureMeshMatchesTheGeneratorsVertexLayout()
        {
            // What the switch tests used to prove, reduced to the invariant that survives the format
            // becoming a project setting: geometry built as Unity.SoftShadow must actually be in
            // Unity.SoftShadow's layout.
            //
            // A matching id with a stale layout is the one failure with no visible signature -- both
            // layouts are self-consistent, so a shader reads garbage out of valid-looking data and
            // draws a plausible wrong shadow.
            Shadow2DGeometry.globalGeneratorOverride = SoftShadowGeometryGenerator.k_Id;

            var go = new GameObject("ShadowCaster2D generator tag test");
            m_Created.Add(go);
            var caster = go.AddComponent<ShadowCaster2D>();
            caster.EnsureMeshInitialized();
            caster.Update();

            Assert.AreEqual(SoftShadowGeometryGenerator.k_Id, caster.m_ShadowMesh.generatedGeneratorId,
                "The caster did not build with the overridden generator.");
            CollectionAssert.AreEqual(
                ShadowGeometryGeneratorRegistry.Get(SoftShadowGeometryGenerator.k_Id).vertexLayout,
                caster.mesh.GetVertexAttributes(),
                "Mesh vertex layout is not Unity.SoftShadow's 14-float payload.");
        }
    }
}
