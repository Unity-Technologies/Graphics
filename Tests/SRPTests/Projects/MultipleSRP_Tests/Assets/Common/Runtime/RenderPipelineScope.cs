using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Common
{
    public class RenderPipelineScope : IDisposable
    {
        readonly RenderPipelineAsset m_GraphicsPipelineAsset;
        readonly RenderPipelineAsset m_QualityPipelineAsset;

        readonly RenderPipelineAsset m_CreatedAsset;

        readonly bool m_WasChanged;
        readonly bool m_WasCreated;

        public RenderPipelineScope()
        {
        }

        public RenderPipelineScope(bool createDummyPipeline, bool forceInitialization = false)
        {
            m_GraphicsPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            m_QualityPipelineAsset = QualitySettings.renderPipeline;

            if (createDummyPipeline)
            {
                m_CreatedAsset = ScriptableObject.CreateInstance<DummyRenderPipelineAsset>();
                GraphicsSettings.defaultRenderPipeline = m_CreatedAsset;
                QualitySettings.renderPipeline = m_CreatedAsset;
                m_WasCreated = true;
                m_WasChanged = true;
            }

            if (forceInitialization)
                ForceInitialization();
        }

        public RenderPipelineScope(Type renderPipelineType, bool forceInitialization = false)
        {
            if (GraphicsSettings.currentRenderPipelineAssetType == renderPipelineType)
            {
                m_WasChanged = false;
            }
            else
            {
                m_GraphicsPipelineAsset = GraphicsSettings.defaultRenderPipeline;
                m_QualityPipelineAsset = QualitySettings.renderPipeline;

                if (renderPipelineType == null)
                {
                    GraphicsSettings.defaultRenderPipeline = null;
                    QualitySettings.renderPipeline = null;
                }
                else
                {
#if UNITY_EDITOR
                    var asset = RenderPipelineUtils.LoadAsset(renderPipelineType);
                    GraphicsSettings.defaultRenderPipeline = asset;
                    QualitySettings.renderPipeline = asset;
#else
                    throw new NotImplementedException("There is no implementation for Runtime RenderPipeline switch.");
#endif
                }

                if (forceInitialization)
                    ForceInitialization();

                m_WasChanged = true;
            }
        }

        public static void ForceInitialization()
        {
            if (Camera.main != null)
                Camera.main.Render();
            else
            {
                var gameObject = new GameObject();
                var camera = gameObject.AddComponent<Camera>();
                camera.Render();
                Object.DestroyImmediate(camera);
            }
        }

        public void Dispose()
        {
            if (!m_WasChanged) return;

            GraphicsSettings.defaultRenderPipeline = m_GraphicsPipelineAsset;
            QualitySettings.renderPipeline = m_QualityPipelineAsset;

            if (!m_WasCreated) return;

            Object.DestroyImmediate(m_CreatedAsset);
        }
    }
}
