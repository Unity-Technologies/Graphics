using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.TestTools;
[Category("Graphics Tools")]

class AutodeskInteractiveCutoutMaterialUpgraderTest : MaterialUpgraderTestBase<AutodeskInteractiveUpgrader>
{
    [OneTimeSetUp]
    public override void OneTimeSetUp()
    {
        m_Upgrader = new AutodeskInteractiveUpgrader("Autodesk Interactive");
    }

    public AutodeskInteractiveCutoutMaterialUpgraderTest() : base("Autodesk Interactive",
        "Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveMasked")
    {
    }

    [Test]
    [TestCaseSource(nameof(MaterialUpgradeCases))]
    public void UpgradeAutodeskInteractiveMaterial(MaterialUpgradeTestCase testCase)
    {
        base.UpgradeMaterial(testCase);
    }

    private static IEnumerable MaterialUpgradeCases()
    {
        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAutodeskInteractiveMaterial_When_Upgrading_Then_TheShaderUpgradedToAutodeskInteractiveMasked",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
            },
            verify = material =>
            {
                Assert.AreEqual("Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveMasked", material.shader.name);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAlbedoColorRedAutodeskInteractiveMaterial_When_Upgrading_Then_TheAlbedoColorRemainsRed",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
                material.SetColor("_Color", Color.red);
            },
            verify = material =>
            {
                Assert.AreEqual(Color.red, material.GetColor("_Color"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAutodeskInteractiveMaterialAlphaClipValue_When_Upgrading_Then_TheAlphaClipValueRemainsTheSame",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
                material.SetFloat("_Cutoff", 0.3f);
            },
            verify = material =>
            {
                Assert.AreEqual(0.3f, material.GetFloat("_OpacityThreshold"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAutodeskInteractiveMaterialMetallicValue_When_Upgrading_Then_TheMetallicValueRemainsTheSame",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
                material.SetFloat("_Metallic", 0.2f);
            },
            verify = material =>
            {
                Assert.AreEqual(0.2f, material.GetFloat("_Metallic"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAutodeskInteractiveMaterialRoughnessValue_When_Upgrading_Then_TheRoughnessValueRemainsTheSame",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
                material.SetFloat("_Glossiness", 0.3f);
            },
            verify = material =>
            {
                Assert.AreEqual(0.3f, material.GetFloat("_Glossiness"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAutodeskInteractiveMaterialEmissionColorGreen_When_Upgrading_Then_TheEmissionColorRemainsGreen",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
                material.SetColor("_EmissionColor", Color.green);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            },
            verify = material =>
            {
                Assert.AreEqual(Color.green, material.GetColor("_EmissionColor"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAutodeskInteractiveMaterialEnableGPUInstancingEnabled_When_Upgrading_Then_TheGPUInstancingRemainsEnabled",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
                material.enableInstancing = true;
            },
            verify = material =>
            {
                Assert.IsTrue(material.enableInstancing);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAutodeskInteractiveMaterialEnableGPUInstancingDisabled_When_Upgrading_Then_TheGPUInstancingRemainsDisabled",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // cutout
                material.enableInstancing = false;
            },
            verify = material =>
            {
                Assert.IsFalse(material.enableInstancing);
            }
        };
    }
}
