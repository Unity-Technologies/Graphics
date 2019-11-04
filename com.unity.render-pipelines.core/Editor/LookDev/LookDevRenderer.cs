using System;
using UnityEngine;
using UnityEngine.Rendering.LookDev;
using IDataProvider = UnityEngine.Rendering.LookDev.IDataProvider;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>Data container to be used with Renderer class</summary>
    public class RenderingData : IDisposable
    {
        /// <summary>
        /// Internally set to true when the given RenderTexture <see cref="output"/> was not the good size regarding <see cref="viewPort"/> and needed to be recreated
        /// </summary>
        public bool sizeMissmatched;
        /// <summary>The stage that possess every object in your view</summary>
        public Stage stage;
        /// <summary>Callback to update the Camera position. Only done in First phase.</summary>
        public ICameraUpdater updater;
        /// <summary>Viewport size</summary>
        public Rect viewPort;
        /// <summary>Render texture handling captured image</summary>
        public RenderTexture output;

        private bool disposed = false;
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            stage?.Dispose();
            stage = null;
            updater = null;
            output?.Release();
            output = null;
        }
    }

    /// <summary>Basic renderer to draw scene in texture</summary>
    public class Renderer
    {
        public bool pixelPerfect { get; set; }

        public Renderer(bool pixelPerfect = false)
            => this.pixelPerfect = pixelPerfect;

        public void BeginRendering(RenderingData data)
        {
            data.stage.SetGameObjectVisible(true);
            data.updater?.UpdateCamera(data.stage.camera);
            data.stage.camera.enabled = true;
        }

        public void EndRendering(RenderingData data)
        {
            data.stage.camera.enabled = false;
            data.stage.SetGameObjectVisible(false);
        }

        bool CheckWrongSizeOutput(RenderingData data)
        {
            if (data.viewPort.IsNullOrInverted()
                || data.viewPort.width != data.output.width
                || data.viewPort.height != data.viewPort.height)
            {
                data.output = null;
                data.sizeMissmatched = true;
                return true;
            }

            data.sizeMissmatched = false;
            return false;
        }

        /// <summary>
        /// Capture image of the scene.
        /// </summary>
        /// <param name="data">Datas required to compute the capture</param>
        /// [Optional] When drawing several time the scene, you can remove First and/or Last to not initialize objects.
        /// Be careful though to always start your frame with a First and always end with a Last.
        /// </param>
        public void Acquire(RenderingData data)
        {
            if (CheckWrongSizeOutput(data))
                return;

            data.stage.camera.targetTexture = data.output;
            data.stage.camera.Render();
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
        /// <summary>Return true if the <see cref="Rect"/> is null sized or inverted.</summary>
        public static bool IsNullOrInverted(this Rect r)
            => r.width <= 0f || r.height <= 0f
            || float.IsNaN(r.width) || float.IsNaN(r.height);
    }
}
