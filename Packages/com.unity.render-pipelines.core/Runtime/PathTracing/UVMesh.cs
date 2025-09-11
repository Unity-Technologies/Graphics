using System;
using Unity.Collections;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.Rendering;

namespace UnityEngine.PathTracing.Integration
{
    internal class UVMesh : IDisposable
    {
        public Mesh Mesh;
        public float UVAspectRatio; // width / height

        public void Dispose()
        {
            Object.DestroyImmediate(Mesh);
        }

        private struct OutputVertex
        {
            public Vector3 Position;
        };

        public bool Build(Mesh mesh)
        {
#if UNITY_EDITOR
                var inputDataArray = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh);
#else
            var inputDataArray = Mesh.AcquireReadOnlyMeshData(mesh);
#endif
            var outputDataArray = Mesh.AllocateWritableMeshData(inputDataArray.Length);
            if (inputDataArray.Length != 1)
            {
                Debug.Assert(inputDataArray.Length == 1);
                return false;
            }
            int vertexCount = inputDataArray[0].vertexCount;
            var tmpVtxArray0 = new NativeArray<Vector2>(vertexCount, Allocator.TempJob);
            var ok = Build(outputDataArray[0], inputDataArray[0], tmpVtxArray0, out UVAspectRatio);
            tmpVtxArray0.Dispose();
            inputDataArray.Dispose();
            if (!ok)
                return false;
            Mesh = new Mesh
            {
                hideFlags = HideFlags.DontSaveInEditor,
                name = mesh.name
            };
            Mesh.ApplyAndDisposeWritableMeshData(outputDataArray, Mesh);
            Mesh.RecalculateBounds();
            Mesh.UploadMeshData(false); // should be passing true, but this doesn't work in playmode
            return true;
        }

        // Make a new mesh which position values are taken from the UVs of the input mesh
        private static bool Build(Mesh.MeshData outputMesh, Mesh.MeshData inputMesh, NativeArray<Vector2> tmpVtxArray0, out float uvAspectRatio)
        {
            uvAspectRatio = 0f;

            // check that the input mesh has uv2 or uv
            if (!(inputMesh.HasVertexAttribute(VertexAttribute.TexCoord0) || inputMesh.HasVertexAttribute(VertexAttribute.TexCoord1)))
                return false;

            // work out normalized uv bounds
            int vertexCount = inputMesh.vertexCount;
            var inputUVData = tmpVtxArray0;
            inputMesh.GetUVs(inputMesh.HasVertexAttribute(VertexAttribute.TexCoord1) ? 1 : 0, inputUVData);

            LightmapIntegrationHelpers.ComputeUVBounds(inputUVData, out Vector2 uvBoundSize, out Vector2 uvBoundsOffset);
            if (!(uvBoundSize.x > 0.0f && uvBoundSize.y > 0.0f))
                return false;

            Vector2 normalizationOffset = -uvBoundsOffset;
            Vector2 normalizationScale = new Vector2(1.0f / uvBoundSize.x, 1.0f / uvBoundSize.y);

            uvAspectRatio = uvBoundSize.x / uvBoundSize.y;

            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            };
            outputMesh.SetVertexBufferParams(vertexCount, layout);
            var outputVertexData = outputMesh.GetVertexData<OutputVertex>();
            for (int i = 0; i < vertexCount; i++)
            {
                OutputVertex outputVertex = new OutputVertex
                {
                    Position = new Vector3((inputUVData[i].x + normalizationOffset.x) * normalizationScale.x, (inputUVData[i].y + normalizationOffset.y) * normalizationScale.y , 0.0f),
                };
                outputVertexData[i] = outputVertex;
            }

            // Copy the index buffer across
            var subMeshCount = inputMesh.subMeshCount;
            outputMesh.subMeshCount = subMeshCount;
            int indexCount;
            if (inputMesh.indexFormat == IndexFormat.UInt16)
                indexCount = inputMesh.GetIndexData<ushort>().Length;
            else if (inputMesh.indexFormat == IndexFormat.UInt32)
                indexCount = inputMesh.GetIndexData<int>().Length;
            else
                return false;
            outputMesh.SetIndexBufferParams(indexCount, inputMesh.indexFormat);

            if (inputMesh.indexFormat == IndexFormat.UInt16)
            {
                var inputIndices = inputMesh.GetIndexData<ushort>();
                var outputIndices = outputMesh.GetIndexData<ushort>();
                for (int i = 0; i < inputIndices.Length; i++)
                {
                    outputIndices[i] = inputIndices[i];
                }
            }
            else if (inputMesh.indexFormat == IndexFormat.UInt32)
            {
                var inputIndices = inputMesh.GetIndexData<int>();
                var outputIndices = outputMesh.GetIndexData<int>();
                for (int i = 0; i < inputIndices.Length; i++)
                {
                    outputIndices[i] = inputIndices[i];
                }
            }
            else
            {
                return false;
            }

            for (int sm = 0; sm < subMeshCount; ++sm)
            {
                SubMeshDescriptor smd = inputMesh.GetSubMesh(sm);
                outputMesh.SetSubMesh(sm, smd);
            }
            return true;
        }
    }
}
