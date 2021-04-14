using System;
using System.IO;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal static partial class HDTextureUtilities
    {
        public static void WriteTextureFileToDisk(Texture target, string filePath)
        {
            var rt = target as RenderTexture;
            var cube = target as Cubemap;
            if (rt != null)
            {
                var t2D = CopyRenderTextureToTexture2D(rt);
                var bytes = t2D.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                HDBakingUtilities.CreateParentDirectoryIfMissing(filePath);
                File.WriteAllBytes(filePath, bytes);
                return;
            }
            else if (cube != null)
            {
                var t2D = new Texture2D(cube.width * 6, cube.height, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.None);
                var cmd = new CommandBuffer { name = "CopyCubemapToTexture2D" };
                for (int i = 0; i < 6; ++i)
                {
                    cmd.CopyTexture(
                        cube, i, 0, 0, 0, cube.width, cube.height,
                        t2D, 0, 0, cube.width * i, 0
                    );
                }
                Graphics.ExecuteCommandBuffer(cmd);
                var bytes = t2D.EncodeToEXR(Texture2D.EXRFlags.CompressZIP);
                HDBakingUtilities.CreateParentDirectoryIfMissing(filePath);
                File.WriteAllBytes(filePath, bytes);
                return;
            }
            throw new ArgumentException();
        }

        /// <summary>
        /// Export a render texture to a texture2D.
        ///
        /// <list type="bullet">
        /// <item>Cubemap will be exported in a Texture2D of size (size * 6, size) and with a layout +X,-X,+Y,-Y,+Z,-Z</item>
        /// <item>Texture2D will be copied to a Texture2D</item>
        /// </list>
        /// </summary>
        /// <param name="source"></param>
        /// <returns>The copied Texture2D.</returns>
        public static Texture2D CopyRenderTextureToTexture2D(RenderTexture source)
        {
            GraphicsFormat format = source.graphicsFormat;

            switch (source.dimension)
            {
                case TextureDimension.Cube:
                {
                    var resolution = source.width;

                    var result = RenderTexture.GetTemporary(resolution * 6, resolution, 0, source.format);
                    var cmd = new CommandBuffer();
                    for (var i = 0; i < 6; ++i)
                        cmd.CopyTexture(source, i, 0,  0, 0, resolution, resolution, result, 0, 0, i * resolution, 0);
                    Graphics.ExecuteCommandBuffer(cmd);

                    var t2D = new Texture2D(resolution * 6, resolution, format, TextureCreationFlags.None);
                    var a = RenderTexture.active;
                    RenderTexture.active = result;
                    t2D.ReadPixels(new Rect(0, 0, 6 * resolution, resolution), 0, 0, false);
                    RenderTexture.active = a;
                    RenderTexture.ReleaseTemporary(result);

                    return t2D;
                }
                case TextureDimension.Tex2D:
                {
                    var resolution = source.width;
                    var result = new Texture2D(resolution, resolution, format, TextureCreationFlags.None);

                    Graphics.SetRenderTarget(source, 0);
                    result.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
                    result.Apply();
                    Graphics.SetRenderTarget(null);

                    return result;
                }
                default:
                    throw new ArgumentException();
            }
        }
    }
}
