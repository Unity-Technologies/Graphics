using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDCameraEditor
    {
        void OnSceneGUI()
        {
            if (HDRenderPipeline.currentPipeline == null)
                return;

            if (!(target is Camera c) || c == null)
                return;

            if (!CameraEditorUtils.IsViewPortRectValidToRender(c.rect))
                return;

            UnityEditor.CameraEditorUtils.HandleFrustum(c, c.GetInstanceID());
        } 
    }
}
