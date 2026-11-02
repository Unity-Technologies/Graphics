using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
[Category("Graphics Tools")]

class ParticleSurfaceMaterialUpgraderTest : MaterialUpgraderTestBase<ParticleUpgrader>
{
    [OneTimeSetUp]
    public override void OneTimeSetUp()
    {
        m_Upgrader = new ParticleUpgrader("Particles/Standard Surface");
    }

    public ParticleSurfaceMaterialUpgraderTest() : base("Particles/Standard Surface", "Universal Render Pipeline/Particles/Lit")
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
                "Given_OpaqueParticleStandardSurface_WhenUpgrading_Then_TheOpaqueURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to opaque
                material.SetFloat("_Mode", 0.0f); // Opaque
            },
            verify = material =>
            {
                //check the material is still opaque
                Assert.AreEqual(0.0f, material.GetFloat("_Surface"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutParticleStandardSurface_WhenUpgrading_Then_TheCutoutURPParticleLitGoesOpaque",
            setup = material =>
            {
                //set the material to cutout mode
                material.SetFloat("_Mode", 1.0f); // Cutout
            },
            verify = material =>
            {
                //check the material is opaque
                Assert.AreEqual(0.0f, material.GetFloat("_Surface"));
            }
        };

         yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeParticleStandardSurface_WhenUpgrading_Then_TheFadeURPParticleLitGoesTransparent",
            setup = material =>
            {
                //set the material to fade mode
                material.SetFloat("_Mode", 2.0f); // Fade
            },
            verify = material =>
            {
                //check the material is transparent
                Assert.AreEqual(1.0f, material.GetFloat("_Surface"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentParticleStandardSurface_WhenUpgrading_Then_TheTransparentURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to transparent mode
                material.SetFloat("_Mode", 3.0f); // Transparent
            },
            verify = material =>
            {
                //check the material is still transparent
                Assert.AreEqual(1.0f, material.GetFloat("_Surface"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AdditiveParticleStandardSurface_WhenUpgrading_Then_TheAdditiveURPParticleLitGoesTransparent",
            setup = material =>
            {
                //set the material to additive mode
                material.SetFloat("_Mode", 4.0f); // Additive
            },
            verify = material =>
            {
                //check the material is transparent
                Assert.AreEqual(1.0f, material.GetFloat("_Surface"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_SubtractiveParticleStandardSurface_WhenUpgrading_Then_TheSubtractiveURPParticleLitSurfaceIsNotEmpty",
            setup = material =>
            {
                //set the material to subtractive mode
                material.SetFloat("_Mode", 5.0f); // Subtractive
            },
            verify = material =>
            {
                //check material surface type is transparent
                Assert.AreEqual(1.0f, material.GetFloat("_Surface"));
                //check color mode is subtractive
                Assert.AreEqual(2.0f, material.GetFloat("_ColorMode"));
                //check blend mode is alpha
                Assert.AreEqual(0.0f, material.GetFloat("_Blend"));
                //check that the _COLORADDSUBDIFF_ON keyword is enabled
                Assert.IsTrue(material.IsKeywordEnabled("_COLORADDSUBDIFF_ON"));
                //check that _BaseColorAddSubDiff is set correctly for subtractive mode
                Assert.AreEqual(new Vector4(-1.0f, 0.0f, 0.0f, 0.0f), material.GetVector("_BaseColorAddSubDiff"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_ModulateParticleStandardSurface_WhenUpgrading_Then_TheModulateURPParticleLitSurfaceGoesTransparent",
            setup = material =>
            {
                //set the material to modulate mode
                material.SetFloat("_Mode", 6.0f); // Modulate
            },
            verify = material =>
            {
                //check the material is transparent
                Assert.AreEqual(1.0f, material.GetFloat("_Surface"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueFlipBookFrameBlendingStandardSurface_WhenUpgrading_Then_TheOpaqueFlipBookFrameBlendingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to opaque with flipbook frame blending
                material.SetFloat("_Mode", 0.0f); // Opaque
                material.SetFloat("_FlipbookMode", 1.0f); // Frame Blending
            },
            verify = material =>
            {
                //check the material flipbook mode is still frame blending
                Assert.AreEqual(1.0f, material.GetFloat("_FlipbookBlending"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueTwoSidedStandardSurface_WhenUpgrading_Then_TheOpaqueTwoSidedURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to opaque and two sided
                material.SetFloat("_Mode", 0.0f); // Opaque
                material.SetFloat("_Cull", 1.0f); // Two Sided
            },
            verify = material =>
            {
                //check the material is still two sided
                Assert.AreEqual(1.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueMetallicValueStandardSurface_WhenUpgrading_Then_TheOpaqueURPParticleLitPreserveMetallicValue",
            setup = material =>
            {
                //set the material to opaque with metallic value
                material.SetFloat("_Mode", 0.0f); // Opaque
                material.SetFloat("_Metallic", 0.7f); // Metallic Value
            },
            verify = material =>
            {
                //check the material metallic value is preserved
                Assert.AreEqual(0.7f, material.GetFloat("_Metallic"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueSmoothnessValueStandardSurface_WhenUpgrading_Then_TheOpaqueURPParticleLitPreserveSmoothnessValue",
            setup = material =>
            {
                //set the material to opaque with smoothness value
                material.SetFloat("_Mode", 0.0f); // Opaque
                material.SetFloat("_Glossiness", 0.3f); // Smoothness Value
            },
            verify = material =>
            {
                //check the material smoothness value is preserved
                Assert.AreEqual(0.3f, material.GetFloat("_Smoothness"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueEmissionCheckboxEnabledStandardSurface_WhenUpgrading_Then_TheOpaqueURPParticleLitPreserveEmissionCheckbox",
            setup = material =>
            {
                //set the material to opaque with emission enabled
                material.SetFloat("_Mode", 0.0f); // Opaque
                CoreUtils.SetKeyword(material, "_EMISSION", true);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            },
            verify = material =>
            {
                //check the material emission checkbox is preserved
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueAlbedoColorRedStandardSurface_WhenUpgrading_Then_TheOpaqueURPParticleLitPreserveAlbedoColorRed",
            setup = material =>
            {
                //set the material to opaque with albedo color red
                material.SetFloat("_Mode", 0.0f); // Opaque
                material.SetColor("_Color", Color.red);
            },
            verify = material =>
            {
                //check the material albedo color is preserved as red
                Assert.AreEqual(Color.red, material.GetColor("_BaseColor"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutAlphaCutoffValueStandardSurface_WhenUpgrading_Then_TheCutoutAlphaCutoffValueURPParticleLitPreserve",
            setup = material =>
            {
               //set the material to cutout with alpha cutoff value
                material.SetFloat("_Mode", 1.0f); // Cutout
                material.SetFloat("_Cutoff", 0.2f); // Alpha Cutoff Value
            },
            verify = material =>
            {
                //check the material alpha cutoff value is preserved
                Assert.AreEqual(0.2f, material.GetFloat("_Cutoff"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_CutoutStandardSurface_WhenUpgrading_Then_TheCutoutAlphaClippingCheckboxURPParticleLitEnabled",
            setup = material =>
            {
                //set the material to cutout mode
                material.SetFloat("_Mode", 1.0f); // Cutout
            },
            verify = material =>
            {
                //check the material alpha Clipping checkbox is enabled
                Assert.AreEqual(1f, material.GetFloat("_AlphaClip"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeFlipBookFrameBlendingStandardSurface_WhenUpgrading_Then_TheFadeFlipBookFrameBlendingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with flipbook frame blending
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_FlipbookMode", 1.0f); // Frame Blending
            },
            verify = material =>
            {
                //check the material flipbook mode is still frame blending
                Assert.AreEqual(1.0f, material.GetFloat("_FlipbookBlending"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeTwoSidedStandardSurface_WhenUpgrading_Then_TheFadeTwoSidedURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade and two sided
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_Cull", 1.0f); // Two Sided
            },
            verify = material =>
            {
                //check the material is still two sided
                Assert.AreEqual(1.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeSoftParticlesStandardSurface_WhenUpgrading_Then_TheFadeSoftParticlesURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with soft particles
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_SoftParticlesEnabled", 1.0f); // Soft Particles
            },
            verify = material =>
            {
                //check the material soft particles is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_SoftParticlesEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeSoftParticleNearValueStandardSurface_WhenUpgrading_Then_TheFadeSoftParticleNearValueURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with soft particles and set near value
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_SoftParticlesEnabled", 1.0f); // Soft Particles
                material.SetFloat("_SoftParticlesNearFadeDistance", 0.1f); // Soft Particle Near
            },
            verify = material =>
            {
                //check the material soft particle near value is preserved
                Assert.AreEqual(0.1f, material.GetFloat("_SoftParticlesNearFadeDistance"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeSoftParticleFarValueStandardSurface_WhenUpgrading_Then_TheFadeSoftParticleFarValueURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with soft particles and set far value
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_SoftParticlesEnabled", 1.0f); // Soft Particles
                material.SetFloat("_SoftParticlesFarFadeDistance", 0.7f); // Soft Particle Far
            },
            verify = material =>
            {
                //check the material soft particle far value is preserved
                Assert.AreEqual(0.7f, material.GetFloat("_SoftParticlesFarFadeDistance"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeCameraFadingStandardSurface_WhenUpgrading_Then_TheFadeCameraFadingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with camera fading
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_CameraFadingEnabled", 1.0f); // Camera Fading
            },
            verify = material =>
            {
                //check the material camera fading is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_CameraFadingEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeCameraDistortionStandardSurface_WhenUpgrading_Then_TheFadeCameraDistortionURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with camera distortion
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_DistortionEnabled", 1.0f); // Camera Distortion
            },
            verify = material =>
            {
                //check the material camera distortion is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_DistortionEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeDistortionStrengthValueStandardSurface_WhenUpgrading_Then_TheFadeDistortionStrengthValueURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with camera distortion and set distortion strength value
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_DistortionEnabled", 1.0f); // Camera Distortion
                material.SetFloat("_DistortionStrength", 2f); // Distortion Strength
            },
            verify = material =>
            {
                //check the material distortion strength value is preserved
                Assert.AreEqual(2f, material.GetFloat("_DistortionStrength"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_FadeDistortionBlendValueStandardSurface_WhenUpgrading_Then_TheFadeDistortionBlendValueURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to fade with camera distortion and set distortion blend value
                material.SetFloat("_Mode", 2.0f); // Fade
                material.SetFloat("_DistortionEnabled", 1.0f); // Camera Distortion
                material.SetFloat("_DistortionBlend", 0.1f); // Distortion Blend
            },
            verify = material =>
            {
                //check the material distortion blend value is preserved
                Assert.AreEqual(0.1f, material.GetFloat("_DistortionBlend"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentFlipBookFrameBlendingStandardSurface_WhenUpgrading_Then_TheTransparentFlipBookFrameBlendingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to transparent with flipbook frame blending
                material.SetFloat("_Mode", 3.0f); // Transparent
                material.SetFloat("_FlipbookMode", 1.0f); // Frame Blending
            },
            verify = material =>
            {
                //check the material flipbook mode is still frame blending
                Assert.AreEqual(1.0f, material.GetFloat("_FlipbookBlending"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentTwoSidedStandardSurface_WhenUpgrading_Then_TheTransparentTwoSidedURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to transparent and two sided
                material.SetFloat("_Mode", 3.0f); // Transparent
                material.SetFloat("_Cull", 1.0f); // Two Sided
            },
            verify = material =>
            {
                //check the material is still two sided
                Assert.AreEqual(1.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentCameraFadingStandardSurface_WhenUpgrading_Then_TheTransparentCameraFadingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to transparent with camera fading
                material.SetFloat("_Mode", 3.0f); // Transparent
                material.SetFloat("_CameraFadingEnabled", 1.0f); // Camera Fading
            },
            verify = material =>
            {
                //check the material camera fading is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_CameraFadingEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentCameraDistortionStandardSurface_WhenUpgrading_Then_TheTransparentCameraDistortionURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to transparent with camera distortion
                material.SetFloat("_Mode", 3.0f); // Transparent
                material.SetFloat("_DistortionEnabled", 1.0f); // Camera Distortion
            },
            verify = material =>
            {
                //check the material camera distortion is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_DistortionEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentEmissionEnabledStandardSurface_WhenUpgrading_Then_TheTransparentEmissionEnabledURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to transparent with emission enabled
                material.SetFloat("_Mode", 3.0f); // Transparent
                CoreUtils.SetKeyword(material, "_EMISSION", true);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            },
            verify = material =>
            {
                //check the material emission enabled is preserved
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AdditiveFlipBookFrameBlendingStandardSurface_WhenUpgrading_Then_TheAdditiveFlipBookFrameBlendingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to additive with flipbook frame blending
                material.SetFloat("_Mode", 4.0f); // Additive
                material.SetFloat("_FlipbookMode", 1.0f); // Frame Blending
            },
            verify = material =>
            {
                //check the material flipbook mode is still frame blending
                Assert.AreEqual(1.0f, material.GetFloat("_FlipbookBlending"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AdditiveTwoSidedStandardSurface_WhenUpgrading_Then_TheAdditiveTwoSidedURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to additive and two sided
                material.SetFloat("_Mode", 4.0f); // Additive
                material.SetFloat("_Cull", 1.0f); // Two Sided
            },
            verify = material =>
            {
                //check the material is still two sided
                Assert.AreEqual(1.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AdditiveSoftParticlesStandardSurface_WhenUpgrading_Then_TheAdditiveSoftParticlesURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to additive with soft particles
                material.SetFloat("_Mode", 4.0f); // Additive
                material.SetFloat("_SoftParticlesEnabled", 1.0f); // Soft Particles
            },
            verify = material =>
            {
                //check the material soft particles is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_SoftParticlesEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AdditiveCameraFadingStandardSurface_WhenUpgrading_Then_TheAdditiveCameraFadingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to additive with camera fading
                material.SetFloat("_Mode", 4.0f); // Additive
                material.SetFloat("_CameraFadingEnabled", 1.0f); // Camera Fading
            },
            verify = material =>
            {
                //check the material camera fading is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_CameraFadingEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_AdditiveDistortionEnabledStandardSurface_WhenUpgrading_Then_TheAdditiveDistortionEnabledURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to additive with camera distortion
                material.SetFloat("_Mode", 4.0f); // Additive
                material.SetFloat("_DistortionEnabled", 1.0f); // Camera Distortion
            },
            verify = material =>
            {
                //check the material camera distortion is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_DistortionEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_SubtractiveFlipBookFrameBlendingStandardSurface_WhenUpgrading_Then_TheSubtractiveFlipBookFrameBlendingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to subtractive with flipbook frame blending
                material.SetFloat("_Mode", 5.0f); // Subtractive
                material.SetFloat("_FlipbookMode", 1.0f); // Frame Blending
            },
            verify = material =>
            {
                //check the material flipbook mode is still frame blending
                Assert.AreEqual(1.0f, material.GetFloat("_FlipbookBlending"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_SubtractiveTwoSidedStandardSurface_WhenUpgrading_Then_TheSubtractiveTwoSidedURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to subtractive and two sided
                material.SetFloat("_Mode", 5.0f); // Subtractive
                material.SetFloat("_Cull", 1.0f); // Two Sided
            },
            verify = material =>
            {
                //check the material is still two sided
                Assert.AreEqual(1.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_SubtractiveSoftParticlesStandardSurface_WhenUpgrading_Then_TheSubtractiveSoftParticlesURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to subtractive with soft particles
                material.SetFloat("_Mode", 5.0f); // Subtractive
                material.SetFloat("_SoftParticlesEnabled", 1.0f); // Soft Particles
            },
            verify = material =>
            {
                //check the material soft particles is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_SoftParticlesEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_SubtractiveCameraFadingStandardSurface_WhenUpgrading_Then_TheSubtractiveCameraFadingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to subtractive with camera fading
                material.SetFloat("_Mode", 5.0f); // Subtractive
                material.SetFloat("_CameraFadingEnabled", 1.0f); // Camera Fading
            },
            verify = material =>
            {
                //check the material camera fading is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_CameraFadingEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_SubtractiveDistortionEnabledStandardSurface_WhenUpgrading_Then_TheSubtractiveDistortionEnabledURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to subtractive with camera distortion
                material.SetFloat("_Mode", 5.0f); // Subtractive
                material.SetFloat("_DistortionEnabled", 1.0f); // Camera Distortion
            },
            verify = material =>
            {
                //check the material camera distortion is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_DistortionEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_ModulateFlipBookFrameBlendingStandardSurface_WhenUpgrading_Then_TheModulateFlipBookFrameBlendingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to modulate with flipbook frame blending
                material.SetFloat("_Mode", 6.0f); // Modulate
                material.SetFloat("_FlipbookMode", 1.0f); // Frame Blending
            },
            verify = material =>
            {
                //check the material flipbook mode is still frame blending
                Assert.AreEqual(1.0f, material.GetFloat("_FlipbookBlending"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_ModulateTwoSidedStandardSurface_WhenUpgrading_Then_TheModulateTwoSidedURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to modulate and two sided
                material.SetFloat("_Mode", 6.0f); // Modulate
                material.SetFloat("_Cull", 1.0f); // Two Sided
            },
            verify = material =>
            {
                //check the material is still two sided
                Assert.AreEqual(1.0f, material.GetFloat("_Cull"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_ModulateSoftParticlesStandardSurface_WhenUpgrading_Then_TheModulateSoftParticlesURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to modulate with soft particles
                material.SetFloat("_Mode", 6.0f); // Modulate
                material.SetFloat("_SoftParticlesEnabled", 1.0f); // Soft Particles
            },
            verify = material =>
            {
                //check the material soft particles is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_SoftParticlesEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_ModulateCameraFadingStandardSurface_WhenUpgrading_Then_TheModulateCameraFadingURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to modulate with camera fading
                material.SetFloat("_Mode", 6.0f); // Modulate
                material.SetFloat("_CameraFadingEnabled", 1.0f); // Camera Fading
            },
            verify = material =>
            {
                //check the material camera fading is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_CameraFadingEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_ModulateDistortionEnabledStandardSurface_WhenUpgrading_Then_TheModulateDistortionEnabledURPParticleLitPreserve",
            setup = material =>
            {
                //set the material to modulate with camera distortion
                material.SetFloat("_Mode", 6.0f); // Modulate
                material.SetFloat("_DistortionEnabled", 1.0f); // Camera Distortion
            },
            verify = material =>
            {
                //check the material camera distortion is preserved
                Assert.AreEqual(1.0f, material.GetFloat("_DistortionEnabled"));
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueEmissionKeywordEnabledStandardSurface_WhenUpgrading_Then_TheOpaqueURPParticleLitPreserveEmissionKeyword",
            setup = material =>
            {
                //set the material to opaque with emission keyword enabled
                material.SetFloat("_Mode", 0.0f); // Opaque
                CoreUtils.SetKeyword(material, "_EMISSION", true);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            },
            verify = material =>
            {
                //check the material emission keyword is preserved
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
                Assert.AreNotEqual(MaterialGlobalIlluminationFlags.None, material.globalIlluminationFlags);
                Assert.AreNotEqual(MaterialGlobalIlluminationFlags.EmissiveIsBlack, material.globalIlluminationFlags);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_OpaqueEmissionDisabledStandardSurface_WhenUpgrading_Then_TheOpaqueURPParticleLitEmissionRemainsDisabled",
            setup = material =>
            {
                //set the material to opaque without emission enabled (no keyword, None flags)
                material.SetFloat("_Mode", 0.0f); // Opaque
                CoreUtils.SetKeyword(material, "_EMISSION", false);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            },
            verify = material =>
            {
                //check the material emission remains disabled
                Assert.IsFalse(material.IsKeywordEnabled("_EMISSION"));
                // Should be set to EmissiveIsBlack to match BiRP behavior when emission unchecked
                Assert.AreEqual(MaterialGlobalIlluminationFlags.EmissiveIsBlack, material.globalIlluminationFlags);
            }
        };

        yield return new MaterialUpgradeTestCase
        {
            name =
                "Given_TransparentEmissionKeywordEnabledStandardSurface_WhenUpgrading_Then_TheTransparentURPParticleLitPreserveEmissionKeyword",
            setup = material =>
            {
                //set the material to transparent with emission keyword enabled
                material.SetFloat("_Mode", 3.0f); // Transparent
                CoreUtils.SetKeyword(material, "_EMISSION", true);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            },
            verify = material =>
            {
                //check the material emission keyword is preserved
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"));
                Assert.AreNotEqual(MaterialGlobalIlluminationFlags.None, material.globalIlluminationFlags);
                Assert.AreNotEqual(MaterialGlobalIlluminationFlags.EmissiveIsBlack, material.globalIlluminationFlags);
            }
        };
    }
}
