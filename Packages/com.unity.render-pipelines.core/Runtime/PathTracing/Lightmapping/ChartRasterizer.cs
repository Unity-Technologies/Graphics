using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Lightmapping
{
    internal class ChartRasterizer : IDisposable
    {
        // Buffers required to execute chart rasterization
        public struct Buffers
        {
            public GraphicsBuffer vertex; // Only used in software path
            public GraphicsBuffer vertexToOriginalVertex; // Only used in software path
            public GraphicsBuffer vertexToChartID;
        }

        private readonly Material _softwareRasterizationMaterial;
        private readonly Material _hardwareRasterizationMaterial;

        private static class ShaderProperties
        {
            public static readonly int VertexBuffer = Shader.PropertyToID("g_VertexBuffer");
            public static readonly int VertexToOriginalVertex = Shader.PropertyToID("g_VertexToOriginalVertex");
            public static readonly int VertexToChartID = Shader.PropertyToID("g_VertexToChartID");
            public static readonly int ScaleAndOffset = Shader.PropertyToID("g_ScaleAndOffset");
            public static readonly int ChartIndexOffset = Shader.PropertyToID("g_ChartIndexOffset");
            public static readonly int Width = Shader.PropertyToID("g_Width");
            public static readonly int Height = Shader.PropertyToID("g_Height");
        }

        public ChartRasterizer(Shader softwareRasterizationShader, Shader hardwareRasterizationShader)
        {
            _softwareRasterizationMaterial = new Material(softwareRasterizationShader);
            _hardwareRasterizationMaterial = new Material(hardwareRasterizationShader);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(_softwareRasterizationMaterial);
            CoreUtils.Destroy(_hardwareRasterizationMaterial);
        }

#if UNITY_EDITOR
        public static void LoadShaders(out Shader softwareRasterizationShader, out Shader hardwareRasterizationShader)
        {
            softwareRasterizationShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/Lightmapping/ChartRasterizerSoftware.shader");
            hardwareRasterizationShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(
                "Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/Lightmapping/ChartRasterizerHardware.shader");
        }
#endif

        private static Vector2[] SelectUVBuffer(Mesh from)
        {
            var uv2 = from.uv2;
            if (uv2 != null && uv2.Length > 0)
                return uv2;
            else
                return from.uv;
        }

        public static void PrepareRasterizeSoftware(CommandBuffer cmd, Mesh from, GraphicsBuffer vertexBuffer, GraphicsBuffer vertexToOriginalVertexBuffer)
        {
            var originalUVs = from.vertices;
            var originalIndices = from.triangles;
            var uvs = new Vector2[originalIndices.Length];
            var vertexIds = new uint[originalIndices.Length];

            for (int i = 0; i < originalIndices.Length; i++)
            {
                uint originalVertexIdx = (uint)originalIndices[i];
                uvs[i] = originalUVs[originalVertexIdx];
                vertexIds[i] = originalVertexIdx;
            }

            cmd.SetBufferData(vertexBuffer, uvs);
            cmd.SetBufferData(vertexToOriginalVertexBuffer, vertexIds);
        }

        public void RasterizeSoftware(CommandBuffer cmd, GraphicsBuffer vertexBuffer, GraphicsBuffer vertexToOriginalVertexBuffer, GraphicsBuffer vertexToChartIdBuffer, uint indexCount, Vector4 scaleAndOffset, uint chartIndexOffset, RenderTexture destination)
        {
            cmd.SetGlobalBuffer(ShaderProperties.VertexBuffer, vertexBuffer);
            cmd.SetGlobalBuffer(ShaderProperties.VertexToOriginalVertex, vertexToOriginalVertexBuffer);
            cmd.SetGlobalBuffer(ShaderProperties.VertexToChartID, vertexToChartIdBuffer);
            cmd.SetGlobalVector(ShaderProperties.ScaleAndOffset, scaleAndOffset);
            cmd.SetGlobalInt(ShaderProperties.ChartIndexOffset, (int)chartIndexOffset);
            cmd.SetGlobalInteger(ShaderProperties.Width, destination.width);
            cmd.SetGlobalInteger(ShaderProperties.Height, destination.height);

            cmd.SetRenderTarget(destination);
            cmd.DrawProcedural(Matrix4x4.identity, _softwareRasterizationMaterial, 0, MeshTopology.Triangles, (int)indexCount);
        }

        public void RasterizeHardware(CommandBuffer cmd, Mesh mesh, GraphicsBuffer vertexToChartIdBuffer, Vector4 scaleAndOffset, uint chartIndexOffset, RenderTexture destination)
        {
            Debug.Assert(SystemInfo.supportsConservativeRaster, "Conservative rasterization is not supported on the current platform.");

            cmd.SetGlobalBuffer(ShaderProperties.VertexToChartID, vertexToChartIdBuffer);
            cmd.SetGlobalVector(ShaderProperties.ScaleAndOffset, scaleAndOffset);
            cmd.SetGlobalInt(ShaderProperties.ChartIndexOffset, (int)chartIndexOffset);

            cmd.SetRenderTarget(destination);

            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                cmd.DrawMesh(mesh, Matrix4x4.identity, _hardwareRasterizationMaterial, i, 0);
            }
        }
    }
}
