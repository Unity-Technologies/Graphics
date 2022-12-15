using System;
using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    public class RenderPipelineTests
    {
        RenderPipelineAsset m_PreviousRenderPipelineAssetInGraphicsSettings;
        RenderPipelineAsset m_PreviousRenderPipelineAssetInQualitySettings;
        RenderPipelineAsset m_CreatedRenderPipelineAsset;

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousRenderPipelineAssetInGraphicsSettings = GraphicsSettings.renderPipelineAsset;
            m_PreviousRenderPipelineAssetInQualitySettings = QualitySettings.renderPipeline;
        }

        [TearDown]
        public virtual void TearDown()
        {
            GraphicsSettings.renderPipelineAsset = m_PreviousRenderPipelineAssetInGraphicsSettings;
            QualitySettings.renderPipeline = m_PreviousRenderPipelineAssetInQualitySettings;
            Object.DestroyImmediate(m_CreatedRenderPipelineAsset);
        }

        protected void SetupRenderPipeline<T>() where T : RenderPipelineAsset
        {
            m_CreatedRenderPipelineAsset = ScriptableObject.CreateInstance<T>();
            GraphicsSettings.renderPipelineAsset = m_CreatedRenderPipelineAsset;
            QualitySettings.renderPipeline = m_CreatedRenderPipelineAsset;
        }

        protected void SetupRenderPipeline(Type renderPipelineType)
        {
            m_CreatedRenderPipelineAsset = (RenderPipelineAsset) ScriptableObject.CreateInstance(renderPipelineType);
            GraphicsSettings.renderPipelineAsset = m_CreatedRenderPipelineAsset;
            QualitySettings.renderPipeline = m_CreatedRenderPipelineAsset;
        }

        protected void RemoveRenderPipeline()
        {
            GraphicsSettings.renderPipelineAsset = null;
            QualitySettings.renderPipeline = null;
        }

        protected Editor SetupDummyObject<T>() where T : Component
        {
            var gameObject = new GameObject();
            var scriptComp = gameObject.AddComponent<T>();
            return Editor.CreateEditor(scriptComp);
        }
    }
}
