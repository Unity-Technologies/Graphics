using NUnit.Framework;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.RenderPipelines.Core.Runtime.Shared;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering.Tests
{
    internal partial class RenderGraphTests : RenderGraphTestsCore
    {       
        TextureHandle CreateTextureHandle()
        {
            var desc = new TextureDesc(16, 16);
            desc.format = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            return m_RenderGraph.CreateTexture(desc);
        }

        TextureHandle CreateTextureHandleDepth()
        {
            var desc = new TextureDesc(16, 16);
            desc.format = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
            return m_RenderGraph.CreateTexture(desc);
        }

        class OnTileValidationTestData{

        }

        [Test]
        public void OnTileValidationLayer_SingleRasterPass()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();

            validationLayer.Add(tex);

            Assert.DoesNotThrow(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Test Pass", out OnTileValidationTestData passData))
                {
                    builder.SetRenderAttachment(tex, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_NoOutOfBounds()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();
            validationLayer.Add(tex);

            int numberOfPasses = 50;
            int numberOfOnTileResources = 20;

            // Test if the buffers get reallocated properly, ie no out of bounds exception
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < numberOfPasses; i++)
                {
                    using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Test Pass", out OnTileValidationTestData passData))
                    {
                        builder.SetRenderAttachment(tex, 0);
                        builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                    }
                }

                for (int i = 0; i < numberOfOnTileResources; i++)
                {
                    var t = CreateTextureHandle();
                    validationLayer.Add(t);
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_NoValidationWhenRemoved()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();

            validationLayer.Add(tex);

            using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Test Pass", out OnTileValidationTestData passData))
            {
                builder.SetRenderAttachment(tex, 0);
                builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
            }

            validationLayer.Remove(tex);

            Assert.DoesNotThrow(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Test Pass", out OnTileValidationTestData passData))
                {
                    builder.UseTexture(tex);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });
        }

        OnTileValidationLayer CreateValidationLayer()
        {
            var validationLayer = new OnTileValidationLayer();
            m_RenderGraph.validationLayer = validationLayer;
            validationLayer.renderGraph = m_RenderGraph;
            return validationLayer;
        }

        [Test]
        public void OnTileValidationLayer_UnsafePassInBetweenRasterThrows()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();

            validationLayer.Add(tex);

            using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 1", out OnTileValidationTestData passData))
            {
                builder.SetRenderAttachment(tex, 0);
                builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddUnsafePass<OnTileValidationTestData>("Unsafe 1", out OnTileValidationTestData passData))
            {
                builder.SetRenderFunc(static (OnTileValidationTestData data, UnsafeGraphContext context) => { });
            }

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 2", out OnTileValidationTestData passData))
                {
                    builder.SetRenderAttachment(tex, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_ComputePassInBetweenRasterThrows()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();

            validationLayer.Add(tex);

            using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 1", out OnTileValidationTestData passData))
            {
                builder.SetRenderAttachment(tex, 0);
                builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddComputePass<OnTileValidationTestData>("Compute 1", out OnTileValidationTestData passData))
            {
                builder.SetRenderFunc(static (OnTileValidationTestData data, ComputeGraphContext context) => { });
            }

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 2", out OnTileValidationTestData passData))
                {
                    builder.SetRenderAttachment(tex, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_UseTextureThrows()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();

            validationLayer.Add(tex);

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 1", out OnTileValidationTestData passData))
                {
                    builder.UseTexture(tex, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_ContraintsAreAddedToRenderAttachments()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();
            var texOutput = CreateTextureHandle();
            var texOutputDepth = CreateTextureHandleDepth();

            // We explicitly NOT add texOutput, it needs to be added automatically by the propagation
            validationLayer.Add(tex);

            using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 1", out OnTileValidationTestData passData))
            {
                builder.SetRenderAttachment(tex, 0);
                builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
            }

            using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 1", out OnTileValidationTestData passData))
            {
                builder.SetInputAttachment(tex, 0);
                builder.SetRenderAttachment(texOutput, 0);
                builder.SetRenderAttachmentDepth(texOutputDepth);
                builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
            }

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 2", out OnTileValidationTestData passData))
                {
                    builder.UseTexture(texOutput, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 3", out OnTileValidationTestData passData))
                {
                    builder.UseTexture(texOutputDepth, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_UseTextureInUnsafePassThrows()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();

            validationLayer.Add(tex);

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddUnsafePass<OnTileValidationTestData>("Unsafe 1", out OnTileValidationTestData passData))
                {
                    builder.UseTexture(tex, AccessFlags.Write);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, UnsafeGraphContext context) => { });
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_UseTextureInComputePassThrows()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();

            validationLayer.Add(tex);

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddComputePass<OnTileValidationTestData>("Compute 1", out OnTileValidationTestData passData))
                {
                    builder.UseTexture(tex, AccessFlags.Write);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, ComputeGraphContext context) => { });
                }
            });
        }

        [Test]
        public void OnTileValidationLayer_SetGlobalTextureAfterPassThrows()
        {
            var validationLayer = CreateValidationLayer();

            var tex = CreateTextureHandle();            

            validationLayer.Add(tex); 
            
            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 1", out OnTileValidationTestData passData))
                {
                    builder.SetRenderAttachment(tex, 0);
                    builder.SetGlobalTextureAfterPass(tex, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                var texOutput = CreateTextureHandle();

                // Checks the constraint propagation
                using (var builder = m_RenderGraph.AddRasterRenderPass<OnTileValidationTestData>("Raster 1", out OnTileValidationTestData passData))
                {
                    builder.SetInputAttachment(tex, 0);
                    builder.SetRenderAttachment(texOutput, 0);
                    builder.SetGlobalTextureAfterPass(texOutput, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, RasterGraphContext context) => { });
                }
            });

            Assert.Catch<System.InvalidOperationException>(() =>
            {
                using (var builder = m_RenderGraph.AddUnsafePass<OnTileValidationTestData>("Unsafe 1", out OnTileValidationTestData passData))
                {
                    builder.SetGlobalTextureAfterPass(tex, 0);
                    builder.SetRenderFunc(static (OnTileValidationTestData data, UnsafeGraphContext context) => { });
                }
            });
        }
    }
}
