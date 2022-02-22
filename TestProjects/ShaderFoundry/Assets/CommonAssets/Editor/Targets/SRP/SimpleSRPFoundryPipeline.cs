using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Foundry
{
    public class SimpleSRPFoundryPipelineInstance : RenderPipeline
    {
        SimpleSRPFoundryPipeline m_Parent;

        public SimpleSRPFoundryPipelineInstance(SimpleSRPFoundryPipeline parent)
        {
            m_Parent = parent;
        }

        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            m_Parent.Render(renderContext, cameras);
        }
    }

    [ExecuteInEditMode]
    public class SimpleSRPFoundryPipeline : RenderPipelineAsset
    {
#if UNITY_EDITOR
        [UnityEditor.MenuItem("RenderPipeline/Create SimpleSRPFoundryPipeline")]
        static void CreateSimpleRenderLoop()
        {
            var instance = CreateInstance<SimpleSRPFoundryPipeline>();
            UnityEditor.AssetDatabase.CreateAsset(instance,
                "Assets/CommonAssets/Editor/Targets/SRP/SimpleSRPFoundryPipeline.asset");
        }

#endif

        protected override RenderPipeline CreatePipeline()
        {
            return new SimpleSRPFoundryPipelineInstance(this);
        }

        [NonSerialized]
        private CubemapArray m_ReflProbes;


        public void Render(ScriptableRenderContext context, IEnumerable<Camera> cameras)
        {
            foreach (var camera in cameras)
            {
                // Culling
                ScriptableCullingParameters cullingParams;

                if (!camera.TryGetCullingParameters(out cullingParams))
                    continue;
                CullingResults cull = context.Cull(ref cullingParams);

                // Setup camera for rendering (sets render target, view/projection matrices and other
                // per-camera built-in shader variables).
                context.SetupCameraProperties(camera);

                // render opaque objects using Deferred pass
                var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
                var drawingSettings = new DrawingSettings(new ShaderTagId("Foundry Unlit Forward"), sortingSettings);
                var filterSettings = new FilteringSettings(RenderQueueRange.opaque);

                CommandBuffer cmd = new CommandBuffer();
                cmd.ClearRenderTarget(true, true, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Dispose();

                context.DrawRenderers(cull, ref drawingSettings, ref filterSettings);

                context.Submit();
            }
        }
    }
}
