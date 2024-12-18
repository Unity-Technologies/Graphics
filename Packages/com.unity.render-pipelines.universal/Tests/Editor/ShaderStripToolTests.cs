using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SocialPlatforms;
using IShaderScriptableStrippingData = UnityEditor.Rendering.Universal.ShaderScriptableStripper.IShaderScriptableStrippingData;

namespace ShaderStrippingAndPrefiltering
{
    class ShaderStripToolTests
    {
        private static List<string> s_EnabledKeywords;
        private static List<string> s_PassKeywords;
        internal struct TestStrippingData : IShaderScriptableStrippingData
        {
            public ShaderFeatures shaderFeatures { get; set; }
            public VolumeFeatures volumeFeatures { get; set; }

            public bool isGLDevice { get; set; }
            public bool stripSoftShadowQualityLevels { get; set; }
            public bool strip2DPasses { get; set; }
            public bool stripDebugDisplayShaders { get; set; }
            public bool stripScreenCoordOverrideVariants { get; set; }
            public bool stripBicubicLightmapSamplingVariants { get; set; }
            public bool stripUnusedVariants { get; set; }
            public bool stripUnusedPostProcessingVariants { get; set; }
            public bool stripUnusedXRVariants { get; set; }

            public Shader shader { get; set; }
            public ShaderType shaderType { get; set; }
            public ShaderCompilerPlatform shaderCompilerPlatform { get; set; }

            public string passName { get; set; }
            public PassType passType { get; set; }
            public PassIdentifier passIdentifier { get; set; }

            public bool IsHDRDisplaySupportEnabled { get; set; }
            public bool IsHDRShaderVariantValid { get; set; }
            public bool IsRenderCompatibilityMode { get; set; }


            public bool IsKeywordEnabled(LocalKeyword keyword)
            {
                return s_EnabledKeywords != null && s_EnabledKeywords.Contains(keyword.name);
            }

            public bool IsShaderFeatureEnabled(ShaderFeatures feature)
            {
                return (shaderFeatures & feature) != 0;
            }

            public bool IsVolumeFeatureEnabled(VolumeFeatures feature)
            {
                return (volumeFeatures & feature) != 0;
            }

            public void ClearEnablePasses()
            {
                s_PassKeywords = null;
            }

            public bool PassHasKeyword(LocalKeyword keyword)
            {
                return s_PassKeywords != null && s_PassKeywords.Contains(keyword.name);
            }
        }

        [Test]
        public void TestContainsKeyword()
        {
            s_PassKeywords = new List<string>();
            s_EnabledKeywords = new List<string>();
            ShaderStripTool<ShaderFeatures> stripTool;
            IShaderScriptableStrippingData strippingData;
            LocalKeyword kw = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.ScreenSpaceOcclusion);
            bool actual;

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.ContainsKeyword(kw);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.ContainsKeyword(kw);
            Assert.IsTrue(actual);
        }

        [Test]
        public void TestStripMultiCompileKeepOffVariant1()
        {
            s_PassKeywords = new List<string>();
            s_EnabledKeywords = new List<string>();
            ShaderStripTool<ShaderFeatures> stripTool;
            IShaderScriptableStrippingData strippingData;
            LocalKeyword kw = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.ScreenSpaceOcclusion);
            bool actual;

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);
        }

        [Test]
        public void TestStripMultiCompile1()
        {
            s_PassKeywords = new List<string>();
            s_EnabledKeywords = new List<string>();
            ShaderStripTool<ShaderFeatures> stripTool;
            IShaderScriptableStrippingData strippingData;
            LocalKeyword kw = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.ScreenSpaceOcclusion);
            bool actual;

            // stripUnusedVariants = false; => Same as Keep Off variant
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            // stripUnusedVariants = true => takes out the Off variant
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsFalse(actual);

            // Here the OFF variant is stripped
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceOcclusion;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.ScreenSpaceOcclusion };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw, ShaderFeatures.ScreenSpaceOcclusion);
            Assert.IsTrue(actual);
        }

        [Test]
        public void TestStripMultiCompileKeepOffVariant2()
        {
            s_PassKeywords = new List<string>();
            s_EnabledKeywords = new List<string>();
            ShaderStripTool<ShaderFeatures> stripTool;
            IShaderScriptableStrippingData strippingData;
            LocalKeyword kw1 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.AdditionalLightsVertex);
            LocalKeyword kw2 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.AdditionalLightsPixel);
            bool actual;

            // All keywords disabled
            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            // With enabled keywords
            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            // Both features enabled
            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);
        }

        [Test]
        public void TestStripMultiCompile2()
        {
            s_PassKeywords = new List<string>();
            s_EnabledKeywords = new List<string>();
            ShaderStripTool<ShaderFeatures> stripTool;
            IShaderScriptableStrippingData strippingData;
            LocalKeyword kw1 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.AdditionalLightsVertex);
            LocalKeyword kw2 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.AdditionalLightsPixel);
            bool actual;

            /////////////////////////////////////
            /// Strip Unused Variants Disabled
            /////////////////////////////////////

            // All keywords disabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            // With enabled keywords
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            // Both features enabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            /////////////////////////////////////
            /// Strip Unused Variants Enabled
            /////////////////////////////////////

            // All keywords disabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            // With enabled keywords
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);

            // Both features enabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.AdditionalLightsVertex | ShaderFeatures.AdditionalLightsPixel;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.AdditionalLightsVertex, kw2, ShaderFeatures.AdditionalLightsPixel);
            Assert.IsTrue(actual);
        }

        [Test]
        public void TestStripMultiCompileKeepOffVariant3()
        {
            s_PassKeywords = new List<string>();
            s_EnabledKeywords = new List<string>();
            ShaderStripTool<ShaderFeatures> stripTool;
            IShaderScriptableStrippingData strippingData;
            LocalKeyword kw1 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.MainLightShadows);
            LocalKeyword kw2 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.MainLightShadowCascades);
            LocalKeyword kw3 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.MainLightShadowScreen);
            bool actual;

            // All keywords disabled
            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            // With enabled keywords
            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            // Two features enabled
            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);


            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            // All features enabled
            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompileKeepOffVariant(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);
        }

        [Test]
        public void TestStripMultiCompile3()
        {
            s_PassKeywords = new List<string>();
            s_EnabledKeywords = new List<string>();
            ShaderStripTool<ShaderFeatures> stripTool;
            IShaderScriptableStrippingData strippingData;
            LocalKeyword kw1 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.MainLightShadows);
            LocalKeyword kw2 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.MainLightShadowCascades);
            LocalKeyword kw3 = new (Shader.Find("Universal Render Pipeline/Lit"), ShaderKeywordStrings.MainLightShadowScreen);
            bool actual;


            /////////////////////////////////////
            /// Strip Unused Variants Disabled
            /////////////////////////////////////

            // All keywords disabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            // With enabled keywords
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            // Two features enabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);


            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            // All features enabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = false;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            /////////////////////////////////////
            /// Strip Unused Variants Enabled
            /////////////////////////////////////

            // All keywords disabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            // With enabled keywords
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.None;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);



            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords  = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);



            // Two features enabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsTrue(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);


            // All features enabled
            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            s_PassKeywords = new List<string>();
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);

            strippingData = new TestStrippingData();
            strippingData.stripUnusedVariants = true;
            strippingData.shaderFeatures = ShaderFeatures.MainLightShadows | ShaderFeatures.MainLightShadowsCascade | ShaderFeatures.ScreenSpaceShadows;
            s_EnabledKeywords = new List<string>();
            s_PassKeywords = new List<string>() { ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen };
            stripTool = new ShaderStripTool<ShaderFeatures>(strippingData.shaderFeatures, ref strippingData);
            actual = stripTool.StripMultiCompile(kw1, ShaderFeatures.MainLightShadows, kw2, ShaderFeatures.MainLightShadowsCascade, kw3, ShaderFeatures.ScreenSpaceShadows);
            Assert.IsFalse(actual);
        }
    }
}
