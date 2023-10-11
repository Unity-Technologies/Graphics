using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class CustomClear2D
    {
        static readonly string k_CustomClearPass = "CustomClear";
        static readonly string k_ClearColor = "_ClearColor";

        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_CustomClearPass);
        static Material m_ClearMaterial;
        static Mesh s_TriangleMesh;
        static MaterialPropertyBlock s_PropertyBlock = new MaterialPropertyBlock();

        static Vector4[] clearColors = new Vector4[4];
        static Color[] intermediateColor = new Color[1];

        internal static bool useCustomClear
        {
            get => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan ||
                   SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12 ||
                   SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal;
        }

        internal static bool isMetalArm64 
        {
            get => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal &&
                   System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64;
        }

        internal static void Cleanup()
        {
            CoreUtils.Destroy(s_TriangleMesh);
        }

        internal static void Initialize(Material clearMaterial)
        {
            m_ClearMaterial = clearMaterial;

            if (SystemInfo.graphicsShaderLevel < 30)
            {
                float nearClipZ = -1;
                if (SystemInfo.usesReversedZBuffer)
                    nearClipZ = 1;

                if (!s_TriangleMesh)
                {
                    s_TriangleMesh = new Mesh();
                    s_TriangleMesh.vertices = GetFullScreenTriangleVertexPosition(nearClipZ);
                    s_TriangleMesh.uv = GetFullScreenTriangleTexCoord();
                    s_TriangleMesh.triangles = new int[3] { 0, 1, 2 };
                }

                // Should match Common.hlsl
                static Vector3[] GetFullScreenTriangleVertexPosition(float z /*= UNITY_NEAR_CLIP_VALUE*/)
                {
                    var r = new Vector3[3];
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 uv = new Vector2((i << 1) & 2, i & 2);
                        r[i] = new Vector3(uv.x * 2.0f - 1.0f, uv.y * 2.0f - 1.0f, z);
                    }
                    return r;
                }

                // Should match Common.hlsl
                static Vector2[] GetFullScreenTriangleTexCoord()
                {
                    var r = new Vector2[3];
                    for (int i = 0; i < 3; i++)
                    {
                        if (SystemInfo.graphicsUVStartsAtTop)
                            r[i] = new Vector2((i << 1) & 2, 1.0f - (i & 2));
                        else
                            r[i] = new Vector2((i << 1) & 2, i & 2);
                    }
                    return r;
                }
            }
        }

        internal static void Clear(RasterCommandBuffer cmd, Color color)
        {
            intermediateColor[0] = color;
            Clear(cmd, intermediateColor);
        }

        internal static void Clear(RasterCommandBuffer cmd, Color[] color)
        {
            if (useCustomClear)
                Internal_Clear(cmd, color);
            else
                cmd.ClearRenderTarget(RTClearFlags.Color, color, 1, 0);
        }

        private static void Internal_Clear(RasterCommandBuffer cmd, Color[] colors)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                for (int i = 0; i < colors.Length; ++i)
                    clearColors[i] = colors[i];

                int shaderPass = 0;
                s_PropertyBlock.SetVectorArray(k_ClearColor, clearColors);

                if (SystemInfo.graphicsShaderLevel < 30)
                    cmd.DrawMesh(s_TriangleMesh, Matrix4x4.identity, m_ClearMaterial, 0, shaderPass, s_PropertyBlock);
                else
                    cmd.DrawProcedural(Matrix4x4.identity, m_ClearMaterial, shaderPass, MeshTopology.Triangles, 3, 1, s_PropertyBlock);
            }
        }

        private class PassData
        {
            internal RTClearFlags clearFlags;
            internal Color clearColor;
        }

        internal static void RasterPassClear(RenderGraph graph, in TextureHandle colorHandle, in TextureHandle depthHandle, RTClearFlags clearFlags, Color clearColor)
        {
            if (clearFlags == RTClearFlags.None)
                return;

            Debug.Assert(colorHandle.IsValid(), "Trying to clear an invalid render color target");

            if (clearFlags != RTClearFlags.Color)
                Debug.Assert(depthHandle.IsValid(), "Trying to clear an invalid depth target");

            using (var builder = graph.AddRasterRenderPass<PassData>("Clear Target", out var passData, m_ProfilingSampler))
            {
                builder.UseTextureFragment(colorHandle, 0);
                if (depthHandle.IsValid())
                    builder.UseTextureFragmentDepth(depthHandle);

                passData.clearFlags = clearFlags;
                passData.clearColor = clearColor;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(data.clearFlags, data.clearColor, 1, 0);
                });
            }
        }
    }
}
