using UnityEditor.Experimental.Rendering.TestFramework;
using NUnit.Framework;
using System;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Experimental.Rendering.HDPipeline.Tests
{
    public class FrameSettingsTests
    {
        Object m_ToClean;

        [TearDown]
        public void TearDown()
        {
            if (m_ToClean != null)
                CoreUtils.Destroy(m_ToClean);
            FrameSettingsHistory.frameSettingsHistory.Clear();
        }

        [Test]
        public void NoDoubleBitIndex()
        {
            var values = Enum.GetValues(typeof(FrameSettingsField));
            var singleValues = (values as IEnumerable<int>).Distinct();

            //gathering helpful debug info
            var messageDuplicates = new StringBuilder();
            if (values.Length != singleValues.Count())
            {
                var names = Enum.GetNames(typeof(FrameSettingsField));
                for (int i = 0; i < values.Length - 1; ++i)
                {
                    var a = values.GetValue(i);
                    var b = values.GetValue(i + 1);
                    if ((int)values.GetValue(i) == (int)values.GetValue(i + 1))
                    {
                        messageDuplicates.AppendFormat("{{ {0}: {1}, {2}", (int)values.GetValue(i), names[i], names[i + 1]);
                        ++i;
                        while (values.GetValue(i) == values.GetValue(i + 1))
                        {
                            if (values.GetValue(i) == values.GetValue(i + 1))
                            {
                                messageDuplicates.AppendFormat(", {0}", names[i + 1]);
                                ++i;
                            }
                        }
                        messageDuplicates.Append(" }, ");
                    }
                }
            }

            Assert.AreEqual(values.Length, singleValues.Count(), String.Format("Double bit index found: {0}\nNumber of bit index against number of distinct bit index:", messageDuplicates.ToString()));
        }

        // deactivate this test for template package making issue
        //[Test]
        public void FrameSettingsAggregation()
        {
            for (int i = 0; i < 10; ++i)
            {
                //init
                FrameSettings fs = default;
                FrameSettingsOverrideMask fso = default;
                FrameSettingsRenderType defaultFSType = RandomUtilities.RandomEnumValue<FrameSettingsRenderType>(i);
                FrameSettings defaultFS;
                FrameSettings result = FrameSettings.defaultCamera;
                FrameSettings tester = default;
                RenderPipelineSettings supportedFeatures = new RenderPipelineSettings();
                switch (defaultFSType)
                {
                    case FrameSettingsRenderType.Camera:
                        defaultFS = FrameSettings.defaultCamera;
                        break;
                    case FrameSettingsRenderType.CustomOrBakedReflection:
                        defaultFS = FrameSettings.defaultCustomOrBakeReflectionProbe;
                        break;
                    case FrameSettingsRenderType.RealtimeReflection:
                        defaultFS = FrameSettings.defaultRealtimeReflectionProbe;
                        break;
                    default:
                        throw new ArgumentException("Unknown FrameSettingsRenderType");
                }

                //change randomly override values
                for (int j = 0; j < 10; ++j)
                {
                    FrameSettingsField field = RandomUtilities.RandomEnumValue<FrameSettingsField>((i + 0.5f) * (j + 0.3f));
                    fs.SetEnabled(field, RandomUtilities.RandomBool((i + 1) * j));
                    fso.mask[(uint)field] = true;
                }

                //create and init gameobjects
                var go = new GameObject("TestObject");
                m_ToClean = go;
                var cam = go.AddComponent<Camera>();

                var add = cam.GetComponent<HDAdditionalCameraData>() ?? cam.gameObject.AddComponent<HDAdditionalCameraData>();
                Assert.True(add != null && !add.Equals(null));

                add.renderingPathCustomFrameSettings = fs;
                add.renderingPathCustomFrameSettingsOverrideMask = fso;
                add.defaultFrameSettings = defaultFSType;
                add.customRenderingSettings = true;

                //gather data two different ways
                FrameSettings.AggregateFrameSettings(ref result, cam, add, ref defaultFS, supportedFeatures);

                foreach (FrameSettingsField field in Enum.GetValues(typeof(FrameSettingsField)))
                {
                    tester.SetEnabled(field, fso.mask[(uint)field] ? fs.IsEnabled(field) : defaultFS.IsEnabled(field));
                }
                FrameSettings.Sanitize(ref tester, cam, supportedFeatures);

                //test
                Assert.AreEqual(result, tester);

                Object.DestroyImmediate(go);
            }
        }

        // deactivate this test for template package making issue
        //[Test]
        public void FrameSettingsHistoryAggregation()
        {
            for (int i = 0; i < 10; ++i)
            {
                //init
                FrameSettings fs = default;
                FrameSettingsOverrideMask fso = default;
                FrameSettingsRenderType defaultFSType = RandomUtilities.RandomEnumValue<FrameSettingsRenderType>(i);
                FrameSettings defaultFS;
                FrameSettings result = FrameSettings.defaultCamera;
                FrameSettings tester = default;
                RenderPipelineSettings supportedFeatures = new RenderPipelineSettings();
                switch (defaultFSType)
                {
                    case FrameSettingsRenderType.Camera:
                        defaultFS = FrameSettings.defaultCamera;
                        break;
                    case FrameSettingsRenderType.CustomOrBakedReflection:
                        defaultFS = FrameSettings.defaultCustomOrBakeReflectionProbe;
                        break;
                    case FrameSettingsRenderType.RealtimeReflection:
                        defaultFS = FrameSettings.defaultRealtimeReflectionProbe;
                        break;
                    default:
                        throw new ArgumentException("Unknown FrameSettingsRenderType");
                }

                //change randomly override values
                for (int j = 0; j < 10; ++j)
                {
                    FrameSettingsField field = RandomUtilities.RandomEnumValue<FrameSettingsField>((i + 0.5f) * (j + 0.3f));
                    fs.SetEnabled(field, RandomUtilities.RandomBool((i + 1) * j));
                    fso.mask[(uint)field] = true;
                }

                //create and init gameobjects
                var go = new GameObject("TestObject");
                m_ToClean = go;
                var cam = go.AddComponent<Camera>();

                var add = cam.GetComponent<HDAdditionalCameraData>() ?? cam.gameObject.AddComponent<HDAdditionalCameraData>();
                Assert.True(add != null && !add.Equals(null));

                add.renderingPathCustomFrameSettings = fs;
                add.renderingPathCustomFrameSettingsOverrideMask = fso;
                add.defaultFrameSettings = defaultFSType;
                add.customRenderingSettings = true;

                //gather data two different ways
                FrameSettingsHistory.AggregateFrameSettings(ref result, cam, add, ref defaultFS, supportedFeatures);

                foreach (FrameSettingsField field in Enum.GetValues(typeof(FrameSettingsField)))
                {
                    tester.SetEnabled(field, fso.mask[(uint)field] ? fs.IsEnabled(field) : defaultFS.IsEnabled(field));
                }
                FrameSettings.Sanitize(ref tester, cam, supportedFeatures);

                //simulate debugmenu changes
                for (int j = 0; j < 10; ++j)
                {
                    FrameSettingsField field = RandomUtilities.RandomEnumValue<FrameSettingsField>((i + 0.5f) * (j + 0.3f));
                    var fsh = FrameSettingsHistory.frameSettingsHistory[cam];
                    bool debugValue = RandomUtilities.RandomBool((i + 1) * j);
                    fsh.debug.SetEnabled(field, debugValue);
                    FrameSettingsHistory.frameSettingsHistory[cam] = fsh;

                    tester.SetEnabled(field, debugValue);
                }

                //test
                result = FrameSettingsHistory.frameSettingsHistory[cam].debug;
                Assert.AreEqual(result, tester);

                Object.DestroyImmediate(go);
            }
        }

        public enum LegacyLitShaderMode
        {
            Forward,
            Deferred
        }

        public enum LegacyLightLoopSettingsOverrides
        {
            FptlForForwardOpaque = 1 << 0,
            BigTilePrepass = 1 << 1,
            ComputeLightEvaluation = 1 << 2,
            ComputeLightVariants = 1 << 3,
            ComputeMaterialVariants = 1 << 4,
            TileAndCluster = 1 << 5,
        }

        public enum LegacyFrameSettingsOverrides
        {
            //lighting settings
            Shadow = 1 << 0,
            ContactShadow = 1 << 1,
            ShadowMask = 1 << 2,
            SSR = 1 << 3,
            SSAO = 1 << 4,
            SubsurfaceScattering = 1 << 5,
            Transmission = 1 << 6,
            AtmosphericScaterring = 1 << 7,
            Volumetrics = 1 << 8,
            ReprojectionForVolumetrics = 1 << 9,
            LightLayers = 1 << 10,
            MSAA = 1 << 11,
            ExposureControl = 1 << 12,

            //rendering pass
            TransparentPrepass = 1 << 13,
            TransparentPostpass = 1 << 14,
            MotionVectors = 1 << 15,
            ObjectMotionVectors = 1 << 16,
            Decals = 1 << 17,
            RoughRefraction = 1 << 18,
            Distortion = 1 << 19,
            Postprocess = 1 << 20,

            //rendering settings
            ShaderLitMode = 1 << 21,
            DepthPrepassWithDeferredRendering = 1 << 22,
            OpaqueObjects = 1 << 24,
            TransparentObjects = 1 << 25,
            RealtimePlanarReflection = 1 << 26,

            // Async settings
            AsyncCompute = 1 << 23,
            LightListAsync = 1 << 27,
            SSRAsync = 1 << 28,
            SSAOAsync = 1 << 29,
            ContactShadowsAsync = 1 << 30,
            VolumeVoxelizationsAsync = 1 << 31,
        }

        public class LegacyLightLoopSettings
        {
            public LegacyLightLoopSettingsOverrides overrides;
            public bool enableDeferredTileAndCluster;
            public bool enableComputeLightEvaluation;
            public bool enableComputeLightVariants;
            public bool enableComputeMaterialVariants;
            public bool enableFptlForForwardOpaque;
            public bool enableBigTilePrepass;
            public bool isFptlEnabled;
        }

        public class LegacyFrameSettings
        {
            public LegacyFrameSettingsOverrides overrides;

            public bool enableShadow;
            public bool enableContactShadows;
            public bool enableShadowMask;
            public bool enableSSR;
            public bool enableSSAO;
            public bool enableSubsurfaceScattering;
            public bool enableTransmission;
            public bool enableAtmosphericScattering;
            public bool enableVolumetrics;
            public bool enableReprojectionForVolumetrics;
            public bool enableLightLayers;
            public bool enableExposureControl;

            public float diffuseGlobalDimmer;
            public float specularGlobalDimmer;

            public LegacyLitShaderMode shaderLitMode;
            public bool enableDepthPrepassWithDeferredRendering;

            public bool enableTransparentPrepass;
            public bool enableMotionVectors; // Enable/disable whole motion vectors pass (Camera + Object).
            public bool enableObjectMotionVectors;
            public bool enableDecals;
            public bool enableRoughRefraction; // Depends on DepthPyramid - If not enable, just do a copy of the scene color (?) - how to disable rough refraction ?
            public bool enableTransparentPostpass;
            public bool enableDistortion;
            public bool enablePostprocess;

            public bool enableOpaqueObjects;
            public bool enableTransparentObjects;
            public bool enableRealtimePlanarReflection;

            public bool enableMSAA;

            public bool enableAsyncCompute;
            public bool runLightListAsync;
            public bool runSSRAsync;
            public bool runSSAOAsync;
            public bool runContactShadowsAsync;
            public bool runVolumeVoxelizationAsync;

            public LegacyLightLoopSettings lightLoopSettings;
        }

        static object[] s_LegacyFrameSettingsDatas =
        {
            new LegacyFrameSettings
            {
                overrides = LegacyFrameSettingsOverrides.SSR | LegacyFrameSettingsOverrides.MSAA | LegacyFrameSettingsOverrides.ShaderLitMode,
                enableSSR = true,
                enableMSAA = true,
                shaderLitMode = LegacyLitShaderMode.Deferred,
                lightLoopSettings = new LegacyLightLoopSettings()
            },
            new LegacyFrameSettings
            {
                overrides = LegacyFrameSettingsOverrides.ObjectMotionVectors | LegacyFrameSettingsOverrides.OpaqueObjects | LegacyFrameSettingsOverrides.ShaderLitMode,
                enableOpaqueObjects = false,
                enableMSAA = true,
                enableMotionVectors = true,
                shaderLitMode = LegacyLitShaderMode.Forward,
                lightLoopSettings = new LegacyLightLoopSettings()
            },
            new LegacyFrameSettings
            {
                overrides = LegacyFrameSettingsOverrides.Postprocess | LegacyFrameSettingsOverrides.Shadow | LegacyFrameSettingsOverrides.ShaderLitMode,
                diffuseGlobalDimmer = 42f,
                enableMSAA = true,
                enablePostprocess = false,
                lightLoopSettings = new LegacyLightLoopSettings
                {
                    overrides = LegacyLightLoopSettingsOverrides.ComputeLightVariants | LegacyLightLoopSettingsOverrides.ComputeLightEvaluation,
                    enableComputeLightVariants = true,
                    enableComputeMaterialVariants = false
                }
            }
        };

        [Test, TestCaseSource(nameof(s_LegacyFrameSettingsDatas))]
        public void MigrationTest(LegacyFrameSettings legacyFrameSettingsData)
        {
            using (new PrefabMigrationTests(
                GetType().Name,
                GeneratePrefabYAML(legacyFrameSettingsData),
                out GameObject prefab
            ))
            {
                var instance = Object.Instantiate(prefab);
                m_ToClean = instance;

                var probe = instance.GetComponent<HDAdditionalReflectionData>();
                prefab.SetActive(true);
                probe.enabled = true;

                var frameSettingsData = probe.frameSettings;
                var frameSettingsMask = probe.frameSettingsOverrideMask;

                LitShaderMode litShaderModeEquivalent;
                switch (legacyFrameSettingsData.shaderLitMode)
                {
                    case LegacyLitShaderMode.Deferred:
                        litShaderModeEquivalent = LitShaderMode.Deferred;
                        break;
                    case LegacyLitShaderMode.Forward:
                        litShaderModeEquivalent = LitShaderMode.Forward;
                        break;
                    default:
                        throw new ArgumentException("Unknown LitShaderMode");
                }
                Assert.AreEqual(litShaderModeEquivalent, frameSettingsData.litShaderMode);

                Assert.AreEqual(legacyFrameSettingsData.enableShadow, frameSettingsData.IsEnabled(FrameSettingsField.Shadow));
                Assert.AreEqual(legacyFrameSettingsData.enableContactShadows, frameSettingsData.IsEnabled(FrameSettingsField.ContactShadows));
                Assert.AreEqual(legacyFrameSettingsData.enableShadowMask, frameSettingsData.IsEnabled(FrameSettingsField.ShadowMask));
                Assert.AreEqual(legacyFrameSettingsData.enableSSR, frameSettingsData.IsEnabled(FrameSettingsField.SSR));
                Assert.AreEqual(legacyFrameSettingsData.enableSSAO, frameSettingsData.IsEnabled(FrameSettingsField.SSAO));
                Assert.AreEqual(legacyFrameSettingsData.enableSubsurfaceScattering, frameSettingsData.IsEnabled(FrameSettingsField.SubsurfaceScattering));
                Assert.AreEqual(legacyFrameSettingsData.enableTransmission, frameSettingsData.IsEnabled(FrameSettingsField.Transmission));
                Assert.AreEqual(legacyFrameSettingsData.enableAtmosphericScattering, frameSettingsData.IsEnabled(FrameSettingsField.AtmosphericScattering));
                Assert.AreEqual(legacyFrameSettingsData.enableVolumetrics, frameSettingsData.IsEnabled(FrameSettingsField.Volumetrics));
                Assert.AreEqual(legacyFrameSettingsData.enableReprojectionForVolumetrics, frameSettingsData.IsEnabled(FrameSettingsField.ReprojectionForVolumetrics));
                Assert.AreEqual(legacyFrameSettingsData.enableLightLayers, frameSettingsData.IsEnabled(FrameSettingsField.LightLayers));
                Assert.AreEqual(legacyFrameSettingsData.enableExposureControl, frameSettingsData.IsEnabled(FrameSettingsField.ExposureControl));
                Assert.AreEqual(legacyFrameSettingsData.enableDepthPrepassWithDeferredRendering, frameSettingsData.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering));
                Assert.AreEqual(legacyFrameSettingsData.enableTransparentPrepass, frameSettingsData.IsEnabled(FrameSettingsField.TransparentPrepass));
                Assert.AreEqual(legacyFrameSettingsData.enableMotionVectors, frameSettingsData.IsEnabled(FrameSettingsField.MotionVectors));
                Assert.AreEqual(legacyFrameSettingsData.enableObjectMotionVectors, frameSettingsData.IsEnabled(FrameSettingsField.ObjectMotionVectors));
                Assert.AreEqual(legacyFrameSettingsData.enableDecals, frameSettingsData.IsEnabled(FrameSettingsField.Decals));
                Assert.AreEqual(legacyFrameSettingsData.enableRoughRefraction, frameSettingsData.IsEnabled(FrameSettingsField.RoughRefraction));
                Assert.AreEqual(legacyFrameSettingsData.enableTransparentPostpass, frameSettingsData.IsEnabled(FrameSettingsField.TransparentPostpass));
                Assert.AreEqual(legacyFrameSettingsData.enableDistortion, frameSettingsData.IsEnabled(FrameSettingsField.Distortion));
                Assert.AreEqual(legacyFrameSettingsData.enablePostprocess, frameSettingsData.IsEnabled(FrameSettingsField.Postprocess));
                Assert.AreEqual(legacyFrameSettingsData.enableOpaqueObjects, frameSettingsData.IsEnabled(FrameSettingsField.OpaqueObjects));
                Assert.AreEqual(legacyFrameSettingsData.enableTransparentObjects, frameSettingsData.IsEnabled(FrameSettingsField.TransparentObjects));
                Assert.AreEqual(legacyFrameSettingsData.enableRealtimePlanarReflection, frameSettingsData.IsEnabled(FrameSettingsField.RealtimePlanarReflection));
                Assert.AreEqual(legacyFrameSettingsData.enableMSAA, frameSettingsData.IsEnabled(FrameSettingsField.MSAA));
                Assert.AreEqual(legacyFrameSettingsData.enableAsyncCompute, frameSettingsData.IsEnabled(FrameSettingsField.AsyncCompute));
                Assert.AreEqual(legacyFrameSettingsData.runLightListAsync, frameSettingsData.IsEnabled(FrameSettingsField.LightListAsync));
                Assert.AreEqual(legacyFrameSettingsData.runSSRAsync, frameSettingsData.IsEnabled(FrameSettingsField.SSRAsync));
                Assert.AreEqual(legacyFrameSettingsData.runSSAOAsync, frameSettingsData.IsEnabled(FrameSettingsField.SSAOAsync));
                Assert.AreEqual(legacyFrameSettingsData.runContactShadowsAsync, frameSettingsData.IsEnabled(FrameSettingsField.ContactShadowsAsync));
                Assert.AreEqual(legacyFrameSettingsData.runVolumeVoxelizationAsync, frameSettingsData.IsEnabled(FrameSettingsField.VolumeVoxelizationsAsync));

                Assert.AreEqual(legacyFrameSettingsData.lightLoopSettings.enableBigTilePrepass, frameSettingsData.IsEnabled(FrameSettingsField.BigTilePrepass));
                Assert.AreEqual(legacyFrameSettingsData.lightLoopSettings.enableComputeLightEvaluation, frameSettingsData.IsEnabled(FrameSettingsField.ComputeLightEvaluation));
                Assert.AreEqual(legacyFrameSettingsData.lightLoopSettings.enableComputeLightVariants, frameSettingsData.IsEnabled(FrameSettingsField.ComputeLightVariants));
                Assert.AreEqual(legacyFrameSettingsData.lightLoopSettings.enableComputeMaterialVariants, frameSettingsData.IsEnabled(FrameSettingsField.ComputeMaterialVariants));
                Assert.AreEqual(legacyFrameSettingsData.lightLoopSettings.enableDeferredTileAndCluster, frameSettingsData.IsEnabled(FrameSettingsField.DeferredTile));
                Assert.AreEqual(legacyFrameSettingsData.lightLoopSettings.enableFptlForForwardOpaque, frameSettingsData.IsEnabled(FrameSettingsField.FPTLForForwardOpaque));


                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.Shadow) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.Shadow]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.ContactShadow) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ContactShadows]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.ShadowMask) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ShadowMask]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.SSR) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.SSR]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.SSAO) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.SSAO]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.SubsurfaceScattering) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.SubsurfaceScattering]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.Transmission) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.Transmission]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.AtmosphericScaterring) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.AtmosphericScattering]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.Volumetrics) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.Volumetrics]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.ReprojectionForVolumetrics) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ReprojectionForVolumetrics]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.LightLayers) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.LightLayers]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.ExposureControl) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ExposureControl]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.DepthPrepassWithDeferredRendering) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.DepthPrepassWithDeferredRendering]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.TransparentPrepass) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.TransparentPrepass]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.MotionVectors) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.MotionVectors]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.ObjectMotionVectors) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ObjectMotionVectors]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.Decals) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.Decals]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.RoughRefraction) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.RoughRefraction]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.TransparentPostpass) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.TransparentPostpass]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.Distortion) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.Distortion]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.Postprocess) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.Postprocess]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.OpaqueObjects) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.OpaqueObjects]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.TransparentObjects) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.TransparentObjects]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.RealtimePlanarReflection) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.RealtimePlanarReflection]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.MSAA) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.MSAA]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.AsyncCompute) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.AsyncCompute]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.LightListAsync) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.LightListAsync]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.SSRAsync) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.SSRAsync]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.SSAOAsync) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.SSAOAsync]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.ContactShadowsAsync) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ContactShadowsAsync]);
                Assert.AreEqual((legacyFrameSettingsData.overrides & LegacyFrameSettingsOverrides.VolumeVoxelizationsAsync) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.VolumeVoxelizationsAsync]);

                Assert.AreEqual((legacyFrameSettingsData.lightLoopSettings.overrides & LegacyLightLoopSettingsOverrides.BigTilePrepass) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.BigTilePrepass]);
                Assert.AreEqual((legacyFrameSettingsData.lightLoopSettings.overrides & LegacyLightLoopSettingsOverrides.ComputeLightEvaluation) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ComputeLightEvaluation]);
                Assert.AreEqual((legacyFrameSettingsData.lightLoopSettings.overrides & LegacyLightLoopSettingsOverrides.ComputeLightVariants) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ComputeLightVariants]);
                Assert.AreEqual((legacyFrameSettingsData.lightLoopSettings.overrides & LegacyLightLoopSettingsOverrides.ComputeMaterialVariants) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.ComputeMaterialVariants]);
                Assert.AreEqual((legacyFrameSettingsData.lightLoopSettings.overrides & LegacyLightLoopSettingsOverrides.TileAndCluster) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.DeferredTile]);
                Assert.AreEqual((legacyFrameSettingsData.lightLoopSettings.overrides & LegacyLightLoopSettingsOverrides.FptlForForwardOpaque) > 0, frameSettingsMask.mask[(uint)FrameSettingsField.FPTLForForwardOpaque]);
            }
        }

        string GeneratePrefabYAML(LegacyFrameSettings legacyFrameSettings)
                => $@"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &3102262843427888416
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: 3102262843427888420}}
  - component: {{fileID: 3102262843427888421}}
  - component: {{fileID: 3102262843427888418}}
  m_Layer: 5
  m_Name: Reflection Probe
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &3102262843427888420
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3102262843427888416}}
  m_LocalRotation: {{x: 0, y: 0, z: 0.26681787, w: 0.963747}}
  m_LocalPosition: {{x: 3.9601986, y: 0.8451278, z: -1.4354408}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_Children: []
  m_Father: {{fileID: 0}}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 30.95}}
--- !u!215 &3102262843427888421
ReflectionProbe:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3102262843427888416}}
  m_Enabled: 1
  serializedVersion: 2
  m_Type: 0
  m_Mode: 1
  m_RefreshMode: 0
  m_TimeSlicingMode: 0
  m_Resolution: 128
  m_UpdateFrequency: 0
  m_BoxSize: {{x: 6, y: 6, z: 6}}
  m_BoxOffset: {{x: 0.32623026, y: 1.5948586, z: 1.3}}
  m_NearClip: 2.76
  m_FarClip: 5
  m_ShadowDistance: 100
  m_ClearFlags: 2
  m_BackGroundColor: {{r: 0.1882353, g: 0.023529412, b: 0.13529739, a: 0}}
  m_CullingMask:
    serializedVersion: 2
    m_Bits: 310
  m_IntensityMultiplier: 1
  m_BlendDistance: 0
  m_HDR: 1
  m_BoxProjection: 0
  m_RenderDynamicObjects: 0
  m_UseOcclusionCulling: 1
  m_Importance: 1
  m_CustomBakedTexture: {{fileID: 8900000, guid: b7a0288be1440c140849eb49d3b12573,
    type: 3}}
--- !u!114 &3102262843427888418
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 3102262843427888416}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: d0ef8dc2c2eabfa4e8cb77be57a837c0, type: 3}}
  m_Name: 
  m_EditorClassIdentifier: 
  m_ProxyVolume: {{fileID: 0}}
  m_InfiniteProjection: 1
  m_InfluenceVolume:
    m_Shape: 1
    m_Offset: {{x: 1.1, y: 1.2, z: 1.3}}
    m_BoxSize: {{x: 7, y: 8, z: 9}}
    m_BoxBlendDistancePositive: {{x: 1, y: 2, z: 3}}
    m_BoxBlendDistanceNegative: {{x: 1.5, y: 2.5, z: 3.5}}
    m_BoxBlendNormalDistancePositive: {{x: 0.5, y: 0.4, z: 0.3}}
    m_BoxBlendNormalDistanceNegative: {{x: 0.2, y: 0.1, z: 0.6}}
    m_BoxSideFadePositive: {{x: 0.1, y: 0.2, z: 0.3}}
    m_BoxSideFadeNegative: {{x: 0.15, y: 0.25, z: 0.35}}
    m_EditorAdvancedModeBlendDistancePositive: {{x: 1, y: 2, z: 3}}
    m_EditorAdvancedModeBlendDistanceNegative: {{x: 1.5, y: 2.5, z: 3.5}}
    m_EditorSimplifiedModeBlendDistance: 3.5
    m_EditorAdvancedModeBlendNormalDistancePositive: {{x: 0.5, y: 0.4, z: 0.3}}
    m_EditorAdvancedModeBlendNormalDistanceNegative: {{x: 0.2, y: 0.1, z: 0.6}}
    m_EditorSimplifiedModeBlendNormalDistance: 4.5
    m_EditorAdvancedModeEnabled: 1
    m_EditorAdvancedModeFaceFadePositive: {{x: 0.1, y: 0.2, z: 0.3}}
    m_EditorAdvancedModeFaceFadeNegative: {{x: 0.15, y: 0.25, z: 0.35}}
    m_SphereRadius: 6
    m_SphereBlendDistance: 2
    m_SphereBlendNormalDistance: 1
    m_Version: 1
    m_ObsoleteSphereBaseOffset: {{x: 0, y: 0, z: 0}}
  m_FrameSettings:
    overrides: {legacyFrameSettings.overrides}
    enableShadow: {(legacyFrameSettings.enableShadow ? 1 : 0)}
    enableContactShadows: {(legacyFrameSettings.enableContactShadows ? 1 : 0)}
    enableShadowMask: {(legacyFrameSettings.enableShadowMask ? 1 : 0)}
    enableSSR: {(legacyFrameSettings.enableSSR ? 1 : 0)}
    enableSSAO: {(legacyFrameSettings.enableSSAO ? 1 : 0)}
    enableSubsurfaceScattering: {(legacyFrameSettings.enableSubsurfaceScattering ? 1 : 0)}
    enableTransmission: {(legacyFrameSettings.enableTransmission ? 1 : 0)}
    enableAtmosphericScattering: {(legacyFrameSettings.enableAtmosphericScattering ? 1 : 0)}
    enableVolumetrics: {(legacyFrameSettings.enableVolumetrics ? 1 : 0)}
    enableReprojectionForVolumetrics: {(legacyFrameSettings.enableReprojectionForVolumetrics ? 1 : 0)}
    enableLightLayers: {(legacyFrameSettings.enableLightLayers ? 1 : 0)}
    enableExposureControl: {(legacyFrameSettings.enableExposureControl ? 1 : 0)}
    diffuseGlobalDimmer: {legacyFrameSettings.diffuseGlobalDimmer}
    specularGlobalDimmer: {legacyFrameSettings.specularGlobalDimmer}
    shaderLitMode: {(legacyFrameSettings.shaderLitMode == LegacyLitShaderMode.Deferred ? 1 : 0)}
    enableDepthPrepassWithDeferredRendering: {(legacyFrameSettings.enableDepthPrepassWithDeferredRendering ? 1 : 0)}
    enableTransparentPrepass: {(legacyFrameSettings.enableTransparentPrepass ? 1 : 0)}
    enableMotionVectors: {(legacyFrameSettings.enableMotionVectors ? 1 : 0)}
    enableObjectMotionVectors: {(legacyFrameSettings.enableObjectMotionVectors ? 1 : 0)}
    enableDecals: {(legacyFrameSettings.enableDecals ? 1 : 0)}
    enableRoughRefraction: {(legacyFrameSettings.enableRoughRefraction ? 1 : 0)}
    enableTransparentPostpass: {(legacyFrameSettings.enableTransparentPostpass ? 1 : 0)}
    enableDistortion: {(legacyFrameSettings.enableDistortion ? 1 : 0)}
    enablePostprocess: {(legacyFrameSettings.enablePostprocess ? 1 : 0)}
    enableAsyncCompute: {(legacyFrameSettings.enableAsyncCompute ? 1 : 0)}
    runLightListAsync: {(legacyFrameSettings.runLightListAsync ? 1 : 0)}
    runSSRAsync: {(legacyFrameSettings.runSSRAsync ? 1 : 0)}
    runSSAOAsync: {(legacyFrameSettings.runSSAOAsync ? 1 : 0)}
    runContactShadowsAsync: {(legacyFrameSettings.runContactShadowsAsync ? 1 : 0)}
    runVolumeVoxelizationAsync: {(legacyFrameSettings.runVolumeVoxelizationAsync ? 1 : 0)}
    enableOpaqueObjects: {(legacyFrameSettings.enableOpaqueObjects ? 1 : 0)}
    enableTransparentObjects: {(legacyFrameSettings.enableTransparentObjects ? 1 : 0)}
    enableRealtimePlanarReflection: {(legacyFrameSettings.enableRealtimePlanarReflection ? 1 : 0)}
    enableMSAA: {(legacyFrameSettings.enableMSAA ? 1 : 0)}
    lightLoopSettings:
      overrides: {legacyFrameSettings.lightLoopSettings.overrides}
      enableTileAndCluster: {(legacyFrameSettings.lightLoopSettings.enableDeferredTileAndCluster ? 1 : 0)}
      enableComputeLightEvaluation: {(legacyFrameSettings.lightLoopSettings.enableComputeLightEvaluation ? 1 : 0)}
      enableComputeLightVariants: {(legacyFrameSettings.lightLoopSettings.enableComputeLightVariants ? 1 : 0)}
      enableComputeMaterialVariants: {(legacyFrameSettings.lightLoopSettings.enableComputeMaterialVariants ? 1 : 0)}
      enableFptlForForwardOpaque: {(legacyFrameSettings.lightLoopSettings.enableFptlForForwardOpaque ? 1 : 0)}
      enableBigTilePrepass: {(legacyFrameSettings.lightLoopSettings.enableBigTilePrepass ? 1 : 0)}
      isFptlEnabled: {(legacyFrameSettings.lightLoopSettings.isFptlEnabled ? 1 : 0)}
  m_CaptureSettings:
    overrides: 0
    clearColorMode: 2
    backgroundColorHDR: {{r: 0.1882353, g: 0.023529412, b: 0.13529739, a: 0}}
    clearDepth: 0
    cullingMask:
      serializedVersion: 2
      m_Bits: 310
    useOcclusionCulling: 0
    volumeLayerMask:
      serializedVersion: 2
      m_Bits: 33
    volumeAnchorOverride: {{fileID: 0}}
    projection: 0
    nearClipPlane: 2.76
    farClipPlane: 5
    fieldOfView: 90
    orthographicSize: 5
    renderingPath: 1
    shadowDistance: 666
  m_Multiplier: 20
  m_Weight: 0.66
  m_Mode: 1
  m_RefreshMode: 1
  m_CustomTexture: {{fileID: 0}}
  m_BakedTexture: {{fileID: 0}}
  m_RenderDynamicObjects: 0
  lightLayers: 9
  m_ReflectionProbeVersion: 6
  m_ObsoleteInfluenceShape: 0
  m_ObsoleteInfluenceSphereRadius: 3
  m_ObsoleteBlendDistancePositive: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBlendDistanceNegative: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBlendNormalDistancePositive: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBlendNormalDistanceNegative: {{x: 0, y: 0, z: 0}}
  m_ObsoleteBoxSideFadePositive: {{x: 1, y: 1, z: 1}}
  m_ObsoleteBoxSideFadeNegative: {{x: 1, y: 1, z: 1}}
";


    }
}
