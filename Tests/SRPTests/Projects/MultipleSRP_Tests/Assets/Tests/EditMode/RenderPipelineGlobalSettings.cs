using NUnit.Framework;
using System;
using Common;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;

namespace MultipleSRP.EditMode
{
    [TestFixture("Assets/URPDefaultResources/UniversalRenderPipelineAsset.asset", typeof(UniversalRenderPipeline), typeof(UniversalRenderPipelineGlobalSettings), TestName = "URP")]
    [TestFixture("Assets/HDRPDefaultResources/HDRenderPipelineAsset.asset", typeof(HDRenderPipeline), typeof(HDRenderPipelineGlobalSettings), TestName = "HDRP") ]
    public class RenderPipelineGlobalSettingsTests
    {
        RenderPipelineAsset m_GraphicsSettingsRPAsset = null;

        private readonly string m_RenderPipelineAssetPath;
        private readonly Type m_RenderPipelineType;
        private readonly Type m_RenderPipelineGlobalSettingsType;

        public RenderPipelineGlobalSettingsTests(string renderPipelineAssetPath, Type renderPipelineType, Type renderPipelineGlobalSettingsType)
        {
            m_RenderPipelineAssetPath = renderPipelineAssetPath;
            m_RenderPipelineType = renderPipelineType;
            m_RenderPipelineGlobalSettingsType = renderPipelineGlobalSettingsType;
        }

        [SetUp]
        public void SetUp()
        {
            m_GraphicsSettingsRPAsset = GraphicsSettings.defaultRenderPipeline;
        }

        [TearDown]
        public void TearDown()
        {
            var rpAsset = GraphicsSettings.defaultRenderPipeline;
            GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var settings);

            GraphicsSettings.defaultRenderPipeline = m_GraphicsSettingsRPAsset;

            Resources.UnloadAsset(rpAsset);
            Resources.UnloadAsset(settings);
        }

        private RenderPipelineAsset LoadAsset()
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(m_RenderPipelineAssetPath);
            if (!string.IsNullOrEmpty(m_RenderPipelineAssetPath))
                Assert.IsNotNull(asset, m_RenderPipelineAssetPath);
            return asset as RenderPipelineAsset;
        }

        [Test]
        public void TryGetCurrentRenderPipelineGlobalSettings()
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset();
            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var settings));
            Assert.AreEqual(m_RenderPipelineGlobalSettingsType, settings.GetType());
        }

        [Test]
        [Description("Case 1342987 - Support undo on Global Settings assignation ")]
        public void UndoUnregisterGlobalSettings()
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset();

            Undo.IncrementCurrentGroup();

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(m_RenderPipelineType, null);

            Assert.IsFalse(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var _));

            Undo.PerformUndo();

            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var settings));
            Assert.AreEqual(m_RenderPipelineGlobalSettingsType, settings.GetType());
        }

        [Test]
        [Description("Case 1342987 - Support undo on Global Settings assignation ")]
        public void UndoRegisterGlobalSettings()
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset();

            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var settings));

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(m_RenderPipelineType, null);

            Assert.IsFalse(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var _));

            Undo.IncrementCurrentGroup();

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(m_RenderPipelineType, settings);

            Undo.PerformUndo();

            Assert.IsFalse(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var _));

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(m_RenderPipelineType, settings);

            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var newSettings));
            Assert.IsTrue(ReferenceEquals(settings, newSettings));
        }

        [Test]
        public void GlobalSettingsAreEnsuredWhenRendering()
        {
            GraphicsSettings.defaultRenderPipeline = LoadAsset();

            EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(m_RenderPipelineType, null);
            Assert.IsFalse(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var _));
            
            RenderPipelineScope.ForceInitialization();

            Assert.IsTrue(GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var settings));
            Assert.AreEqual(m_RenderPipelineGlobalSettingsType, settings.GetType());
        }
    }
}
