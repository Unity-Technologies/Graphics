using System;
using UnityEngine.Profiling;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeDynamicGIManager
    {
        private static ProbeDynamicGIManager s_Instance = new ProbeDynamicGIManager();

        internal static ProbeDynamicGIManager instance { get { return s_Instance; } }

        private ComputeShader m_ExtractionShader = null;

        internal void Allocate(RenderPipelineResources resources)
        {
            Dispose(); // To avoid double alloc.

            m_ExtractionShader = resources.shaders.extactProbeExtraDataCS;

            unwrappedPool = RTHandles.Alloc(kUnwrappedTextureSize, kUnwrappedTextureSize, slices: kPoolSize, dimension: TextureDimension.Tex2DArray, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, name: "Extra Data Dynamic GI Pool");
            readbackBuffer = new ComputeBuffer(kRWComputeBuffersSize, Marshal.SizeOf<ExtraDataRequestOutput>());
            inputBuffer = new ComputeBuffer(kRWComputeBuffersSize, Marshal.SizeOf<ExtraDataRequests>());
            nextUnwrappedDst = 0;
        }

        public void Dispose()
        {
            RTHandles.Release(unwrappedPool);
            CoreUtils.SafeRelease(readbackBuffer);
            CoreUtils.SafeRelease(inputBuffer);
        }


        #region ExtraData Definition

        internal static readonly int s_AxisCount = 14;
        internal static readonly float s_DiagonalDist = Mathf.Sqrt(3.0f);
        internal static readonly float s_Diagonal = 1.0f / s_DiagonalDist;
        internal static readonly float s_2DDiagonalDist = Mathf.Sqrt(2.0f);
        internal static readonly float s_2DDiagonal = 1.0f / s_2DDiagonalDist;

        internal static readonly Vector4[] NeighbourAxis =
        {
            // primary axis
            new Vector4(1, 0, 0, 1),
            new Vector4(-1, 0, 0, 1),
            new Vector4(0, 1, 0, 1),
            new Vector4(0, -1, 0, 1),
            new Vector4(0, 0, 1, 1),
            new Vector4(0, 0, -1, 1),

            // 3D diagonals
            new Vector4(s_Diagonal,  s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(s_Diagonal,  s_Diagonal, -s_Diagonal, s_DiagonalDist),
            new Vector4(s_Diagonal, -s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(s_Diagonal, -s_Diagonal, -s_Diagonal, s_DiagonalDist),

            new Vector4(-s_Diagonal,  s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(-s_Diagonal,  s_Diagonal, -s_Diagonal, s_DiagonalDist),
            new Vector4(-s_Diagonal, -s_Diagonal,  s_Diagonal, s_DiagonalDist),
            new Vector4(-s_Diagonal, -s_Diagonal, -s_Diagonal, s_DiagonalDist),
        };


        static internal void InitExtraData(ref ProbeExtraData extraData)
        {
            extraData.neighbourColour = new Vector3[s_AxisCount];
            extraData.neighbourNormal = new Vector3[s_AxisCount];
            extraData.neighbourDistance = new float[s_AxisCount];
            extraData.requestIndex = new int[s_AxisCount];
            for (int i = 0; i < s_AxisCount; ++i)
            {
                extraData.requestIndex[i] = -1;
            }
        }


        #endregion

        #region PopulatingBuffer
        private uint PackAlbedo(Vector3 color, float distance)
        {
            float albedoR = Mathf.Clamp01(color.x);
            float albedoG = Mathf.Clamp01(color.y);
            float albedoB = Mathf.Clamp01(color.z);

            float normalizedDistance = Mathf.Clamp01(distance / (ProbeReferenceVolume.instance.MinDistanceBetweenProbes() * Mathf.Sqrt(3.0f)));

            uint packedOutput = 0;

            packedOutput |= ((uint)(albedoR * 255.5f) << 0);
            packedOutput |= ((uint)(albedoG * 255.5f) << 8);
            packedOutput |= ((uint)(albedoB * 255.5f) << 16);
            packedOutput |= ((uint)(normalizedDistance * 255.0f) << 24);

            return packedOutput;
        }


        // { probeIndex: 19 bits, validity: 8bit, axis: 5bit }
        private uint PackIndexAndValidity(uint probeIndex, uint axisIndex, float validity)
        {
            uint output = 0;

            output |= axisIndex;
            output |= ((uint)(validity * 255.5f) << 5);
            output |= (probeIndex << 13);

            return output;
        }

        private uint PackAxisDir(Vector4 axis)
        {
            uint axisType = (axis.w == 1.0f) ? 0u : 1u;

            uint encodedX = axis.x < 0 ? 0u :
                axis.x == 0 ? 1u :
                2u;

            uint encodedY = axis.y < 0 ? 0u :
                axis.y == 0 ? 1u :
                2u;

            uint encodedZ = axis.z < 0 ? 0u :
                axis.z == 0 ? 1u :
                2u;

            uint output = 0;
            // Encode type of axis in bit 7
            output |= (axisType << 6);
            // Encode axis signs in [5:6] [3:4] [1:2]
            output |= (encodedZ << 4);
            output |= (encodedY << 2);
            output |= (encodedX << 0);

            return output;
        }

        // Same as PackNormalOctQuadEncode and PackFloat2To888 in Packing.hlsl
        private uint PackNormalAndAxis(Vector3 N, int axisIndex)
        {
            uint packedOutput = 0;
            float L1Norm = Mathf.Abs(N.x) + Mathf.Abs(N.y) + Mathf.Abs(N.z);
            N /= L1Norm;
            float t = Mathf.Clamp01(-N.z);

            Vector2 p = new Vector2(N.x + (N.x >= 0.0f ? t : -t),
                N.y + (N.y >= 0.0f ? t : -t));
            p *= 0.5f;
            p.x += 0.5f;
            p.y += 0.5f;


            uint i0 = (uint)(p.x * 4095.5f); uint i1 = (uint)(p.y * 4095.5f);
            uint hi0 = i0 >> 8; uint hi1 = i1 >> 8;
            uint lo0 = hi0 & 255; uint lo1 = hi1 & 255;

            packedOutput |= (lo0 << 0);
            packedOutput |= (lo1 << 8);
            packedOutput |= ((hi0 | (hi1 << 4)) << 16);

            packedOutput |= (PackAxisDir(NeighbourAxis[axisIndex]) << 24);

            return packedOutput;
        }

        private struct FinalDataPacked
        {
            internal uint packedIndices;
            internal uint packedAlbedo;
            internal uint packedNormal;

            internal Vector3 position;
        };

        // We 1 albedo, 1 normal and 1 distance per axis in ProbeExtraData.NeighbourAxis.
        // These informations are packed as follow in a uint2:
        //  UINT32:  8bit: AlbedoR, 8bit: AlbedoG, 8bit: AlbedoB, 8bit: normalized t along ray (the length of the ray is constant so we can derive the actual distance after reading data.
        //  UINT32:  8-8-8: Normal encoded with PackNormalOctQuadEncode like HDRP's normal encoding, 8bit: UNUSED
        private struct PackedExtraData
        {
            public uint[] packedAlbedo;
            public uint[] packedNormal;
        }

        private PackedExtraData PackProbeExtraData(ProbeExtraData probeExtraData)
        {
            PackedExtraData packedData;
            packedData.packedAlbedo = new uint[s_AxisCount];
            packedData.packedNormal = new uint[s_AxisCount];

            for (int i = 0; i < s_AxisCount; ++i)
            {
                bool miss = probeExtraData.neighbourDistance[i] >= ProbeReferenceVolume.instance.MinDistanceBetweenProbes() * Mathf.Sqrt(3.0f) || probeExtraData.neighbourDistance[i] < 0.005f;

                packedData.packedAlbedo[i] = PackAlbedo(probeExtraData.neighbourColour[i], miss ? 0.0f : probeExtraData.neighbourDistance[i]);
                packedData.packedNormal[i] = PackNormalAndAxis(probeExtraData.neighbourNormal[i], i);
            }

            return packedData;
        }

        private void PopulateExtraDataBuffer(ProbeReferenceVolume.Cell cell)
        {
            var probeCount = cell.probePositions.Length;

            List<FinalDataPacked> hitIndices = new List<FinalDataPacked>(probeCount * s_AxisCount);
            List<FinalDataPacked> missIndices = new List<FinalDataPacked>(probeCount * s_AxisCount);

            var finalExtraData = new List<uint>(probeCount * s_AxisCount * 3);

            var probeLocations = new List<float>(probeCount * 3);

            for (int i = 0; i < probeCount; ++i)
            {
                int probeIndex = probeLocations.Count / 3;
                var probeLocation = cell.probePositions[i];

                probeLocations.Add(probeLocation.x);
                probeLocations.Add(probeLocation.y);
                probeLocations.Add(probeLocation.z);

                var probeExtraData = cell.extraData[i];
                PackedExtraData extraDataPacked = PackProbeExtraData(probeExtraData);

                for (int axis = 0; axis < s_AxisCount; ++axis)
                {
                    bool miss = probeExtraData.neighbourDistance[axis] >= (ProbeReferenceVolume.instance.MinDistanceBetweenProbes() * Mathf.Sqrt(3.0f)) || probeExtraData.neighbourDistance[axis] == 0.0f;

                    FinalDataPacked index;
                    index.packedIndices = PackIndexAndValidity((uint)probeIndex, (uint)axis, probeExtraData.validity);
                    index.packedAlbedo = extraDataPacked.packedAlbedo[axis];
                    index.packedNormal = extraDataPacked.packedNormal[axis];
                    index.position = probeLocation;

                    if (miss)
                    {
                        missIndices.Add(index);
                    }
                    else
                    {
                        hitIndices.Add(index);
                    }
                }
            }

            var refVol = ProbeReferenceVolume.instance;

            for (int i = 0; i < hitIndices.Count; ++i)
            {
                FinalDataPacked index = hitIndices[i];
                finalExtraData.Add(index.packedAlbedo);
                finalExtraData.Add(index.packedNormal);
                finalExtraData.Add(index.packedIndices);
            }

            int hitProbesAxisCount = hitIndices.Count;

            for (int i = 0; i < missIndices.Count; ++i)
            {
                FinalDataPacked index = missIndices[i];
                finalExtraData.Add(index.packedAlbedo);
                finalExtraData.Add(index.packedNormal);
                finalExtraData.Add(index.packedIndices);
            }

            int missProbesAxisCount = missIndices.Count;

            cell.probeExtraDataBuffers.PopulateComputeBuffer(probeLocations, finalExtraData, hitProbesAxisCount, missProbesAxisCount);
            cell.probeExtraDataBuffers.ClearIrradianceCaches(probeCount, s_AxisCount);

        }

        internal void InitExtraDataBuffers(List<ProbeReferenceVolume.Cell> cells)
        {
            foreach (var cell in cells)
            {
                if (cell.probeExtraDataBuffers != null)
                {
                    cell.probeExtraDataBuffers.Dispose();
                }

                cell.probeExtraDataBuffers = new ProbeExtraDataBuffers(cell, s_AxisCount);

                PopulateExtraDataBuffer(cell);
                cell.extraDataBufferInit = true;

            }
        }
        #endregion


        #region Extract Extra Data

        private const int kUnwrappedTextureSize = 32;
        private const int kPoolSize = 256;
        private const int kRWComputeBuffersSize = 8192;

        internal struct ExtraDataRequests
        {
            internal Vector2 uv;
            internal int requestIndex;
        }

        internal struct ExtraDataRequestOutput
        {
            internal Vector3 albedo;
        }

        internal struct RequestInput
        {
            internal MeshRenderer renderer;
            internal Mesh mesh;
            internal int subMesh;
        }

        internal static Dictionary<RequestInput, List<ExtraDataRequests>> requestsList = new Dictionary<RequestInput, List<ExtraDataRequests>>();
        internal static List<ExtraDataRequestOutput> extraRequestsOutput = new List<ExtraDataRequestOutput>();

        internal RTHandle unwrappedPool;
        internal ComputeBuffer readbackBuffer;
        internal ComputeBuffer inputBuffer;

        private int nextUnwrappedDst = 0;

        private static int EnqueueRequest(RequestInput input, Vector2 uv)
        {
            ExtraDataRequestOutput output;
            output.albedo = new Vector3(0.0f, 0.0f, 0.0f);
            
            int requestIndex = extraRequestsOutput.Count;
            extraRequestsOutput.Add(output);

            ExtraDataRequests request;
            request.requestIndex = requestIndex;
            request.uv = uv;

            if (!requestsList.ContainsKey(input))
            {
                requestsList.Add(input, new List<ExtraDataRequests>());
            }

            requestsList[input].Add(request);

            return requestIndex;
        }

        // TODO: TODO_FCC MAKE THIS USING CMD AND CONTEXT !!! NOT IMMEDIATE MODE.
        internal bool UnwrapInput(RequestInput input, out bool poolFull)
        {
            poolFull = (nextUnwrappedDst == kPoolSize - 1);


            Material material = input.renderer.sharedMaterials[input.subMesh];

            var passIdx = -1;
            for (int i = 0; i < material.passCount; ++i)
            {
                if (material.GetPassName(i).IndexOf("DynamicGIDataGen") >= 0)
                {
                    passIdx = i;
                    break;
                }

            }
            if (passIdx >= 0)
            {
                material.SetPass(passIdx);
                Graphics.SetRenderTarget(unwrappedPool, 0, CubemapFace.Unknown, nextUnwrappedDst);
                GL.Clear(false, true, Color.black);
                Graphics.DrawMeshNow(input.mesh, Matrix4x4.identity, 0);
            }

            nextUnwrappedDst = (nextUnwrappedDst + 1) % kPoolSize;

            return passIdx >= 0;
        }

        private void ExecutePendingRequests()
        {
            nextUnwrappedDst = 0;
            if (requestsList.Keys.Count > 0)
            {
                bool poolFull = false;
                var keys = requestsList.Keys;
                RequestInput[] keysArray = new RequestInput[keys.Count];
                keys.CopyTo(keysArray, 0);

                int firstKeyForBatch = 0;
                for (int i = 0; i < keys.Count; ++i)
                {
                    UnwrapInput(keysArray[i], out poolFull);
                    if (poolFull)
                    {
                        ExtractData(firstKeyForBatch, i);
                        firstKeyForBatch = i + 1;
                    }
                }

                if ((keys.Count - firstKeyForBatch - 1) > 0)
                {
                    ExtractData(firstKeyForBatch, keys.Count - 1);
                }
            }
        }

        // TODO TODO_FCC Use cmd + context here ? 
        private void PerformDataExtraction(List<ExtraDataRequests> inputs, List<int> dstRequestIndices)
        {
            CommandBuffer cmd = CommandBufferPool.Get("");

            inputBuffer.SetData(inputs, 0, 0, inputs.Count);

            int kernel = m_ExtractionShader.FindKernel("ExtractData");
            cmd.SetComputeTextureParam(m_ExtractionShader, kernel, HDShaderIDs._UnwrappedDataPool, unwrappedPool);
            cmd.SetComputeIntParam(m_ExtractionShader, HDShaderIDs._RequestBatchSize, inputs.Count);
            cmd.SetComputeBufferParam(m_ExtractionShader, kernel, HDShaderIDs._RequestsInputData, inputBuffer);
            cmd.SetComputeBufferParam(m_ExtractionShader, kernel, HDShaderIDs._RWRequestsOutputData, readbackBuffer);

            int dispatchX = (inputs.Count + 63) / 64;
            cmd.DispatchCompute(m_ExtractionShader, kernel, dispatchX, 1, 1);

            Graphics.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ExtraDataRequestOutput[] outputs = new ExtraDataRequestOutput[inputs.Count];
            readbackBuffer.GetData(outputs, 0, 0, inputs.Count);

            for (int i = 0; i < inputs.Count; ++i)
            {
                int dstIndex = dstRequestIndices[i];
                extraRequestsOutput[dstIndex] = outputs[i];
            }
        }

        private void ExtractData(int keyStart, int keyEnd)
        {
            var keys = requestsList.Keys;
            RequestInput[] keysArray = new RequestInput[keys.Count];
            keys.CopyTo(keysArray, 0);

            List<ExtraDataRequests> inputs = new List<ExtraDataRequests>();
            // Yucky, will fix. 
            List<int> dstRequestIndices = new List<int>();


            for (int i = keyStart; i <= keyEnd; ++i)
            {
                var currKey = keysArray[i];
                var listForKey = requestsList[currKey];
                foreach (var request in listForKey)
                {
                    // Modified the request so that we have texture array index in request index as we don't need the former in the shader. 
                    ExtraDataRequests moddedRequest = request;
                    moddedRequest.requestIndex = i;
                    if (inputs.Count == kRWComputeBuffersSize)
                    {
                        PerformDataExtraction(inputs, dstRequestIndices);
                        dstRequestIndices.Clear();
                        inputs.Clear();
                    }
                    dstRequestIndices.Add(request.requestIndex);
                    inputs.Add(moddedRequest);
                }
            }

            if (inputs.Count > 0)
            {
                PerformDataExtraction(inputs, dstRequestIndices);
            }
        }

        internal void ClearContent()
        {
            requestsList.Clear();
            extraRequestsOutput.Clear();
            nextUnwrappedDst = 0;
        }

        internal static ExtraDataRequestOutput RetrieveRequestOutput(int requestIndex)
        {
            Debug.Assert(requestIndex < extraRequestsOutput.Count);
            return extraRequestsOutput[requestIndex];
        }

        #endregion

        #region ExtraData Baking
        
        private static bool IsValidForBaking(GameObject gameObject)
        {
            // TODO: Do a better filtering here.
            return gameObject.activeInHierarchy;
        }

#if UNITY_EDITOR
        static List<MeshRenderer> addedOccluders;

        private static void AddOccluders(Vector3 pos, Vector3 size)
        {
            addedOccluders = new List<MeshRenderer>();
            Bounds volumeBounds = new Bounds(pos, size);
            for (int sceneIndex = 0; sceneIndex < UnityEngine.SceneManagement.SceneManager.sceneCount; ++sceneIndex)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(sceneIndex);
                GameObject[] gameObjects = scene.GetRootGameObjects();
                foreach (GameObject gameObject in gameObjects)
                {
                    MeshRenderer[] renderComponents = gameObject.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer mr in renderComponents)
                    {
                        if (IsValidForBaking(mr.gameObject))
                        {
                            if (mr.bounds.Intersects(volumeBounds))
                            {
                                if (!mr.gameObject.GetComponent<MeshCollider>())
                                {
                                    mr.gameObject.AddComponent<MeshCollider>();
                                    addedOccluders.Add(mr);
                                }
                            }
                        }
                    }
                }
            }

            var autoSimState = Physics.autoSimulation;
            Physics.autoSimulation = false;
            Physics.Simulate(0.1f);
            Physics.autoSimulation = autoSimState;
        }
        private static void CleanupOccluders()
        {
            foreach (MeshRenderer meshRenderer in addedOccluders)
            {
                MeshCollider collider = meshRenderer.gameObject.GetComponent<MeshCollider>();
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }


        private static float FindDistance(RaycastHit[] hits, float maxDist, ref int index, bool findInDistance)
        {
            float distance = maxDist;
            for (int i = 0; i < hits.Length; ++i)
            {
                RaycastHit hit = hits[i];
                float hitDistance = findInDistance ? (maxDist - hit.distance) : hit.distance;
                if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject) && hitDistance < distance)
                {
                    distance = hitDistance;
                    index = i;
                }
            }

            return distance;
        }

        private static bool HasMeshColliderHits(RaycastHit[] outBoundHits, RaycastHit[] inBoundHits)
        {
            foreach (var hit in outBoundHits)
            {
                if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject))
                {
                    return true;
                }
            }

            foreach (var hit in inBoundHits)
            {
                if (hit.collider is MeshCollider && IsValidForBaking(hit.collider.gameObject))
                {
                    return true;
                }
            }

            return false;
        }

        private static int EnqueueExtraDataRequest(RaycastHit hit)
        {
            int requestTicket = -1;

            MeshCollider collider = hit.collider as MeshCollider;
            if (collider != null)
            {
                Mesh mesh = collider.sharedMesh;

                uint limit = (uint)hit.triangleIndex * 3;
                int submesh = 0;
                bool foundSubMesh = false;
                for (; submesh < mesh.subMeshCount; submesh++)
                {
                    uint numIndices = mesh.GetIndexCount(submesh);
                    if (numIndices > limit)
                    {
                        foundSubMesh = true;
                        break;
                    }

                    limit -= numIndices;
                }

                if (!foundSubMesh)
                {
                    submesh = mesh.subMeshCount - 1;
                }

                MeshRenderer renderer = collider.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    Material material = renderer.sharedMaterials[submesh];
                    RequestInput requestInput;
                    requestInput.mesh = mesh;
                    requestInput.renderer = renderer;
                    requestInput.subMesh = submesh;
                    requestTicket = EnqueueRequest(requestInput, hit.textureCoord);
                }
            }

            return requestTicket;
        }

        private static bool GetNormalAndRequestTicketForOccluder(Vector3 worldPosition, Vector3 ray, ref int requestIndex, ref float outDistance, ref Vector3 normal)
        {
            Vector3 normalizedRay = ray.normalized;
            var collisionLayerMask = ~0;

            RaycastHit[] outBoundHits = Physics.RaycastAll(worldPosition, normalizedRay, ray.magnitude, collisionLayerMask);
            RaycastHit[] inBoundHits = Physics.RaycastAll(worldPosition + ray, -1.0f * normalizedRay, ray.magnitude, collisionLayerMask);

            bool hasMeshColliderHits = HasMeshColliderHits(outBoundHits, inBoundHits);
            if (hasMeshColliderHits)
            {
                int outIndex = 0;
                outDistance = FindDistance(outBoundHits, ray.magnitude, ref outIndex, false);
                if (outBoundHits.Length > 0)
                {
                    RaycastHit hit = outBoundHits[outIndex];
                    MeshCollider collider = hit.collider as MeshCollider;
                    if (collider != null)
                    {
                        requestIndex = EnqueueExtraDataRequest(hit);
                        normal = outBoundHits[outIndex].normal;
                    }
                    else
                    {
                        outDistance = 0;
                        requestIndex = -1;
                        // put a normal opposite of ray if no mesh collider found
                        normal = -normalizedRay;
                    }
                }
                else
                {
                    outDistance = 0;
                    requestIndex = -1;
                }

                return true;
            }

            return false;
        }

        private static void GenerateExtraData(Vector3 position, ref ProbeExtraData extraData, float validity)
        {
            InitExtraData(ref extraData);

            int hits = 0;
            for (int i = 0; i < s_AxisCount; ++i)
            {
                Vector4 axis = NeighbourAxis[i];
                Vector3 dirAxis = axis;
                float distance = axis.w * ProbeReferenceVolume.instance.MinDistanceBetweenProbes();


                Color color = Color.black;
                int requestIndex = -1;
                float hitDistance = 0.0f;
                Vector3 normal = Vector3.zero;
                if (GetNormalAndRequestTicketForOccluder(position, dirAxis * distance, ref requestIndex, ref hitDistance, ref normal))
                {
                    extraData.requestIndex[i] = requestIndex;
                    extraData.neighbourDistance[i] = hitDistance;
                    extraData.neighbourNormal[i] = normal;
                    hits++;
                }
                else
                {
                    extraData.neighbourColour[i] = Vector3.zero;
                    extraData.neighbourDistance[i] = 0;
                    extraData.neighbourNormal[i] = -dirAxis.normalized;
                    extraData.requestIndex[i] = -1;
                }
            }

            extraData.validity = validity;
        }

        private static void ResolveExtraDataRequest(ref ProbeExtraData extraData)
        {
            for (int i = 0; i < s_AxisCount; ++i)
            {
                if (extraData.requestIndex[i] >= 0)
                {
                    var extraDataRequestOutput = RetrieveRequestOutput(extraData.requestIndex[i]);
                    extraData.neighbourColour[i] = extraDataRequestOutput.albedo;
                }
            }
        }

        internal void GenerateExtraDataForDynamicGI(Vector3 volumePos, Vector3 volumeScale)
        {
            var refVol = ProbeReferenceVolume.instance;
            // TODO TODO_FCC: IMPORTANT USE A VOLUME OF THE PROBE VOLUMES NOT OF THE WHOLE REF VOLUME
            AddOccluders(volumePos, volumeScale);

            var cells = refVol.GetCellsWithExtraDataToInit();

            foreach (var cell in cells)
            {
                int numProbes = cell.probePositions.Length;

                cell.extraData = new ProbeExtraData[numProbes];

                for (int i = 0; i<numProbes; ++i)
                {
                    GenerateExtraData(cell.probePositions[i], ref cell.extraData[i], cell.validity[i]);
                }

                ExecutePendingRequests();

                for (int i = 0; i < numProbes; ++i)
                {
                    ResolveExtraDataRequest(ref cell.extraData[i]);
                }

                CleanupOccluders();
                ClearContent();
            }
        }
#endif

        #endregion
    }
}
