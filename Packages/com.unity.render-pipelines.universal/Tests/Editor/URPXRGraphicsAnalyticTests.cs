using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.XRGraphics
{
    class URPXRGraphicsAnalyticTests
    {
        class TestCustomFeature : ScriptableRendererFeature
        {
            public override void Create() { }
            public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
        }

        static readonly string[] s_Loaders = { "OpenXRLoader" };

        readonly List<Object> m_ToDestroy = new();

        System.Func<BuildTargetGroup, IReadOnlyList<string>> m_OriginalGetLoaders;
        System.Action<UnityEngine.Analytics.IAnalytic> m_OriginalSend;
        readonly List<UnityEngine.Analytics.IAnalytic> m_Sent = new();

        [SetUp]
        public void SetUp()
        {
            m_OriginalGetLoaders = URPXRGraphicsAnalytic.s_GetActiveXRLoaders;
            m_OriginalSend = URPXRGraphicsAnalytic.s_Send;
            URPXRGraphicsAnalytic.s_Send = m_Sent.Add;
            m_Sent.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            URPXRGraphicsAnalytic.s_GetActiveXRLoaders = m_OriginalGetLoaders;
            URPXRGraphicsAnalytic.s_Send = m_OriginalSend;
            URPXRGraphicsAnalytic.s_ShadersInBuild.Clear();
            URPXRGraphicsAnalytic.s_Collecting = false;

            foreach (var o in m_ToDestroy)
                if (o != null)
                    Object.DestroyImmediate(o);
            m_ToDestroy.Clear();
        }

        T Track<T>(T o) where T : Object
        {
            m_ToDestroy.Add(o);
            return o;
        }

        UniversalRendererData CreateRenderer(RenderingMode mode, bool tileOnly, params ScriptableRendererFeature[] features)
        {
            var rendererData = Track(ScriptableObject.CreateInstance<UniversalRendererData>());
            rendererData.renderingMode = mode;
            rendererData.tileOnlyMode = tileOnly;
            foreach (var f in features)
                rendererData.rendererFeatures.Add(Track(f));
            return rendererData;
        }

        UniversalRenderPipelineAsset CreateAsset(UniversalRendererData rendererData, int msaa)
        {
            var asset = Track(UniversalRenderPipelineAsset.Create(rendererData));
            asset.msaaSampleCount = msaa;
            return asset;
        }

        static URPXRGraphicsAnalytic.Analytic.AnalyticsData Gather(BuildTarget target, params UniversalRenderPipelineAsset[] assets)
            => URPXRGraphicsAnalytic.Analytic.Gather(target, s_Loaders, assets, null);

        [Test]
        public void NoAssets_ProducesEmptyPayload()
        {
            var data = URPXRGraphicsAnalytic.Analytic.Gather(BuildTarget.Android, null, null, null);

            Assert.AreEqual(0, data.urp_asset_count);
            Assert.AreEqual(0, data.universal_renderer_count);
            Assert.IsEmpty(data.msaa_sample_counts);
            Assert.IsEmpty(data.rendering_modes);
            Assert.IsEmpty(data.shader_graph_targets);
            Assert.IsEmpty(data.xr_loaders);
        }

        [Test]
        public void Payload_IsTaggedWithXRGraphicsCategoryAndLoaders()
        {
            var data = Gather(BuildTarget.Android);

            Assert.AreEqual(XRGraphicsAnalyticsUtils.k_Category, data.category);
            CollectionAssert.AreEqual(s_Loaders, data.xr_loaders);
        }

        [Test]
        public void EventName_UsesXRGraphicsPrefix()
        {
            var info = (UnityEngine.Analytics.AnalyticInfoAttribute)System.Attribute.GetCustomAttribute(
                typeof(URPXRGraphicsAnalytic.Analytic), typeof(UnityEngine.Analytics.AnalyticInfoAttribute));

            Assert.IsNotNull(info);
            StringAssert.StartsWith(XRGraphicsAnalyticsUtils.k_EventPrefix, info.eventName);
        }

        [Test]
        public void MsaaAndRenderingModes_AreReportedPerAssetAndRenderer()
        {
            var forwardPlus = CreateRenderer(RenderingMode.ForwardPlus, false);
            var deferredPlus = CreateRenderer(RenderingMode.DeferredPlus, false);

            var data = Gather(BuildTarget.Android, CreateAsset(forwardPlus, 4), CreateAsset(deferredPlus, 1));

            Assert.AreEqual(2, data.urp_asset_count);
            CollectionAssert.AreEquivalent(new[] { 4, 1 }, data.msaa_sample_counts);
            Assert.AreEqual(2, data.universal_renderer_count);
            CollectionAssert.AreEquivalent(new[] { "ForwardPlus", "DeferredPlus" }, data.rendering_modes);
        }

        [Test]
        public void SharedRendererData_IsCountedOnce()
        {
            var shared = CreateRenderer(RenderingMode.Forward, true);

            var data = Gather(BuildTarget.Android, CreateAsset(shared, 2), CreateAsset(shared, 4));

            Assert.AreEqual(2, data.urp_asset_count);
            Assert.AreEqual(1, data.universal_renderer_count);
            Assert.AreEqual(1, data.tile_only_mode_renderer_count);
        }

        [Test]
        public void OnTilePostProcessing_ReportsTextureFallbackWhenTileOnlyModeIsOff()
        {
            var tileOnly = CreateRenderer(RenderingMode.Forward, true, ScriptableObject.CreateInstance<OnTilePostProcessFeature>());
            var notTileOnly = CreateRenderer(RenderingMode.Forward, false, ScriptableObject.CreateInstance<OnTilePostProcessFeature>());

            var data = Gather(BuildTarget.Android, CreateAsset(tileOnly, 1), CreateAsset(notTileOnly, 1));

            Assert.AreEqual(1, data.tile_only_mode_renderer_count);
            Assert.AreEqual(2, data.on_tile_post_processing_renderer_count);
            Assert.AreEqual(1, data.on_tile_post_processing_texture_fallback_count);
            CollectionAssert.Contains(data.builtin_renderer_features, nameof(OnTilePostProcessFeature));
        }

        [Test]
        public void InactiveFeatures_AreIgnored()
        {
            var feature = ScriptableObject.CreateInstance<OnTilePostProcessFeature>();
            feature.SetActive(false);
            var renderer = CreateRenderer(RenderingMode.Forward, true, feature);

            var data = Gather(BuildTarget.Android, CreateAsset(renderer, 1));

            Assert.AreEqual(0, data.on_tile_post_processing_renderer_count);
            Assert.IsEmpty(data.builtin_renderer_features);
        }

        [Test]
        public void RenderObjects_ReportsDepthInputAttachment()
        {
            var withDepthInput = ScriptableObject.CreateInstance<RenderObjects>();
            withDepthInput.settings.depthInputAttachment = true;
            var withoutDepthInput = ScriptableObject.CreateInstance<RenderObjects>();

            var renderer = CreateRenderer(RenderingMode.Forward, true, withDepthInput, withoutDepthInput);

            var data = Gather(BuildTarget.Android, CreateAsset(renderer, 1));

            Assert.AreEqual(2, data.render_objects_feature_count);
            Assert.AreEqual(1, data.render_objects_depth_input_attachment_count);
        }

        [Test]
        public void CustomFeatures_AreCountedWithoutSendingTypeNames()
        {
            var renderer = CreateRenderer(RenderingMode.Forward, false,
                ScriptableObject.CreateInstance<TestCustomFeature>(),
                ScriptableObject.CreateInstance<TestCustomFeature>(),
                ScriptableObject.CreateInstance<RenderObjects>());

            var data = Gather(BuildTarget.Android, CreateAsset(renderer, 1));

            Assert.AreEqual(2, data.custom_renderer_feature_count);
            Assert.AreEqual(1, data.custom_renderer_feature_type_count);
            CollectionAssert.AreEqual(new[] { nameof(RenderObjects) }, data.builtin_renderer_features);
        }

        [Test]
        public void TileOnlyMode_IsReportedUnsupportedOnWebGL()
        {
            Assert.IsFalse(Gather(BuildTarget.WebGL).tile_only_mode_supported_on_build_target);
            Assert.IsTrue(Gather(BuildTarget.Android).tile_only_mode_supported_on_build_target);
        }

        [Test]
        public void PackageShaders_AreNotCountedAsCustomOrShaderGraph()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Assume.That(lit, Is.Not.Null);

            var data = URPXRGraphicsAnalytic.Analytic.Gather(BuildTarget.Android, s_Loaders, null, new[] { lit, null });

            Assert.AreEqual(0, data.shader_graph_shader_count);
            Assert.AreEqual(0, data.custom_shader_count);
        }

        [TestCase("Assets/Shaders/MyShader.shader", null, "Project")]
        [TestCase("Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader", "com.unity.render-pipelines.universal", "UnityPackage")]
        [TestCase("Packages/com.mystudio.shaders/Toon.shader", "com.mystudio.shaders", "ThirdPartyPackage")]
        [TestCase("Packages/com.unityfan.shaders/Toon.shader", "com.unityfan.shaders", "ThirdPartyPackage")]
        [TestCase("Packages/unknown/Toon.shader", null, "Unknown")]
        [TestCase("", null, "Unknown")]
        public void ShaderOrigin_IsClassifiedByLocationAndPackageName(string path, string packageName, string expected)
        {
            Assert.AreEqual(expected, URPXRGraphicsAnalytic.Analytic.GetShaderOrigin(path, packageName).ToString());
        }


        [Test]
        public void ShaderCollector_IgnoresShadersWithAllVariantsStripped()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Assume.That(lit, Is.Not.Null);

            URPXRGraphicsAnalytic.s_ShadersInBuild.Clear();
            URPXRGraphicsAnalytic.s_Collecting = true;
            var collector = new URPXRGraphicsAnalytic.ShaderCollector();

            collector.OnProcessShader(lit, default, new List<ShaderCompilerData>());
            Assert.IsEmpty(URPXRGraphicsAnalytic.s_ShadersInBuild);

            collector.OnProcessShader(lit, default, new List<ShaderCompilerData> { default });
            CollectionAssert.AreEqual(new[] { lit }, URPXRGraphicsAnalytic.s_ShadersInBuild);
        }

        [Test]
        public void ShaderCollector_CollectsNothingWhenNotCollecting()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Assume.That(lit, Is.Not.Null);

            URPXRGraphicsAnalytic.s_ShadersInBuild.Clear();
            URPXRGraphicsAnalytic.s_Collecting = false;

            new URPXRGraphicsAnalytic.ShaderCollector().OnProcessShader(lit, default, new List<ShaderCompilerData> { default });

            Assert.IsEmpty(URPXRGraphicsAnalytic.s_ShadersInBuild);
        }

        [TestCase(true, true, ExpectedResult = true)]
        [TestCase(true, false, ExpectedResult = false)]
        [TestCase(false, true, ExpectedResult = false)]
        [TestCase(false, false, ExpectedResult = false)]
        public bool ShouldCollect_RequiresAnalyticsEnabledAndXRActive(bool analyticsEnabled, bool xrActive)
        {
            URPXRGraphicsAnalytic.s_GetActiveXRLoaders = _ => xrActive ? s_Loaders : new string[0];
            return URPXRGraphicsAnalytic.ShouldCollect(analyticsEnabled, BuildTargetGroup.Android);
        }

        [Test]
        public void ShaderCollector_RunsAfterOtherShaderPreprocessors()
        {
            Assert.AreEqual(int.MaxValue, new URPXRGraphicsAnalytic.ShaderCollector().callbackOrder);
        }

        bool TrySend(bool buildingForURP)
        {
            var renderer = CreateRenderer(RenderingMode.Forward, true);
            return URPXRGraphicsAnalytic.TrySend(BuildTarget.Android, BuildTargetGroup.Android, buildingForURP,
                new[] { CreateAsset(renderer, 4) }, null);
        }

        [Test]
        public void NoXRLoaders_EventIsNotSent()
        {
            URPXRGraphicsAnalytic.s_GetActiveXRLoaders = _ => new List<string>();

            Assert.IsFalse(TrySend(buildingForURP: true));
            Assert.IsEmpty(m_Sent);
        }

        [Test]
        public void NullXRLoaders_EventIsNotSent()
        {
            URPXRGraphicsAnalytic.s_GetActiveXRLoaders = _ => null;

            Assert.IsFalse(TrySend(buildingForURP: true));
            Assert.IsEmpty(m_Sent);
        }

        [Test]
        public void NotBuildingForURP_EventIsNotSentEvenWithXR()
        {
            URPXRGraphicsAnalytic.s_GetActiveXRLoaders = _ => s_Loaders;

            Assert.IsFalse(TrySend(buildingForURP: false));
            Assert.IsEmpty(m_Sent);
        }

        [Test]
        public void XRLoaderActive_OneEventIsSentWithXRPayload()
        {
            BuildTargetGroup queriedGroup = BuildTargetGroup.Unknown;
            URPXRGraphicsAnalytic.s_GetActiveXRLoaders = group => { queriedGroup = group; return s_Loaders; };

            Assert.IsTrue(TrySend(buildingForURP: true));
            Assert.AreEqual(BuildTargetGroup.Android, queriedGroup);
            Assert.AreEqual(1, m_Sent.Count);

            Assert.IsTrue(m_Sent[0].TryGatherData(out var raw, out var error));
            Assert.IsNull(error);
            var data = (URPXRGraphicsAnalytic.Analytic.AnalyticsData)raw;
            Assert.AreEqual(XRGraphicsAnalyticsUtils.k_Category, data.category);
            CollectionAssert.AreEqual(s_Loaders, data.xr_loaders);
            CollectionAssert.AreEqual(new[] { 4 }, data.msaa_sample_counts);
            Assert.AreEqual(1, data.tile_only_mode_renderer_count);
        }
    }
}
