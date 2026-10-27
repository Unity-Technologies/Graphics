using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowUtilsTests
    {
        const float k_Tolerance = 1e-6f;

        [Description("The legacy mode always uses the fixed 2.5 slope, whatever the bias value or kernel radius. The slope-scale mode maps the normalized bias to a slope between the fixed legacy slope and 5 times the kernel radius. The 7.0 case covers values authored in the legacy mode, where the range goes up to 10. They saturate at the maximum slope instead of scaling past it.")]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.0f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.5f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 7.0f, 3.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.5f, 1.0f, 3.75f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 1.0f, 5.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 7.0f, 1.0f, 5.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, -1.0f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 1.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 2.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 3.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 3.5f, 17.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 2.5f, 12.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 1.5f, 7.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.25f, 3.5f, 6.25f)]
        public void GetSlopeScaleDepthBias_MapsModeBiasAndKernelRadiusToRasterizerSlope(ShadowDepthBiasMode depthBiasMode, float normalizedSlopeScale, float softShadowKernelRadius, float expected)
        {
            Assert.That(ShadowUtils.GetSlopeScaleDepthBias(normalizedSlopeScale, depthBiasMode, softShadowKernelRadius), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("The legacy mode pushes casters away from the light by the bias value in texels. The slope-scale mode keeps a fixed 0.05 texel push while the caster normal bias mode is active, because the slope term and the caster normal bias both vanish on receivers facing the light head-on. The receiver normal bias mode covers those receivers itself, so the push is dropped there.")]
        [TestCase(ShadowDepthBiasMode.Legacy, ShadowNormalBiasMode.Caster, 1.0f, 2.0f, -2.0f)]
        [TestCase(ShadowDepthBiasMode.Legacy, ShadowNormalBiasMode.Receiver, 0.05f, 2.0f, -0.1f)]
        [TestCase(ShadowDepthBiasMode.Legacy, ShadowNormalBiasMode.Caster, 0.0f, 2.0f, 0.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, ShadowNormalBiasMode.Caster, 0.0f, 2.0f, -0.1f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, ShadowNormalBiasMode.Caster, 1.0f, 2.0f, -0.1f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, ShadowNormalBiasMode.Caster, 0.5f, 0.4f, -0.02f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, ShadowNormalBiasMode.Receiver, 1.0f, 2.0f, 0.0f)]
        public void GetCasterDepthBias_MapsModesAndTexelSizeToCasterOffset(ShadowDepthBiasMode depthBiasMode, ShadowNormalBiasMode normalBiasMode, float depthBias, float texelSize, float expected)
        {
            Assert.That(ShadowUtils.GetCasterDepthBias(depthBias, depthBiasMode, normalBiasMode, texelSize), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("Each soft shadow quality maps to the radius of its PCF kernel. The shadow biases scale with this radius to compensate for the wider filter.")]
        [TestCase(SoftShadowQuality.Low, 1.5f)]
        [TestCase(SoftShadowQuality.Medium, 2.5f)]
        [TestCase(SoftShadowQuality.High, 3.5f)]
        public void GetSoftShadowKernelRadius_MapsSoftShadowQualityToKernelRadius(SoftShadowQuality softShadowQuality, float expected)
        {
            Assert.That(ShadowUtils.GetSoftShadowKernelRadius(softShadowQuality), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("A light set to UsePipelineSettings takes its soft shadow quality from the pipeline asset. The bias scaling and the shader filtering then agree on the kernel.")]
        [TestCase(SoftShadowQuality.Low)]
        [TestCase(SoftShadowQuality.Medium)]
        [TestCase(SoftShadowQuality.High)]
        public void ResolveSoftShadowQuality_UsePipelineSettings_ResolvesToAssetQuality(SoftShadowQuality assetQuality)
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                asset.softShadowQuality = assetQuality;
                Assert.That(ShadowUtils.ResolveSoftShadowQuality(SoftShadowQuality.UsePipelineSettings, asset), Is.EqualTo(assetQuality));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Description("An explicit per-light soft shadow quality is used as is.")]
        [TestCase(SoftShadowQuality.Low)]
        [TestCase(SoftShadowQuality.High)]
        public void ResolveSoftShadowQuality_ExplicitQuality_PassesThrough(SoftShadowQuality softShadowQuality)
        {
            Assert.That(ShadowUtils.ResolveSoftShadowQuality(softShadowQuality, null), Is.EqualTo(softShadowQuality));
        }

        [Description("If the asset's own quality is also UsePipelineSettings, the resolved quality falls back to Low. A value below Low would reach the shader as zero, which reads as soft shadows turned off.")]
        [Test]
        public void ResolveSoftShadowQuality_AssetQualityIsUsePipelineSettings_ClampsToLow()
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                asset.softShadowQuality = SoftShadowQuality.UsePipelineSettings;
                Assert.That(ShadowUtils.ResolveSoftShadowQuality(SoftShadowQuality.UsePipelineSettings, asset), Is.EqualTo(SoftShadowQuality.Low));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Description("Without a pipeline asset the quality falls back to Medium, the default of a new asset.")]
        [Test]
        public void ResolveSoftShadowQuality_UsePipelineSettingsWithoutAsset_FallsBackToMedium()
        {
            Assert.That(ShadowUtils.ResolveSoftShadowQuality(SoftShadowQuality.UsePipelineSettings, null), Is.EqualTo(SoftShadowQuality.Medium));
        }

        [Description("A light without a UniversalAdditionalLightData component resolves its soft shadow quality from the pipeline asset, so the shader filter agrees with the kernel radius path.")]
        [Test]
        public void GetShadowStrengthAndSoftShadows_LightWithoutAdditionalData_ResolvesFromAsset()
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var go = new GameObject();
            try
            {
                asset.softShadowQuality = SoftShadowQuality.High;
                var light = go.AddComponent<Light>();
                light.shadowStrength = 0.5f;

                ShadowUtils.GetShadowStrengthAndSoftShadows(light, null, true, asset, out float shadowStrength, out float softShadows);

                Assert.That(softShadows, Is.EqualTo((int)SoftShadowQuality.High));
                Assert.That(shadowStrength, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(asset);
            }
        }

        [Description("A slope-scale bias of 0 produces exactly the fixed slope of the legacy mode, so switching an existing project to the slope-scale mode never renders a shadow map with less slope bias than before.")]
        [Test]
        public void GetSlopeScaleDepthBias_SlopeScaleAtZero_EqualsLegacySlope()
        {
            float legacySlope = ShadowUtils.GetSlopeScaleDepthBias(123.0f, ShadowDepthBiasMode.Legacy, 1.0f);
            float slopeScaleAtZero = ShadowUtils.GetSlopeScaleDepthBias(0.0f, ShadowDepthBiasMode.SlopeScale, 1.0f);
            Assert.That(slopeScaleAtZero, Is.EqualTo(legacySlope));
        }

        [Description("For every soft shadow kernel the slope-scale floor equals the legacy slope, so no shadow map gets less slope bias than the legacy mode at any slider value.")]
        [TestCase(1.0f)]
        [TestCase(1.5f)]
        [TestCase(2.5f)]
        [TestCase(3.5f)]
        public void GetSlopeScaleDepthBias_FloorNeverBelowLegacySlope(float softShadowKernelRadius)
        {
            float legacySlope = ShadowUtils.GetSlopeScaleDepthBias(0.0f, ShadowDepthBiasMode.Legacy, softShadowKernelRadius);
            float slopeScaleFloor = ShadowUtils.GetSlopeScaleDepthBias(0.0f, ShadowDepthBiasMode.SlopeScale, softShadowKernelRadius);
            Assert.That(slopeScaleFloor, Is.EqualTo(legacySlope).Within(k_Tolerance));
        }

        [Description("The receiver-side offset is 0 in the caster mode, where the normal bias is applied to the caster geometry instead. In the receiver mode the sampling position moves by the bias scaled by the world-space texel size and the soft shadow kernel radius.")]
        [TestCase(ShadowNormalBiasMode.Caster, 1.0f, 1.0f, 1.0f, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Caster, 7.0f, 0.5f, 3.5f, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 0.0f, 1.0f, 1.0f, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, 1.0f, 1.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 0.25f, 1.0f, 0.25f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, 3.5f, 3.5f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 0.5f, 0.03f, 2.5f, 0.0375f)]
        public void GetReceiverNormalBias_MapsModeBiasTexelSizeAndKernelRadiusToWorldSpaceOffset(ShadowNormalBiasMode normalBiasMode, float normalBias, float texelSize, float softShadowKernelRadius, float expected)
        {
            Assert.That(ShadowUtils.GetReceiverNormalBias(normalBias, normalBiasMode, texelSize, softShadowKernelRadius), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("In the caster mode the caster vertices move against the normal by bias times world-space texel size, so the sign is negative. Point lights are kept at 0 to match the Built-in pipeline. In the receiver mode the channel is zeroed because the offset moves to the sampling side.")]
        [TestCase(ShadowNormalBiasMode.Caster, 1.0f, 1.0f, LightType.Directional, -1.0f)]
        [TestCase(ShadowNormalBiasMode.Caster, 2.0f, 0.5f, LightType.Spot, -1.0f)]
        [TestCase(ShadowNormalBiasMode.Caster, 1.0f, 1.0f, LightType.Point, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, LightType.Directional, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, LightType.Point, 0.0f)]
        public void GetCasterNormalBias_MapsModeBiasAndLightTypeToCasterOffset(ShadowNormalBiasMode normalBiasMode, float normalBias, float texelSize, LightType lightType, float expected)
        {
            Assert.That(ShadowUtils.GetCasterNormalBias(normalBias, normalBiasMode, texelSize, lightType), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("Point lights never get a caster-side normal bias, matching the Built-in pipeline. The receiver channel has no such exception, so a point light's normal bias only takes effect in the receiver mode.")]
        [Test]
        public void NormalBias_PointLight_OnlyTakesEffectInReceiverMode()
        {
            Assert.That(ShadowUtils.GetCasterNormalBias(1.0f, ShadowNormalBiasMode.Caster, 1.0f, LightType.Point), Is.EqualTo(0.0f).Within(k_Tolerance));
            Assert.That(ShadowUtils.GetCasterNormalBias(1.0f, ShadowNormalBiasMode.Receiver, 1.0f, LightType.Point), Is.EqualTo(0.0f).Within(k_Tolerance));
            Assert.That(ShadowUtils.GetReceiverNormalBias(1.0f, ShadowNormalBiasMode.Receiver, 1.0f, 2.5f), Is.EqualTo(2.5f).Within(k_Tolerance));
        }

        [Description("For a non-point light the receiver offset is the caster offset with the sign flipped, soft shadow kernel compensation included. Switching modes keeps the authored bias at the same strength.")]
        [Test]
        public void NormalBias_ModeSwitch_MovesTheSameOffsetBetweenChannels()
        {
            float softShadowKernelRadius = ShadowUtils.GetSoftShadowKernelRadius(SoftShadowQuality.High);
            float casterNormalBias = ShadowUtils.GetCasterNormalBias(0.7f, ShadowNormalBiasMode.Caster, 0.42f, LightType.Directional) * softShadowKernelRadius;
            float receiverNormalBias = ShadowUtils.GetReceiverNormalBias(0.7f, ShadowNormalBiasMode.Receiver, 0.42f, softShadowKernelRadius);
            Assert.That(receiverNormalBias, Is.EqualTo(-casterNormalBias).Within(k_Tolerance));
        }

        [Description("PCSS replaces the highest soft shadow quality only, so a light resolving to Low or Medium keeps the tent filter even when the asset enables PCSS.")]
        [TestCase(true, SoftShadowQuality.High, ExpectedResult = true)]
        [TestCase(true, SoftShadowQuality.Medium, ExpectedResult = false)]
        [TestCase(true, SoftShadowQuality.Low, ExpectedResult = false)]
        [TestCase(false, SoftShadowQuality.High, ExpectedResult = false)]
        public bool IsAdditionalLightPCSSEnabled_NeedsTheAssetToggleAndTheHighestQuality(bool enabledInAsset, SoftShadowQuality resolvedQuality)
        {
            if (!ShadowUtils.SupportsPerLightSoftShadowQuality())
                Assert.Ignore("XR mobile cannot select the soft shadow quality per light, so PCSS is never enabled there.");

            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                asset.additionalLightShadowsPCSS = enabledInAsset;
                return ShadowUtils.IsAdditionalLightPCSSEnabled((int)resolvedQuality, asset);
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Description("Without a pipeline asset there is no PCSS toggle to read, so the light keeps the tent filter.")]
        [Test]
        public void IsAdditionalLightPCSSEnabled_WithoutAsset_IsFalse()
        {
            Assert.That(ShadowUtils.IsAdditionalLightPCSSEnabled((int)SoftShadowQuality.High, null), Is.False);
        }

        [Description("The light inspector offers the PCSS parameters for a light that would take the PCSS path, which needs soft shadows on the light, soft shadow support in the asset, and a resolved quality of High.")]
        [TestCase(LightShadows.Soft, true, SoftShadowQuality.High, ExpectedResult = true)]
        [TestCase(LightShadows.Soft, true, SoftShadowQuality.Medium, ExpectedResult = false)]
        [TestCase(LightShadows.Soft, false, SoftShadowQuality.High, ExpectedResult = false)]
        [TestCase(LightShadows.Hard, true, SoftShadowQuality.High, ExpectedResult = false)]
        [TestCase(LightShadows.None, true, SoftShadowQuality.High, ExpectedResult = false)]
        public bool IsPCSSAvailableForLight_FollowsTheResolvedSoftShadowQuality(LightShadows shadows, bool supportsSoftShadows, SoftShadowQuality assetQuality)
        {
            if (!ShadowUtils.SupportsPerLightSoftShadowQuality())
                Assert.Ignore("XR mobile cannot select the soft shadow quality per light, so PCSS is never available there.");

            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var go = new GameObject();
            try
            {
                asset.supportsSoftShadows = supportsSoftShadows;
                asset.softShadowQuality = assetQuality;

                var light = go.AddComponent<Light>();
                light.shadows = shadows;

                return ShadowUtils.IsPCSSAvailableForLight(light, asset);
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(asset);
            }
        }

        // Mirrors LinearEyeDepth in the shader
        static float LinearEyeDepth(float deviceDepth, Vector4 zBufferParams)
        {
            return 1.0f / (zBufferParams.z * deviceDepth + zBufferParams.w);
        }

        [Description("The punctual blocker depth is linearized from the shadow slice projection, so feeding the device depth of the near and far planes back through the shader formula returns the planes of that projection.")]
        [Test]
        public void ComputeAdditionalLightPCSSParams_LinearizesTheSliceDepthRange()
        {
            const float near = 0.1f;
            const float far = 10.0f;

            ShadowUtils.ComputeAdditionalLightPCSSParams(0.0f, null, 512, Matrix4x4.Perspective(90.0f, 1.0f, near, far),
                out _, out Vector4 zBufferParams);

            float nearDeviceDepth = SystemInfo.usesReversedZBuffer ? 1.0f : 0.0f;
            float farDeviceDepth = SystemInfo.usesReversedZBuffer ? 0.0f : 1.0f;

            Assert.That(LinearEyeDepth(nearDeviceDepth, zBufferParams), Is.EqualTo(near).Within(1e-4f));
            Assert.That(LinearEyeDepth(farDeviceDepth, zBufferParams), Is.EqualTo(far).Within(1e-3f));
        }

        [Description("Zero samples would make the shader divide by the sample count, and the sample direction table only holds k_PCSSMaxSampleCount entries, so both counts are clamped into that range.")]
        [TestCase(0, 0, 1, 1)]
        [TestCase(1000, 1000, ShadowUtils.k_PCSSMaxSampleCount, ShadowUtils.k_PCSSMaxSampleCount)]
        [TestCase(8, 12, 8, 12)]
        public void ComputeAdditionalLightPCSSParams_ClampsTheSampleCounts(int blockerSampleCount, int filterSampleCount, int expectedBlocker, int expectedFilter)
        {
            var go = new GameObject();
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Spot;

                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();
                additionalLightData.softShadowPCSSSampleQuality = PCSSSampleQuality.Custom;
                additionalLightData.softShadowBlockerSampleCount = blockerSampleCount;
                additionalLightData.softShadowFilterSampleCount = filterSampleCount;

                ShadowUtils.ComputeAdditionalLightPCSSParams(light.shapeRadius, additionalLightData, 512,
                    Matrix4x4.Perspective(90.0f, 1.0f, 0.1f, 10.0f), out Vector4 pcssParams, out _);

                Assert.That(pcssParams.z, Is.EqualTo(expectedBlocker));
                Assert.That(pcssParams.w, Is.EqualTo(expectedFilter));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Description("A cascade the frame did not render keeps a zeroed atlas rectangle, which is how the shader tells it apart from the tile at the atlas origin. ComputeCascadeIndex returns the trailing slot for a pixel past the last cascade.")]
        [Test]
        public void ComputeMainLightPCSSParams_ZeroesTheCascadesThatWereNotRendered()
        {
            var cascadeSlices = new ShadowSliceData[4];
            cascadeSlices[0].projectionMatrix = Matrix4x4.Ortho(-10.0f, 10.0f, -10.0f, 10.0f, 0.0f, 100.0f);
            cascadeSlices[0].offsetX = 0;
            cascadeSlices[0].offsetY = 512;
            cascadeSlices[0].resolution = 512;

            MainLightShadowCascadeVectors params0 = default, params1 = default, atlas = default;
            ShadowUtils.ComputeMainLightPCSSParams(null, cascadeSlices, 1, 1024, 1024, ref params0, ref params1, ref atlas);

            Assert.That(atlas[0], Is.EqualTo(new Vector4(0.0f, 0.5f, 0.5f, 0.5f)));

            for (int i = 1; i < MainLightShadowCascadeVectors.count; ++i)
            {
                Assert.That(atlas[i], Is.EqualTo(Vector4.zero), "cascade {0} atlas rectangle", i);
                Assert.That(params0[i], Is.EqualTo(Vector4.zero), "cascade {0} params0", i);
                Assert.That(params1[i], Is.EqualTo(Vector4.zero), "cascade {0} params1", i);
            }
        }

        [Description("The shader converts between a depth difference and a filter radius in both directions, so the two scales must stay exact reciprocals. The radius follows the tangent of the half angular diameter, which is what makes a wider light cast a wider penumbra.")]
        [Test]
        public void ComputeMainLightPCSSParams_RadialAndDepthScalesAreReciprocal()
        {
            var go = new GameObject();
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;

                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();
                additionalLightData.softShadowAngularDiameter = 0.5f;
                additionalLightData.softShadowScale = 1.0f;

                var cascadeSlices = new ShadowSliceData[4];
                cascadeSlices[0].projectionMatrix = Matrix4x4.Ortho(-10.0f, 10.0f, -10.0f, 10.0f, 0.0f, 100.0f);
                cascadeSlices[0].resolution = 1024;

                MainLightShadowCascadeVectors params0 = default, params1 = default, atlas = default;
                ShadowUtils.ComputeMainLightPCSSParams(additionalLightData, cascadeSlices, 1, 1024, 1024, ref params0, ref params1, ref atlas);

                // 2 * |m00 / m22| of the projection above
                const float depthToRadialScale = 10.0f;
                float expectedDepth2RadialScale = Mathf.Tan(0.5f * Mathf.Deg2Rad * 0.5f) * depthToRadialScale;

                Assert.That(params0[0].x, Is.EqualTo(expectedDepth2RadialScale).Within(1e-6f));
                Assert.That(params0[0].y, Is.EqualTo(1.0f / expectedDepth2RadialScale).Within(1e-3f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Description("The authored clump exponent spreads the blocker samples over a linear range, while the shader raises an already square-rooted distance to it, so the value is halved on the way to the GPU.")]
        [Test]
        public void ComputeMainLightPCSSParams_HalvesTheAuthoredClumpExponent()
        {
            var go = new GameObject();
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;

                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();
                additionalLightData.softShadowPCSSSampleQuality = PCSSSampleQuality.Custom;
                additionalLightData.softShadowBlockerSamplingClumpExponent = 3.0f;

                var cascadeSlices = new ShadowSliceData[4];
                cascadeSlices[0].projectionMatrix = Matrix4x4.Ortho(-10.0f, 10.0f, -10.0f, 10.0f, 0.0f, 100.0f);
                cascadeSlices[0].resolution = 1024;

                MainLightShadowCascadeVectors params0 = default, params1 = default, atlas = default;
                ShadowUtils.ComputeMainLightPCSSParams(additionalLightData, cascadeSlices, 1, 1024, 1024, ref params0, ref params1, ref atlas);

                Assert.That(params1[0].w, Is.EqualTo(1.5f).Within(k_Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Description("The minimum filter radius is authored in texels of one cascade tile, so the shader is given the texel size of that tile rather than of the whole atlas.")]
        [Test]
        public void GetMainLightPCSSScalars_UsesTheTileTexelSizeAndClampsTheSampleCounts()
        {
            var go = new GameObject();
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;

                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();
                additionalLightData.softShadowPCSSSampleQuality = PCSSSampleQuality.Custom;
                additionalLightData.softShadowBlockerSampleCount = 1000;
                additionalLightData.softShadowFilterSampleCount = 0;

                Vector4 scalars = ShadowUtils.GetMainLightPCSSScalars(additionalLightData, 512);

                Assert.That(scalars.x, Is.EqualTo(1.0f / 512.0f).Within(k_Tolerance));
                Assert.That(scalars.y, Is.EqualTo(ShadowUtils.k_PCSSMaxSampleCount));
                Assert.That(scalars.z, Is.EqualTo(1));
                Assert.That(scalars.w, Is.EqualTo(0), "the temporal frame index is filled in at upload");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Description("Medium is the default for new lights and lights serialized before the preset existed, so it has to render the same as the previous default sample counts.")]
        [Test]
        public void PCSSSampleQuality_DefaultsToMediumWithTheDefaultSampleCounts()
        {
            var go = new GameObject();
            try
            {
                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();

                Assert.That(additionalLightData.softShadowPCSSSampleQuality, Is.EqualTo(PCSSSampleQuality.Medium));

                ShadowUtils.GetPCSSPresetSampleCounts(PCSSSampleQuality.Medium, out int blockerSampleCount, out int filterSampleCount);
                Assert.That(blockerSampleCount, Is.EqualTo(UniversalAdditionalLightData.k_DefaultSoftShadowBlockerSampleCount));
                Assert.That(filterSampleCount, Is.EqualTo(UniversalAdditionalLightData.k_DefaultSoftShadowFilterSampleCount));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Description("A preset ignores the sample counts stored on the light, which only apply once the light is set to Custom.")]
        [TestCase(PCSSSampleQuality.Low, 12, 8)]
        [TestCase(PCSSSampleQuality.Medium, 24, 16)]
        [TestCase(PCSSSampleQuality.High, 32, 24)]
        [TestCase(PCSSSampleQuality.Custom, 5, 7)]
        public void GetPCSSSampleCounts_ResolvesThePresetOrTheCustomValues(PCSSSampleQuality quality, int expectedBlocker, int expectedFilter)
        {
            var go = new GameObject();
            try
            {
                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();
                additionalLightData.softShadowPCSSSampleQuality = quality;
                additionalLightData.softShadowBlockerSampleCount = 5;
                additionalLightData.softShadowFilterSampleCount = 7;

                ShadowUtils.GetPCSSSampleCounts(additionalLightData, out int blockerSampleCount, out int filterSampleCount);

                Assert.That(blockerSampleCount, Is.EqualTo(expectedBlocker));
                Assert.That(filterSampleCount, Is.EqualTo(expectedFilter));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GetPCSSSampleCounts_LightWithoutAdditionalData_UsesMedium()
        {
            ShadowUtils.GetPCSSSampleCounts(null, out int blockerSampleCount, out int filterSampleCount);
            ShadowUtils.GetPCSSPresetSampleCounts(PCSSSampleQuality.Medium, out int expectedBlocker, out int expectedFilter);

            Assert.That(blockerSampleCount, Is.EqualTo(expectedBlocker));
            Assert.That(filterSampleCount, Is.EqualTo(expectedFilter));
        }

        [Description("A preset always renders with the default tuning values, so values left over from Custom cannot change the shadows while they are hidden. The angular diameter, softness scale and minimum filter size apply in every mode.")]
        [Test]
        public void ComputeMainLightPCSSParams_PresetIgnoresTheCustomTuningValues()
        {
            var go = new GameObject();
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Directional;

                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();
                additionalLightData.softShadowPCSSSampleQuality = PCSSSampleQuality.High;
                additionalLightData.softShadowMaxPenumbraSize = 2.0f;
                additionalLightData.softShadowMaxSamplingDistance = 2.0f;
                additionalLightData.softShadowMinFilterMaxAngularDiameter = 20.0f;
                additionalLightData.softShadowBlockerSearchAngularDiameter = 20.0f;
                additionalLightData.softShadowBlockerSamplingClumpExponent = 3.0f;

                var cascadeSlices = new ShadowSliceData[4];
                cascadeSlices[0].projectionMatrix = Matrix4x4.Ortho(-10.0f, 10.0f, -10.0f, 10.0f, 0.0f, 100.0f);
                cascadeSlices[0].resolution = 1024;

                MainLightShadowCascadeVectors params0 = default, params1 = default, atlas = default;
                ShadowUtils.ComputeMainLightPCSSParams(additionalLightData, cascadeSlices, 1, 1024, 1024, ref params0, ref params1, ref atlas);

                MainLightShadowCascadeVectors expectedParams0 = default, expectedParams1 = default, expectedAtlas = default;
                ShadowUtils.ComputeMainLightPCSSParams(null, cascadeSlices, 1, 1024, 1024, ref expectedParams0, ref expectedParams1, ref expectedAtlas);

                Assert.That(params0[0], Is.EqualTo(expectedParams0[0]));
                Assert.That(params1[0], Is.EqualTo(expectedParams1[0]));

                additionalLightData.softShadowAngularDiameter = 2.0f;
                ShadowUtils.ComputeMainLightPCSSParams(additionalLightData, cascadeSlices, 1, 1024, 1024, ref params0, ref params1, ref atlas);

                Assert.That(params0[0].x, Is.GreaterThan(expectedParams0[0].x), "a wider light widens the penumbra under a preset");

                additionalLightData.softShadowMinFilterSizeTexels = 4.0f;
                ShadowUtils.ComputeMainLightPCSSParams(additionalLightData, cascadeSlices, 1, 1024, 1024, ref params0, ref params1, ref atlas);

                Assert.That(params1[0].x, Is.EqualTo(4.0f), "the minimum filter size applies under a preset");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Description("The minimum filter size changes the look as much as the softness scale, so it applies under a preset as well as under Custom.")]
        [Test]
        public void ComputeAdditionalLightPCSSParams_MinFilterSizeAppliesInEveryMode()
        {
            var go = new GameObject();
            try
            {
                var light = go.AddComponent<Light>();
                light.type = LightType.Spot;

                var additionalLightData = go.AddComponent<UniversalAdditionalLightData>();
                additionalLightData.softShadowPCSSSampleQuality = PCSSSampleQuality.Low;
                additionalLightData.softShadowMinFilterSize = 1.0f;

                Matrix4x4 projection = Matrix4x4.Perspective(90.0f, 1.0f, 0.1f, 10.0f);
                ShadowUtils.ComputeAdditionalLightPCSSParams(light.shapeRadius, additionalLightData, 512, projection, out Vector4 presetParams, out _);

                additionalLightData.softShadowPCSSSampleQuality = PCSSSampleQuality.Custom;
                ShadowUtils.ComputeAdditionalLightPCSSParams(light.shapeRadius, additionalLightData, 512, projection, out Vector4 customParams, out _);

                Assert.That(presetParams.y, Is.EqualTo(0.001f).Within(k_Tolerance));
                Assert.That(customParams.y, Is.EqualTo(0.001f).Within(k_Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
