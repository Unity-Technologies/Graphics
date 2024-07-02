using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SocialPlatforms;
using IShaderScriptableStrippingData = UnityEditor.Rendering.Universal.ShaderScriptableStripper.IShaderScriptableStrippingData;

namespace ShaderStrippingAndPrefiltering
{

    class ShaderScriptableStripperTests
    {
        internal struct TestStrippingData : IShaderScriptableStrippingData
        {
            public ShaderFeatures shaderFeatures { get; set; }
            public VolumeFeatures volumeFeatures { get; set; }

            public bool isGLDevice { get; set; }

            public bool stripSoftShadowQualityLevels { get; set; }
            public bool strip2DPasses { get; set; }
            public bool stripDebugDisplayShaders { get; set; }
            public bool stripScreenCoordOverrideVariants { get; set; }
            public bool stripUnusedVariants { get; set; }
            public bool stripUnusedPostProcessingVariants { get; set; }
            public bool stripUnusedXRVariants { get; set; }
            public bool IsHDRDisplaySupportEnabled { get; set; }

            public Shader shader { get; set; }
            public ShaderType shaderType { get; set; }
            public ShaderCompilerPlatform shaderCompilerPlatform { get; set; }

            public string passName { get; set; }
            public PassType passType { get; set; }
            public PassIdentifier passIdentifier { get; set; }

            public bool IsHDRShaderVariantValid { get; set; }


            public bool IsKeywordEnabled(LocalKeyword keyword)
            {
                return TestHelper.s_EnabledKeywords != null && TestHelper.s_EnabledKeywords.Contains(keyword.name);
            }

            public bool IsShaderFeatureEnabled(ShaderFeatures feature)
            {
                return (shaderFeatures & feature) != 0;
            }

            public bool IsVolumeFeatureEnabled(VolumeFeatures feature)
            {
                return (volumeFeatures & feature) != 0;
            }

            public bool PassHasKeyword(LocalKeyword keyword)
            {
                return TestHelper.s_PassKeywords != null && TestHelper.s_PassKeywords.Contains(keyword.name);
            }
        }

        class TestHelper
        {
            public static List<string> s_EnabledKeywords;
            public static List<string> s_PassKeywords;

            public ShaderScriptableStripper stripper;
            public IShaderScriptableStrippingData data;

            public ShaderStripTool<ShaderFeatures> featureStripTool;

            private Shader shader;

            public static readonly VolumeFeatures s_AllBloomFeatures = VolumeFeatures.BloomLQ | VolumeFeatures.BloomLQDirt
                                                                     | VolumeFeatures.BloomHQ | VolumeFeatures.BloomHQDirt;


            public TestHelper(Shader shader, ShaderFeatures shaderFeatures, VolumeFeatures volumeFeatures = VolumeFeatures.None, bool stripUnusedVariants = true, bool stripUnusedXRVariants = true)
            {
                s_PassKeywords = new List<string>() { };
                s_EnabledKeywords = new List<string>() { };

                stripper = new();
                stripper.BeforeShaderStripping(shader);

                data = new TestStrippingData();
                data.shader = shader;
                data.shaderFeatures = shaderFeatures;
                data.volumeFeatures = volumeFeatures;
                data.stripUnusedVariants = stripUnusedVariants;
                data.strip2DPasses = false;
                data.stripUnusedXRVariants = stripUnusedXRVariants;

                featureStripTool = new ShaderStripTool<ShaderFeatures>(data.shaderFeatures, ref data);
            }

            public void AreEqual(bool expected, bool actual)
            {
                Assert.AreEqual(expected, actual);
            }

            public void IsTrue(bool actual)
            {
                Assert.IsTrue(actual);
            }

            public void IsFalse(bool actual)
            {
                Assert.IsFalse(actual);
            }
        }

        /*****************************************************
         * Strip Unused Shaders...
         *****************************************************/

        [TestCase(null, false, false)]
        [TestCase("", false, false)]
        [TestCase("Universal Render Pipeline/Lit", false, false)]
        [TestCase("Universal Render Pipeline/Simple Lit", false, false)]
        [TestCase("Universal Render Pipeline/Unlit", false, false)]
        [TestCase("Universal Render Pipeline/Terrain/Lit", false, false)]
        [TestCase("Universal Render Pipeline/Particles/Lit", false, false)]
        [TestCase("Universal Render Pipeline/Particles/Simple Lit", false, false)]
        [TestCase("Universal Render Pipeline/Particles/Unlit", false, false)]
        [TestCase("Universal Render Pipeline/Baked Lit", false, false)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7", false, false)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7 Billboard", false, false)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree8_PBRLit", false, false)]
        [TestCase("Universal Render Pipeline/Complex Lit", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/BokehDepthOfField", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/GaussianDepthOfField", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionBlur", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/PaniniProjection", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/Bloom", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/StencilDeferred", true, false)]
        [TestCase("Hidden/Universal Render Pipeline/UberPost", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceShadows", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/DBufferClear", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionVectors", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/CopyDepth", false, false)]
        [TestCase("Hidden/Universal Render Pipeline/SubpixelMorphologicalAntialiasing", false, false)]
        public void TestStripUnusedShaders(string shaderName, bool expectedNoFeatures, bool expectedWithDeferredShading)
        {
            TestHelper helper;
            Shader shader = Shader.Find(shaderName);
            ShaderScriptableStripper scriptableStripper = new();

            // Test each individual function...
            TestStripUnusedShaders_Deferred(shader, expectedNoFeatures, expectedWithDeferredShading);

            // Test the parent function...

            // Strip unused variants enabled/disabled
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.AreEqual(expectedNoFeatures, scriptableStripper.StripUnusedShaders(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, stripUnusedVariants:false);
            helper.IsFalse(scriptableStripper.StripUnusedShaders(ref helper.data));
        }

        public void TestStripUnusedShaders_Deferred(Shader shader, bool expectedNoFeatures, bool expectedWithDeferredShading)
        {
            TestHelper helper;

            // Currently only StencilDeferred is stripped out (when deferred is not in use)
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.AreEqual(expectedNoFeatures, helper.stripper.StripUnusedShaders_Deferred(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DeferredShading);
            helper.AreEqual(expectedWithDeferredShading, helper.stripper.StripUnusedShaders_Deferred(ref helper.data));
        }

        /*****************************************************
         * Strip Unused passes...
         *****************************************************/

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Universal Render Pipeline/Lit")]
        [TestCase("Universal Render Pipeline/Simple Lit")]
        [TestCase("Universal Render Pipeline/Unlit")]
        [TestCase("Universal Render Pipeline/Terrain/Lit")]
        [TestCase("Universal Render Pipeline/Particles/Lit")]
        [TestCase("Universal Render Pipeline/Particles/Simple Lit")]
        [TestCase("Universal Render Pipeline/Particles/Unlit")]
        [TestCase("Universal Render Pipeline/Baked Lit")]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7")]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7 Billboard")]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree8_PBRLit")]
        [TestCase("Universal Render Pipeline/Complex Lit")]
        [TestCase("Hidden/Universal Render Pipeline/BokehDepthOfField")]
        [TestCase("Hidden/Universal Render Pipeline/GaussianDepthOfField")]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionBlur")]
        [TestCase("Hidden/Universal Render Pipeline/PaniniProjection")]
        [TestCase("Hidden/Universal Render Pipeline/Bloom")]
        [TestCase("Hidden/Universal Render Pipeline/StencilDeferred")]
        [TestCase("Hidden/Universal Render Pipeline/UberPost")]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceShadows")]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion")]
        [TestCase("Hidden/Universal Render Pipeline/DBufferClear")]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionVectors")]
        [TestCase("Hidden/Universal Render Pipeline/CopyDepth")]
        [TestCase("Hidden/Universal Render Pipeline/SubpixelMorphologicalAntialiasing")]
        public void TestStripUnusedPass(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            TestHelper helper;

            // Check Nulls
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = null;
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            TestStripUnusedPass_2D(shader);
            TestStripUnusedPass_XR(shader);
            TestStripUnusedPass_ShadowCaster(shader);
            TestStripUnusedPass_Decals(shader);
        }

        public void TestStripUnusedPass_2D(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.strip2DPasses = false;
            helper.data.passName = ShaderScriptableStripper.kPassNameUniversal2D;
            helper.IsFalse(helper.stripper.StripUnusedPass_2D(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.strip2DPasses = true;
            helper.data.passName = ShaderScriptableStripper.kPassNameUniversal2D;
            helper.IsTrue(helper.stripper.StripUnusedPass_2D(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));
        }


        public void TestStripUnusedPass_XR(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripUnusedXRVariants = false;
            helper.data.passName = ShaderScriptableStripper.kPassNameXRMotionVectors;
            helper.IsFalse(helper.stripper.StripUnusedPass_XRMotionVectors(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripUnusedXRVariants = true;
            helper.data.passName = ShaderScriptableStripper.kPassNameXRMotionVectors;
            helper.IsTrue(helper.stripper.StripUnusedPass_XRMotionVectors(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));
        }

        public void TestStripUnusedPass_ShadowCaster(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passType = PassType.ShadowCaster;
            helper.IsTrue(helper.stripper.StripUnusedPass_ShadowCaster(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows);
            helper.data.passType = PassType.ShadowCaster;
            helper.IsFalse(helper.stripper.StripUnusedPass_ShadowCaster(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightShadows);
            helper.data.passType = PassType.ShadowCaster;
            helper.IsFalse(helper.stripper.StripUnusedPass_ShadowCaster(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));
        }

        public void TestStripUnusedPass_Decals(Shader shader)
        {
            TestHelper helper;

            // Currently only Decals are stripped in this part
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            // DBuffer
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DBufferMesh;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            helper.data.passName = DecalShaderPassNames.DBufferMesh;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            helper.data.passName = DecalShaderPassNames.DBufferMesh;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            helper.data.passName = DecalShaderPassNames.DBufferMesh;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));


            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DBufferProjector;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            helper.data.passName = DecalShaderPassNames.DBufferProjector;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            helper.data.passName = DecalShaderPassNames.DBufferProjector;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            helper.data.passName = DecalShaderPassNames.DBufferProjector;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));


            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DecalMeshForwardEmissive;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            helper.data.passName = DecalShaderPassNames.DecalMeshForwardEmissive;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            helper.data.passName = DecalShaderPassNames.DecalMeshForwardEmissive;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            helper.data.passName = DecalShaderPassNames.DecalMeshForwardEmissive;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));


            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DecalProjectorForwardEmissive;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            helper.data.passName = DecalShaderPassNames.DecalProjectorForwardEmissive;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            helper.data.passName = DecalShaderPassNames.DecalProjectorForwardEmissive;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            helper.data.passName = DecalShaderPassNames.DecalProjectorForwardEmissive;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));


            // Decal Screen Space
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DecalScreenSpaceMesh;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalScreenSpace);
            helper.data.passName = DecalShaderPassNames.DecalScreenSpaceMesh;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DecalScreenSpaceProjector;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalScreenSpace);
            helper.data.passName = DecalShaderPassNames.DecalScreenSpaceProjector;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));


            // Decal Gbuffer
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DecalGBufferMesh;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalGBuffer);
            helper.data.passName = DecalShaderPassNames.DecalGBufferMesh;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = DecalShaderPassNames.DecalGBufferProjector;
            helper.IsTrue(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsTrue(helper.stripper.StripUnusedPass(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalGBuffer);
            helper.data.passName = DecalShaderPassNames.DecalGBufferProjector;
            helper.IsFalse(helper.stripper.StripUnusedPass_Decals(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedPass(ref helper.data));
        }


        /*****************************************************
         * Strip Invalid Variant passes...
         *****************************************************/

        [TestCase(null, false, false, false)]
        [TestCase("", false, false, false)]
        [TestCase("Universal Render Pipeline/Lit", false, false, true)]
        [TestCase("Universal Render Pipeline/Simple Lit", false, false, true)]
        [TestCase("Universal Render Pipeline/Unlit", false, false, true)]
        [TestCase("Universal Render Pipeline/Terrain/Lit", false, true, true)]
        [TestCase("Universal Render Pipeline/Particles/Lit", false, false, true)]
        [TestCase("Universal Render Pipeline/Particles/Simple Lit", false, false, true)]
        [TestCase("Universal Render Pipeline/Particles/Unlit", false, false, true)]
        [TestCase("Universal Render Pipeline/Baked Lit", false, false, true)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7", false, false, true)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7 Billboard", false, false, true)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree8_PBRLit", false, false, true)]
        [TestCase("Universal Render Pipeline/Complex Lit", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/BokehDepthOfField", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/GaussianDepthOfField", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionBlur", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/PaniniProjection", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/Bloom", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/StencilDeferred", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/UberPost", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceShadows", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/DBufferClear", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionVectors", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/CopyDepth", false, false, true)]
        [TestCase("Hidden/Universal Render Pipeline/SubpixelMorphologicalAntialiasing", false, false, true)]
        public void TestStripInvalidVariants(string shaderName, bool expectedTerrainHoles, bool expectedTerrainHolesWithAlphaTestOn, bool expectedSoftShadows)
        {
            Shader shader = Shader.Find(shaderName);

            TestStripInvalidVariants_HDR(shader);
            StripInvalidVariants_TerrainHoles(shader, expectedTerrainHoles, expectedTerrainHolesWithAlphaTestOn);
            TestStripInvalidVariants_Shadows(shader, expectedSoftShadows);
        }

        public void TestStripInvalidVariants_HDR(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRDisplaySupportEnabled = false;
            helper.data.IsHDRShaderVariantValid = false;
            helper.IsTrue(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsTrue(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRDisplaySupportEnabled = false;
            helper.data.IsHDRShaderVariantValid = true;
            helper.IsFalse(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRDisplaySupportEnabled = true;
            helper.data.IsHDRShaderVariantValid = false;
            helper.IsFalse(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRDisplaySupportEnabled = true;
            helper.data.IsHDRShaderVariantValid = true;
            helper.IsFalse(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalGBuffer);
            helper.data.IsHDRDisplaySupportEnabled = false;
            helper.data.IsHDRShaderVariantValid = false;
            helper.IsTrue(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsTrue(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalGBuffer);
            helper.data.IsHDRDisplaySupportEnabled = false;
            helper.data.IsHDRShaderVariantValid = true;
            helper.IsFalse(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalGBuffer);
            helper.data.IsHDRDisplaySupportEnabled = true;
            helper.data.IsHDRShaderVariantValid = false;
            helper.IsFalse(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DecalGBuffer);
            helper.data.IsHDRDisplaySupportEnabled = true;
            helper.data.IsHDRShaderVariantValid = true;
            helper.IsFalse(helper.stripper.StripInvalidVariants_HDR(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));
        }

        public void StripInvalidVariants_TerrainHoles(Shader shader, bool expectedTerrainHoles, bool expectedTerrainHolesWithAlphaTestOn)
        {
            TestHelper helper;

            // Disabled m_AlphaTestOn
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            helper.AreEqual(expectedTerrainHoles, helper.stripper.StripInvalidVariants_TerrainHoles(ref helper.data));
            helper.AreEqual(expectedTerrainHoles, helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.TerrainHoles);
            helper.data.IsHDRShaderVariantValid = true;
            helper.AreEqual(expectedTerrainHoles, helper.stripper.StripInvalidVariants_TerrainHoles(ref helper.data));
            helper.AreEqual(expectedTerrainHoles, helper.stripper.StripInvalidVariants(ref helper.data));

            // Enabled m_AlphaTestOn
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings._ALPHATEST_ON};
            helper.AreEqual(expectedTerrainHolesWithAlphaTestOn, helper.stripper.StripInvalidVariants_TerrainHoles(ref helper.data));
            helper.AreEqual(expectedTerrainHolesWithAlphaTestOn, helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.TerrainHoles);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings._ALPHATEST_ON};
            helper.IsFalse(helper.stripper.StripInvalidVariants_TerrainHoles(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));
        }

        public void TestStripInvalidVariants_Shadows(Shader shader, bool expectedSoftShadows)
        {
            TestHelper helper;

            // Soft Shadows
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.SoftShadows};
            helper.AreEqual(expectedSoftShadows, helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.AreEqual(expectedSoftShadows, helper.stripper.StripInvalidVariants(ref helper.data));

            // MainLightShadows
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.MainLightShadows};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.SoftShadows};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            // MainLightShadowCascades
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.MainLightShadowCascades};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.SoftShadows};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            // MainLightShadowScreen
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.MainLightShadowScreen};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.MainLightShadowScreen, ShaderKeywordStrings.SoftShadows};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            // AdditionalLightShadows
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.AdditionalLightShadows};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.IsHDRShaderVariantValid = true;
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.AdditionalLightShadows, ShaderKeywordStrings.SoftShadows};
            helper.IsFalse(helper.stripper.StripInvalidVariants_Shadows(ref helper.data));
            helper.IsFalse(helper.stripper.StripInvalidVariants(ref helper.data));
        }



        /*****************************************************
         * Unsupported Variants
         *****************************************************/

        [TestCase(null, false, false, false)]
        [TestCase("", false, false, false)]
        [TestCase("Universal Render Pipeline/Lit", true, true, true)]
        [TestCase("Universal Render Pipeline/Simple Lit", true, true, true)]
        [TestCase("Universal Render Pipeline/Unlit", true, true, true)]
        [TestCase("Universal Render Pipeline/Terrain/Lit", true, true, true)]
        [TestCase("Universal Render Pipeline/Particles/Lit", true, true, true)]
        [TestCase("Universal Render Pipeline/Particles/Simple Lit", true, true, true)]
        [TestCase("Universal Render Pipeline/Particles/Unlit", true, true, true)]
        [TestCase("Universal Render Pipeline/Baked Lit", true, true, true)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7", true, true, true)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7 Billboard", true, true, true)]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree8_PBRLit", true, true, true)]
        [TestCase("Universal Render Pipeline/Complex Lit", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/BokehDepthOfField", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/GaussianDepthOfField", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionBlur", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/PaniniProjection", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/Bloom", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/StencilDeferred", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/UberPost", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceShadows", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/DBufferClear", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionVectors", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/CopyDepth", true, true, true)]
        [TestCase("Hidden/Universal Render Pipeline/SubpixelMorphologicalAntialiasing", true, true, true)]
        public void TestStripUnsupportedVariants(string shaderName, bool expectedDirLightmap, bool expectedLightmapProbes, bool expectedEditorVizualization)
        {
            Shader shader = Shader.Find(shaderName);

            StripUnsupportedVariants_DirectionalLightmap(shader, expectedDirLightmap);
            StripUnsupportedVariants_EditorVisualization(shader, expectedEditorVizualization);
        }

        public void StripUnsupportedVariants_DirectionalLightmap(Shader shader, bool expectedDirLightmap)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnsupportedVariants_DirectionalLightmap(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnsupportedVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.DIRLIGHTMAP_COMBINED};
            helper.AreEqual(expectedDirLightmap, helper.stripper.StripUnsupportedVariants_DirectionalLightmap(ref helper.data));
            helper.AreEqual(expectedDirLightmap, helper.stripper.StripUnsupportedVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.DIRLIGHTMAP_COMBINED, ShaderKeywordStrings.LIGHTMAP_ON};
            helper.IsFalse(helper.stripper.StripUnsupportedVariants_DirectionalLightmap(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnsupportedVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>{ShaderKeywordStrings.DIRLIGHTMAP_COMBINED, ShaderKeywordStrings.DYNAMICLIGHTMAP_ON};
            helper.IsFalse(helper.stripper.StripUnsupportedVariants_DirectionalLightmap(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnsupportedVariants(ref helper.data));
        }

        public void StripUnsupportedVariants_EditorVisualization(Shader shader, bool expectedEditorVizualization)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnsupportedVariants_EditorVisualization(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnsupportedVariants(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.EDITOR_VISUALIZATION };
            helper.AreEqual(expectedEditorVizualization, helper.stripper.StripUnsupportedVariants_EditorVisualization(ref helper.data));
            helper.AreEqual(expectedEditorVizualization, helper.stripper.StripUnsupportedVariants(ref helper.data));
        }


        /*****************************************************
         * Unused Features
         *****************************************************/

        [TestCase(null)]
        [TestCase("")]
        [TestCase("Universal Render Pipeline/Lit")]
        [TestCase("Universal Render Pipeline/Simple Lit")]
        [TestCase("Universal Render Pipeline/Unlit")]
        [TestCase("Universal Render Pipeline/Terrain/Lit")]
        [TestCase("Universal Render Pipeline/Particles/Lit")]
        [TestCase("Universal Render Pipeline/Particles/Simple Lit")]
        [TestCase("Universal Render Pipeline/Particles/Unlit")]
        [TestCase("Universal Render Pipeline/Baked Lit")]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7")]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree7 Billboard")]
        [TestCase("Universal Render Pipeline/Nature/SpeedTree8_PBRLit")]
        [TestCase("Universal Render Pipeline/Complex Lit")]
        [TestCase("Hidden/Universal Render Pipeline/BokehDepthOfField")]
        [TestCase("Hidden/Universal Render Pipeline/GaussianDepthOfField")]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionBlur")]
        [TestCase("Hidden/Universal Render Pipeline/PaniniProjection")]
        [TestCase("Hidden/Universal Render Pipeline/Bloom")]
        [TestCase("Hidden/Universal Render Pipeline/StencilDeferred")]
        [TestCase("Hidden/Universal Render Pipeline/UberPost")]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceShadows")]
        [TestCase("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion")]
        [TestCase("Hidden/Universal Render Pipeline/DBufferClear")]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionVectors")]
        [TestCase("Hidden/Universal Render Pipeline/CopyDepth")]
        [TestCase("Hidden/Universal Render Pipeline/SubpixelMorphologicalAntialiasing")]
        [TestCase("Hidden/Universal Render Pipeline/LensFlareDataDriven")]
        [TestCase("Hidden/Universal Render Pipeline/LensFlareScreenSpace")]
        [TestCase("Hidden/Universal Render Pipeline/XR/XROcclusionMesh")]
        [TestCase("Hidden/Universal Render Pipeline/XR/XRMirrorView")]
        [TestCase("Hidden/Universal Render Pipeline/XR/XRMotionVector")]
        public void TestStripUnusedFeatures(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);

            TestStripUnusedFeatures_DebugDisplay(shader);
            TestStripUnusedFeatures_ScreenCoordOverride(shader);
            TestStripUnusedFeatures_PunctualLightShadows(shader);
            TestStripUnusedFeatures_FoveatedRendering(shader);
            TestStripUnusedFeatures_DeferredRendering(shader);
            TestStripUnusedFeatures_MainLightShadows(shader);
            TestStripUnusedFeatures_AdditionalLightShadows(shader);
            TestStripUnusedFeatures_MixedLighting(shader);
            TestStripUnusedFeatures_SoftShadows(shader);
            TestStripUnusedFeatures_HDRGrading(shader);
            TestStripUnusedFeatures_UseFastSRGBLinearConversion(shader);
            TestStripUnusedFeatures_LightLayers(shader);
            TestStripUnusedFeatures_RenderPassEnabled(shader);
            TestStripUnusedFeatures_ReflectionProbes(shader);
            TestStripUnusedFeatures_ForwardPlus(shader);
            TestStripUnusedFeatures_AdditionalLights(shader);
            TestStripUnusedFeatures_ScreenSpaceOcclusion(shader);
            TestStripUnusedFeatures_DecalsDbuffer(shader);
            TestStripUnusedFeatures_DecalsNormalBlend(shader);
            TestStripUnusedFeatures_DecalLayers(shader);
            TestStripUnusedFeatures_WriteRenderingLayers(shader);
            TestStripUnusedFeatures_AccurateGbufferNormals(shader);
            TestStripUnusedFeatures_LightCookies(shader);
            TestStripUnusedFeatures_ProbesVolumes(shader);
            TestStripUnusedFeatures_SHAuto(shader);
            TestStripUnusedFeatures_DataDrivenLensFlare(shader);
            TestStripUnusedFeatures_ScreenSpaceLensFlare(shader);
            TestStripUnusedFeatures_XR(shader);
        }

        public void TestStripUnusedFeatures_DebugDisplay(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripDebugDisplayShaders = false;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DEBUG_DISPLAY};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DebugDisplay(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripDebugDisplayShaders = false;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DEBUG_DISPLAY};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DEBUG_DISPLAY};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DebugDisplay(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripDebugDisplayShaders = true;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DEBUG_DISPLAY};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DebugDisplay(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripDebugDisplayShaders = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DEBUG_DISPLAY};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DEBUG_DISPLAY};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DebugDisplay(ref helper.data));
        }

        public void TestStripUnusedFeatures_ScreenCoordOverride(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripScreenCoordOverrideVariants = false;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.SCREEN_COORD_OVERRIDE};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenCoordOverride(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripScreenCoordOverrideVariants = false;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.SCREEN_COORD_OVERRIDE};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.SCREEN_COORD_OVERRIDE};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenCoordOverride(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripScreenCoordOverrideVariants = true;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.SCREEN_COORD_OVERRIDE};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenCoordOverride(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.stripScreenCoordOverrideVariants = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.SCREEN_COORD_OVERRIDE};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.SCREEN_COORD_OVERRIDE};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ScreenCoordOverride(ref helper.data));
        }

        public void TestStripUnusedFeatures_PunctualLightShadows(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passType = PassType.ForwardBase;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passType = PassType.ShadowCaster;
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows);
            helper.data.passType = PassType.ShadowCaster;
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows);
            helper.data.passType = PassType.ShadowCaster;
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadowsCascade);
            helper.data.passType = PassType.ShadowCaster;
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceShadows);
            helper.data.passType = PassType.ShadowCaster;
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));



            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows | ShaderFeatures.AdditionalLightShadows);
            helper.data.passType = PassType.ShadowCaster;
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows | ShaderFeatures.AdditionalLightShadows);
            helper.data.passType = PassType.ShadowCaster;
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.CastingPunctualLightShadow };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_PunctualLightShadows(ref helper.data));
        }

        public void TestStripUnusedFeatures_FoveatedRendering(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.None;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_FoveatedRendering(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.None;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.FoveatedRenderingNonUniformRaster};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_FoveatedRendering(ref helper.data));
        }

        public void TestStripUnusedFeatures_SHAuto(Shader shader)
        {
            TestHelper helper;
            List<string> shShaderKeywords = new List<string>() { ShaderKeywordStrings.EVALUATE_SH_VERTEX, ShaderKeywordStrings.EVALUATE_SH_MIXED };

            // None, should not strip any variant(stripping handled by ShaderKeywordFilter system instead).
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.EVALUATE_SH_VERTEX };
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.EVALUATE_SH_MIXED };
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.AutoSHMode | AutoSHModePerVertex
            helper = new TestHelper(shader, ShaderFeatures.AutoSHMode | ShaderFeatures.AutoSHModePerVertex);
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AutoSHMode | ShaderFeatures.AutoSHModePerVertex);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.EVALUATE_SH_VERTEX };
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AutoSHMode | ShaderFeatures.AutoSHModePerVertex);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.EVALUATE_SH_MIXED };
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));


            // ShaderFeatures.AutoSHMode
            helper = new TestHelper(shader, ShaderFeatures.AutoSHMode);
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AutoSHMode);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.EVALUATE_SH_VERTEX };
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AutoSHMode);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.EVALUATE_SH_MIXED };
            TestHelper.s_PassKeywords = shShaderKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_SHAuto(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_ScreenSpaceLensFlare(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceLensFlare);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenSpaceLensFlare(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            bool isLensFlareScreenSpace = shader != null && shader.name == "Hidden/Universal Render Pipeline/LensFlareScreenSpace";
            //We should strip the shader only if it's the lens flare one.
            helper.IsTrue(isLensFlareScreenSpace ? helper.stripper.StripUnusedFeatures_ScreenSpaceLensFlare(ref helper.data) : !helper.stripper.StripUnusedFeatures_ScreenSpaceLensFlare(ref helper.data));
        }

        public void TestStripUnusedFeatures_DataDrivenLensFlare(Shader shader)
        {

            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.DataDrivenLensFlare);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DataDrivenLensFlare(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            bool isLensFlareDataDriven = shader != null && shader.name == "Hidden/Universal Render Pipeline/LensFlareDataDriven";
            //We should strip the shader only if it's the lens flare one.
            helper.IsTrue(isLensFlareDataDriven ? helper.stripper.StripUnusedFeatures_DataDrivenLensFlare(ref helper.data) : !helper.stripper.StripUnusedFeatures_DataDrivenLensFlare(ref helper.data));
        }

        public void TestStripUnusedFeatures_XR(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None, stripUnusedXRVariants: false);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_XROcclusionMesh(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedFeatures_XRMirrorView(ref helper.data));
            helper.IsFalse(helper.stripper.StripUnusedFeatures_XRMotionVector(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, stripUnusedXRVariants: true);
            bool isXROcclusion = shader != null && shader.name == "Hidden/Universal Render Pipeline/XR/XROcclusionMesh";
            bool isXRMirror = shader != null && shader.name == "Hidden/Universal Render Pipeline/XR/XRMirrorView";
            bool isXRMotionVector = shader != null && shader.name == "Hidden/Universal Render Pipeline/XR/XRMotionVector";

            //We should strip the shader only if it's the XR shader.
            helper.IsTrue(isXROcclusion ? helper.stripper.StripUnusedFeatures_XROcclusionMesh(ref helper.data) : !helper.stripper.StripUnusedFeatures_XROcclusionMesh(ref helper.data));
            helper.IsTrue(isXRMirror ? helper.stripper.StripUnusedFeatures_XRMirrorView(ref helper.data) : !helper.stripper.StripUnusedFeatures_XRMirrorView(ref helper.data));
            helper.IsTrue(isXRMotionVector ? helper.stripper.StripUnusedFeatures_XRMotionVector(ref helper.data) : !helper.stripper.StripUnusedFeatures_XRMotionVector(ref helper.data));

        }

        public void TestStripUnusedFeatures_DeferredRendering(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameUniversal2D;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DeferredRendering(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            helper.IsTrue(helper.stripper.StripUnusedFeatures_DeferredRendering(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DeferredShading);
            helper.data.passName = ShaderScriptableStripper.kPassNameUniversal2D;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DeferredRendering(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.DeferredShading);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DeferredRendering(ref helper.data));
        }

        public void TestStripUnusedFeatures_MainLightShadows(Shader shader)
        {
            TestHelper helper;
            List<string> mainLightShadowKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };

            helper = new TestHelper(shader, ShaderFeatures.None, stripUnusedVariants:false);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // None
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // MainLightShadows
            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // MainLightShadowsCascade
            helper = new TestHelper(shader, ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // ScreenSpaceShadows
            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));


            // ShadowsKeepOffVariants

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.ShadowsKeepOffVariants
            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadows
            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadows);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadowsCascade
            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.MainLightShadowsCascade);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.
            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.ScreenSpaceShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            TestHelper.s_PassKeywords = mainLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MainLightShadows(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_AdditionalLightShadows(Shader shader)
        {
            TestHelper helper;
            List<string> additionalLightShadowKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightShadows };

            // None
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightShadows };
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));

            // AdditionalLightShadows
            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightShadows);
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightShadows };
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));


            // ShadowsKeepOffVariants

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants);
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightShadows };
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.AdditionalLightShadows);
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ShadowsKeepOffVariants | ShaderFeatures.AdditionalLightShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightShadows };
            TestHelper.s_PassKeywords = additionalLightShadowKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLightShadows(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_MixedLighting(Shader shader)
        {
            TestHelper helper;

            // None
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MixedLightingSubtractive };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.LightmapShadowMixing };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ShadowsShadowMask };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));

            // MixedLighting
            helper = new TestHelper(shader, ShaderFeatures.MixedLighting);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MixedLighting);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MixedLightingSubtractive };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MixedLighting);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.LightmapShadowMixing };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.MixedLighting);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ShadowsShadowMask };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_MixedLighting(ref helper.data));
        }

        public void TestStripUnusedFeatures_SoftShadows(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SoftShadows(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.SoftShadows };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_SoftShadows(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.SoftShadows);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SoftShadows(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.SoftShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.SoftShadows };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SoftShadows(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.SoftShadows);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.SoftShadowsLow };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SoftShadows(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SoftShadowsQualityLevels(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {  ShaderKeywordStrings.SoftShadowsLow };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_SoftShadowsQualityLevels(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.SoftShadowsLow);
            TestHelper.s_EnabledKeywords = new List<string>() {  ShaderKeywordStrings.SoftShadowsLow };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_SoftShadowsQualityLevels(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.SoftShadowsLow);
            TestHelper.s_EnabledKeywords = new List<string>() {  ShaderKeywordStrings.SoftShadowsLow};
            helper.data.stripSoftShadowQualityLevels = true;
            helper.IsTrue(helper.stripper.StripUnusedFeatures_SoftShadowsQualityLevels(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.SoftShadowsLow);
            helper.data.stripSoftShadowQualityLevels = true;
            helper.IsTrue(helper.stripper.StripUnusedFeatures_SoftShadowsQualityLevels(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_HDRGrading(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_HDRGrading(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.HDRGrading };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_HDRGrading(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.HdrGrading);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_HDRGrading(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.HdrGrading);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.HDRGrading };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_HDRGrading(ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_UseFastSRGBLinearConversion(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_UseFastSRGBLinearConversion(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.UseFastSRGBLinearConversion };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_UseFastSRGBLinearConversion(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.UseFastSRGBLinearConversion);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_UseFastSRGBLinearConversion(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.UseFastSRGBLinearConversion);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.UseFastSRGBLinearConversion };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_UseFastSRGBLinearConversion(ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_LightLayers(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_LightLayers(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.LightLayers };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_LightLayers(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.LightLayers);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_LightLayers(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.LightLayers);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.LightLayers };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_LightLayers(ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_RenderPassEnabled(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_RenderPassEnabled(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.RenderPassEnabled };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_RenderPassEnabled(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.RenderPassEnabled);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_RenderPassEnabled(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.RenderPassEnabled);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.RenderPassEnabled };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_RenderPassEnabled(ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_ReflectionProbes(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ReflectionProbeBlending };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ReflectionProbeBoxProjection };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));


            helper = new TestHelper(shader, ShaderFeatures.ReflectionProbeBlending);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ReflectionProbeBlending);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ReflectionProbeBlending };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ReflectionProbeBlending);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ReflectionProbeBoxProjection };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));


            helper = new TestHelper(shader, ShaderFeatures.ReflectionProbeBoxProjection);
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ReflectionProbeBoxProjection);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ReflectionProbeBlending };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ReflectionProbeBoxProjection);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ReflectionProbeBoxProjection };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ReflectionProbes(ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_ForwardPlus(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.ForwardPlus };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ForwardPlus(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ForwardPlus };
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.ForwardPlus };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ForwardPlus(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ForwardPlus);
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.ForwardPlus };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ForwardPlus(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ForwardPlus);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ForwardPlus };
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.ForwardPlus };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ForwardPlus(ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_AdditionalLights(Shader shader)
        {
            TestHelper helper;
            List<string> additionalLightKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };

            // None
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            // AdditionalLightsVertex
            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsVertex);
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsVertex);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsVertex);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            // AdditionalLightsPixel
            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsPixel);
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsPixel);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsPixel);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));


            // AdditionalLightsKeepOffVariants

            // AdditionalLightsKeepOffVariants
            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants);
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            // AdditionalLightsKeepOffVariants & AdditionalLightsVertex
            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants | ShaderFeatures.AdditionalLightsVertex);
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants | ShaderFeatures.AdditionalLightsVertex);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants | ShaderFeatures.AdditionalLightsVertex);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            // AdditionalLightsKeepOffVariants & AdditionalLightsPixel
            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants | ShaderFeatures.AdditionalLightsPixel);
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants | ShaderFeatures.AdditionalLightsPixel);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AdditionalLightsKeepOffVariants | ShaderFeatures.AdditionalLightsPixel);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));


            // Forward Plus
            helper = new TestHelper(shader, ShaderFeatures.ForwardPlus);
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ForwardPlus);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ForwardPlus);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            TestHelper.s_PassKeywords = additionalLightKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AdditionalLights(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_ScreenSpaceOcclusion(Shader shader)
        {
            TestHelper helper;

            // None
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));

            // ScreenSpaceOcclusion
            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceOcclusion);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceOcclusion);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));

            // After Opaque..
            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceOcclusionAfterOpaque);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceOcclusionAfterOpaque);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));

            // ScreenSpaceOcclusion
            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceOcclusionAfterOpaque | ShaderFeatures.ScreenSpaceOcclusion);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ScreenSpaceOcclusionAfterOpaque | ShaderFeatures.ScreenSpaceOcclusion);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.ScreenSpaceOcclusion};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ScreenSpaceOcclusion(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_DecalsDbuffer(Shader shader)
        {
            TestHelper helper;
            List<string> passKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3};

            // No Features + Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            // DBufferMRT1 + Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT1);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            // DBufferMRT2 + Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT2);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            // DBufferMRT3 + Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DBufferMRT3);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            // No Features + GL Device
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsDbuffer(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_DecalsNormalBlend(Shader shader)
        {
            TestHelper helper;
            List<string> passKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendLow, ShaderKeywordStrings.DecalNormalBlendMedium, ShaderKeywordStrings.DecalNormalBlendHigh};

            // ShaderFeatures.None
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendLow};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendMedium};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendHigh};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.DecalNormalBlendLow
            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendLow);
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendLow);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendLow};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendLow);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendMedium};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendLow);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendHigh};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.DecalNormalBlendMedium
            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendMedium);
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendMedium);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendLow};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendMedium);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendMedium};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendMedium);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendHigh};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            // ShaderFeatures.DecalNormalBlendHigh
            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendHigh);
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendHigh);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendLow};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendHigh);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendMedium};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalNormalBlendHigh);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalNormalBlendHigh};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalsNormalBlend(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_DecalLayers(Shader shader)
        {
            TestHelper helper;

            // Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.AreEqual(shader != null,helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalLayers);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.AreEqual(shader != null,helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalLayers);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));

            // GL Device
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.AreEqual(shader != null,helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalLayers);
            helper.data.isGLDevice = true;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.DecalLayers);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.DecalLayers};
            helper.AreEqual(shader != null,helper.stripper.StripUnusedFeatures_DecalLayers(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_WriteRenderingLayers(Shader shader)
        {
            TestHelper helper;

            // No Features + Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameDepthNormals;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameDepthNormals;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameForwardLit;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameForwardLit;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            // OpaqueWriteRenderingLayers + Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameDepthNormals;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameDepthNormals;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameForwardLit;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameForwardLit;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.OpaqueWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            // GBufferWriteRenderingLayers + Not GL Device
            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameDepthNormals;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameDepthNormals;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameForwardLit;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameForwardLit;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.GBufferWriteRenderingLayers);
            helper.data.passName = ShaderScriptableStripper.kPassNameGBuffer;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));


            // GL Device
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.isGLDevice = true;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings.WriteRenderingLayers};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_WriteRenderingLayers(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_AccurateGbufferNormals(Shader shader)
        {
            TestHelper helper;

            // Not Vulkan
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.None;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.None;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AccurateGbufferNormals);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.None;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AccurateGbufferNormals);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.None;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));

            // Vulkan
            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.Vulkan;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.Vulkan;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AccurateGbufferNormals);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.Vulkan;
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.AccurateGbufferNormals);
            helper.data.shaderCompilerPlatform = ShaderCompilerPlatform.Vulkan;
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            TestHelper.s_PassKeywords = new List<string>() {ShaderKeywordStrings._GBUFFER_NORMALS_OCT};
            helper.IsFalse(helper.stripper.StripUnusedFeatures_AccurateGbufferNormals(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_LightCookies(Shader shader)
        {
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.LightCookies };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_LightCookies(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.LightCookies };
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.LightCookies };
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_LightCookies(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.LightCookies);
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.LightCookies };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_LightCookies(ref helper.data, ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.LightCookies);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.LightCookies };
            TestHelper.s_PassKeywords = new List<string>() { ShaderKeywordStrings.LightCookies };
            helper.IsFalse(helper.stripper.StripUnusedFeatures_LightCookies(ref helper.data, ref helper.featureStripTool));
        }

        public void TestStripUnusedFeatures_ProbesVolumes(Shader shader)
        {
            TestHelper helper;
            List<string> passKeywords = new List<string>() { ShaderKeywordStrings.ProbeVolumeL1, ShaderKeywordStrings.ProbeVolumeL2 };

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ProbeVolumeL1 };
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ProbeVolumeL2 };
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            // L1 Enabled
            helper = new TestHelper(shader, ShaderFeatures.ProbeVolumeL1);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ProbeVolumeL1);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ProbeVolumeL1 };
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ProbeVolumeL1);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ProbeVolumeL2 };
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            // L2 Enabled
            helper = new TestHelper(shader, ShaderFeatures.ProbeVolumeL2);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ProbeVolumeL2);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ProbeVolumeL1 };
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(shader != null, helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));

            helper = new TestHelper(shader, ShaderFeatures.ProbeVolumeL2);
            TestHelper.s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ProbeVolumeL2 };
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripUnusedFeatures_ProbesVolumes(ref helper.featureStripTool));
        }


        [TestCase(null)]
        [TestCase("")]
        [TestCase("Hidden/Universal Render Pipeline/UberPost")]
        [TestCase("Hidden/Universal Render Pipeline/BokehDepthOfField")]
        [TestCase("Hidden/Universal Render Pipeline/GaussianDepthOfField")]
        [TestCase("Hidden/Universal Render Pipeline/CameraMotionBlur")]
        [TestCase("Hidden/Universal Render Pipeline/PaniniProjection")]
        [TestCase("Hidden/Universal Render Pipeline/Bloom")]
        public void TestStripVolumeFeatures(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            TestStripVolumeFeatures_UberPostShader(shader);
            TestStripVolumeFeatures_BokehDepthOfFieldShader(shader);
            TestStripVolumeFeatures_GaussianDepthOfFieldShader(shader);
            TestStripVolumeFeatures_CameraMotionBlurShader(shader);
            TestStripVolumeFeatures_PaniniProjectionShader(shader);
            TestStripVolumeFeatures_BloomShader(shader);
        }

        public void TestStripVolumeFeatures_UberPostShader(Shader shader)
        {
            bool isCorrectShader = shader == Shader.Find("Hidden/Universal Render Pipeline/UberPost");
            TestHelper helper;
            List<string> passKeywords = new List<string>()
            {
                ShaderKeywordStrings.Distortion,
                ShaderKeywordStrings.ChromaticAberration,
                ShaderKeywordStrings.BloomLQ,
                ShaderKeywordStrings.BloomHQ,
                ShaderKeywordStrings.BloomLQDirt,
                ShaderKeywordStrings.BloomHQDirt,
                ShaderKeywordStrings.TonemapACES,
                ShaderKeywordStrings.TonemapNeutral,
                ShaderKeywordStrings.FilmGrain,
            };

            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Lens Distortion
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.Distortion};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.LensDistortion);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.LensDistortion);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.Distortion};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Chromatic Aberration
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.ChromaticAberration};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.ChromaticAberration);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.ChromaticAberration);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.ChromaticAberration};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Bloom LQ
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomLQ};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomLQ};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Bloom HQ
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomHQ};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomHQ};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Bloom LQ Dirt
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomLQDirt};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomLQDirt};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Bloom HQ Dirt
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomHQDirt};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.BloomHQDirt};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Tonemap ACES
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.TonemapACES};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.ToneMapping);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.ToneMapping);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.TonemapACES};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Tonemap Neutral
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.TonemapNeutral};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.ToneMapping);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.ToneMapping);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.TonemapNeutral};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            // Film Grain
            helper = new TestHelper(shader, ShaderFeatures.None);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.FilmGrain};
            TestHelper.s_PassKeywords = passKeywords;
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.FilmGrain);
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.FilmGrain);
            TestHelper.s_EnabledKeywords = new List<string>() {ShaderKeywordStrings.FilmGrain};
            TestHelper.s_PassKeywords = passKeywords;
            helper.IsFalse(helper.stripper.StripVolumeFeatures_UberPostShader(ref helper.data));
        }

        public void TestStripVolumeFeatures_BokehDepthOfFieldShader(Shader shader)
        {
            bool isCorrectShader = shader == Shader.Find("Hidden/Universal Render Pipeline/BokehDepthOfField");
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_BokehDepthOfFieldShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.DepthOfField);
            helper.IsFalse(helper.stripper.StripVolumeFeatures_BokehDepthOfFieldShader(ref helper.data));
        }

        public void TestStripVolumeFeatures_GaussianDepthOfFieldShader(Shader shader)
        {
            bool isCorrectShader = shader == Shader.Find("Hidden/Universal Render Pipeline/GaussianDepthOfField");
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_GaussianDepthOfFieldShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.DepthOfField);
            helper.IsFalse(helper.stripper.StripVolumeFeatures_GaussianDepthOfFieldShader(ref helper.data));
        }

        public void TestStripVolumeFeatures_CameraMotionBlurShader(Shader shader)
        {
            bool isCorrectShader = shader == Shader.Find("Hidden/Universal Render Pipeline/CameraMotionBlur");
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_CameraMotionBlurShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.CameraMotionBlur);
            helper.IsFalse(helper.stripper.StripVolumeFeatures_CameraMotionBlurShader(ref helper.data));
        }

        public void TestStripVolumeFeatures_PaniniProjectionShader(Shader shader)
        {
            bool isCorrectShader = shader == Shader.Find("Hidden/Universal Render Pipeline/PaniniProjection");
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_PaniniProjectionShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:VolumeFeatures.PaniniProjection);
            helper.IsFalse(helper.stripper.StripVolumeFeatures_PaniniProjectionShader(ref helper.data));
        }

        public void TestStripVolumeFeatures_BloomShader(Shader shader)
        {
            bool isCorrectShader = shader == Shader.Find("Hidden/Universal Render Pipeline/Bloom");
            TestHelper helper;

            helper = new TestHelper(shader, ShaderFeatures.None);
            helper.AreEqual(isCorrectShader, helper.stripper.StripVolumeFeatures_BloomShader(ref helper.data));

            helper = new TestHelper(shader, ShaderFeatures.None, volumeFeatures:TestHelper.s_AllBloomFeatures);
            helper.IsFalse(helper.stripper.StripVolumeFeatures_BloomShader(ref helper.data));
        }
    }
}
