#if ENABLE_IMAGECONVERSION_MODULE
using System;
using System.Reflection;
#endif
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Lightmapping
{
    internal static class SerializationHelpers
    {
        internal static byte[] EncodeToR2D(this Texture2D tex)
        {
#if ENABLE_IMAGECONVERSION_MODULE
            Type encoder = typeof(ImageConversion);

            MethodInfo encodeMethod = encoder.GetMethod("EncodeToR2DInternal", BindingFlags.Static | BindingFlags.NonPublic);
            if (encodeMethod == null)
                return Array.Empty<byte>();

            var encodeFunc = (Func<Texture2D, byte[]>)Delegate.CreateDelegate(typeof(Func<Texture2D, byte[]>), encodeMethod);

            return encodeFunc(tex);
#else
            Debug.Assert(false, "The Image Conversion Module is not available.");
            return new byte[] { };
#endif
        }

        private static void ReadbackTexture(Texture2D texture, AsyncGPUReadbackRequest request)
        {
            if (request.hasError)
            {
                Debug.Assert(request.hasError, "GPU readback request error detected.");
                return;
            }
            if (request.done)
            {
                var data = request.GetData<byte>();
                texture.LoadRawTextureData(data);
                return;
            }
            Debug.Assert(false, "GPU readback request not done.");
        }

        internal static void WriteRenderTexture(CommandBuffer cmd, RenderTargetIdentifier renderTex, TextureFormat textureFormat, int width, int height, string path)
        {
            Texture2D readableTex = new Texture2D(width, height, textureFormat, false) { name = "readableTex (WriteRenderTexture)", hideFlags = HideFlags.HideAndDontSave };
            cmd.CopyTexture(renderTex, readableTex);
            cmd.RequestAsyncReadback(readableTex, 0, (AsyncGPUReadbackRequest request) => { ReadbackTexture(readableTex, request); });
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(path)))
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, readableTex.EncodeToR2D());
            CoreUtils.Destroy(readableTex);
        }

        internal static void WriteRenderTexture(CommandBuffer cmd, string path, RenderTexture renderTex)
        {
            Debug.Assert(renderTex.format == RenderTextureFormat.ARGBFloat);
            WriteRenderTexture(cmd, renderTex, TextureFormat.RGBAFloat, renderTex.width, renderTex.height, path);
        }
    }
}
