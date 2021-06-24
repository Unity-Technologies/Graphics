using System.Collections.Generic;
using UnityEngine.VFX;
using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    // Then split full VB/IB in clusters and change accordingly.

    public partial class HDRenderPipeline
    {
        ComputeBuffer CompactedVB = null;
        ComputeBuffer CompactedIB = null;
        ComputeBuffer InstanceVDataB = null;
        uint instanceCountBack = 0;
        uint instanceCountFront = 0;
        uint instanceCountDouble = 0;
        Material m_VisibilityBufferMaterial = null;
        Material m_CreateMaterialDepthMaterial = null;


        struct MaterialData
        {
            public int numRenderers;
            public int globalMaterialID;
            public int bucketID;
        }

        Dictionary<Material, MaterialData> materials = new Dictionary<Material, MaterialData>();

        [GenerateHLSL]
        class VisibilityBufferConstants
        {
            public static int s_ClusterSizeInTriangles = 128;
            public static int s_ClusterSizeInIndices = s_ClusterSizeInTriangles * 3;
        }

        [GenerateHLSL(needAccessors = false)]
        internal struct CompactVertex
        {
            public Vector3 pos;
            public Vector2 uv;
            public Vector2 uv1;
            public Vector3 N;
            public Vector4 T;
        }

        [GenerateHLSL(needAccessors = false)]
        internal struct InstanceVData
        {
            public Matrix4x4 localToWorld;
            public uint materialData;
            public uint chunkStartIndex;
            public Vector4 lightmapST;
        }

        void InitVBuffer()
        {
            CompactedVB = null;
            CompactedIB = null;
            InstanceVDataB = null;
            m_VisibilityBufferMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.renderVisibilityBufferPS);
            m_VisibilityBufferMaterial.enableInstancing = true;
            m_CreateMaterialDepthMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.createMaterialDepthPS);
        }

        void DisposeVBufferStuff()
        {
            CoreUtils.Destroy(m_VisibilityBufferMaterial);
            CoreUtils.Destroy(m_CreateMaterialDepthMaterial);
            CoreUtils.SafeRelease(CompactedIB);
            CoreUtils.SafeRelease(CompactedVB);
            CoreUtils.SafeRelease(InstanceVDataB);
        }

        int GetFormatByteCount(VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32: return 4;
                case VertexAttributeFormat.Float16: return 2;
                case VertexAttributeFormat.UNorm8: return 1;
                case VertexAttributeFormat.SNorm8: return 1;
                case VertexAttributeFormat.UNorm16: return 2;
                case VertexAttributeFormat.SNorm16: return 2;
                case VertexAttributeFormat.UInt8: return 1;
                case VertexAttributeFormat.SInt8: return 1;
                case VertexAttributeFormat.UInt16: return 2;
                case VertexAttributeFormat.SInt16: return 2;
                case VertexAttributeFormat.UInt32: return 4;
                case VertexAttributeFormat.SInt32: return 4;
            }
            return 4;
        }

        int DivideMeshInClusters(Mesh mesh, MeshRenderer renderer, ref Dictionary<Mesh, uint> meshes, ref List<InstanceVData> instancesBack, ref List<InstanceVData> instancesFront, ref List<InstanceVData> instancesDouble)
        {
            int clusterCount = 0;
            for (int matIndex = 0; matIndex < renderer.sharedMaterials.Length; ++matIndex)
            {
                int subMeshIndex = (matIndex + renderer.subMeshStartIndex) % mesh.subMeshCount;
                uint subMeshIndexSize = mesh.GetIndexCount(subMeshIndex);
                int clustersForSubmesh = HDUtils.DivRoundUp((int)subMeshIndexSize, VisibilityBufferConstants.s_ClusterSizeInIndices);

                Material currentMat = renderer.sharedMaterials[matIndex];
                if (currentMat == null)
                    continue;

                if (IsTransparentMaterial(currentMat) || IsAlphaTestedMaterial(currentMat) || currentMat.shader.name != "HDRP/Lit")
                    clusterCount += clustersForSubmesh;
                else
                {
                    bool doubleSided = currentMat.doubleSidedGI || currentMat.IsKeywordEnabled("_DOUBLESIDED_ON");

                    float cullMode = 2.0f;
                    if (currentMat.HasProperty("_CullMode"))
                        cullMode = currentMat.GetFloat("_CullMode");

                    MaterialData materialData = new MaterialData();
                    materials.TryGetValue(currentMat, out materialData);
                    uint materialID = ((uint)materialData.globalMaterialID) & 0xffff;
                    uint bucketID = ((uint)materialData.bucketID) & 0xffff;

                    // Instance data common to all clusters
                    InstanceVData data;
                    data.materialData = materialID | (bucketID << 16);
                    data.localToWorld = renderer.localToWorldMatrix;
                    data.lightmapST = renderer.lightmapScaleOffset;

                    for (int c = 0; c < clustersForSubmesh; ++c)
                    {
                        // Adjust the chunk start index
                        data.chunkStartIndex = meshes[mesh] + (uint)clusterCount;

                        if (doubleSided)
                            instancesDouble.Add(data);
                        else if (cullMode == 2.0f)
                            instancesBack.Add(data);
                        else
                            instancesFront.Add(data);

                        clusterCount++;
                    }
                }
            }

            return clusterCount;
        }

        GraphicsBuffer GetVertexAttribInfo(Mesh mesh, VertexAttribute attribute, out int streamStride, out int attributeOffset, out int attributeBytes)
        {
            if (mesh.HasVertexAttribute(attribute))
            {
                int stream = mesh.GetVertexAttributeStream(attribute);
                streamStride = mesh.GetVertexBufferStride(stream);
                attributeOffset = mesh.GetVertexAttributeOffset(attribute);
                attributeBytes = GetFormatByteCount(mesh.GetVertexAttributeFormat(attribute)) * mesh.GetVertexAttributeDimension(attribute);

                return mesh.GetVertexBuffer(stream);
            }
            else
            {
                streamStride = attributeOffset = attributeBytes = 0;
                return null;
            }
        }

        void AddMeshToCompactedBuffer(ref uint clusterIndex, ref uint vbStart, Mesh mesh)
        {
            var ib = mesh.GetIndexBuffer();
            var cs = defaultResources.shaders.vbCompactionCS;
            var kernel = cs.FindKernel("VBCompactionKernel");
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

            int posStreamStride, posOffset, posBytes;
            var posVBStream = GetVertexAttribInfo(mesh, VertexAttribute.Position, out posStreamStride, out posOffset, out posBytes);

            int uvStreamStride, uvOffset, uvBytes;
            var uvVBStream = GetVertexAttribInfo(mesh, VertexAttribute.TexCoord0, out uvStreamStride, out uvOffset, out uvBytes);

            int normalStreamStride, normalOffset, normalBytes;
            var normalVBStream = GetVertexAttribInfo(mesh, VertexAttribute.Normal, out normalStreamStride, out normalOffset, out normalBytes);

            int tangentStreamStride, tangentOffset, tangentBytes;
            var tangentVBStream = GetVertexAttribInfo(mesh, VertexAttribute.Tangent, out tangentStreamStride, out tangentOffset, out tangentBytes);

            Vector4 uv1CompactionParam = Vector4.zero;
            bool hasTexCoord1 = mesh.HasVertexAttribute(VertexAttribute.TexCoord1);
            GraphicsBuffer uv1VBStream = null;
            if (hasTexCoord1)
            {
                int uv1StreamStride, uv1Offset, uv1Bytes;
                uv1VBStream = GetVertexAttribInfo(mesh, VertexAttribute.TexCoord1, out uv1StreamStride, out uv1Offset, out uv1Bytes);
                List<Vector2> blah = new List<Vector2>();
                mesh.GetUVs(1, blah);
                uv1CompactionParam = new Vector4(uv1Offset, mesh.vertexCount, uv1StreamStride, vbStart);
                cs.SetVector(HDShaderIDs._UV1CompactionParams, uv1CompactionParam);
            }

            Vector4 uvCompactionParam = new Vector4(uvOffset, mesh.vertexCount, uvStreamStride, vbStart);
            Vector4 normalCompactionParam = new Vector4(normalOffset, mesh.vertexCount, normalStreamStride, vbStart);
            Vector4 posCompactionParam = new Vector4(posOffset, mesh.vertexCount, posStreamStride, vbStart);
            Vector4 tangentCompactionParam = new Vector4(tangentOffset, mesh.vertexCount, tangentStreamStride, vbStart);


            cs.SetBuffer(kernel, HDShaderIDs._InputUVVB, uvVBStream);
            cs.SetBuffer(kernel, HDShaderIDs._InputNormalVB, normalVBStream);
            cs.SetBuffer(kernel, HDShaderIDs._InputPosVB, posVBStream);
            if (tangentVBStream != null)
            {
                cs.SetBuffer(kernel, HDShaderIDs._InputTangentVB, tangentVBStream);
            }
            else
            {
                cs.SetBuffer(kernel, HDShaderIDs._InputTangentVB, normalVBStream);
                tangentCompactionParam = Vector4.zero;
            }

            if (hasTexCoord1)
                cs.SetBuffer(kernel, HDShaderIDs._InputUV1VB, uv1VBStream);
            else
                cs.SetBuffer(kernel, HDShaderIDs._InputUV1VB, uvVBStream);

            cs.SetBuffer(kernel, HDShaderIDs._OutputVB, CompactedVB);


            cs.SetVector(HDShaderIDs._UVCompactionParams, uvCompactionParam);
            cs.SetVector(HDShaderIDs._NormalCompactionParams, normalCompactionParam);
            cs.SetVector(HDShaderIDs._PosCompactionParams, posCompactionParam);
            cs.SetVector(HDShaderIDs._TangentCompactionParams, tangentCompactionParam);

            int dispatchSize = HDUtils.DivRoundUp(mesh.vertexCount, 64);

            cs.Dispatch(kernel, dispatchSize, 1, 1);


            if (mesh.indexFormat == IndexFormat.UInt16)
                kernel = cs.FindKernel("IBCompactionKernelUINT16");
            else
                kernel = cs.FindKernel("IBCompactionKernelUINT32");

            cs.SetBuffer(kernel, HDShaderIDs._InputIB, ib);
            cs.SetBuffer(kernel, HDShaderIDs._OutputIB, CompactedIB);

            for (int i = 0; i < mesh.subMeshCount; ++i)
            {
                uint indexCount = mesh.GetIndexCount(i);
                int clusterCount = HDUtils.DivRoundUp((int)indexCount, VisibilityBufferConstants.s_ClusterSizeInIndices);

                Vector4 ibCompactionParams = new Vector4(indexCount, HDShadowUtils.Asfloat((uint)(clusterIndex * VisibilityBufferConstants.s_ClusterSizeInIndices)), vbStart, mesh.GetIndexStart(i));
                dispatchSize = HDUtils.DivRoundUp((int)indexCount / 3, VisibilityBufferConstants.s_ClusterSizeInTriangles);
                cs.SetVector(HDShaderIDs._IBCompactionParams, ibCompactionParams);
                cs.Dispatch(kernel, dispatchSize, 1, 1);
                clusterIndex += (uint)clusterCount;
            }

            vbStart += (uint)mesh.vertexCount;
            posVBStream.Dispose();
            uvVBStream.Dispose();
            normalVBStream.Dispose();
            tangentVBStream.Dispose();
            if (hasTexCoord1)
                uv1VBStream.Dispose();
            ib.Dispose();
        }

        int ComputeNumberOfClusters(Mesh currentMesh)
        {
            int numberClusters = 0;
            for (int subMeshIdx = 0; subMeshIdx < currentMesh.subMeshCount; ++subMeshIdx)
            {
                numberClusters += HDUtils.DivRoundUp((int)currentMesh.GetIndexCount(subMeshIdx), VisibilityBufferConstants.s_ClusterSizeInIndices);
            }
            return numberClusters;
        }

        void CompactAllTheThings()
        {
            int vertexCount = 0;
            int clusterCount = 0;

            Dictionary<Mesh, uint> meshes = new Dictionary<Mesh, uint>();
            Dictionary<Mesh, Material[]> meshToMaterial = new Dictionary<Mesh, Material[]>();
            List<InstanceVData> instanceDataBack = new List<InstanceVData>();
            List<InstanceVData> instanceDataFront = new List<InstanceVData>();
            List<InstanceVData> instanceDataDouble = new List<InstanceVData>();
            materials.Clear();
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            int materialIdx = 1;

            int validRenderers = 0;
            // Grab all the renderers from the scene
            var rendererArray = UnityEngine.GameObject.FindObjectsOfType<MeshRenderer>();

            for (var i = 0; i < rendererArray.Length; i++)
            {
                // Fetch the current renderer
                MeshRenderer currentRenderer = rendererArray[i];

                // If it is not active skip it
                if (currentRenderer.enabled == false) continue;

                // Grab the current game object
                GameObject gameObject = currentRenderer.gameObject;

                if (gameObject.TryGetComponent<ReflectionProbe>(out reflectionProbe)) continue;

                currentRenderer.TryGetComponent(out MeshFilter meshFilter);
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                uint ibStartQ = 0;
                if (!meshes.TryGetValue(meshFilter.sharedMesh, out ibStartQ))
                {
                    meshes.Add(meshFilter.sharedMesh, 0);
                    vertexCount += meshFilter.sharedMesh.vertexCount;
                    clusterCount += ComputeNumberOfClusters(meshFilter.sharedMesh);
                }

                MaterialData materialData = new MaterialData();
                foreach (var mat in currentRenderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (!materials.TryGetValue(mat, out materialData))
                    {
                        mat.enableInstancing = true;
                        materialData.numRenderers = 1;
                        materialData.globalMaterialID = materialIdx & 0xffff;
                        materials.Add(mat, materialData);
                        materialIdx++;
                    }
                    else
                    {
                        materialData.numRenderers += 1;
                        materials[mat] = materialData;
                    }
                }
                validRenderers++;
            }

            // If we don't have any valid renderer
            if (validRenderers == 0)
                return;

            // TODO: Worked on the sorted set of materials to optimize the space
            // We need to assign every material to a bucket
            int renderersPerBucket = HDUtils.DivRoundUp(validRenderers, 8);
            int currentBucket = 1;
            int currentBucketRenderers = 0;
            var materialCouple = materials.ToArray();
            foreach (var mat in materialCouple)
            {
                // This goes into the current bucket
                MaterialData newData = mat.Value;
                currentBucketRenderers += newData.numRenderers;
                newData.bucketID = currentBucket;
                materials[mat.Key] = newData;

                if (currentBucketRenderers >= renderersPerBucket)
                {
                    currentBucket++;
                    currentBucketRenderers = 0;
                }
            }

            int currVBCount = CompactedVB == null ? 0 : CompactedVB.count;
            if (vertexCount != currVBCount)
            {
                if (CompactedVB != null && CompactedIB != null)
                {
                    CoreUtils.SafeRelease(CompactedIB);
                    CoreUtils.SafeRelease(CompactedVB);
                    CompactedVB = null;
                    CompactedIB = null;
                }

                var stride = System.Runtime.InteropServices.Marshal.SizeOf<CompactVertex>();
                CompactedVB = new ComputeBuffer(vertexCount, stride);
                CompactedIB = new ComputeBuffer(clusterCount * VisibilityBufferConstants.s_ClusterSizeInIndices, sizeof(int));
            }

            uint vbStart = 0;
            uint clusterIndex = 0;
            var keyArrays = meshes.Keys.ToArray();
            foreach (var mesh in keyArrays)
            {
                meshes[mesh] = clusterIndex;
                AddMeshToCompactedBuffer(ref clusterIndex, ref vbStart, mesh);
            }

            for (var i = 0; i < rendererArray.Length; i++)
            {
                // Fetch the current renderer
                MeshRenderer currentRenderer = rendererArray[i];

                // If it is not active skip it
                if (currentRenderer.enabled == false) continue;

                // Grab the current game object
                GameObject gameObject = currentRenderer.gameObject;

                if (gameObject.TryGetComponent<ReflectionProbe>(out reflectionProbe)) continue;

                currentRenderer.TryGetComponent(out MeshFilter meshFilter);
                if (meshFilter == null || meshFilter.sharedMesh == null) continue;

                DivideMeshInClusters(meshFilter.sharedMesh, currentRenderer, ref meshes, ref instanceDataBack, ref instanceDataFront, ref instanceDataDouble);
            }

            instanceCountBack = (uint)instanceDataBack.Count;
            instanceCountFront = (uint)instanceDataFront.Count;
            instanceCountDouble = (uint)instanceDataDouble.Count;

            uint totalInstanceCount = instanceCountBack + instanceCountFront + instanceCountDouble;
            if (totalInstanceCount == 0)
                return;

            if (InstanceVDataB == null || InstanceVDataB.count != totalInstanceCount)
            {
                if (InstanceVDataB != null)
                {
                    CoreUtils.SafeRelease(InstanceVDataB);
                }
                InstanceVDataB = new ComputeBuffer((int)totalInstanceCount, System.Runtime.InteropServices.Marshal.SizeOf<InstanceVData>());
            }

            InstanceVDataB.SetData(instanceDataBack.ToArray(), 0, 0, instanceDataBack.Count);
            InstanceVDataB.SetData(instanceDataFront.ToArray(), 0, instanceDataBack.Count, instanceDataFront.Count);
            InstanceVDataB.SetData(instanceDataDouble.ToArray(), 0, instanceDataBack.Count + instanceDataFront.Count, instanceDataDouble.Count);
        }
    }
}
