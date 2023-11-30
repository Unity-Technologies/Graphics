using System;
using System.IO;
using Unity.Collections;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    internal static partial class HDTextureUtilities
    {
        public static void WriteTextureToDisk(Texture target, string filePath)
        {
            var rt = target as RenderTexture;
            var cube = target as Cubemap;
            if (rt != null)
            {
                var t2D = RenderTextureToTexture(rt) as Texture2D;
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

        // Write to disk via the Unity Asset Pipeline rather than File.WriteAllBytes.
        public static void WriteTextureToAsset(Texture target, string filePath)
        {
#if UNITY_EDITOR
            var rt = target as RenderTexture;

            if (rt == null)
                return;

            HDBakingUtilities.CreateParentDirectoryIfMissing(filePath);
            AssetDatabase.CreateAsset(RenderTextureToTexture(rt), filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// Export a render texture to a texture2D/3D.
        ///
        /// <list type="bullet">
        /// <item>Cubemap will be exported in a Texture2D of size (size * 6, size) and with a layout +X,-X,+Y,-Y,+Z,-Z</item>
        /// <item>Texture2D will be copied to a Texture2D</item>
        /// </list>
        /// </summary>
        /// <param name="source"></param>
        /// <returns>The copied texture.</returns>
        public static Texture2D CopyRenderTextureToTexture2D(RenderTexture source)
        {
            Assert.IsTrue(source.dimension is TextureDimension.Tex2D or TextureDimension.Cube);

            return (Texture2D)RenderTextureToTexture(source);
        }

        private static Texture RenderTextureToTexture(RenderTexture source)
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
                        cmd.CopyTexture(source, i, 0, 0, 0, resolution, resolution, result, 0, 0, i * resolution, 0);
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
                case TextureDimension.Tex3D:
                {
                    var result = new Texture3D(source.width, source.height, source.volumeDepth, format, TextureCreationFlags.None);

                    // Determine the number of bytes elements that need to be read based on the texture format.
                    int stagingMemorySize = (int)GraphicsFormatUtility.GetBlockSize(format) * (source.width * source.height * source.volumeDepth);

                    // Staging memory for the readback.
                    var stagingReadback = new NativeArray<byte>(stagingMemorySize, Allocator.Persistent);

                    // Async-readbacks do not work if the RT resource is not registered with the graphics API backend.
                    Assert.IsTrue(source.IsCreated());

                    // Request and wait for the GPU data to transfer into staging memory.
                    var request = AsyncGPUReadback.RequestIntoNativeArray(ref stagingReadback, source, 0, format);
                    request.WaitForCompletion();

                    // Finally transfer the staging memory into the texture asset.
                    result.SetPixelData(stagingReadback, 0);

                    // Free the staging memory.
                    stagingReadback.Dispose();

                    return result;
                }
                default:
                    throw new ArgumentException();
            }
        }
    }
}
