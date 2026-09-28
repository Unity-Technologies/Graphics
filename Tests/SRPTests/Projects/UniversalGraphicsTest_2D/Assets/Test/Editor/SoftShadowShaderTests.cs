using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.Universal.Tests
{
    /// <summary>
    /// That the soft shadow test shader compiles, and that a Material using it is a valid
    /// <c>ShadowCaster2D.material</c> override.
    /// </summary>
    /// <remarks>
    /// A ShaderLab pass exists whether or not its HLSL compiled, so <c>FindPass</c> succeeding proves
    /// nothing about the program. <c>ShaderUtil.ShaderHasError</c> is what actually catches a broken
    /// include, and without a test like this the first sign of trouble is an unhelpfully pink shadow.
    /// </remarks>
    [TestFixture]
    class SoftShadowShaderTests
    {
        const string k_ShaderName = "Test/Shadow2D-SoftShadow";
        const string k_MaterialPath =
            "Assets/Scenes/140_Shadow_Materials/TestShadow2D-SoftShadow.mat";

        Shader m_Shader;

        [SetUp]
        public void Setup()
        {
            m_Shader = Shader.Find(k_ShaderName);
            Assert.IsNotNull(m_Shader, $"Could not find '{k_ShaderName}'.");
        }

        const string k_FixtureDir = "Assets/Scenes/140_Shadow_Materials";

        static void AssertCompiles(Shader shader, string label)
        {
            if (!ShaderUtil.ShaderHasError(shader))
                return;

            // Surface the actual messages: "shader has an error" on its own is useless for fixing it.
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{label} has compile errors:");
            foreach (var m in ShaderUtil.GetShaderMessages(shader))
            {
                if (m.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)
                    continue;
                sb.AppendLine($"  {m.file}({m.line}): {m.message}");
                if (!string.IsNullOrEmpty(m.messageDetails))
                    sb.AppendLine($"      {m.messageDetails.Trim()}");
            }
            Assert.Fail(sb.ToString());
        }

        [Test]
        public void ShaderCompilesWithoutErrors()
        {
            AssertCompiles(m_Shader, k_ShaderName);
        }

        [Test]
        public void EveryFixtureShaderInThisFolderCompiles()
        {
            // Deliberately every shader in the folder, not just this feature's.
            //
            // TestShadow2D-Complete.shader shipped broken -- it included ShadowProjectVertex.hlsl
            // without Core.hlsl, so GetWorldToHClipMatrix was undeclared -- and nothing caught it,
            // because the fixture's tests check pass NAMES and the compatibility rule, and a ShaderLab
            // pass exists whether or not its HLSL compiled. Compiling them is the only thing that
            // notices, and it costs nothing to do for the whole folder.
            string[] guids = AssetDatabase.FindAssets("t:Shader", new[] { k_FixtureDir });

            // At least the three fixtures: Complete, Incomplete and SoftShadow. Asserted so the test
            // cannot quietly shrink to checking only its own shader -- Complete is the one that was
            // broken, and it is the whole reason this exists.
            Assert.GreaterOrEqual(guids.Length, 3,
                $"Expected at least 3 shaders under {k_FixtureDir}, found {guids.Length}.");

            var compiled = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                Assert.IsNotNull(shader, $"Could not load a Shader at {path}.");
                AssertCompiles(shader, path);
                compiled.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }

            CollectionAssert.Contains(compiled, "TestShadow2D-Complete",
                "TestShadow2D-Complete was not among the shaders compiled; it is the fixture that " +
                "shipped broken and the reason this test exists.");
            Debug.Log($"[soft shadow] compiled {compiled.Count} fixture shaders: " +
                      string.Join(", ", compiled));
        }

        [Test]
        public void ShaderDeclaresEveryShadowPassRole()
        {
            // All five are required regardless of the caster's ShadowCastingOptions, because that
            // option is mutable at runtime -- a material validated against the current one would
            // break silently when it changed, with no inspector in a player to say so.
            var material = new Material(m_Shader);
            try
            {
                ShadowRendering.ShadowPassRoles missing =
                    ShadowRendering.GetMissingShadowPasses(material);
                Assert.AreEqual(ShadowRendering.ShadowPassRoles.None, missing,
                    $"Material is missing shadow pass roles: {missing}");
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void EveryPassRoleResolvesToItsOwnDistinctIndex()
        {
            // A duplicated Name in the shader would collapse two roles onto one index, which draws
            // the wrong pass for a phase rather than skipping it.
            // Uses the k_*PassName constants rather than string literals, so a rename in the shader
            // that is not mirrored in ShadowRendering fails here instead of silently resolving to -1
            // and stopping a phase from drawing.
            string[] names =
            {
                ShadowRendering.k_SelfPassName,
                ShadowRendering.k_UnshadowMarkPassName,
                ShadowRendering.k_UnshadowUnmarkPassName,
                ShadowRendering.k_ProjectedSelfPassName,
                ShadowRendering.k_ProjectedUnshadowPassName,
            };
            Assert.AreEqual((int)ShadowRendering.ShadowPassRole.Count, names.Length,
                "A pass role was added; extend this test alongside it.");

            var material = new Material(m_Shader);
            try
            {
                var indices = new System.Collections.Generic.HashSet<int>();
                foreach (string name in names)
                {
                    int pass = material.FindPass(name);
                    Assert.GreaterOrEqual(pass, 0, $"Pass '{name}' did not resolve.");
                    Assert.IsTrue(indices.Add(pass),
                        $"Pass '{name}' shares pass index {pass} with another role.");
                }
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void ShippedMaterialAssetIsAValidOverride()
        {
            // The asset, not a freshly constructed Material: this is what the fixture scene and the
            // filtered picker actually use, so a wrong shader GUID in the .mat would show up here.
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_MaterialPath);
            Assert.IsNotNull(material, $"Could not load {k_MaterialPath}.");
            Assert.AreEqual(m_Shader, material.shader,
                "The material asset does not point at the soft shadow shader.");

            ShadowRendering.ShadowPassRoles missing =
                ShadowRendering.GetMissingShadowPasses(material);
            Assert.AreEqual(ShadowRendering.ShadowPassRoles.None, missing,
                $"Material asset is missing shadow pass roles: {missing}");
        }

        [Test]
        public void ShippedMaterialCarriesTheSoftShadowProperties()
        {
            // LENGTH and R live on the material rather than per light, so the fixture is only
            // meaningful if the asset actually has them at sane values. A shortened shadow is the
            // visible proof the override is driving the draw.
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_MaterialPath);
            Assert.IsNotNull(material);

            Assert.IsTrue(material.HasProperty("_ShadowLength"), "_ShadowLength missing.");
            Assert.IsTrue(material.HasProperty("_FilletRadiusScale"), "_FilletRadiusScale missing.");
            Assert.Greater(material.GetFloat("_ShadowLength"), 0.0f,
                "A shadow length of 0 would collapse the whole projection.");
            Assert.GreaterOrEqual(material.GetFloat("_FilletRadiusScale"), 0.0f);
        }
    }
}
