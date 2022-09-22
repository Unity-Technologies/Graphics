using NUnit.Framework;
using UnityEditor;

namespace UnityEngine.Rendering.Tests
{
    public class RenderPipelineTests
    {
        RenderPipelineAsset m_PreviousRenderPipelineAsset;
        RenderPipelineAsset m_CreatedRenderPipelineAsset;

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousRenderPipelineAsset = GraphicsSettings.renderPipelineAsset;
        }

        [TearDown]
        public virtual void TearDown()
        {
            GraphicsSettings.renderPipelineAsset = m_PreviousRenderPipelineAsset;
            Object.DestroyImmediate(m_CreatedRenderPipelineAsset);
        }

        protected void SetupRenderPipeline<T>() where T : RenderPipelineAsset
        {
            m_CreatedRenderPipelineAsset = ScriptableObject.CreateInstance<T>();
            GraphicsSettings.renderPipelineAsset = m_CreatedRenderPipelineAsset;
        }

        protected void RemoveRenderPipeline()
        {
            GraphicsSettings.renderPipelineAsset = null;
        }

        protected Editor SetupDummyObject<T>() where T : Component
        {
            var gameObject = new GameObject();
            var scriptComp = gameObject.AddComponent<T>();
            return Editor.CreateEditor(scriptComp);
        }
    }
}
