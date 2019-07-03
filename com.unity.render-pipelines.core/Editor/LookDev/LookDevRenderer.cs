using System;
using UnityEngine;
using UnityEngine.Rendering.Experimental.LookDev;
using IDataProvider = UnityEngine.Rendering.Experimental.LookDev.IDataProvider;

namespace UnityEditor.Rendering.Experimental.LookDev
{
    [Flags]
    public enum RenderingPass
    {
        First = 1,
        Last = 2
    }

    public class RenderingData
    {
        public bool resized;
        public Stage stage;
        public ICameraUpdater updater;
        public Rect viewPort;
        public RenderTexture output;
    }

    public class Renderer
    {
        public bool pixelPerfect { get; set; }

        public Renderer(bool pixelPerfect = false)
            => this.pixelPerfect = pixelPerfect;

        void BeginRendering(RenderingData data, RenderingPass pass = 0)
        {
            data.stage.SetGameObjectVisible(true);
            data.updater?.UpdateCamera(data.stage.camera);
            data.stage.camera.enabled = true;
            UpdateOutputSize(data);
        }

        void EndRendering(RenderingData data)
        {
            data.stage.camera.enabled = false;
            data.stage.SetGameObjectVisible(false);
        }

        void UpdateOutputSize(RenderingData data)
        {
            var oldOutput = data.output;
            data.output = RenderTextureCache.UpdateSize(
                data.output, data.viewPort, pixelPerfect, data.stage.camera);
            data.resized = oldOutput != data.output;
        }

        bool CheckInvertedOutput(RenderingData data)
        {
            if (data.viewPort.IsNullOrInverted())
            {
                data.output = null;
                data.resized = true;
                return true;
            }
            return false;
        }

        public void Acquire(RenderingData data, RenderingPass pass = RenderingPass.First | RenderingPass.Last)
        {
            if (CheckInvertedOutput(data))
                return;

            if((pass & RenderingPass.First) != 0)
                BeginRendering(data, pass);
            data.stage.camera.targetTexture = data.output;
            data.stage.camera.Render();
            if ((pass & RenderingPass.Last) != 0)
                EndRendering(data);
        }
        
        internal static void DrawFullScreenQuad(Rect rect)
        {
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Viewport(rect);

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0, 0);
            GL.Vertex3(0f, 0f, 0);
            GL.TexCoord2(0, 1);
            GL.Vertex3(0f, 1f, 0);
            GL.TexCoord2(1, 1);
            GL.Vertex3(1f, 1f, 0);
            GL.TexCoord2(1, 0);
            GL.Vertex3(1f, 0f, 0);
            GL.End();
            GL.PopMatrix();
        }
    }

    public static partial class RectExtension
    {
        public static bool IsNullOrInverted(this Rect r)
            => r.width <= 0f || r.height <= 0f
            || float.IsNaN(r.width) || float.IsNaN(r.height);
    }
}
