using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.TestTools;
[Category("Graphics Tools")]

class AutodeskInteractiveTransparentMaterialUpgraderTest : MaterialUpgraderTestBase<AutodeskInteractiveUpgrader>
{
    [OneTimeSetUp]
    public override void OneTimeSetUp()
    {
        m_Upgrader = new AutodeskInteractiveUpgrader("Autodesk Interactive");
    }

    public AutodeskInteractiveTransparentMaterialUpgraderTest() : base("Autodesk Interactive",
        "Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveTransparent")
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
                "Given_TransparentAutodeskInteractiveMaterial_When_Upgrading_Then_TheShaderUpgradedToAutodeskInteractiveMasked",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); // transparent
            },
            verify = material =>
            {
                Assert.AreEqual("Universal Render Pipeline/Autodesk Interactive/AutodeskInteractiveTransparent", material.shader.name);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentAlbedoColorRedAutodeskInteractiveMaterial_When_Upgrading_Then_TheAlbedoColorRemainsRed",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); // transparent
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
                "Given_TransparentAutodeskInteractiveMaterialMetallicValue_When_Upgrading_Then_TheMetallicValueRemainsTheSame",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); // transparent
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
                "Given_TransparentAutodeskInteractiveMaterialRoughnessValue_When_Upgrading_Then_TheRoughnessValueRemainsTheSame",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); // transparent
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
                "Given_TransparentAutodeskInteractiveMaterialEmissionColorGreen_When_Upgrading_Then_TheEmissionColorRemainsGreen",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); // transparent
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
                "Given_TransparentAutodeskInteractiveMaterialEnableGPUInstancingEnabled_When_Upgrading_Then_TheGPUInstancingRemainsEnabled",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); // transparent
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
                "Given_TransparentAutodeskInteractiveMaterialEnableGPUInstancingDisabled_When_Upgrading_Then_TheGPUInstancingRemainsDisabled",
            setup = material =>
            {
                material.SetFloat("_Mode", 3.0f); // transparent
                material.enableInstancing = false;
            },
            verify = material =>
            {
                Assert.IsFalse(material.enableInstancing);
            }
        };
    }
}
