
using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Specifies the execution timing for a <c>ScriptableRenderPass2D</c> within the 2D renderer pipeline.
    /// This enum allows you to control when a custom render pass is injected relative to key stages
    /// of the 2D rendering process, such as normals, shadows, lights, sprites, and post-processing.
    /// </summary>
    public enum RenderPassEvent2D
    {
        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> before rendering any other passes in the pipeline.
        /// Camera matrices and stereo rendering are not set up at this point.
        /// Use this to draw to custom input textures used later in the pipeline, for example, LUT textures.
        /// </summary>
        BeforeRendering = 0,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> before rendering normals.
        /// You can target sorting layers when using this event.
        /// </summary>
        BeforeRenderingNormals = 100,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> after rendering normals.
        /// You can target sorting layers when using this event.
        /// </summary>
        AfterRenderingNormals = 200,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> before rendering shadows.
        /// You can target sorting layers when using this event.
        /// </summary>
        BeforeRenderingShadows = 300,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> after rendering shadows.
        /// You can target sorting layers when using this event.
        /// </summary>
        AfterRenderingShadows = 400,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> before rendering lights.
        /// You can target sorting layers when using this event.
        /// </summary>
        BeforeRenderingLights = 500,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> after rendering lights.
        /// You can target sorting layers when using this event.
        /// </summary>
        AfterRenderingLights = 600,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> before rendering sprites.
        /// You can target sorting layers when using this event.
        /// </summary>
        BeforeRenderingSprites = 700,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> after rendering sprites.
        /// You can target sorting layers when using this event.
        /// </summary>
        AfterRenderingSprites = 800,
        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> before rendering post-processing.
        /// </summary>
        BeforeRenderingPostProcessing = 900,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> after rendering post-processing.
        /// </summary>
        AfterRenderingPostProcessing = 1000,

        /// <summary>
        /// Executes a <c>ScriptableRenderPass2D</c> after all rendering is complete.
        /// </summary>
        AfterRendering = 1100,
    }

    internal static class RenderPassEvents2DEnumValues
    {
        // We cache the values in this array at construction time to avoid runtime allocations, which we would cause if we accessed valuesInternal directly.
        public static int[] values;

        static RenderPassEvents2DEnumValues()
        {
            System.Array valuesInternal = Enum.GetValues(typeof(RenderPassEvent2D));

            values = new int[valuesInternal.Length];

            int index = 0;
            foreach (int value in valuesInternal)
            {
                values[index] = value;
                index++;
            }
        }
    }

    /// <summary>
    /// <c>ScriptableRenderPass2D</c> implements a logical rendering pass with which you can extend the 2D renderer.
    /// </summary>
    public abstract class ScriptableRenderPass2D : ScriptableRenderPass
    {
        /// <summary>
        /// The event that occurs when the render pass executes with the 2d renderer.
        /// </summary>
        public RenderPassEvent2D renderPassEvent2D { get; set; }

        /// <summary>
        /// The sorting layer that the render pass executes on the 2d renderer.
        /// </summary>
        public int renderPassSortingLayerID { get; set; }

        static internal int GetRenderPassEventRange(RenderPassEvent2D renderPassEvent2D)
        {
            int numEvents = RenderPassEvents2DEnumValues.values.Length;
            int currentIndex = 0;

            // Find the index of the renderPassEvent in the values array.
            for (int i = 0; i < numEvents; ++i)
            {
                if (RenderPassEvents2DEnumValues.values[currentIndex] == (int)renderPassEvent2D)
                    break;

                currentIndex++;
            }

            if (currentIndex >= numEvents)
            {
                Debug.LogError("GetRenderPassEventRange: invalid renderPassEvent2D value cannot be found in the RenderPassEvent2D enumeration");
                return 0;
            }

            if (currentIndex + 1 >= numEvents)
                return 50; // If this was the enum's last event, add 50 as the range.

            int nextValue = RenderPassEvents2DEnumValues.values[currentIndex + 1];

            return nextValue - (int)renderPassEvent2D;
        }

        static internal bool IsSortingLayerEvent(RenderPassEvent2D renderPassEvent)
        {
            return renderPassEvent >= RenderPassEvent2D.BeforeRenderingNormals && renderPassEvent <= RenderPassEvent2D.AfterRenderingSprites;
        }
    }

    static internal class ScriptableRenderPass2DExtension
    {
        static internal void GetInjectionPoint2D(this ScriptableRenderPass renderPass, out RenderPassEvent2D rpEvent, out int rpLayer)
        {
            ScriptableRenderPass2D renderPass2D = renderPass as ScriptableRenderPass2D;

            if (renderPass2D == null)
            {
                rpLayer = int.MinValue;

                if (renderPass.renderPassEvent <= RenderPassEvent.BeforeRenderingTransparents)
                    rpEvent = RenderPassEvent2D.BeforeRendering;
                else if (renderPass.renderPassEvent <= RenderPassEvent.BeforeRenderingPostProcessing)
                    rpEvent = RenderPassEvent2D.BeforeRenderingPostProcessing;
                else if (renderPass.renderPassEvent <= RenderPassEvent.AfterRenderingPostProcessing)
                    rpEvent = RenderPassEvent2D.AfterRenderingPostProcessing;
                else
                    rpEvent = RenderPassEvent2D.AfterRendering;
            }
            else
            {
                rpEvent = renderPass2D.renderPassEvent2D;
                rpLayer = renderPass2D.renderPassSortingLayerID;
            }
        }
    }
}
