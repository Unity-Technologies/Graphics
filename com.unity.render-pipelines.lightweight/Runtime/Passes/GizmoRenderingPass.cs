using UnityEngine.Rendering;
using UnityEditor;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    /// <summary>
    /// </summary>
    public class GizmoRenderingPass : ScriptableRenderPass
    {
        private bool renderLitGizmos { get; set; }

        public GizmoRenderingPass()
        {
            this.renderLitGizmos = true;
        }

        public void Setup(bool renderLitGizmos)
        {
            this.renderLitGizmos = renderLitGizmos;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if UNITY_EDITOR
            Camera camera = renderingData.cameraData.camera;

            if (Handles.ShouldRenderGizmos())
                context.DrawGizmos(camera, this.renderLitGizmos ? GizmoSubset.PreImageEffects : GizmoSubset.PostImageEffects);
#endif
        }
    }
}
