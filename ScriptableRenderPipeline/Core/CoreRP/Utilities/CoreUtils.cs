using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering
{
    using UnityObject = UnityEngine.Object;

    [Flags]
    public enum ClearFlag
    {
        None  = 0,
        Color = 1,
        Depth = 2,

        All = Depth | Color
    }

    public static class CoreUtils
    {
        // Data useful for various cubemap processes.
        // Ref: https://msdn.microsoft.com/en-us/library/windows/desktop/bb204881(v=vs.85).aspx
        static public readonly Vector3[] lookAtList =
        {
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(-1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, -1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 0.0f, -1.0f),
        };

        static public readonly Vector3[] upVectorList =
        {
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 0.0f, -1.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
        };

        // Note: Color.Black have alpha channel set to 1. Most of the time we want alpha channel set to 0 as we use black to clear render target
        public static Color clearColorAllBlack { get { return new Color(0f, 0f, 0f, 0f); } }

        public const int editMenuPriority1 = 320;
        public const int editMenuPriority2 = 331;
        public const int editMenuPriority3 = 342;
        public const int assetCreateMenuPriority1 = 230;
        public const int assetCreateMenuPriority2 = 241;
        public const int gameObjectMenuPriority = 10;

        static Cubemap m_BlackCubeTexture;
        public static Cubemap blackCubeTexture
        {
            get
            {
                if (m_BlackCubeTexture == null)
                {
                    m_BlackCubeTexture = new Cubemap(1, TextureFormat.ARGB32, false);
                    for (int i = 0; i < 6; ++i)
                        m_BlackCubeTexture.SetPixel((CubemapFace)i, 0, 0, Color.black);
                    m_BlackCubeTexture.Apply();
                }

                return m_BlackCubeTexture;
            }
        }

        static Cubemap m_MagentaCubeTexture;
        public static Cubemap magentaCubeTexture
        {
            get
            {
                if (m_MagentaCubeTexture == null)
                {
                    m_MagentaCubeTexture = new Cubemap(1, TextureFormat.ARGB32, false);
                    for (int i = 0; i < 6; ++i)
                        m_MagentaCubeTexture.SetPixel((CubemapFace)i, 0, 0, Color.magenta);
                    m_MagentaCubeTexture.Apply();
                }

                return m_MagentaCubeTexture;
            }
        }

        static Cubemap m_WhiteCubeTexture;
        public static Cubemap whiteCubeTexture
        {
            get
            {
                if (m_WhiteCubeTexture == null)
                {
                    m_WhiteCubeTexture = new Cubemap(1, TextureFormat.ARGB32, false);
                    for (int i = 0; i < 6; ++i)
                        m_WhiteCubeTexture.SetPixel((CubemapFace)i, 0, 0, Color.white);
                    m_WhiteCubeTexture.Apply();
                }

                return m_WhiteCubeTexture;
            }
        }

        static RenderTexture m_EmptyUAV;
        public static RenderTexture emptyUAV
        {
            get
            {
                if(m_EmptyUAV == null)
                {
                    m_EmptyUAV = new RenderTexture(1, 1, 0);
                    m_EmptyUAV.enableRandomWrite = true;
                    m_EmptyUAV.Create();
                }

                return m_EmptyUAV;
            }
        }

        public static void ClearRenderTarget(CommandBuffer cmd, ClearFlag clearFlag, Color clearColor)
        {
            if (clearFlag != ClearFlag.None)
                cmd.ClearRenderTarget((clearFlag & ClearFlag.Depth) != 0, (clearFlag & ClearFlag.Color) != 0, clearColor);
        }

        // Render Target Management.
        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            cmd.SetRenderTarget(buffer, miplevel, cubemapFace, depthSlice);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier buffer, ClearFlag clearFlag = ClearFlag.None, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            SetRenderTarget(cmd, buffer, clearFlag, clearColorAllBlack, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, ClearFlag.None, clearColorAllBlack, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            SetRenderTarget(cmd, colorBuffer, depthBuffer, clearFlag, clearColorAllBlack, miplevel, cubemapFace, depthSlice);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor, int miplevel = 0, CubemapFace cubemapFace = CubemapFace.Unknown, int depthSlice = 0)
        {
            cmd.SetRenderTarget(colorBuffer, depthBuffer, miplevel, cubemapFace, depthSlice);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, ClearFlag.None, clearColorAllBlack);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag = ClearFlag.None)
        {
            SetRenderTarget(cmd, colorBuffers, depthBuffer, clearFlag, clearColorAllBlack);
        }

        public static void SetRenderTarget(CommandBuffer cmd, RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthBuffer, ClearFlag clearFlag, Color clearColor)
        {
            cmd.SetRenderTarget(colorBuffers, depthBuffer);
            ClearRenderTarget(cmd, clearFlag, clearColor);
        }

        public static string GetRenderTargetAutoName(int width, int height, RenderTextureFormat format, string name = "", bool mips = false, bool enableMSAA = false, MSAASamples msaaSamples = MSAASamples.None)
        {
            string temp;
            if (enableMSAA)
                temp = string.Format("{0}x{1}_{2}{3}_{4}", width, height, format, mips ? "_Mips"  : "", msaaSamples.ToString());
            else
                temp = string.Format("{0}x{1}_{2}{3}", width, height, format, mips ? "_Mips" : "");

            temp = String.Format("{0}_{1}", name == "" ? "RenderTarget" : name, temp);

            return temp;
        }

        public static string GetTextureAutoName(int width, int height, TextureFormat format, TextureDimension dim = TextureDimension.None, string name = "", bool mips = false, int depth = 0)
        {
            string temp;
            if(depth == 0)
                temp = string.Format("{0}x{1}_{2}{3}", width, height, format, mips ? "_Mips" : "");
            else
                temp = string.Format("{0}x{1}x{2}_{3}{4}", width, height, depth, format, mips ? "_Mips" : "");
            temp = String.Format("{0}_{1}_{2}", name == "" ? "Texture" : name, (dim == TextureDimension.None) ? "" : dim.ToString(), temp);

            return temp;
        }

        public static void ClearCubemap(CommandBuffer cmd, RenderTexture renderTexture, Color clearColor, bool clearMips = false)
        {
            int mipCount = 1;
            if (renderTexture.useMipMap && clearMips)
            {
                mipCount = (int)Mathf.Log((float)renderTexture.width, 2.0f) + 1;
            }

            for (int i = 0; i < 6; ++i)
            {
                for (int mip = 0; mip < mipCount; ++mip)
                {
                    SetRenderTarget(cmd, new RenderTargetIdentifier(renderTexture), ClearFlag.Color, clearColor, mip, (CubemapFace)i);
                }
            }
        }

        // Draws a full screen triangle as a faster alternative to drawing a full screen quad.
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier colorBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier colorBuffer, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffer, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers, RenderTargetIdentifier depthStencilBuffer,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            commandBuffer.SetRenderTarget(colorBuffers, depthStencilBuffer);
            commandBuffer.DrawProcedural(Matrix4x4.identity, material, shaderPassId, MeshTopology.Triangles, 3, 1, properties);
        }

        // Important: the first RenderTarget must be created with 0 depth bits!
        public static void DrawFullScreen(CommandBuffer commandBuffer, Material material,
            RenderTargetIdentifier[] colorBuffers,
            MaterialPropertyBlock properties = null, int shaderPassId = 0)
        {
            // It is currently not possible to have MRT without also setting a depth target.
            // To work around this deficiency of the CommandBuffer.SetRenderTarget() API,
            // we pass the first color target as the depth target. If it has 0 depth bits,
            // no depth target ends up being bound.
            DrawFullScreen(commandBuffer, material, colorBuffers, colorBuffers[0], properties, shaderPassId);
        }

        // Post-processing misc
        public static bool IsPostProcessingActive(PostProcessLayer layer)
        {
            return layer != null
                && layer.enabled;
        }

        public static bool IsTemporalAntialiasingActive(PostProcessLayer layer)
        {
            return IsPostProcessingActive(layer)
                && layer.antialiasingMode == PostProcessLayer.Antialiasing.TemporalAntialiasing
                && layer.temporalAntialiasing.IsSupported();
        }

        // Color space utilities
        public static Color ConvertSRGBToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color.linear : color;
        }

        public static Color ConvertLinearToActiveColorSpace(Color color)
        {
            return (QualitySettings.activeColorSpace == ColorSpace.Linear) ? color : color.gamma;
        }

        // Unity specifics
        public static Material CreateEngineMaterial(string shaderPath)
        {
            var mat = new Material(Shader.Find(shaderPath))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public static Material CreateEngineMaterial(Shader shader)
        {
            var mat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return mat;
        }

        public static void SetKeyword(CommandBuffer cmd, string keyword, bool state)
        {
            if (state)
                cmd.EnableShaderKeyword(keyword);
            else
                cmd.DisableShaderKeyword(keyword);
        }

        // Caution: such a call should not be use interlaced with command buffer command, as it is immediate
        public static void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        public static void SelectKeyword(Material material, string keyword1, string keyword2, bool enableFirst)
        {
            material.EnableKeyword(enableFirst ? keyword1 : keyword2);
            material.DisableKeyword(enableFirst ? keyword2 : keyword1);
        }

        public static void SelectKeyword(Material material, string[] keywords, int enabledKeywordIndex)
        {
            material.EnableKeyword(keywords[enabledKeywordIndex]);

            for (int i = 0; i < keywords.Length; i++)
            {
                if (i != enabledKeywordIndex)
                    material.DisableKeyword(keywords[i]);
            }
        }

        public static void Destroy(UnityObject obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    UnityObject.Destroy(obj);
                else
                    UnityObject.DestroyImmediate(obj);
#else
                UnityObject.Destroy(obj);
#endif
            }
        }

        static IEnumerable<Type> m_AssemblyTypes;

        public static IEnumerable<Type> GetAllAssemblyTypes()
        {
            if (m_AssemblyTypes == null)
            {
                m_AssemblyTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(t =>
                    {
                        // Ugly hack to handle mis-versioned dlls
                        var innerTypes = new Type[0];
                        try
                        {
                            innerTypes = t.GetTypes();
                        }
                        catch { }
                        return innerTypes;
                    });
            }

            return m_AssemblyTypes;
        }

        public static void Destroy(params UnityObject[] objs)
        {
            if (objs == null)
                return;

            foreach (var o in objs)
                Destroy(o);
        }

        public static void SafeRelease(ComputeBuffer buffer)
        {
            if (buffer != null)
                buffer.Release();
        }

        // Just a sort function that doesn't allocate memory
        // Note: Shoud be repalc by a radix sort for positive integer
        public static int Partition(uint[] numbers, int left, int right)
        {
            uint pivot = numbers[left];
            while (true)
            {
                while (numbers[left] < pivot)
                    left++;

                while (numbers[right] > pivot)
                    right--;

                if (left < right)
                {
                    uint temp = numbers[right];
                    numbers[right] = numbers[left];
                    numbers[left] = temp;
                }
                else
                {
                    return right;
                }
            }
        }

        public static void QuickSort(uint[] arr, int left, int right)
        {
            // For Recursion
            if (left < right)
            {
                int pivot = Partition(arr, left, right);

                if (pivot > 1)
                    QuickSort(arr, left, pivot - 1);

                if (pivot + 1 < right)
                    QuickSort(arr, pivot + 1, right);
            }
        }

        public static Mesh CreateCubeMesh(Vector3 min, Vector3 max)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[8];

            vertices[0] = new Vector3(min.x, min.y, min.z);
            vertices[1] = new Vector3(max.x, min.y, min.z);
            vertices[2] = new Vector3(max.x, max.y, min.z);
            vertices[3] = new Vector3(min.x, max.y, min.z);
            vertices[4] = new Vector3(min.x, min.y, max.z);
            vertices[5] = new Vector3(max.x, min.y, max.z);
            vertices[6] = new Vector3(max.x, max.y, max.z);
            vertices[7] = new Vector3(min.x, max.y, max.z);

            mesh.vertices = vertices;

            int[] triangles = new int[36];

            triangles[0] = 0; triangles[1] = 2; triangles[2] = 1;
            triangles[3] = 0; triangles[4] = 3; triangles[5] = 2;
            triangles[6] = 1; triangles[7] = 6; triangles[8] = 5;
            triangles[9] = 1; triangles[10] = 2; triangles[11] = 6;
            triangles[12] = 5; triangles[13] = 7; triangles[14] = 4;
            triangles[15] = 5; triangles[16] = 6; triangles[17] = 7;
            triangles[18] = 4; triangles[19] = 3; triangles[20] = 0;
            triangles[21] = 4; triangles[22] = 7; triangles[23] = 3;
            triangles[24] = 3; triangles[25] = 6; triangles[26] = 2;
            triangles[27] = 3; triangles[28] = 7; triangles[29] = 6;
            triangles[30] = 4; triangles[31] = 1; triangles[32] = 5;
            triangles[33] = 4; triangles[34] = 0; triangles[35] = 1;

            mesh.triangles = triangles;
            return mesh;
        }
    }
}
