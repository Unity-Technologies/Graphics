using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEditor;

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
            internal Vector2 uvDdx;
            internal Vector2 uvDdy;
            internal Vector3 position;
            internal Vector3 positionDdx;
            internal Vector3 positionDdy;
            internal Vector3 normalWS;
            internal int requestIdx;
        }

        [GenerateHLSL(needAccessors = false)]
        internal struct ExtraDataRequestOutput
        {
            internal Vector3 albedo;
            internal Vector3 emission;
        }

        internal struct RequestInput : IEquatable<RequestInput>
        {
            internal MeshRenderer renderer;
            internal Mesh mesh;
            internal int subMesh;

            public bool Equals(RequestInput other)
            {
                return Equals(renderer, other.renderer) && Equals(mesh, other.mesh) && subMesh == other.subMesh;
            }

            public override bool Equals(object obj)
            {
                return obj is RequestInput other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = renderer != null ? renderer.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (mesh != null ? mesh.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ subMesh;
                    return hashCode;
                }
            }
        }

        // Debugging code
        private Material _DebugNeighborMaterial = null;
        private Shader _ProbeVolumeDebugNeighbors = null;

        private const int kDummyRTHeight = 64;
        private const int kDummyRTWidth = 4096;
        private const int kMaxRequestsPerColumn = kDummyRTHeight / 2;
        private const int kMaxRequestsPerRow = kDummyRTWidth / 2;
        private const int kMaxRequestsPerDraw = kMaxRequestsPerColumn * kMaxRequestsPerRow;

        internal Dictionary<RequestInput, List<ExtraDataRequests>> requestsList = new Dictionary<RequestInput, List<ExtraDataRequests>>();
        internal List<ExtraDataRequestOutput> extraRequestsOutput = new List<ExtraDataRequestOutput>();
        RaycastHit[] hitBuffer = new RaycastHit[256];

        List<int> processingIdxToOutputIdx = new List<int>();

        internal RTHandle dummyColor;

        private struct ProbeBakeNeighborData
        {
            public Vector3[] neighborAlbedo;
            public Vector3[] neighborEmission;
            public Vector3[] neighborNormal;
            public float[] neighborDistance;
            public int[] requestIndex;
            public float validity;
        }

        internal void ConstructNeighborData(Vector3[] probePositionsWS, Quaternion rotation, ref ProbeVolumeAsset probeVolumeAsset, in ProbeVolumeArtistParameters parameters, bool overwriteValidity)
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(ConstructNeighborData)}");

            int numProbes = probePositionsWS.Length;
            Debug.Assert(numProbes == probeVolumeAsset.payload.dataValidity.Length);
            var neighborBakeDatas = new ProbeBakeNeighborData[numProbes];

            Vector3 voxelSize = new Vector3(1.0f / parameters.densityX, 1.0f / parameters.densityY, 1.0f / parameters.densityZ);
            Matrix4x4 voxelTransform = Matrix4x4.TRS(Vector3.zero, rotation, voxelSize);
            Matrix4x4 inverseVoxelTransform = Matrix4x4.Inverse(voxelTransform);
            
            int hits = 0;
            for (int i = 0; i < numProbes; ++i)
            {
                var probePositionWS = probePositionsWS[i];
                hits += GenerateBakeNeighborData(probePositionWS, voxelTransform, inverseVoxelTransform, ref neighborBakeDatas[i], ref probeVolumeAsset.payload.dataValidity[i], overwriteValidity);
            }

            ExecutePendingRequests();
            for (int i = 0; i < numProbes; ++i)
            {
                ResolveExtraDataRequest(ref neighborBakeDatas[i]);
            }

            GeneratePackedNeighborData(neighborBakeDatas, ref probeVolumeAsset, in parameters, hits);
            ClearContent();
            
            Profiling.Profiler.EndSample();
        }

        internal void DebugDrawNeighborhood(ProbeVolumeHandle probeVolume, Camera camera)
        {
            if (probeVolume.HitNeighborAxisLength != 0
                && probeVolume.GetProbeVolumeEngineDataIndex() >= 0)
            {
                var material = GetDebugNeighborMaterial();
                if (material != null)
                {
                    InitializePropagationBuffers(probeVolume);

                    Shader.SetGlobalBuffer("_ProbeVolumeDebugNeighborHits", probeVolume.propagationBuffers.neighborHits);
                    Shader.SetGlobalInt("_ProbeVolumeDebugNeighborHitCount", probeVolume.propagationBuffers.neighborHits.count);
                    Shader.SetGlobalFloat("_ProbeVolumeDebugNeighborQuadScale", probeVolume.parameters.neighborsQuadScale);
                    Shader.SetGlobalInt("_ProbeVolumeDebugNeighborMode", probeVolume.parameters.drawEmission ? 1 : 0);

                    HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                    if (hdrp != null)
                    {
                        var obb = probeVolume.ConstructOBBEngineData( Vector3.zero);
                        var data = probeVolume.parameters.ConvertToEngineData(hdrp.GetProbeVolumeAtlasSHRTDepthSliceCount(), probeVolume.parameters.distanceFadeStart, probeVolume.parameters.distanceFadeEnd);

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

                        Graphics.DrawProcedural(material, bounds, MeshTopology.Triangles, numVerticesPerAxis, probeVolume.propagationBuffers.neighborHits.count, camera, null, ShadowCastingMode.Off, false);
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
                    neighborData.neighborAlbedo[i] = extraDataRequestOutput.albedo;
                    neighborData.neighborEmission[i] = extraDataRequestOutput.emission;
                }
            }
        }

        internal ExtraDataRequestOutput RetrieveRequestOutput(int requestIndex)
        {
            Debug.Assert(requestIndex < extraRequestsOutput.Count);
            return extraRequestsOutput[requestIndex];
        }

        private int GenerateBakeNeighborData(Vector3 positionWS, Matrix4x4 voxelTransform, Matrix4x4 inverseVoxelTransform, ref ProbeBakeNeighborData neighborBakeData, ref float validity, bool overwriteValidity)
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(GenerateBakeNeighborData)}");
            
            InitBakeNeighborData(ref neighborBakeData);

            int anyHits = 0;
            int backfaceHits = 0;
            int hitsWithMaterial = 0;
            for (int i = 0; i < s_NeighborAxis.Length; ++i)
            {
                var offsetVS = (Vector3)ProbeVolumeAsset.s_Offsets[i];
                
                var requestIndex = GetRequestIndexForOccluder(positionWS, offsetVS, voxelTransform, inverseVoxelTransform, out float hitDistance, out Vector3 normal, out RaycastFaceSide faceSide);

                if (faceSide != RaycastFaceSide.None)
                    anyHits++;
                if (faceSide == RaycastFaceSide.Backface)
                    backfaceHits++;
                
                neighborBakeData.requestIndex[i] = requestIndex;
                if (requestIndex != -1)
                {
                    neighborBakeData.neighborDistance[i] = hitDistance;
                    neighborBakeData.neighborNormal[i] = normal;
                    hitsWithMaterial++;
                }
                else
                {
                    neighborBakeData.neighborAlbedo[i] = Vector3.zero;
                    neighborBakeData.neighborEmission[i] = Vector3.zero;
                    neighborBakeData.neighborDistance[i] = 0;
                    neighborBakeData.neighborNormal[i] = Vector3.zero;
                }
            }

            if (overwriteValidity)
                validity = anyHits != 0 ? (float)backfaceHits / anyHits : 0f;
            
            neighborBakeData.validity = validity;
            
            Profiling.Profiler.EndSample();
            
            return hitsWithMaterial;
        }

        private void InitBakeNeighborData(ref ProbeBakeNeighborData bakeNeighborData)
        {
            bakeNeighborData.neighborAlbedo = new Vector3[s_NeighborAxis.Length];
            bakeNeighborData.neighborEmission = new Vector3[s_NeighborAxis.Length];
            bakeNeighborData.neighborNormal = new Vector3[s_NeighborAxis.Length];
            bakeNeighborData.neighborDistance = new float[s_NeighborAxis.Length];
            bakeNeighborData.requestIndex = new int[s_NeighborAxis.Length];
        }

        private void GeneratePackedNeighborData(ProbeBakeNeighborData[] neighborBakeDatas, ref ProbeVolumeAsset probeVolumeAsset, in ProbeVolumeArtistParameters parameters, int hits)
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(GeneratePackedNeighborData)}");
            
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
                    var albedo = neighborBakeData.neighborAlbedo[axis];
                    var emission = neighborBakeData.neighborEmission[axis];
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
                        SetNeighborDataHit(ref probeVolumeAsset.payload, albedo, emission, normal, distance, validity, i, axis, hitAxisCount, maxNeighborDistance );
                        SetNeighborData(ref probeVolumeAsset.payload, validity, i, axis, (uint)hitAxisCount);
                        ++hitAxisCount;
                    }
                }
            }
            
            Profiling.Profiler.EndSample();
        }

        enum RaycastFaceSide
        {
            None,
            Frontface,
            Backface
        }
        
        private int GetRequestIndexForOccluder(Vector3 positionWS, Vector3 offsetVS, Matrix4x4 voxelTransform, Matrix4x4 inverseVoxelTransform, out float distance, out Vector3 normalWS, out RaycastFaceSide faceSide)
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(GetRequestIndexForOccluder)}");
            
            var offsetWS = voxelTransform.MultiplyVector(offsetVS);
            var neighborDistanceWS = offsetWS.magnitude;
            if (neighborDistanceWS == 0f)
            {
                distance = 0f;
                normalWS = Vector3.zero;
                faceSide = RaycastFaceSide.None;

                Profiling.Profiler.EndSample();
                return -1;
            }

            var directionWS = offsetWS / neighborDistanceWS;
            var collisionLayerMask = ~0;

            var inHitCount = Physics.RaycastNonAlloc(positionWS + offsetWS, -1.0f * directionWS, hitBuffer, neighborDistanceWS, collisionLayerMask);
            var hasInHit = TryFindNearestHit(inHitCount, neighborDistanceWS, true, out var inDistance, out _, out _);

            var outHitCount = Physics.RaycastNonAlloc(positionWS, directionWS, hitBuffer, neighborDistanceWS, collisionLayerMask);
            var hasOutHit = TryFindNearestHit(outHitCount, neighborDistanceWS, false, out var outDistance, out var hit, out var collider);

            if (hasOutHit)
            {
                var requestIndex = EnqueueExtraDataRequest(voxelTransform, inverseVoxelTransform, hit, collider, positionWS + directionWS * outDistance);
                if (requestIndex != -1)
                {
                    distance = outDistance;
                    normalWS = inverseVoxelTransform.MultiplyVector(hit.normal).normalized;
                    faceSide = outDistance < inDistance ? RaycastFaceSide.Frontface : RaycastFaceSide.Backface;

                    Profiling.Profiler.EndSample();
                    return requestIndex;
                }
            }

            distance = 0f;
            normalWS = Vector3.zero;
            faceSide = hasInHit ? RaycastFaceSide.Backface : RaycastFaceSide.None;

            Profiling.Profiler.EndSample();
            return -1;
        }

        private bool TryFindNearestHit(int hitCount, float maxDist, bool rayIsInverted, out float distance, out RaycastHit hit, out MeshCollider collider)
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(TryFindNearestHit)}");
            
            hit = default;
            distance = maxDist;
            collider = null;

            var hitFound = false;
            for (int i = 0; i < hitCount; ++i)
            {
                RaycastHit candidateHit = hitBuffer[i];
                var candidateDistance = rayIsInverted ? (maxDist - candidateHit.distance) : candidateHit.distance;
                
                if (candidateDistance < distance)
                {
                    var candidateCollider = candidateHit.collider;

                    if (candidateCollider is MeshCollider candidateMeshCollider && IsValidForBaking(candidateCollider.gameObject))
                    {
                        hit = candidateHit;
                        distance = candidateDistance;
                        collider = candidateMeshCollider;
                        hitFound = true;
                    }
                }
            }
            
            Profiling.Profiler.EndSample();

            return hitFound;
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

        private int EnqueueExtraDataRequest(Matrix4x4 voxelTransform, Matrix4x4 inverseVoxelTransform, RaycastHit hit, MeshCollider collider, Vector3 hitPositionWS)
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(EnqueueExtraDataRequest)}");
            
            int requestTicket = -1;

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
                
                // World-space sample size
                var hitNormalWS = hit.normal;
                var hitNormalVS = inverseVoxelTransform.MultiplyVector(hitNormalWS);
                Vector3 hitPositionDdxVS;
                Vector3 hitPositionDdyVS;
                if (hitNormalVS.x == 0f && hitNormalVS.z == 0f)
                {
                    hitPositionDdxVS = Vector3.right;
                    hitPositionDdyVS = Vector3.forward;
                }
                else
                {
                    hitPositionDdxVS = ExtendToCubeBounds(Vector3.Cross(Vector3.down, hitNormalVS));
                    hitPositionDdyVS = ExtendToCubeBounds(Vector3.Cross(hitPositionDdxVS, hitNormalVS));
                }
                var hitPositionDdxWS = voxelTransform.MultiplyVector(hitPositionDdxVS);
                var hitPositionDdyWS = voxelTransform.MultiplyVector(hitPositionDdyVS);
                
                // UV-space sample size
                var worldToRenderer = renderer.transform.worldToLocalMatrix;
                var hitPositionDdxOS = worldToRenderer.MultiplyVector(hitPositionDdxWS);
                var hitPositionDdyOS = worldToRenderer.MultiplyVector(hitPositionDdyWS);
                var sampleAreaOS = hitPositionDdxOS.magnitude * hitPositionDdyOS.magnitude;
                var uvDistributionMetric = mesh.GetUVDistributionMetric(0);
                var sampleSideUV = Mathf.Sqrt(sampleAreaOS / uvDistributionMetric);
                
                requestTicket = EnqueueRequest(requestInput,
                    hit.textureCoord, new Vector2(sampleSideUV, 0f), new Vector2(0f, sampleSideUV),
                    hitPositionWS, hitPositionDdxWS, hitPositionDdyWS,
                    hitNormalWS);
            }
            
            Profiling.Profiler.EndSample();

            return requestTicket;
        }

        static Vector3 ExtendToCubeBounds(Vector3 value)
        {
            var maxAxis = Mathf.Abs(value.x);
            maxAxis = Mathf.Max(maxAxis, Mathf.Abs(value.y));
            maxAxis = Mathf.Max(maxAxis, Mathf.Abs(value.z));
            return maxAxis == 0f ? value : value / maxAxis;
        }

        internal void ClearContent()
        {
            requestsList.Clear();
            extraRequestsOutput.Clear();
        }

        private int EnqueueRequest(RequestInput input,
            Vector2 uv, Vector2 uvDdx, Vector2 uvDdy,
            Vector3 posWS, Vector3 posWSDdx, Vector3 posWSDdy,
            Vector3 normalWS)
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(EnqueueRequest)}");
            
            int requestIndex = extraRequestsOutput.Count;
            extraRequestsOutput.Add(default);

            ExtraDataRequests request;
            request.requestIdx = requestIndex;
            request.uv = uv;
            request.uvDdx = uvDdx;
            request.uvDdy = uvDdy;
            request.position = posWS;
            request.positionDdx = posWSDdx;
            request.positionDdy = posWSDdy;
            request.normalWS = normalWS;

            if (!requestsList.TryGetValue(input, out var list))
            {
                list = new List<ExtraDataRequests>();
                requestsList.Add(input, list);
            }

            list.Add(request);
            
            Profiling.Profiler.EndSample();

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
                    processingIdxToOutputIdx.Add(requestData.requestIdx);
                    outRequests.Add(sortedReq);
                }
                maxSubListSize = Mathf.Max(maxSubListSize, requestsForMaterial.Count);
                subListsSizes.Add(requestsForMaterial.Count);
            }

            return outRequests;
        }

        private void ExecuteARequestList(CommandBuffer cmd, Material material, ComputeBuffer inputBuffer, int startOfList, int requestCount)
        {
            int requestsPerRow = Mathf.CeilToInt(requestCount * (1.0f / kMaxRequestsPerColumn));

            Rect dummyDrawRect = new Rect(0, 0, requestsPerRow * 2, kDummyRTHeight);

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
                var hasBakedEmission = material.globalIlluminationFlags == MaterialGlobalIlluminationFlags.BakedEmissive ? 1f : 0f;
                cmd.SetGlobalVector("_MaterialRequestsInfo", new Vector4(requestCount, startOfList, kMaxRequestsPerColumn, hasBakedEmission));
                HDUtils.DrawFullScreen(cmd, dummyDrawRect, material, dummyColor, null, passIdx);
            }
        }

        private void ExecutePendingRequests()
        {
            Profiling.Profiler.BeginSample($"{nameof(ProbeVolumeDynamicGI)}.{nameof(ExecutePendingRequests)}");
            
            List<SortedRequests> sortedRequests;
            List<int> subListSizes;
            int maxSubListSize = 0;
            sortedRequests = SortRequestsForExecution(out subListSizes, out maxSubListSize);
            if (maxSubListSize == 0)
            {
                Profiling.Profiler.EndSample();
                return;
            }

            var cmd = CommandBufferPool.Get("Execute Dynamic GI extra data requests");

            // Alloc input to the max size
            var inputBuffer = new ComputeBuffer(maxSubListSize, Marshal.SizeOf<ExtraDataRequests>());
            var readbackBuffer = new ComputeBuffer(extraRequestsOutput.Count, Marshal.SizeOf<ExtraDataRequestOutput>());

            HDRenderPipeline.currentPipeline.PrepareGlobalMaskVolumeList(cmd);

            // Globally set, very lazily :P
            cmd.SetGlobalBuffer("_RequestsInputData", inputBuffer);
            cmd.SetRandomWriteTarget(1, readbackBuffer);
            cmd.SetRenderTarget(dummyColor);

            int currStart = 0;
            for (int subList = 0; subList < subListSizes.Count; ++subList)
            {
                int subListSize = subListSizes[subList];

                int numberOfIterationsNeeded = Mathf.CeilToInt((float)subListSize / (float)kMaxRequestsPerDraw); // Hopefully this is always one

                for (int draw = 0; draw < numberOfIterationsNeeded; ++draw)
                {
                    int itemsThisDraw = Mathf.Min(subListSize - draw * kMaxRequestsPerDraw, kMaxRequestsPerDraw);
                    // Fill input buffer to what is needed.
                    ExtraDataRequests[] inputs = new ExtraDataRequests[itemsThisDraw];
                    for (int i = 0; i < itemsThisDraw; ++i)
                    {
                        inputs[i] = sortedRequests[currStart + i].dataReq;
                    }
                    cmd.SetComputeBufferData(inputBuffer, inputs, 0, 0, itemsThisDraw);

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
            
            Profiling.Profiler.EndSample();
        }
    }
#endif

} // UnityEngine.Experimental.Rendering.HDPipeline
