using System;
using UnityEngine;
using UnityEngine.Rendering.Experimental.LookDev;
using IDataProvider = UnityEngine.Rendering.Experimental.LookDev.IDataProvider;

namespace UnityEditor.Rendering.Experimental.LookDev
{
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

        public void Acquire(RenderingData data)
        {
            if (data.viewPort.IsNullOrInverted())
            {
                data.output = null;
                data.resized = true;
                return;
            }

            BeginRendering(data);
            data.stage.camera.Render();
            EndRendering(data);
        }

        void BeginRendering(RenderingData data)
        {
            data.stage.SetGameObjectVisible(true);
            var oldOutput = data.output;
            data.output = RenderTextureCache.UpdateSize(
                data.output, data.viewPort, pixelPerfect, data.stage.camera);
            data.updater?.UpdateCamera(data.stage.camera);
            data.stage.camera.enabled = true;
            data.resized = oldOutput != data.output;
        }

        void EndRendering(RenderingData data)
        {
            data.stage.camera.enabled = false;
            data.stage.SetGameObjectVisible(false);
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
