using System;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal abstract class BaseRendererBatcher : IDisposable
    {
        public abstract void UpdateRenderers(NativeArray<int> renderersID);
        public abstract void DestroyInstances(NativeArray<InstanceHandle> destroyedInstances);
        public abstract void DestroyMaterials(NativeArray<int> destroyedMaterials);
        public abstract void DestroyMeshes(NativeArray<int> destroyedMeshes);
        public abstract void UpdateFrame();
        public abstract void PostCullBeginCameraRendering(RenderRequestBatcherContext context);
        public abstract void Dispose();
    }

    /// <summary>
    /// Context struct for passing into BaseRendererBatcherPipeline.PostCullBeginCameraRendering
    /// </summary>
    public struct RenderRequestBatcherContext
    {
        /// <summary>
        /// CommandBuffer that will be used for resulting commands
        /// </summary>
        public CommandBuffer commandBuffer;

        /// <summary>
        /// Ambient probe to be set
        /// </summary>
        public SphericalHarmonicsL2 ambientProbe;
    }

    /// <summary>
    /// Static utility class for updating data post cull in begin camera rendering
    /// </summary>
    public static class BaseRendererBatcherPipeline
    {
        private static BaseRendererBatcher s_Batcher = null;
        internal static BaseRendererBatcher batcher => s_Batcher;

        internal static void Initialize(BaseRendererBatcher batcher)
        {
            Assert.IsTrue(s_Batcher == null);
            s_Batcher = batcher;
        }

        internal static void Shutdown()
        {
            Assert.IsTrue(s_Batcher != null);
            s_Batcher = null;
        }

        /// <summary>
        /// Static utility function for updating data post cull in begin camera rendering
        /// </summary>
        /// <param name="context">
        /// Context containing the data to be set
        /// </param>
        public static void PostCullBeginCameraRendering(RenderRequestBatcherContext context)
        {
            if (s_Batcher == null)
                return;

            s_Batcher.PostCullBeginCameraRendering(context);
        }
    }
}
