using System;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEditor.Experimental;
using Unity.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.Experimental.Rendering;

[assembly: InternalsVisibleTo("Unity.Entities.Hybrid.HybridComponents")]
[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

namespace UnityEngine.Rendering.HighDefinition
{
#if UNITY_EDITOR
    public partial class ProbeVolumeDynamicGI
    {

        private Material GetDebugNeighborMaterial()
        {
            if (_DebugNeighborMaterial == null && _ProbeVolumeDebugNeighbors != null)
            {
                _DebugNeighborMaterial = new Material(_ProbeVolumeDebugNeighbors);
            }

            return _DebugNeighborMaterial;
        }

        [GenerateHLSL(needAccessors = false)]
        internal struct ExtraDataRequests
        {
            internal Vector2 uv;
            internal Vector3 pos;
            internal Vector3 N;
            internal int requestIndex;
        }

        [GenerateHLSL(needAccessors = false)]
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

        // Debugging code
        private Material _DebugNeighborMaterial = null;
        private Shader _ProbeVolumeDebugNeighbors = null;

        private const int kDummyRTHeight = 64;
        private const int kDummyRTWidth = 4096;
        private const int kMaxRequestsPerDraw = kDummyRTWidth * kDummyRTHeight;

        internal Dictionary<RequestInput, List<ExtraDataRequests>> requestsList = new Dictionary<RequestInput, List<ExtraDataRequests>>();
        internal List<ExtraDataRequestOutput> extraRequestsOutput = new List<ExtraDataRequestOutput>();

        List<int> processingIdxToOutputIdx = new List<int>();

        internal RTHandle dummyColor;
        internal ComputeBuffer readbackBuffer;
        internal ComputeBuffer inputBuffer;

        private struct ProbeBakeNeighborData
        {
            public Vector3[] neighborColor;
            public Vector3[] neighborNormal;
            public float[] neighborDistance;
            public int[] requestIndex;
            public float validity;
        }

        internal void ConstructNeighborData(Vector3[] probePositions, ref ProbeVolumeAsset probeVolumeAsset, in ProbeVolumeArtistParameters parameters)
        {
            requestsList.Clear();
            extraRequestsOutput.Clear();

            int numProbes = probePositions.Length;
            Debug.Assert(numProbes == probeVolumeAsset.payload.dataValidity.Length);
            var neighborBakeDatas = new ProbeBakeNeighborData[numProbes];

            int hits = 0;
            for (int i = 0; i < numProbes; ++i)
            {
                var probePosition = probePositions[i];
                var validity = probeVolumeAsset.payload.dataValidity[i];
                hits += GenerateBakeNeighborData(probePosition, ref neighborBakeDatas[i], in parameters, validity);
            }

            ExecutePendingRequests();
            for (int i = 0; i < numProbes; ++i)
            {
                ResolveExtraDataRequest(ref neighborBakeDatas[i]);
            }

            GeneratePackedNeighborData(neighborBakeDatas, ref probeVolumeAsset, in parameters, hits);
            ClearContent();
        }

        internal void DebugDrawNeighborhood(ProbeVolumeHandle probeVolume)
        {
            if (probeVolume.HasNeighbors()
                && probeVolume.GetProbeVolumeEngineDataIndex() >= 0)
            {
                var material = GetDebugNeighborMaterial();
                if (material != null)
                {
                    InitializePropagationBuffers(probeVolume);

                    Shader.SetGlobalBuffer("_ProbeVolumeDebugNeighborHits", probeVolume.propagationBuffers.neighborHits);
                    Shader.SetGlobalInt("_ProbeVolumeDebugNeighborHitCount", probeVolume.propagationBuffers.neighborHits.count);
                    Shader.SetGlobalFloat("_ProbeVolumeDebugNeighborQuadScale", probeVolume.parameters.neighborsQuadScale);

                    HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                    if (hdrp != null)
                    {
                        var obb = probeVolume.ConstructOBBEngineData( Vector3.zero);
                        var data = probeVolume.parameters.ConvertToEngineData(hdrp.GetProbeVolumeAtlasSHRTDepthSliceCount());

                        Shader.SetGlobalFloat("_ProbeVolumeDGIMaxNeighborDistance", data.maxNeighborDistance);
                        Shader.SetGlobalInt("_ProbeVolumeDGIResolutionXY", (int)data.resolutionXY);
                        Shader.SetGlobalInt("_ProbeVolumeDGIResolutionX", (int)data.resolutionX);
                        Shader.SetGlobalVector("_ProbeVolumeDGIResolutionInverse", data.resolutionInverse);
                        Shader.SetGlobalVector( "_ProbeVolumeDGIBoundsRight", obb.right);
                        Shader.SetGlobalVector( "_ProbeVolumeDGIBoundsUp", obb.up);
                        Shader.SetGlobalVector( "_ProbeVolumeDGIBoundsExtents", new Vector3(obb.extentX, obb.extentY, obb.extentZ));
                        Shader.SetGlobalVector( "_ProbeVolumeDGIBoundsCenter", obb.center);

                        const int numVerticesPerAxis = 6;

                        // use infinite bounds for now
                        Bounds bounds = new Bounds();
                        bounds.center = Vector3.zero;
                        bounds.Expand(10000000.0f);

                        Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numVerticesPerAxis, probeVolume.propagationBuffers.neighborHits.count, null, null, ShadowCastingMode.Off, false);
                    }
                }
            }
        }

        private void ResolveExtraDataRequest(ref ProbeBakeNeighborData neighborData)
        {
            for (int i = 0; i < s_NeighborAxis.Length; ++i)
            {
                if (neighborData.requestIndex[i] >= 0)
                {
                    var extraDataRequestOutput = RetrieveRequestOutput(neighborData.requestIndex[i]);
                    neighborData.neighborColor[i] = extraDataRequestOutput.albedo;
                }
            }
        }

        internal ExtraDataRequestOutput RetrieveRequestOutput(int requestIndex)
        {
            Debug.Assert(requestIndex < extraRequestsOutput.Count);
            return extraRequestsOutput[requestIndex];
        }

        private int GenerateBakeNeighborData(Vector3 position, ref ProbeBakeNeighborData neighborBakeData, in ProbeVolumeArtistParameters parameters, float validity)
        {
            InitBakeNeighborData(ref neighborBakeData);

            int hits = 0;
            for (int i = 0; i < s_NeighborAxis.Length; ++i)
            {
                Vector4 axis = s_NeighborAxis[i];
                Vector3 dirAxis = axis;
                float distance = ComputeScaledNeighborDistance(i, in parameters);

                int requestIndex = -1;
                float hitDistance = 0.0f;
                Vector3 normal = Vector3.zero;
                GetNormalAndRequestTicketForOccluder(position, dirAxis * distance, ref requestIndex, ref hitDistance, ref normal);

                if(hitDistance > 0)
                {
                    neighborBakeData.requestIndex[i] = requestIndex;
                    neighborBakeData.neighborDistance[i] = hitDistance;
                    neighborBakeData.neighborNormal[i] = normal;
                    hits++;
                }
                else
                {
                    neighborBakeData.neighborColor[i] = Vector3.zero;
                    neighborBakeData.neighborDistance[i] = 0;
                    neighborBakeData.neighborNormal[i] = -dirAxis.normalized;
                    neighborBakeData.requestIndex[i] = -1;
                }
            }

            neighborBakeData.validity = validity;
            return hits;
        }

        private void InitBakeNeighborData(ref ProbeBakeNeighborData bakeNeighborData)
        {
            bakeNeighborData.neighborColor = new Vector3[s_NeighborAxis.Length];
            bakeNeighborData.neighborNormal = new Vector3[s_NeighborAxis.Length];
            bakeNeighborData.neighborDistance = new float[s_NeighborAxis.Length];
            bakeNeighborData.requestIndex = new int[s_NeighborAxis.Length];
            for (int i = 0; i < s_NeighborAxis.Length; ++i)
            {
                bakeNeighborData.requestIndex[i] = -1;
            }
        }

        private void GeneratePackedNeighborData(ProbeBakeNeighborData[] neighborBakeDatas, ref ProbeVolumeAsset probeVolumeAsset, in ProbeVolumeArtistParameters parameters, int hits)
        {
            int totalAxis = neighborBakeDatas.Length * s_NeighborAxis.Length;
            int missedAxis = totalAxis - hits;
            EnsureNeighbors(ref probeVolumeAsset.payload, missedAxis, hits, totalAxis);

            float maxNeighborDistance = GetMaxNeighborDistance(in parameters);
            int missedAxisCount = 0;
            int hitAxisCount = 0;
            for (int i = 0; i < neighborBakeDatas.Length; ++i)
            {
                var neighborBakeData = neighborBakeDatas[i];
                var validity = neighborBakeData.validity;
                for (int axis = 0; axis < s_NeighborAxis.Length; ++axis)
                {
                    var distance = neighborBakeData.neighborDistance[axis];
                    var color = neighborBakeData.neighborColor[axis];
                    var normal = neighborBakeData.neighborNormal[axis];

                    if (distance == 0)
                    {
                        // miss
                        SetNeighborData(ref probeVolumeAsset.payload, validity, i, axis, NeighborAxis.Miss);
                        ++missedAxisCount;
                    }
                    else
                    {
                        // hit
                        SetNeighborDataHit(ref probeVolumeAsset.payload, color, normal, distance, validity, i, axis, hitAxisCount, maxNeighborDistance );
                        SetNeighborData(ref probeVolumeAsset.payload, validity, i, axis, (uint)hitAxisCount);
                        ++hitAxisCount;
                    }
                }
            }

        }

        private bool GetNormalAndRequestTicketForOccluder(Vector3 worldPosition, Vector3 ray, ref int requestIndex, ref float outDistance, ref Vector3 normal)
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
                        requestIndex = EnqueueExtraDataRequest(hit, worldPosition + normalizedRay * outDistance);
                        normal = outBoundHits[outIndex].normal;
                        if (requestIndex < 0)
                        {
                            outDistance = 0;
                            return false;
                        }
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

        private static bool IsValidForBaking(GameObject gameObject)
        {
            UnityEditor.StaticEditorFlags flags = UnityEditor.GameObjectUtility.GetStaticEditorFlags(gameObject);
            if (gameObject.activeInHierarchy
                && (flags & UnityEditor.StaticEditorFlags.ContributeGI) == UnityEditor.StaticEditorFlags.ContributeGI
                && !SceneVisibilityManager.instance.IsHidden(gameObject))
            {
                return true;
            }

            return false;
        }

        private static float ComputeScaledNeighborDistance(int neighborAxis, in ProbeVolumeArtistParameters parameters)
        {
            var distance = new Vector3(1.0f / parameters.densityX, 1.0f / parameters.densityY, 1.0f / parameters.densityZ);
            var offset = ProbeVolumeAsset.s_Offsets[neighborAxis];
            var fOffset = new Vector3(offset.x, offset.y, offset.z);
            fOffset.x = Mathf.Abs(offset.x);
            fOffset.y = Mathf.Abs(offset.y);
            fOffset.z = Mathf.Abs(offset.z);

            var scaledOffset = Vector3.Scale(distance, fOffset);
            return scaledOffset.magnitude;
        }

        private int EnqueueExtraDataRequest(RaycastHit hit, Vector3 hitPosition)
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
                if (renderer != null && renderer.sharedMaterials[submesh] != null)
                {
                    RequestInput requestInput;
                    requestInput.mesh = mesh;
                    requestInput.renderer = renderer;
                    requestInput.subMesh = submesh;
                    requestTicket = EnqueueRequest(requestInput, hit.textureCoord, hitPosition, hit.normal);
                }
            }

            return requestTicket;
        }

        internal void ClearContent()
        {
            requestsList.Clear();
            extraRequestsOutput.Clear();
        }

        private int EnqueueRequest(RequestInput input, Vector2 uv, Vector3 posWS, Vector3 normalWS)
        {
            ExtraDataRequestOutput output;
            output.albedo = new Vector3(0.0f, 0.0f, 0.0f);

            int requestIndex = extraRequestsOutput.Count;
            extraRequestsOutput.Add(output);

            ExtraDataRequests request;
            request.requestIndex = requestIndex;
            request.uv = uv;
            request.pos = posWS;
            request.N = normalWS;

            if (!requestsList.ContainsKey(input))
            {
                requestsList.Add(input, new List<ExtraDataRequests>());
            }

            requestsList[input].Add(request);

            return requestIndex;
        }

        internal Color GetBaseColor(Material material)
        {
            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }
            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return material.color;
        }


        struct SortedRequests
        {
            public ExtraDataRequests dataReq;
            public Material material;

            public SortedRequests(ExtraDataRequests request, Material mat)
            {
                this.dataReq = request;
                this.material = mat;
            }
        }

        List<SortedRequests> SortRequestsForExecution(out List<int> subListsSizes, out int maxSubListSize)
        {
            List<SortedRequests> outRequests = new List<SortedRequests>();
            subListsSizes = new List<int>();
            maxSubListSize = 0;

            // Can be done more efficiently later.
            Dictionary<Material, List<ExtraDataRequests>> requestsForExtraction = new Dictionary<Material, List<ExtraDataRequests>>();
            foreach (var entry in requestsList)
            {
                var input = entry.Key;
                var material = input.renderer.sharedMaterials[input.subMesh];
                if (material == null) continue;

                if (!requestsForExtraction.ContainsKey(material))
                {
                    requestsForExtraction.Add(material, new List<ExtraDataRequests>());
                }

                requestsForExtraction[material].AddRange(entry.Value);
            }
            processingIdxToOutputIdx = new List<int>();

            foreach (var materialList in requestsForExtraction)
            {
                var currMaterial = materialList.Key;
                var requestsForMaterial = materialList.Value;
                foreach (var requestData in requestsForMaterial)
                {
                    SortedRequests sortedReq = new SortedRequests(requestData, currMaterial);
                    processingIdxToOutputIdx.Add(requestData.requestIndex);
                    outRequests.Add(sortedReq);
                }
                maxSubListSize = Mathf.Max(maxSubListSize, requestsForMaterial.Count);
                subListsSizes.Add(requestsForMaterial.Count);
            }

            return outRequests;
        }

        private void ExecuteARequestList(CommandBuffer cmd, Material material, ComputeBuffer inputBuffer, int startOfList, int requestCount)
        {
            int quadHeight = kDummyRTHeight;
            int requiredQuadLen = Mathf.CeilToInt(requestCount * (1.0f / quadHeight));

            Rect dummyDrawRect = new Rect(0, 0, requiredQuadLen, quadHeight);

            var passIdx = -1;
            for (int i = 0; i < material.passCount; ++i)
            {
                if (material.GetPassName(i).IndexOf("DynamicGIDataSample") >= 0)
                {
                    passIdx = i;
                    break;
                }
            }
            if (passIdx >= 0)
            {
                material.SetPass(passIdx);

                // Globally set, very lazily :P
                cmd.SetGlobalBuffer("_RequestsInputData", inputBuffer);
                cmd.SetGlobalVector("_MaterialRequestsInfo", new Vector4(requestCount, startOfList, quadHeight, 0));

                cmd.SetRandomWriteTarget(1, readbackBuffer);
                cmd.SetRenderTarget(dummyColor);
                HDUtils.DrawFullScreen(cmd, dummyDrawRect, material, dummyColor, null, passIdx);
            }
        }

        private void ExecutePendingRequests()
        {
            List<SortedRequests> sortedRequests;
            List<int> subListSizes;
            int maxSubListSize = 0;
            sortedRequests = SortRequestsForExecution(out subListSizes, out maxSubListSize);
            if (maxSubListSize == 0)
            {
                return;
            }

            var cmd = CommandBufferPool.Get("Execute Dynamic GI extra data requests");

            // Alloc input to the max size
            inputBuffer = new ComputeBuffer(maxSubListSize, Marshal.SizeOf<ExtraDataRequests>());
            readbackBuffer = new ComputeBuffer(extraRequestsOutput.Count, Marshal.SizeOf<ExtraDataRequestOutput>());

            int currStart = 0;
            for (int subList = 0; subList < subListSizes.Count; ++subList)
            {
                int subListSize = subListSizes[subList];

                int numberOfIterationsNeeded = Mathf.CeilToInt((float)subListSize / (float)kMaxRequestsPerDraw); // Hopefully this is always one

                for (int draw = 0; draw < numberOfIterationsNeeded; ++draw)
                {
                    int itemsThisDraw = Mathf.Min(subListSize - draw * kMaxRequestsPerDraw, kMaxRequestsPerDraw);
                    // Fill input buffer to what is needed.
                    List<ExtraDataRequests> inputs = new List<ExtraDataRequests>();
                    for (int i = 0; i < itemsThisDraw; ++i)
                    {
                        inputs.Add(sortedRequests[currStart + i].dataReq);
                    }
                    inputBuffer.SetData(inputs.ToArray(), 0, 0, itemsThisDraw);

                    ExecuteARequestList(cmd, sortedRequests[currStart].material, inputBuffer,  currStart, itemsThisDraw);
                    currStart += itemsThisDraw;
                }
            }

            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);


            // Read back.
            Debug.Assert(currStart == extraRequestsOutput.Count);
            Debug.Assert(processingIdxToOutputIdx.Count == currStart);

            var outputData = new ExtraDataRequestOutput[currStart];
            readbackBuffer.GetData(outputData);
            // Put back directly with the mapping
            for (int i = 0; i < currStart; ++i)
            {
                extraRequestsOutput[processingIdxToOutputIdx[i]] = outputData[i];
            }

            // We are done with GPU buffers.
            CoreUtils.SafeRelease(inputBuffer);
            CoreUtils.SafeRelease(readbackBuffer);
        }
    }
#endif

} // UnityEngine.Experimental.Rendering.HDPipeline
