using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    /// <summary>
    /// </summary>
    internal class GizmoRenderingPass : ScriptableRenderPass
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
