using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.TestTools;

class StandardMaterialUpgraderTest
{
    StandardUpgrader m_StandardUpgrader;
    Material m_Material;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        m_StandardUpgrader = new StandardUpgrader("Standard"); // creating the upgrader once
    }

    //run after each test
    [SetUp]
    public void Setup()
    {
        var shader = Shader.Find("Standard");
        Assume.That(shader, Is.Not.Null, "Built-in Standard shader not found.");
        m_Material = new Material(shader);
    }

    //run after each test
    [TearDown]
    public void Teardown()
    {
        if (m_Material != null)
        {
            UnityEngine.Object.DestroyImmediate(m_Material);
            m_Material = null;
        }
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        m_StandardUpgrader = null;
    }

    public class MaterialUpgradeTestCase
    {
        public string name;
        public Action<Material> setup;
        public Action<Material> verify;
        public bool ignore;
        public String ignoreReason;
        public override string ToString() => name;

    }
    [Test][TestCaseSource(nameof(MaterialUpgradeCases))]
    public void UpgradeStandardMaterial(MaterialUpgradeTestCase testCase)
    {
        if (testCase.ignore)
        {
            Assert.Ignore(testCase.ignoreReason ?? "Test case ignored.");
        }

        // checks to avoid null indirect calls
        Assume.That(testCase.setup, Is.Not.Null, $"Test case '{testCase.name}' has a null setup Action.");
        Assume.That(testCase.verify, Is.Not.Null, $"Test case '{testCase.name}' has a null verify Action.");

        // Arrange
        var material = m_Material;
        testCase.setup(material);

        // Act (common)
        m_StandardUpgrader.Upgrade(material, MaterialUpgrader.UpgradeFlags.None);

        // Assert common
        Assert.AreEqual("Universal Render Pipeline/Lit", material.shader.name, $"Shader mismatch in case: {testCase.name}");

        // Assert specific
        testCase.verify(material);
    }

    private static IEnumerable MaterialUpgradeCases()
    {
        yield return new MaterialUpgradeTestCase
        {
            name = "Given_OpaqueBuiltInMaterial_When_Upgrading_Then_TheMaterialSurfaceOpaqueAndAlphaTestOff", // change the test name here
            setup = material =>
            {
                material.SetFloat("_Mode", 0.0f); // Opaque
            },
            verify = material =>
            {
                Assert.AreEqual(0.0f, material.GetFloat("_Surface")); // Surface type Opaque
                Assert.IsFalse(material.IsKeywordEnabled("_ALPHATEST_ON")); // No alpha test
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name = "Given_CutoutBuiltInMaterial_When_Upgrading_Then_TheMaterialURPSurfaceOpaqueAndAlphaTestOn",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // Cutout
            },
            verify = material =>
            {
                Assert.IsTrue(material.IsKeywordEnabled("_ALPHATEST_ON")); // Alpha clipping enabled
                Assert.AreEqual(0.0f, material.GetFloat("_Surface")); // Surface type Opaque
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_FadeBuiltInMaterial_When_Upgrading_Then_TheMaterialURPSurfaceTransparentAndAlphaBlend",
            setup = material =>
            {
                material.SetFloat("_Mode", 2.0f); // fade mode
            },
            verify = material =>
            {
                Assert.IsTrue(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")); // Surface type Transparent
                Assert.AreEqual((float)BaseShaderGUI.SurfaceType.Transparent, material.GetFloat("_Surface"));
                Assert.AreEqual((float)BaseShaderGUI.BlendMode.Alpha, material.GetFloat("_Blend")); // Blend mode Alpha
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_TransparentBuiltInMaterial_When_Upgrading_Then_TheMaterialURPSurfaceTransparentAndPremultiply",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); //transparent mode
            },
            verify = material =>
            {
                Assert.IsTrue(material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")); // Surface type Transparent
                Assert.AreEqual((float)BaseShaderGUI.SurfaceType.Transparent, material.GetFloat("_Surface"));
                Assert.AreEqual((float)BaseShaderGUI.BlendMode.Premultiply, material.GetFloat("_Blend")); // Blend mode Premultiply
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_GPUInstancingEnabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPGPUInstancingEnabled",
            setup =  material =>
            {
                material.enableInstancing = true; // enable GPU Instancing
            },
            verify = material =>
            {
                Assert.IsTrue(material.enableInstancing); // GPU Instancing enabled
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_GPUInstancingDisabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPGPUInstancingDisabled",
            setup =  material =>
            {
                material.enableInstancing = false; // disable GPU Instancing
            },
            verify = material =>
            {
                Assert.IsFalse(material.enableInstancing); // GPU Instancing disabled
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_EnvironmentReflectionsEnabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPEnvironmentReflectionsEnabled",
            setup = material =>
            {
                material.SetFloat("_GlossyReflections", 1.0f); // Enable environment reflections
            },
            verify = material =>
            {
                Assert.AreEqual(1.0f, material.GetFloat("_GlossyReflections")); // Environment reflections enabled
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_EnvironmentReflectionsDisabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPEnvironmentReflectionsDisabled",
            setup = material =>
            {
                material.SetFloat("_GlossyReflections", 0.0f); // Disable environment reflections
            },
            verify = material =>
            {
                Assert.AreEqual(0.0f, material.GetFloat("_GlossyReflections")); // Environment reflections disabled
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_SpecularHighlightsEnabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPSpecularHighlightsEnabled",
            setup = material =>
            {
                material.SetFloat("_SpecularHighlights", 1.0f); // Enable specular highlights
            },
            verify = material =>
            {
                Assert.AreEqual(1.0f, material.GetFloat("_SpecularHighlights")); // Specular highlights enabled
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_SpecularHighlightsDisabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPSpecularHighlightsDisabled",
            setup = material =>
            {
                material.SetFloat("_SpecularHighlights", 0.0f); // Disable specular highlights
            },
            verify = material =>
            {
                Assert.AreEqual(0.0f, material.GetFloat("_SpecularHighlights")); // Specular highlights disabled
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_RenderQueueSetoToBuiltInMaterial_When_Upgrading_Then_TheMaterialURPRenderQueuePreserved",
            ignore = true,
            ignoreReason = "Fails, needs investigation",
            setup = material =>
            {
                material.renderQueue = 2500; // queue
            },
            verify = material =>
            {
                Assert.AreEqual(2500, material.renderQueue); // Check that render queue is preserved after upgrade
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_TilingSetToBuiltInMaterial_When_Upgrading_Then_TheMaterialURPTilingPreserved",
            setup =  material =>
            {
                material.SetTextureScale("_MainTex", new Vector2(2.0f, 2.0f)); // Set tiling
            },
            verify = material =>
            {
                Assert.AreEqual(new Vector2(2.0f, 2.0f), material.GetTextureScale("_BaseMap")); // Check that tiling is preserved after upgrade
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_TilingSecondaryMapSetToBuiltInMaterial_When_Upgrading_Then_TheMaterialURPTilingSecondaryMapPreserved",
            ignore = true,
            ignoreReason = "Fails, needs investigation",
            setup = material =>
            {
                material.SetTextureScale("_MainTex", new Vector2(3.0f, 3.0f)); // Tiling
                material.SetTextureOffset("_MainTex", new Vector2(9.0f, 9.0f));
            },
            verify = material =>
            {
                Assert.AreEqual(new Vector2(3.0f, 3.0f), material.GetTextureOffset("_BaseMap")); // Check that tiling is preserved after upgrade
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_MetallicValueSetToBuiltInMaterial_When_Upgrading_Then_TheMaterialURPMetallicValuePreserved",
            setup = material =>
            {
                material.SetFloat("_Metallic", 1f); // Metallic
            },
            verify = material =>
            {
                Assert.AreEqual(1f, material.GetFloat("_Metallic")); // Check that metallic value is preserved after upgrade
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_SmoothnessValueSetToBuiltInMaterial_When_Upgrading_Then_TheMaterialURPSmoothnessValuePreserved",
            setup = material =>
            {
                material.SetFloat("_Glossiness", 0.0f); // Smoothness
            },
            verify = material =>
            {
                Assert.AreEqual(0.0f, material.GetFloat("_Glossiness"));
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_MetallicSourceAlphaBuiltInMaterial_When_Upgrading_Then_TheMaterialURPMetallicSourceAlphaPreserved",
            setup = material =>
            {
                material.SetFloat("_SmoothnessTextureChannel", 0.0f); // Metallic source alpha
            },
            verify = material =>
            {
                Assert.AreEqual(0.0f, material.GetFloat("_SmoothnessTextureChannel"));
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_AlbedoSourceAlphaBuiltInMaterial_When_Upgrading_Then_TheMaterialURPAlbedoSourceAlphaPreserved",
            setup = material =>
            {
                material.SetFloat("_SmoothnessTextureChannel", 1.0f);
            },
            verify = material =>
            {
                Assert.AreEqual(1.0f, material.GetFloat("_SmoothnessTextureChannel"));
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_EmissionEnabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPEmissionEnabled",
            ignore = true,
            ignoreReason = "Fails, needs investigation",
            setup = material =>
            {
                material.EnableKeyword("_EMISSION");
            },
            verify = material =>
            {
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_EmissionDisabledBuiltInMaterial_When_Upgrading_Then_TheMaterialURPEmissionDisabled",
            ignore = true,
            ignoreReason = "Fails, needs investigation",
            setup = material =>
            {
                material.DisableKeyword("_EMISSION");
            },
            verify = material =>
            {
                Assert.IsFalse(material.IsKeywordEnabled("_EMISSION"));
            }
        };
    }
}
