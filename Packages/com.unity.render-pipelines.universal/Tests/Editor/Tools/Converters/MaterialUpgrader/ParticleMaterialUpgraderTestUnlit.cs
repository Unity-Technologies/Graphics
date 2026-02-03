using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.TestTools;
[Category("Graphics Tools")]
class ParticleMaterialUpgraderTestUnlit : MaterialUpgraderTestBase<ParticleUpgrader>
{
    [OneTimeSetUp]
    public override void OneTimeSetUp()
    {
        m_Upgrader = new ParticleUpgrader("Particles/Standard Unlit");
    }

    public ParticleMaterialUpgraderTestUnlit() : base("Particles/Standard Unlit", "Universal Render Pipeline/Particles/Unlit")
    {
    }

    [Test]
    [TestCaseSource(nameof(MaterialUpgradeCases))]
    public void UpgradeParticleStandardUnlitMaterial(MaterialUpgradeTestCase testCase)
    {
        base.UpgradeMaterial(testCase);
    }
    private static IEnumerable MaterialUpgradeCases()
    {
        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AlphaCutOffParticleStandardUnlit_When_Upgrading_Then_TheMaterialURPTransparentWithAlphaCutOffPreserve",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // Cutout to enable Alpha Cutoff slider
            },
            verify = material =>
            {
                Assert.AreEqual(1.0f, material.GetFloat("_AlphaClip"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AlphaCutOffValueParticleStandardUnlit_When_Upgrading_Then_TheMaterialURPTransparentWithAlphaCutOffValuePreserve",
            setup = material =>
            {
                material.SetFloat("_Mode", 1.0f); // Cutout to enable Alpha Cutoff slider
                material.SetFloat("_Cutoff", 0.25f); // Alpha Cutoff enabled
            },
            verify = material =>
            {
                Assert.AreEqual(0.25f, material.GetFloat("_Cutoff")); // Alpha Cutoff
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueFlipBookFrameBlendingParticleStandardUnlit_When_Upgrading_Then_OpaqueTheMaterialURPFlipBookBlendingEnabled",
            setup = material =>
            {
                //enable flipbook frame blending
                material.SetFloat("_FlipbookMode", 1.0f);
            },
            verify = material =>
            {
                //check flipbook blending is enabled
                Assert.AreEqual(1.0f, material.GetFloat("_FlipbookBlending"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OldParticleShaderNameStandardUnlit_WhenUpgrading_Then_TheShaderURPKeepParticleUnlitName",
            setup = material =>
            {
                //check old shader name
                Assert.AreEqual("Particles/Standard Unlit", material.shader.name);
            },
            verify = material =>
            {
                //check new shader name
                Assert.AreEqual("Universal Render Pipeline/Particles/Unlit", material.shader.name);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueTwoSidedParticleStandardUnlit_WhenUpgrading_Then_OpaqueTheMaterialURPTwoSidedEnabled",
            setup = material =>
            {
                //enable two sided
                material.SetFloat("_Cull", 0.0f); // Two Sided enabled//
            },
            verify = material =>
            {
                //check two sided is enabled
                Assert.AreEqual(0.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueTwoSidedParticleStandardUnlit_WhenUpgrading_Then_OpaqueTheMaterialURPTwoSidedDisabled",
            setup = material =>
            {
                //enable two sided
                material.SetFloat("_Cull", 1.0f); // Two Sided disabled//
            },
            verify = material =>
            {
                //check two sided is disabled
                Assert.AreEqual(1.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_MaterialNameParticleStandardUnlit_WhenUpgrading_Then_TheMaterialURPParticleUnlitPreserve",
            setup = material =>
            {
                //set material name
                material.name = "MaterialParticleUnlit";
            },
            verify = material =>
            {
                //check material name is preserved
                Assert.AreEqual("MaterialParticleUnlit", material.name);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_EmissionEnabledParticleStandardUnlit_WhenUpgrading_Then_TheEmissionParticleURPUnlitPreserve",
            setup = material =>
            {
                //enable emission _EmissionEnabled
                material.SetFloat("_EmissionEnabled", 1.0f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            },
            verify = material =>
            {
                //check emission is enabled
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_EmissionDisabledParticleStandardUnlit_WhenUpgrading_Then_TheEmissionParticleURPUnlitPreserve",
            setup = material =>
            {
                //enable emission _EmissionDisabled
                material.SetFloat("_EmissionEnabled", 0.0f);
            },
            verify = material =>
            {
                //check emission is disabled
                Assert.IsFalse(material.IsKeywordEnabled("_EMISSION"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_EmissionRedColorParticleStandardUnlit_WhenUpgrading_Then_TheEmissionRedColorParticleURPUnlitPreserve",
            setup = material =>
            {
                //enable emission _EmissionEnabled
                material.SetFloat("_EmissionEnabled", 1.0f);
                material.SetColor("_EmissionColor", Color.red);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                },
            verify = material =>
            {
                //check emission is enabled
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
                //check emission color is red
                Assert.AreEqual(Color.red, material.GetColor("_EmissionColor"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueParticleStandardUnlit_WhenUpgrading_Then_TheOpaqueURPParticleUnlit",
            setup = material =>
            {
                //set material to opaque
                material.SetFloat("_Mode", 0.0f); // Opaque
            },
            verify = material =>
            {
                //check material is opaque
                Assert.AreEqual(0.0f, material.GetFloat("_Surface")); // Surface type
            }
        };

        yield return new MaterialUpgradeTestCase()
        {
            name = "Given_TilingSetToStandardParticleUnlitMaterial_When_Upgrading_Then_TheMaterialURPTilingParticleUnlitPreserved",
            setup =  material =>
            {
                material.SetTextureScale("_MainTex", new Vector2(2.0f, 2.0f)); // Set tiling
            },
            verify = material =>
            {
                Assert.AreEqual(new Vector2(2.0f, 2.0f), material.GetTextureScale("_BaseMap")); // Check that tiling is preserved after upgrade
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_BaseMapColorSetToRedStandardParticleUnlitMaterial_When_Upgradin_Then_TheMaterialURPBaseMapColorRedPreserve",
            setup = material =>
            {
                //set base map color to red
                material.SetColor("_Color", Color.red);
            },
            verify = material =>
            {
                //check base map color is red
                Assert.AreEqual(Color.red, material.GetColor("_BaseColor"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_RenderQueueFromBackgroundStandardParticleUnlitMaterial_When_Upgrading_Then_TheMaterialURPRenderQueueFromBackgroundParticleUnlitPreserve",
            ignore = true,
            ignoreReason = "Fails, does not preserve set render queue",
            setup = material =>
            {
                //set the material to opaque first
                material.SetFloat("_Mode", 0.0f); // Opaque
                //set custom render queue from shader, should be 1000
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;
                //material.renderQueue = 2555;
            },
            verify = material =>
            {
                //check render queue from shader is preserved
                //Assert.AreEqual(2555, material.renderQueue);
                Assert.AreEqual((int)UnityEngine.Rendering.RenderQueue.Background, material.renderQueue);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueColorModeMultiplyStandardParticleUnlitMaterial_When_Upgrading_Then_TheURPParticleUnlitOpaqueMultiplyPreserve",
            setup = material =>
            {
                //set the material to opaque first
                material.SetFloat("_Mode", 0.0f); // Opaque
                //set color mode to multiply
                material.SetFloat("_ColorMode", 0.0f); // Multiply
            },
            verify = material =>
            {
                //check color mode is multiply
                Assert.AreEqual(0.0f, material.GetFloat("_ColorMode"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueColorModeAdditiveStandardParticleUnlitMaterial_When_Upgrading_Then_TheURPParticleUnlitOpaqueAdditivePreserve",
            setup = material =>
            {
                //set the material to opaque first
                material.SetFloat("_Mode", 0.0f); // Opaque
                //set color mode to additive
                material.SetFloat("_ColorMode", 1.0f); // Additive
            },
            verify = material =>
            {
                //check color mode is additive
                Assert.AreEqual(1.0f, material.GetFloat("_ColorMode"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueColorModeSubtractiveStandardParticleUnlitMaterial_When_Upgrading_Then_TheURPParticleUnlitOpaqueSubtractivePreserve",
            setup = material =>
            {
                //set the material to opaque first
                material.SetFloat("_Mode", 0.0f); // Opaque
                //set color mode to subtractive
                material.SetFloat("_ColorMode", 2.0f); // Subtractive
            },
            verify = material =>
            {
                //check color mode is subtractive
                Assert.AreEqual(2.0f, material.GetFloat("_ColorMode"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueColorModeOverlayStandardParticleUnlitMaterial_When_Upgrading_Then_TheURPParticleUnlitOpaqueOverlayPreserve",
            setup = material =>
            {
                //set the material to opaque first
                material.SetFloat("_Mode", 0.0f); // Opaque
                //set color mode to overlay
                material.SetFloat("_ColorMode", 3.0f); // Overlay
            },
            verify = material =>
            {
                //check color mode is overlay
                Assert.AreEqual(3.0f, material.GetFloat("_ColorMode"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueColorModeColorStandardParticleUnlitMaterial_When_Upgrading_Then_TheURPParticleUnlitOpaqueColorPreserve",
            setup = material =>
            {
                //set the material to opaque first
                material.SetFloat("_Mode", 0.0f); // Opaque
                //set color mode to color
                material.SetFloat("_ColorMode", 4.0f); // Color
            },
            verify = material =>
            {
                //check color mode is color
                Assert.AreEqual(4.0f, material.GetFloat("_ColorMode"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueColorModeDifferenceStandardParticleUnlitMaterial_When_Upgrading_Then_TheURPParticleUnlitOpaqueDifferencePreserve",
            setup = material =>
            {
                //set the material to opaque first
                material.SetFloat("_Mode", 0.0f); // Opaque
                //set color mode to difference
                material.SetFloat("_ColorMode", 5.0f); // Difference
            },
            verify = material =>
            {
                //check color mode is difference
                Assert.AreEqual(5.0f, material.GetFloat("_ColorMode"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeSoftParticleStandardParticleUnlitMaterialEnabled_When_Upgrading_Then_TheFadeSoftParticleURPParticleUnlitMaterialPreserve",
            setup = material =>
            {
                //set the material to fade
                material.SetFloat("_Mode", 2.0f); // Fade
                //enable soft particles checkbox
                material.SetFloat("_SoftParticlesEnabled", 1.0f); // Soft Particles enabled
            },
            verify = material =>
            {
                //check soft particles is enabled
                Assert.AreEqual(1.0f, material.GetFloat("_SoftParticlesEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CameraFadingTransarentStandardParticleUnlitMaterialEnabled_When_Upgrading_Then_CameraFadingTransparentURPParticleUnlitMaterialPreserve",
            setup = material =>
            {
                //set the material to Transparent
                material.SetFloat("_Mode", 3.0f); // Transparent
                //enable camera fading checkbox
                material.SetFloat("_CameraFadingEnabled", 1.0f); // Camera Fading enabled
            },
            verify = material =>
            {
                //check camera fading is enabled
                Assert.AreEqual(1.0f, material.GetFloat("_CameraFadingEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_DistortionTransarentStandardParticleUnlitMaterialEnabled_When_Upgrading_Then_DistortionTransparentURPParticleUnlitMaterialPreserve",
            setup = material =>
            {
                //set the material to Transparent
                material.SetFloat("_Mode", 3.0f); // Transparent
                //enable distortion checkbox
                material.SetFloat("_DistortionEnabled", 1.0f); // Distortion enabled
            },
            verify = material =>
            {
                //check distortion is enabled
                Assert.AreEqual(1.0f, material.GetFloat("_DistortionEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentModeStandardParticleUnlitMaterialEnabled_When_Upgrading_Then_TransparentModeURPParticleUnlitMaterialPreserve",
            setup = material =>
            {
                //set the material to Transparent
                material.SetFloat("_Mode", 3.0f); // Transparent
            },
            verify = material =>
            {
                //check material is Transparent
                Assert.AreEqual(1.0f, material.GetFloat("_Surface"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeModeStandardParticleUnlitMaterialEnabled_When_Upgrading_Then_TransparentModeURPParticleUnlitMaterialConverted",
            setup = material =>
            {
                //set the material to Fade
                material.SetFloat("_Mode", 2.0f); // Fade
            },
            verify = material =>
            {
                //check material is Transparent
                Assert.AreEqual(1.0f, material.GetFloat("_Surface"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AdditiveRenderingModeStandardParticleUnlitMaterialEnabled_When_Upgrading_Then_AdditiveModeURPParticleUnlitMaterialPreserve",
            setup = material =>
            {
                //set the material to Additive
                material.SetFloat("_Mode", 4.0f); // Additive
            },
            verify = material =>
            {
                //check material is Additive
                Assert.AreEqual(2.0f, material.GetFloat("_Blend"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_RenderingModeSubtractiveStandardParticleUnlitMaterial_When_Upgrading_Then_SurfaceTypeURPParticleUnlitMaterialDropdownNotBlank",
            ignore = true,
            setup = material =>
            {
                //set the material to Subtractive
                material.SetFloat("_Mode", 5.0f); // Subtractive
            },
            verify = material =>
            {
                //check material surface type is not blank
                float surfaceType = material.GetFloat("_Surface");
                Assert.IsTrue(surfaceType == 0.0f || surfaceType == 1.0f, "Surface type is blank.");
            }
        };
    }
}
