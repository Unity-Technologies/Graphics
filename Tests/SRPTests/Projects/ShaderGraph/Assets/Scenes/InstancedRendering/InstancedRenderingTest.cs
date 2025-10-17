using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering;

public enum TestAPI
{
    DrawMeshInstanced,
    DrawMeshInstancedIndirect,
    DrawMeshInstancedProcedural,
    DrawProcedural,
    DrawProceduralIndirect,
    DrawProceduralNow,
    DrawProceduralIndirectNow,

    CommandBufferDrawMeshInstanced,
    CommandBufferDrawMeshInstancedIndirect,
    CommandBufferDrawMeshInstancedProcedural,
    CommandBufferDrawProcedural,
    CommandBufferDrawProceduralIndirect,
}

[ExecuteAlways]
public class InstancedRenderingTest : MonoBehaviour
{
    [Serializable]
    public struct MyTransform
    {
        public Vector3 Position;
        public Vector3 EulerAngles;
        public Vector3 Scale;
    }

    public TestAPI testApi;

    public Mesh mesh;
    public Material material;
    public List<MyTransform> transforms = new();

    private Matrix4x4[] m_Matrices;
    private MaterialPropertyBlock m_SetupRenderingLayerForCommandBuffer;
    private Material m_ClonedMaterial;
    private int m_LastMaterialCRC;
    private GraphicsBuffer m_DrawArgsBuffer;
    private int m_LastDrawArgsInstanceCount;
    private int m_LastDrawArgsIndexCount;
    private GraphicsBuffer m_TransformsBuffer;
    private uint m_LastTransformsBufferHash;
    private GraphicsBuffer m_VertexBuffer;
    private GraphicsBuffer m_NormalBuffer;
    private Mesh m_LastMesh;
    private GraphicsBuffer m_LastMeshIndexBuffer;

    private static List<float> s_RenderingLayerArray = new();

    private void OnEnable()
    {
        DoRender(null, true);
    }

    private void OnDisable()
    {

        m_Matrices = null;
        if (m_ClonedMaterial != null)
        {
            UnityEngine.Object.DestroyImmediate(m_ClonedMaterial);
            m_ClonedMaterial = null;
        }
        m_LastMaterialCRC = 0;
        m_DrawArgsBuffer?.Dispose();
        m_DrawArgsBuffer = null;
        m_LastDrawArgsInstanceCount = 0;
        m_LastDrawArgsIndexCount = 0;
        m_TransformsBuffer?.Dispose();
        m_TransformsBuffer = null;
        m_LastTransformsBufferHash = 0;
        m_VertexBuffer?.Dispose();
        m_VertexBuffer = null;
        m_NormalBuffer?.Dispose();
        m_NormalBuffer = null;
        m_LastMesh = null;
        m_LastMeshIndexBuffer = null;
    }

    static float3 RotateExtents(float3 extents, float3 m0, float3 m1, float3 m2)
    {
        return math.abs(m0 * extents.x) + math.abs(m1 * extents.y) + math.abs(m2 * extents.z);
    }

    public static Bounds TransformBounds(float4x4 transform, Bounds localBounds)
    {
        return new()
        {
            extents = RotateExtents(localBounds.extents, transform.c0.xyz, transform.c1.xyz, transform.c2.xyz),
            center = math.transform(transform, localBounds.center)
        };
    }

    private bool UseImmediateRendering => testApi is TestAPI.DrawProceduralNow or TestAPI.DrawProceduralIndirectNow
                                                  or TestAPI.CommandBufferDrawMeshInstanced or TestAPI.CommandBufferDrawMeshInstancedIndirect
                                                  or TestAPI.CommandBufferDrawMeshInstancedProcedural or TestAPI.CommandBufferDrawProcedural
                                                  or TestAPI.CommandBufferDrawProceduralIndirect;

    private void Update()
    {
        if (!UseImmediateRendering)
            DoRender(null);
    }

    private void OnAfterOpaque(CommandBuffer cmd)
    {
        if (UseImmediateRendering)
            DoRender(cmd);
    }

    private void DoRender(CommandBuffer cmd, bool dontDraw = false)
    {
        if (mesh == null
            || material == null
            || transforms.Count == 0)
        {
            OnDisable();
            return;
        }

        if (m_Matrices == null || m_Matrices.Length != transforms.Count)
            m_Matrices = new Matrix4x4[transforms.Count];
        float3 boundsMin = float.PositiveInfinity;
        float3 boundsMax = float.NegativeInfinity;
        var meshBounds = mesh.bounds;
        var localToWorld = transform.localToWorldMatrix;
        var hash = 0u;
        for (int i = 0; i < transforms.Count; ++i)
        {
            var mat = localToWorld * Matrix4x4.TRS(transforms[i].Position, Quaternion.Euler(transforms[i].EulerAngles), transforms[i].Scale);
            m_Matrices[i] = mat;
            hash = math.hash(math.uint2(hash, math.hash(mat)));

            var worldBounds = TransformBounds(mat, meshBounds);
            boundsMin = math.min(boundsMin, worldBounds.min);
            boundsMax = math.max(boundsMax, worldBounds.max);
        }

        if (m_ClonedMaterial != null && m_LastMaterialCRC != material.ComputeCRC())
        {
            UnityEngine.Object.DestroyImmediate(m_ClonedMaterial);
            m_ClonedMaterial = null;
        }

        if (m_ClonedMaterial == null)
        {
            m_ClonedMaterial = new Material(material);
            m_ClonedMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_LastMaterialCRC = material.ComputeCRC();
        }

        m_ClonedMaterial.SetInt("_InstanceCount", transforms.Count);

        var drawBounds = new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin);
        if (testApi is TestAPI.DrawMeshInstancedProcedural or TestAPI.DrawMeshInstancedIndirect)
        {
            // Unity sets the object matrix of DrawMeshInstancedProcedural & DrawMeshInstancedIndirect to offset from the bounds' center.
            // Subtract drawBounds.center from each instance.
            var offset = Matrix4x4.Translate(-drawBounds.center);
            for (int i = 0; i < m_Matrices.Length; ++i)
                m_Matrices[i] = offset * m_Matrices[i];
        }

        if (testApi is TestAPI.DrawMeshInstancedIndirect or TestAPI.DrawProceduralIndirect or TestAPI.DrawProceduralIndirectNow
            or TestAPI.CommandBufferDrawMeshInstancedIndirect or TestAPI.CommandBufferDrawProceduralIndirect)
        {
            if (m_DrawArgsBuffer != null && (m_LastDrawArgsInstanceCount != transforms.Count || m_LastDrawArgsIndexCount != mesh.GetIndexCount(0)))
            {
                m_DrawArgsBuffer.Dispose();
                m_DrawArgsBuffer = null;
            }

            if (m_DrawArgsBuffer == null)
            {
                m_DrawArgsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 5, 4);
                m_DrawArgsBuffer.SetData(new[] { (int)mesh.GetIndexCount(0), transforms.Count, 0, 0, 0 });
                m_LastDrawArgsInstanceCount = transforms.Count;
                m_LastDrawArgsIndexCount = (int)mesh.GetIndexCount(0);
            }
        }

        if (testApi is not (TestAPI.DrawMeshInstanced or TestAPI.CommandBufferDrawMeshInstanced))
        {
            if (m_TransformsBuffer != null && hash != m_LastTransformsBufferHash)
            {
                m_TransformsBuffer.Dispose();
                m_TransformsBuffer = null;
            }

            if (m_TransformsBuffer == null)
            {
                m_TransformsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, transforms.Count, 64);
                m_TransformsBuffer.SetData(m_Matrices);
                m_LastTransformsBufferHash = hash;
            }
            m_ClonedMaterial.SetBuffer("_InstanceBuffer", m_TransformsBuffer);
        }

        if (testApi is TestAPI.DrawProcedural or TestAPI.DrawProceduralIndirect or TestAPI.DrawProceduralNow or TestAPI.DrawProceduralIndirectNow
            or TestAPI.CommandBufferDrawProcedural or TestAPI.CommandBufferDrawProceduralIndirect)
        {
            if (m_LastMesh != mesh)
            {
                m_VertexBuffer?.Dispose();
                m_VertexBuffer = null;
                m_NormalBuffer?.Dispose();
                m_NormalBuffer = null;
                m_LastMeshIndexBuffer = null;
            }

            if (m_VertexBuffer == null)
            {
                m_VertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertexCount, 12);
                m_VertexBuffer.SetData(mesh.vertices);
            }
            if (m_NormalBuffer == null)
            {
                m_NormalBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertexCount, 12);
                m_NormalBuffer.SetData(mesh.normals);
            }
            m_LastMesh = mesh;
            m_LastMeshIndexBuffer ??= mesh.GetIndexBuffer();
            m_ClonedMaterial.SetBuffer("_VertexBuffer", m_VertexBuffer);
            m_ClonedMaterial.SetBuffer("_NormalBuffer", m_NormalBuffer);
        }

        int forwardPassIndex = material.FindPass("Universal Forward");
#if UNITY_EDITOR
        if (UseImmediateRendering)
        {
            if (forwardPassIndex == -1)
                return;

            if (!UnityEditor.ShaderUtil.IsPassCompiled(material, forwardPassIndex))
            {
                UnityEditor.ShaderUtil.CompilePass(material, forwardPassIndex);
                return;
            }
        }
#endif

        var rparams = new RenderParams(m_ClonedMaterial)
        {
            layer = 0,
            worldBounds = drawBounds
        };

        // To bad, CommandBuffer.DrawXxx doesn't support specifying rendering layers. Manually set them.
        // Have to set the full float array for DrawMeshInstanced.
        m_SetupRenderingLayerForCommandBuffer ??= new MaterialPropertyBlock();
        if (testApi == TestAPI.CommandBufferDrawMeshInstanced)
        {
            m_SetupRenderingLayerForCommandBuffer.Clear();
            for (int i = 0; i < transforms.Count; ++i)
            {
                if (i >= s_RenderingLayerArray.Count)
                    s_RenderingLayerArray.Add(0);
                s_RenderingLayerArray[i] = math.asfloat(1 << rparams.layer);
            }
            s_RenderingLayerArray.RemoveRange(transforms.Count, s_RenderingLayerArray.Count - transforms.Count);
            m_SetupRenderingLayerForCommandBuffer.SetFloatArray("unity_RenderingLayer", s_RenderingLayerArray);
        }
        else
        {
            m_SetupRenderingLayerForCommandBuffer.Clear();
            m_SetupRenderingLayerForCommandBuffer.SetVector("unity_RenderingLayer", new Vector4(math.asfloat(1 << rparams.layer), 0, 0, 0));
        }

        if (dontDraw)
            return;

        switch (testApi)
        {
            case TestAPI.DrawMeshInstanced:
                if (m_ClonedMaterial.enableInstancing)
                    Graphics.RenderMeshInstanced(rparams, mesh, 0, m_Matrices);
                break;
            case TestAPI.DrawMeshInstancedIndirect:
                Graphics.RenderMeshIndirect(rparams, mesh, m_DrawArgsBuffer);
                break;
            case TestAPI.DrawMeshInstancedProcedural:
                Graphics.RenderMeshPrimitives(rparams, mesh, 0, transforms.Count);
                break;
            case TestAPI.DrawProcedural:
                Graphics.RenderPrimitivesIndexed(rparams, mesh.GetTopology(0), m_LastMeshIndexBuffer, (int)mesh.GetIndexCount(0), instanceCount: transforms.Count);
                break;
            case TestAPI.DrawProceduralIndirect:
                Graphics.RenderPrimitivesIndexedIndirect(rparams, mesh.GetTopology(0), m_LastMeshIndexBuffer, m_DrawArgsBuffer);
                break;
            case TestAPI.DrawProceduralNow:
                // m_ClonedMaterial.SetPass(forwardPassIndex);
                // Graphics.DrawProceduralNow(mesh.GetTopology(0), m_LastMeshIndexBuffer, (int)mesh.GetIndexCount(0), transforms.Count);
                break;
            case TestAPI.DrawProceduralIndirectNow:
                // m_ClonedMaterial.SetPass(forwardPassIndex);
                // Graphics.DrawProceduralIndirectNow(mesh.GetTopology(0), m_LastMeshIndexBuffer, m_DrawArgsBuffer, 0);
                break;
            case TestAPI.CommandBufferDrawMeshInstanced:
                if (m_ClonedMaterial.enableInstancing)
                    cmd.DrawMeshInstanced(mesh, 0, m_ClonedMaterial, forwardPassIndex, m_Matrices, transforms.Count, m_SetupRenderingLayerForCommandBuffer);
                break;
            case TestAPI.CommandBufferDrawMeshInstancedIndirect:
                cmd.DrawMeshInstancedIndirect(mesh, 0, m_ClonedMaterial, forwardPassIndex, m_DrawArgsBuffer, 0, m_SetupRenderingLayerForCommandBuffer);
                break;
            case TestAPI.CommandBufferDrawMeshInstancedProcedural:
                cmd.DrawMeshInstancedProcedural(mesh, 0, m_ClonedMaterial, forwardPassIndex,transforms.Count, m_SetupRenderingLayerForCommandBuffer);
                break;
            case TestAPI.CommandBufferDrawProcedural:
                cmd.DrawProcedural(m_LastMeshIndexBuffer, Matrix4x4.identity, m_ClonedMaterial, forwardPassIndex, mesh.GetTopology(0), (int)mesh.GetIndexCount(0), transforms.Count, m_SetupRenderingLayerForCommandBuffer);
                break;
            case TestAPI.CommandBufferDrawProceduralIndirect:
                cmd.DrawProceduralIndirect(m_LastMeshIndexBuffer, Matrix4x4.identity, m_ClonedMaterial, forwardPassIndex, mesh.GetTopology(0), m_DrawArgsBuffer, 0, m_SetupRenderingLayerForCommandBuffer);
                break;
            default:
                break;
        }
    }
}
