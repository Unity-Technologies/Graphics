using System.Diagnostics;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    public partial class ProbeReferenceVolume
    {
        internal class DiskStreamingRequest
        {
            ReadHandle m_ReadHandle;
            ReadCommandArray m_ReadCommandArray = new ReadCommandArray();
            NativeArray<ReadCommand> m_ReadCommandBuffer;
            int m_BytesWritten;

            public DiskStreamingRequest(int maxRequestCount)
            {
                m_ReadCommandBuffer = new NativeArray<ReadCommand>(maxRequestCount, Allocator.Persistent);
            }

            unsafe public void AddReadCommand(int offset, int size, byte* dest)
            {
                Debug.Assert(m_ReadCommandArray.CommandCount < m_ReadCommandBuffer.Length);

                m_ReadCommandBuffer[m_ReadCommandArray.CommandCount++] = new ReadCommand()
                {
                    Buffer = dest,
                    Offset = offset,
                    Size = size
                };

                m_BytesWritten += size;
            }

            unsafe public int RunCommands(FileHandle file)
            {
                m_ReadCommandArray.ReadCommands = (ReadCommand*)m_ReadCommandBuffer.GetUnsafePtr();
                m_ReadHandle = AsyncReadManager.Read(file, m_ReadCommandArray);

                return m_BytesWritten;
            }

            public void Clear()
            {
                if (m_ReadHandle.IsValid())
                    m_ReadHandle.JobHandle.Complete();
                m_ReadHandle = default;
                m_ReadCommandArray.CommandCount = 0;
                m_BytesWritten = 0;
            }

            public void Cancel()
            {
                if (m_ReadHandle.IsValid())
                    m_ReadHandle.Cancel();
            }

            public void Wait()
            {
                if (m_ReadHandle.IsValid())
                    m_ReadHandle.JobHandle.Complete();
            }

            public void Dispose()
            {
                m_ReadCommandBuffer.Dispose();
            }

            public ReadStatus GetStatus()
            {
                return m_ReadHandle.IsValid() ? m_ReadHandle.Status : ReadStatus.Complete;
            }
        }

        [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
        internal struct CellStreamingScratchBufferLayout
        {
            public int _SharedDestChunksOffset;
            public int _L0L1rxOffset;
            public int _L1GryOffset;
            public int _L1BrzOffset;
            public int _ValidityOffset;
            public int _ProbeOcclusionOffset;
            public int _SkyOcclusionOffset;
            public int _SkyShadingDirectionOffset;
            public int _L2_0Offset;
            public int _L2_1Offset;
            public int _L2_2Offset;
            public int _L2_3Offset;

            public int _L0Size;
            public int _L0ProbeSize; // In bytes
            public int _L1Size;
            public int _L1ProbeSize; // In bytes
            public int _ValiditySize;
            public int _ValidityProbeSize; // In bytes
            public int _ProbeOcclusionSize;
            public int _ProbeOcclusionProbeSize; // In bytes
            public int _SkyOcclusionSize;
            public int _SkyOcclusionProbeSize; // In bytes
            public int _SkyShadingDirectionSize;
            public int _SkyShadingDirectionProbeSize; // In bytes
            public int _L2Size;
            public int _L2ProbeSize; // In bytes

            public int _ProbeCountInChunkLine;
            public int _ProbeCountInChunkSlice;
        }

        internal class CellStreamingScratchBuffer
        {
            public CellStreamingScratchBuffer(int chunkCount, int chunkSize, bool allocateGraphicsBuffers)
            {
                this.chunkCount = chunkCount;

                // With a stride of 4 (one uint)
                // Number of elements for chunk data: chunkCount * chunkSize / 4
                // Number of elements for dest chunk data (Vector4Int): chunkCount * 4;
                var bufferSize = chunkCount * chunkSize / 4 + chunkCount * 4;

                // Account for additional padding needed
                bufferSize += 2 * chunkCount * sizeof(uint);

                if (allocateGraphicsBuffers)
                {
                    for (int i = 0; i < 2; i++)
                        m_GraphicsBuffers[i] = new GraphicsBuffer(GraphicsBuffer.Target.Raw, GraphicsBuffer.UsageFlags.LockBufferForWrite, bufferSize, sizeof(uint));
                }

                m_CurrentBuffer = 0;

                stagingBuffer = new NativeArray<byte>(bufferSize * sizeof(uint), Allocator.Persistent);
            }

            public void Swap()
            {
                m_CurrentBuffer = (m_CurrentBuffer + 1 ) % 2;
            }

            public void Dispose()
            {
                for (int i = 0; i < 2; ++i)
                    m_GraphicsBuffers[i]?.Dispose();
                stagingBuffer.Dispose();
            }

            // The GraphicsBuffer is double buffer because the data upload shader might still be running
            // when we start a new streaming request.
            // We could have double buffered at the CellStreamingScratchBuffer level itself but it would consume more memory (native+graphics buffer x2)
            public GraphicsBuffer buffer => m_GraphicsBuffers[m_CurrentBuffer];
            public NativeArray<byte> stagingBuffer; // Contains data streamed from disk. To be copied into the graphics buffer.
            public int chunkCount { get; }

            int m_CurrentBuffer;
            GraphicsBuffer[] m_GraphicsBuffers = new GraphicsBuffer[2];
        }

        [DebuggerDisplay("Index = {cell.desc.index} State = {state}")]
        internal class CellStreamingRequest
        {
            public enum State
            {
                Pending,
                Active,
                Canceled,
                Invalid,
                Complete,
            }

            public Cell cell { get; set; }
            public State state { get; set; }
            public CellStreamingScratchBuffer scratchBuffer { get; set; }
            public CellStreamingScratchBufferLayout scratchBufferLayout { get; set; }

            public ProbeVolumeBakingSet.PerScenarioDataInfo scenarioData { get; set; }
            public int poolIndex { get; set; }
            public bool streamSharedData { get; set; }

            public delegate void OnStreamingCompleteDelegate(CellStreamingRequest request, CommandBuffer cmd);
            public OnStreamingCompleteDelegate onStreamingComplete = null;

            public DiskStreamingRequest cellDataStreamingRequest = new DiskStreamingRequest(1);
            public DiskStreamingRequest cellOptionalDataStreamingRequest = new DiskStreamingRequest(1);
            public DiskStreamingRequest cellSharedDataStreamingRequest = new DiskStreamingRequest(1);
            public DiskStreamingRequest cellProbeOcclusionDataStreamingRequest = new DiskStreamingRequest(1);
            public DiskStreamingRequest brickStreamingRequest = new DiskStreamingRequest(1);
            public DiskStreamingRequest supportStreamingRequest = new DiskStreamingRequest(5);

            public int bytesWritten;

            public bool IsStreaming()
            {
                return state == State.Pending || state == State.Active;
            }

            public void Cancel()
            {
                if (state == State.Active)
                {
                    brickStreamingRequest.Cancel();
                    supportStreamingRequest.Cancel();
                    cellDataStreamingRequest.Cancel();
                    cellOptionalDataStreamingRequest.Cancel();
                    cellSharedDataStreamingRequest.Cancel();
                    cellProbeOcclusionDataStreamingRequest.Cancel();
                }

                state = State.Canceled;
            }

            public void WaitAll()
            {
                if (state == State.Active)
                {
                    brickStreamingRequest.Wait();
                    supportStreamingRequest.Wait();
                    cellDataStreamingRequest.Wait();
                    cellOptionalDataStreamingRequest.Wait();
                    cellSharedDataStreamingRequest.Wait();
                    cellProbeOcclusionDataStreamingRequest.Wait();
                }
            }

            public bool UpdateRequestState(DiskStreamingRequest request, ref bool isComplete)
            {
                var status = request.GetStatus();
                if (status == ReadStatus.Failed)
                    return false;

                isComplete &= status == ReadStatus.Complete;
                return true;
            }

            public void UpdateState()
            {
                if (state == State.Active)
                {
                    bool isComplete = true;
                    bool success = UpdateRequestState(brickStreamingRequest, ref isComplete);
                    success &= UpdateRequestState(supportStreamingRequest, ref isComplete);
                    success &= UpdateRequestState(cellDataStreamingRequest, ref isComplete);
                    success &= UpdateRequestState(cellOptionalDataStreamingRequest, ref isComplete);
                    success &= UpdateRequestState(cellSharedDataStreamingRequest, ref isComplete);
                    success &= UpdateRequestState(cellProbeOcclusionDataStreamingRequest, ref isComplete);

                    if (!success)
                    {
                        Cancel(); // At least one of the requests failed. Cancel the others.
                        state = State.Invalid;
                    }
                    else if (isComplete)
                    {
                        state = State.Complete;
                    }
                }
            }

            public void Clear()
            {
                cell = null;
                Reset();
            }

            public void Reset()
            {
                state = State.Pending;
                scratchBuffer = null;
                brickStreamingRequest.Clear();
                supportStreamingRequest.Clear();
                cellDataStreamingRequest.Clear();
                cellOptionalDataStreamingRequest.Clear();
                cellSharedDataStreamingRequest.Clear();
                cellProbeOcclusionDataStreamingRequest.Clear();
                bytesWritten = 0;
            }

            public void Dispose()
            {
                brickStreamingRequest.Dispose();
                supportStreamingRequest.Dispose();
                cellDataStreamingRequest.Dispose();
                cellOptionalDataStreamingRequest.Dispose();
                cellSharedDataStreamingRequest.Dispose();
                cellProbeOcclusionDataStreamingRequest.Dispose();
            }
        }

#if UNITY_EDITOR
        // By default on editor we load a lot of cells in one go to avoid having to mess with scene view
        // to see results, this value can still be changed via API.
        bool m_LoadMaxCellsPerFrame = true;
#else
        bool m_LoadMaxCellsPerFrame = false;
#endif

        /// <summary>
        /// Enable streaming as many cells per frame as possible.
        /// </summary>
        /// <param name="value">True to enable streaming as many cells per frame as possible.</param>
        public void EnableMaxCellStreaming(bool value)
        {
            m_LoadMaxCellsPerFrame = value;
        }

        const int kMaxCellLoadedPerFrame = 10;
        int m_NumberOfCellsLoadedPerFrame = 1;

        /// <summary>
        /// Set the number of cells that are loaded per frame when needed. This number is capped at 10.
        /// </summary>
        /// <param name="numberOfCells">Number of cells to be loaded per frame.</param>
        public void SetNumberOfCellsLoadedPerFrame(int numberOfCells)
        {
            m_NumberOfCellsLoadedPerFrame = Mathf.Min(kMaxCellLoadedPerFrame, Mathf.Max(1, numberOfCells));
        }

        /// <summary>Set to true to stream as many cells as possible every frame.</summary>
        public bool loadMaxCellsPerFrame
        {
            get => m_LoadMaxCellsPerFrame;
            set => m_LoadMaxCellsPerFrame = value;
        }

        int numberOfCellsLoadedPerFrame => m_LoadMaxCellsPerFrame ? cells.Count : m_NumberOfCellsLoadedPerFrame;

        int m_NumberOfCellsBlendedPerFrame = 10000;
        /// <summary>Maximum number of cells that are blended per frame.</summary>
        public int numberOfCellsBlendedPerFrame
        {
            get => m_NumberOfCellsBlendedPerFrame;
            set => m_NumberOfCellsBlendedPerFrame = Mathf.Max(1, value);
        }

        float m_TurnoverRate = 0.1f;
        /// <summary>Percentage of cells loaded in the blending pool that can be replaced by out of date cells.</summary>
        public float turnoverRate
        {
            get => m_TurnoverRate;
            set => m_TurnoverRate = Mathf.Clamp01(value);
        }

        DynamicArray<Cell> m_LoadedCells = new(); // List of currently loaded cells.
        DynamicArray<Cell> m_ToBeLoadedCells = new(); // List of currently unloaded cells.
        DynamicArray<Cell> m_WorseLoadedCells = new(); // Reduced list (N cells are processed per frame) of worse loaded cells.
        DynamicArray<Cell> m_BestToBeLoadedCells = new(); // Reduced list (N cells are processed per frame) of best unloaded cells.
        DynamicArray<Cell> m_TempCellToLoadList = new(); // Temp list of cells loaded during this frame.
        DynamicArray<Cell> m_TempCellToUnloadList = new(); // Temp list of cells unloaded during this frame.

        DynamicArray<Cell> m_LoadedBlendingCells = new();
        DynamicArray<Cell> m_ToBeLoadedBlendingCells = new();
        DynamicArray<Cell> m_TempBlendingCellToLoadList = new();
        DynamicArray<Cell> m_TempBlendingCellToUnloadList = new();

        Vector3 m_FrozenCameraPosition;
        Vector3 m_FrozenCameraDirection;

        const float kIndexFragmentationThreshold = 0.2f;
        bool m_IndexDefragmentationInProgress;
        ProbeBrickIndex m_DefragIndex;
        ProbeGlobalIndirection m_DefragCellIndices;
        DynamicArray<Cell> m_IndexDefragCells = new DynamicArray<Cell>();
        DynamicArray<Cell> m_TempIndexDefragCells = new DynamicArray<Cell>();

        internal float minStreamingScore;
        internal float maxStreamingScore;

        // Requests waiting to be run. Needed to preserve order of requests.
        Queue<CellStreamingRequest> m_StreamingQueue = new Queue<CellStreamingRequest>();
        // List of active requests. Needed to query the result every frame.
        List<CellStreamingRequest> m_ActiveStreamingRequests = new List<CellStreamingRequest>();
        ObjectPool<CellStreamingRequest> m_StreamingRequestsPool = new ObjectPool<CellStreamingRequest>(null, (val) => val.Clear());
        bool m_DiskStreamingUseCompute = false;
        ProbeVolumeScratchBufferPool m_ScratchBufferPool;

        CellStreamingRequest.OnStreamingCompleteDelegate m_OnStreamingComplete;
        CellStreamingRequest.OnStreamingCompleteDelegate m_OnBlendingStreamingComplete;

        void InitStreaming()
        {
            m_OnStreamingComplete = OnStreamingComplete;
            m_OnBlendingStreamingComplete = OnBlendingStreamingComplete;
        }

        void CleanupStreaming()
        {
            // Releases all active and pending canceled requests.
            ProcessNewRequests();
            UpdateActiveRequests(null);

            Debug.Assert(m_StreamingQueue.Count == 0);
            Debug.Assert(m_ActiveStreamingRequests.Count == 0);
            Debug.Assert(m_StreamingRequestsPool.countAll == m_StreamingRequestsPool.countInactive); // Everything should have been released.

            for (int i = 0; i < m_StreamingRequestsPool.countAll; ++i)
            {
                var request = m_StreamingRequestsPool.Get();
                request.Dispose();
            }

            if (m_ScratchBufferPool != null)
            {
                m_ScratchBufferPool.Cleanup();
                m_ScratchBufferPool = null;
            }

            m_StreamingRequestsPool = new ObjectPool<CellStreamingRequest>((val) => val.Clear(), null);
            m_ActiveStreamingRequests.Clear();
            m_StreamingQueue.Clear();

            m_OnStreamingComplete = null;
            m_OnBlendingStreamingComplete = null;
        }

        internal void ScenarioBlendingChanged(bool scenarioChanged)
        {
            if (scenarioChanged)
            {
                UnloadAllBlendingCells();
                for (int i = 0; i < m_ToBeLoadedBlendingCells.size; ++i)
                    m_ToBeLoadedBlendingCells[i].blendingInfo.ForceReupload();
            }
        }

        static void ComputeCellStreamingScore(Cell cell, Vector3 cameraPosition, Vector3 cameraDirection)
        {
            var cellPosition = cell.desc.position;
            var cameraToCell = (cellPosition - cameraPosition).normalized;
            cell.streamingInfo.streamingScore = Vector3.Distance(cameraPosition, cell.desc.position);
            // This should give more weight to cells in front of the camera.
            cell.streamingInfo.streamingScore *= (2.0f - Vector3.Dot(cameraDirection, cameraToCell));
        }

        void ComputeStreamingScore(Vector3 cameraPosition, Vector3 cameraDirection, DynamicArray<Cell> cells)
        {
            for (int i = 0; i < cells.size; ++i)
            {
                ComputeCellStreamingScore(cells[i], cameraPosition, cameraDirection);
            }
        }

        void ComputeBestToBeLoadedCells(Vector3 cameraPosition, Vector3 cameraDirection)
        {
            m_BestToBeLoadedCells.Clear();
            m_BestToBeLoadedCells.Reserve(m_ToBeLoadedCells.size); // Pre-reserve to avoid Insert allocating every time.

            foreach (var cell in m_ToBeLoadedCells)
            {
                ComputeCellStreamingScore(cell, cameraPosition, cameraDirection);

                // We need to compute min/max streaming scores here since we don't have the full sorted list anymore (which is used in ComputeMinMaxStreamingScore)
                minStreamingScore = Mathf.Min(minStreamingScore, cell.streamingInfo.streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, cell.streamingInfo.streamingScore);

                int currentBestCellsSize = System.Math.Min(m_BestToBeLoadedCells.size, numberOfCellsLoadedPerFrame);
                int index;
                for (index = 0; index < currentBestCellsSize; ++index)
                {
                    if (cell.streamingInfo.streamingScore < m_BestToBeLoadedCells[index].streamingInfo.streamingScore)
                        break;
                }

                if (index < numberOfCellsLoadedPerFrame)
                    m_BestToBeLoadedCells.Insert(index, cell);

                // Avoids too many copies when Inserting new elements.
                if (m_BestToBeLoadedCells.size > numberOfCellsLoadedPerFrame)
                    m_BestToBeLoadedCells.Resize(numberOfCellsLoadedPerFrame);
            }
        }

        void ComputeStreamingScoreAndWorseLoadedCells(Vector3 cameraPosition, Vector3 cameraDirection)
        {
            m_WorseLoadedCells.Clear();
            m_WorseLoadedCells.Reserve(m_LoadedCells.size); // Pre-reserve to avoid Insert allocating every time.

            int requiredSHChunks = 0;
            int requiredIndexChunks = 0;
            foreach(var cell in m_BestToBeLoadedCells)
            {
                requiredSHChunks += cell.desc.shChunkCount;
                requiredIndexChunks += cell.desc.indexChunkCount;
            }

            foreach (var cell in m_LoadedCells)
            {
                ComputeCellStreamingScore(cell, cameraPosition, cameraDirection);

                // We need to compute min/max streaming scores here since we don't have the full sorted list anymore (which is used in ComputeMinMaxStreamingScore)
                minStreamingScore = Mathf.Min(minStreamingScore, cell.streamingInfo.streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, cell.streamingInfo.streamingScore);

                int currentWorseSize = m_WorseLoadedCells.size;
                int index;
                for (index = 0; index < currentWorseSize; ++index)
                {
                    if (cell.streamingInfo.streamingScore > m_WorseLoadedCells[index].streamingInfo.streamingScore)
                        break;
                }

                m_WorseLoadedCells.Insert(index, cell);

                // Compute the chunk counts of the current worse cells.
                int currentSHChunks = 0;
                int currentIndexChunks = 0;
                int newSize = 0;
                for (int i = 0; i < m_WorseLoadedCells.size; ++i)
                {
                    var worseCell = m_WorseLoadedCells[i];
                    currentSHChunks += worseCell.desc.shChunkCount;
                    currentIndexChunks += worseCell.desc.indexChunkCount;

                    if (currentSHChunks >= requiredSHChunks && currentIndexChunks >= requiredIndexChunks)
                    {
                        newSize = i + 1;
                        break;
                    }
                }

                // Now we resize to keep just enough worse cells that represent enough room to load the required cell.
                // This allows insertions to be cheaper.
                if (newSize != 0)
                    m_WorseLoadedCells.Resize(newSize);
            }
        }

        void ComputeBlendingScore(DynamicArray<Cell> cells, float worstScore)
        {
            float factor = scenarioBlendingFactor;
            for (int i = 0; i < cells.size; ++i)
            {
                var cell = cells[i];
                var blendingInfo = cell.blendingInfo;
                if (factor != blendingInfo.blendingFactor)
                {
                    blendingInfo.blendingScore = cell.streamingInfo.streamingScore;
                    if (blendingInfo.ShouldPrioritize())
                        blendingInfo.blendingScore -= worstScore;
                }
            }
        }

        bool TryLoadCell(Cell cell, ref int shBudget, ref int indexBudget, DynamicArray<Cell> loadedCells)
        {
            // Are we within budget?
            if (cell.poolInfo.shChunkCount <= shBudget && cell.indexInfo.indexChunkCount <= indexBudget)
            {
                // This can still fail because of fragmentation.
                if (LoadCell(cell, ignoreErrorLog: true))
                {
                    loadedCells.Add(cell);

                    shBudget -= cell.poolInfo.shChunkCount;
                    indexBudget -= cell.indexInfo.indexChunkCount;
                    return true;
                }
            }
            return false;
        }

        void UnloadBlendingCell(Cell cell, DynamicArray<Cell> unloadedCells)
        {
            UnloadBlendingCell(cell);

            unloadedCells.Add(cell);
        }

        bool TryLoadBlendingCell(Cell cell, DynamicArray<Cell> loadedCells)
        {
            if (!cell.UpdateCellScenarioData(lightingScenario, m_CurrentBakingSet.otherScenario))
                return false;

            if (!AddBlendingBricks(cell))
                return false;

            loadedCells.Add(cell);

            return true;
        }

        void ComputeMinMaxStreamingScore()
        {
            minStreamingScore = float.MaxValue;
            maxStreamingScore = float.MinValue;

            if (m_ToBeLoadedCells.size != 0)
            {
                minStreamingScore = Mathf.Min(minStreamingScore, m_ToBeLoadedCells[0].streamingInfo.streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, m_ToBeLoadedCells[m_ToBeLoadedCells.size - 1].streamingInfo.streamingScore);
            }

            if (m_LoadedCells.size != 0)
            {
                minStreamingScore = Mathf.Min(minStreamingScore, m_LoadedCells[0].streamingInfo.streamingScore);
                maxStreamingScore = Mathf.Max(maxStreamingScore, m_LoadedCells[m_LoadedCells.size - 1].streamingInfo.streamingScore);
            }
        }

        /// <summary>
        /// Updates the cell streaming for a <see cref="Camera"/>
        /// </summary>
        /// <param name="cmd">The <see cref="CommandBuffer"/></param>
        /// <param name="camera">The <see cref="Camera"/></param>
        public void UpdateCellStreaming(CommandBuffer cmd, Camera camera)
        {
            UpdateCellStreaming(cmd, camera, null);
        }

        /// <summary>
        /// Updates the cell streaming for a <see cref="Camera"/>
        /// </summary>
        /// <param name="cmd">The <see cref="CommandBuffer"/></param>
        /// <param name="camera">The <see cref="Camera"/></param>
        /// <param name="options">Options coming from the volume stack.</param>
        public void UpdateCellStreaming(CommandBuffer cmd, Camera camera, ProbeVolumesOptions options)
        {
            if (!isInitialized || m_CurrentBakingSet == null) return;

            using (new ProfilingScope(ProfilingSampler.Get(CoreProfileId.APVCellStreamingUpdate)))
            {
                var cameraPosition = camera.transform.position;
                if (!probeVolumeDebug.freezeStreaming)
                {
                    m_FrozenCameraPosition = cameraPosition;
                    m_FrozenCameraDirection = camera.transform.forward;
                }

                // Cell position in cell space is the top left corner. So we need to shift the camera position by half a cell to make things comparable.
                var offset = ProbeOffset() + (options != null ? options.worldOffset.value : Vector3.zero);
                var cameraPositionCellSpace = (m_FrozenCameraPosition - offset) / MaxBrickSize() - Vector3.one * 0.5f;

                DynamicArray<Cell> bestUnloadedCells;
                DynamicArray<Cell> worseLoadedCells;

                // When in this mode, we just sort through all loaded/ToBeLoaded cells in order to figure out worse/best cells to process.
                // This is slow so only recommended in the editor.
                if (m_LoadMaxCellsPerFrame)
                {
                    ComputeStreamingScore(cameraPositionCellSpace, m_FrozenCameraDirection, m_ToBeLoadedCells);
                    m_ToBeLoadedCells.QuickSort();
                    bestUnloadedCells = m_ToBeLoadedCells;
                }
                // Otherwise, when we only process a handful of cells per frame, we'll linearly go through the lists to determine two things:
                // - The list of best cells to load.
                // - The list of worse cells to load. This list can be bigger than the previous one since cells have different sizes so we may need to evict more to make room.
                // This allows us to not sort through all the cells every frame which is very slow. Instead we just output very small lists that we then process.
                else
                {
                    minStreamingScore = float.MaxValue;
                    maxStreamingScore = float.MinValue;

                    ComputeBestToBeLoadedCells(cameraPositionCellSpace, m_FrozenCameraDirection);
                    bestUnloadedCells = m_BestToBeLoadedCells;
                }

                // This is only a rough budget estimate at first.
                // It doesn't account for fragmentation.
                int indexChunkBudget = m_Index.GetRemainingChunkCount();
                int shChunkBudget = m_Pool.GetRemainingChunkCount();
                int cellCountToLoad = Mathf.Min(numberOfCellsLoadedPerFrame, bestUnloadedCells.size);

                bool didRecomputeScoresForLoadedCells = false;
                if (m_SupportGPUStreaming)
                {
                    if (m_IndexDefragmentationInProgress)
                    {
                        UpdateIndexDefragmentation();
                    }
                    else
                    {
                        bool needComputeFragmentation = false;

                        while (m_TempCellToLoadList.size < cellCountToLoad)
                        {
                            // Enough memory, we can safely load the cell.
                            var cellInfo = bestUnloadedCells[m_TempCellToLoadList.size];
                            if (!TryLoadCell(cellInfo, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList))
                                break;
                        }

                        // Budget reached. We need to figure out if we can safely unload other cells to make room.
                        // If defrag was triggered by TryLoadCell we should not try to load further cells either.
                        if (m_TempCellToLoadList.size != cellCountToLoad && !m_IndexDefragmentationInProgress)
                        {
                            // We need to unload cells so we have to compute the worse loaded cells now (not earlier as it would be useless)
                            if (m_LoadMaxCellsPerFrame)
                            {
                                ComputeStreamingScore(cameraPositionCellSpace, m_FrozenCameraDirection, m_LoadedCells);
                                m_LoadedCells.QuickSort();
                                worseLoadedCells = m_LoadedCells;
                            }
                            else
                            {
                                ComputeStreamingScoreAndWorseLoadedCells(cameraPositionCellSpace, m_FrozenCameraDirection);
                                worseLoadedCells = m_WorseLoadedCells;
                            }
                            didRecomputeScoresForLoadedCells = true;

                            int pendingUnloadCount = 0;
                            while (m_TempCellToLoadList.size < cellCountToLoad)
                            {
                                // No more cells to unload.
                                if (worseLoadedCells.size - pendingUnloadCount == 0)
                                    break;

                                // List are stored in reverse order depending on the mode.
                                // TODO make the full List be sorted the same way as partial list.
                                int worseCellIndex = m_LoadMaxCellsPerFrame ? worseLoadedCells.size - pendingUnloadCount - 1 : pendingUnloadCount;
                                var worseLoadedCell = worseLoadedCells[worseCellIndex];
                                var bestUnloadedCell = bestUnloadedCells[m_TempCellToLoadList.size];

                                // We are in a "stable" state, all the closest cells are loaded within the budget.
                                if (worseLoadedCell.streamingInfo.streamingScore <= bestUnloadedCell.streamingInfo.streamingScore)
                                    break;

                                // The worse loaded cell is further than the best unloaded cell, we can unload it.
                                while (pendingUnloadCount < worseLoadedCells.size && worseLoadedCell.streamingInfo.streamingScore > bestUnloadedCell.streamingInfo.streamingScore && (shChunkBudget < bestUnloadedCell.desc.shChunkCount || indexChunkBudget < bestUnloadedCell.desc.indexChunkCount))
                                {
                                    if (probeVolumeDebug.verboseStreamingLog)
                                        LogStreaming($"Unloading cell {worseLoadedCell.desc.index}");

                                    pendingUnloadCount++;
                                    UnloadCell(worseLoadedCell);
                                    shChunkBudget += worseLoadedCell.desc.shChunkCount;
                                    indexChunkBudget += worseLoadedCell.desc.indexChunkCount;

                                    m_TempCellToUnloadList.Add(worseLoadedCell);

                                    worseCellIndex = m_LoadMaxCellsPerFrame ? worseLoadedCells.size - pendingUnloadCount - 1 : pendingUnloadCount;
                                    if (pendingUnloadCount < worseLoadedCells.size)
                                        worseLoadedCell = worseLoadedCells[worseCellIndex];
                                }

                                // We unloaded enough space (not taking fragmentation into account)
                                if (shChunkBudget >= bestUnloadedCell.desc.shChunkCount && indexChunkBudget >= bestUnloadedCell.desc.indexChunkCount)
                                {
                                    if (!TryLoadCell(bestUnloadedCell, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList))
                                    {
                                        needComputeFragmentation = true;
                                        break; // Alloc failed because of fragmentation, stop trying to load cells.
                                    }
                                }
                            }
                        }

                        if (needComputeFragmentation)
                            m_Index.ComputeFragmentationRate();

                        if (m_Index.fragmentationRate >= kIndexFragmentationThreshold)
                            StartIndexDefragmentation();
                    }
                }
                else
                {
                    for (int i = 0; i < cellCountToLoad; ++i)
                    {
                        var cellInfo = m_ToBeLoadedCells[m_TempCellToLoadList.size]; // m_TempCellToLoadList.size get incremented in TryLoadCell
                        if (!TryLoadCell(cellInfo, ref shChunkBudget, ref indexChunkBudget, m_TempCellToLoadList))
                        {
                            if (i > 0) // Only warn once
                            {
                                Debug.LogWarning("Max Memory Budget for Adaptive Probe Volumes has been reached, but there is still more data to load. Consider either increasing the Memory Budget, enabling GPU Streaming, or reducing the probe count.");
                            }
                            break;
                        }
                    }
                }

                // If we intend to blend scenarios, compute the streaming scores for the already loaded cells.
                // These will be used to determine which of the loaded cells to perform blending on first.
                // We only need to do this if we didn't already do it above.
                if (!didRecomputeScoresForLoadedCells && supportScenarioBlending)
                {
                    ComputeStreamingScore(cameraPositionCellSpace, m_FrozenCameraDirection, m_LoadedCells);
                }

                if (m_LoadMaxCellsPerFrame)
                    ComputeMinMaxStreamingScore();

                // Update internal load/toBeLoaded lists.

                // Move the successfully loaded cells to the "loaded cells" list.
                foreach (var cell in m_TempCellToLoadList)
                    m_ToBeLoadedCells.Remove(cell);
                m_LoadedCells.AddRange(m_TempCellToLoadList);
                // Move the unloaded cells to the list of cells to be loaded.
                if (m_TempCellToUnloadList.size > 0)
                {
                    foreach (var cell in m_TempCellToUnloadList)
                        m_LoadedCells.Remove(cell);

                    ComputeCellGlobalInfo();
                }
                m_ToBeLoadedCells.AddRange(m_TempCellToUnloadList);
                // Clear temp lists.
                m_TempCellToLoadList.Clear();
                m_TempCellToUnloadList.Clear();

                UpdateDiskStreaming(cmd);
            }

            // Handle cell streaming for blending
            if (supportScenarioBlending)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(CoreProfileId.APVScenarioBlendingUpdate)))
                    UpdateBlendingCellStreaming(cmd);
            }
        }

        int FindWorstBlendingCellToBeLoaded()
        {
            int idx = -1;
            float worstBlending = -1;
            float factor = scenarioBlendingFactor;
            for (int i = m_TempBlendingCellToLoadList.size; i < m_ToBeLoadedBlendingCells.size; ++i)
            {
                float score = Mathf.Abs(m_ToBeLoadedBlendingCells[i].blendingInfo.blendingFactor - factor);
                if (score > worstBlending)
                {
                    idx = i;
                    if (m_ToBeLoadedBlendingCells[i].blendingInfo.ShouldReupload()) // We are not gonna find worse than that
                        break;
                    worstBlending = score;
                }
            }
            return idx;
        }

        static int BlendingComparer(Cell a, Cell b)
        {
            if (a.blendingInfo.blendingScore < b.blendingInfo.blendingScore)
                return -1;
            else if (a.blendingInfo.blendingScore > b.blendingInfo.blendingScore)
                return 1;
            else
                return 0;
        }

        static DynamicArray<Cell>.SortComparer s_BlendingComparer = BlendingComparer;

        void UpdateBlendingCellStreaming(CommandBuffer cmd)
        {
            // Compute the worst score to offset score of cells to prioritize
            float worstLoaded = m_LoadedCells.size != 0 ? m_LoadedCells[m_LoadedCells.size - 1].streamingInfo.streamingScore : 0.0f;
            float worstToBeLoaded = m_ToBeLoadedCells.size != 0 ? m_ToBeLoadedCells[m_ToBeLoadedCells.size - 1].streamingInfo.streamingScore : 0.0f;
            float worstScore = Mathf.Max(worstLoaded, worstToBeLoaded);

            ComputeBlendingScore(m_ToBeLoadedBlendingCells, worstScore);
            ComputeBlendingScore(m_LoadedBlendingCells, worstScore);

            m_ToBeLoadedBlendingCells.QuickSort(s_BlendingComparer);
            m_LoadedBlendingCells.QuickSort(s_BlendingComparer);

            int cellCountToLoad = Mathf.Min(numberOfCellsLoadedPerFrame, m_ToBeLoadedBlendingCells.size);
            while (m_TempBlendingCellToLoadList.size < cellCountToLoad)
            {
                var blendingCell = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size];
                if (!TryLoadBlendingCell(blendingCell, m_TempBlendingCellToLoadList))
                    break;
            }

            // Budget reached
            if (m_TempBlendingCellToLoadList.size != cellCountToLoad)
            {
                // Turnover allows a percentage of the pool to be replaced by cells with a lower streaming score
                // once the system is in a stable state. This ensures all cells get updated regularly.
                int turnoverOffset = -1;
                int idx = (int)(m_LoadedBlendingCells.size * (1.0f - turnoverRate));
                var worstNoTurnover = idx < m_LoadedBlendingCells.size ? m_LoadedBlendingCells[idx] : null;

                while (m_TempBlendingCellToLoadList.size < cellCountToLoad)
                {
                    if (m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size == 0) // We unloaded everything
                        break;

                    var worstCellLoaded = m_LoadedBlendingCells[m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size - 1];
                    var bestCellToBeLoaded = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size];

                    // The best cell to be loaded has WORSE score than the worst cell already loaded.
                    // This means all cells waiting to be loaded are worse than the ones we already have - we are in a "stable" state.
                    if (bestCellToBeLoaded.blendingInfo.blendingScore >= (worstNoTurnover ?? worstCellLoaded).blendingInfo.blendingScore)
                    {
                        if (worstNoTurnover == null) // Disable turnover
                            break;

                        // Find worst cell and assume contiguous cells have roughly the same blending factor
                        // (contiguous cells are spatially close by, so it's good anyway to update them together)
                        if (turnoverOffset == -1)
                            turnoverOffset = FindWorstBlendingCellToBeLoaded();

                        bestCellToBeLoaded = m_ToBeLoadedBlendingCells[turnoverOffset];
                        if (bestCellToBeLoaded.blendingInfo.IsUpToDate()) // Every single cell is blended :)
                            break;
                    }

                    // If we encounter a cell that is still being streamed in (and thus hasn't had a chance to be blended yet), bail
                    // we don't want to keep unloading cells before they get blended, or we will never get any work done.
                    // This branch is only ever true when disk streaming is being used.
                    if (worstCellLoaded.streamingInfo.IsBlendingStreaming())
                        break;

                    UnloadBlendingCell(worstCellLoaded, m_TempBlendingCellToUnloadList);

                    if (probeVolumeDebug.verboseStreamingLog)
                        LogStreaming($"Unloading blending cell {worstCellLoaded.desc.index}");

                    bool loadOk = TryLoadBlendingCell(bestCellToBeLoaded, m_TempBlendingCellToLoadList);

                    // Handle turnover. Loading can still fail cause all cells don't have the same chunk count.
                    if (loadOk && turnoverOffset != -1)
                    {
                        // swap to ensure loaded cells are at the start of m_ToBeLoadedBlendingCells
                        m_ToBeLoadedBlendingCells[turnoverOffset] = m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size-1];
                        m_ToBeLoadedBlendingCells[m_TempBlendingCellToLoadList.size-1] = bestCellToBeLoaded;
                        if (++turnoverOffset >= m_ToBeLoadedBlendingCells.size)
                            turnoverOffset = m_TempBlendingCellToLoadList.size;
                    }
                }

                m_LoadedBlendingCells.RemoveRange(m_LoadedBlendingCells.size - m_TempBlendingCellToUnloadList.size, m_TempBlendingCellToUnloadList.size);
            }

            m_ToBeLoadedBlendingCells.RemoveRange(0, m_TempBlendingCellToLoadList.size);
            m_LoadedBlendingCells.AddRange(m_TempBlendingCellToLoadList);
            m_TempBlendingCellToLoadList.Clear();
            m_ToBeLoadedBlendingCells.AddRange(m_TempBlendingCellToUnloadList);
            m_TempBlendingCellToUnloadList.Clear();

            // Kick off blending.
            if (m_LoadedBlendingCells.size != 0)
            {
                float factor = scenarioBlendingFactor;

                int loadedBlendingCellIndex = 0;
                int blendedCellCount = 0;
                while (blendedCellCount < numberOfCellsBlendedPerFrame && loadedBlendingCellIndex < m_LoadedBlendingCells.size)
                {
                    var blendingCell = m_LoadedBlendingCells[loadedBlendingCellIndex++];
                    if (!blendingCell.streamingInfo.IsBlendingStreaming() && !blendingCell.blendingInfo.IsUpToDate())
                    {
                        if (probeVolumeDebug.verboseStreamingLog)
                            LogStreaming($"Blending cell {blendingCell.desc.index} ({factor})");

                        blendingCell.blendingInfo.blendingFactor = factor;
                        blendingCell.blendingInfo.MarkUpToDate();
                        m_BlendingPool.BlendChunks(blendingCell, m_Pool);
                        blendedCellCount++;
                    }
                }

                m_BlendingPool.PerformBlending(cmd, factor, m_Pool);
            }
        }

        static int DefragComparer(Cell a, Cell b)
        {
            if (a.indexInfo.updateInfo.GetNumberOfChunks() > b.indexInfo.updateInfo.GetNumberOfChunks())
                return 1;
            else if (a.indexInfo.updateInfo.GetNumberOfChunks() < b.indexInfo.updateInfo.GetNumberOfChunks())
                return -1;
            else return 0;
        }

        static DynamicArray<Cell>.SortComparer s_DefragComparer = DefragComparer;

        void StartIndexDefragmentation()
        {
            // We can end up here during baking (dilation) when trying to load all cells even without supporting GPU streaming.
            if (!m_SupportGPUStreaming)
                return;

            m_IndexDefragmentationInProgress = true;

            // Prepare the list of cells.
            // We want to relocate cells with more indices first.
            m_IndexDefragCells.Clear();
            m_IndexDefragCells.AddRange(m_LoadedCells);
            m_IndexDefragCells.QuickSort(s_DefragComparer);

            m_DefragIndex.Clear();
        }

        void UpdateIndexDefragmentation()
        {
            using (new ProfilingScope(ProfilingSampler.Get(CoreProfileId.APVIndexDefragUpdate)))
            {
                m_TempIndexDefragCells.Clear();

                int numberOfCellsToProcess = Mathf.Min(m_IndexDefragCells.size, numberOfCellsLoadedPerFrame);
                int i = 0;
                int processedCells = 0;
                while(i < m_IndexDefragCells.size && processedCells < numberOfCellsToProcess)
                {
                    var cell = m_IndexDefragCells[m_IndexDefragCells.size - i - 1];

                    m_DefragIndex.FindSlotsForEntries(ref cell.indexInfo.updateInfo.entriesInfo);
                    m_DefragIndex.ReserveChunks(cell.indexInfo.updateInfo.entriesInfo, false);

                    // Index of cells being streamed is not up to date yet so we can't defrag this cell.
                    if (!(cell.streamingInfo.IsStreaming() || cell.streamingInfo.IsBlendingStreaming()))
                    {
                        // Update index and indirection
                        m_DefragIndex.AddBricks(cell.indexInfo, cell.data.bricks, cell.poolInfo.chunkList, ProbeBrickPool.GetChunkSizeInBrickCount(), m_Pool.GetPoolWidth(), m_Pool.GetPoolHeight());
                        m_DefragCellIndices.UpdateCell(cell.indexInfo);
                        processedCells++;
                    }
                    else
                    {
                        m_TempIndexDefragCells.Add(cell);
                    }

                    i++;
                }

                // Remove processed cells from the list.
                // For faster removal, just resize by removing all processed cells and add back those that were streaming.
                m_IndexDefragCells.Resize(m_IndexDefragCells.size - i);
                m_IndexDefragCells.AddRange(m_TempIndexDefragCells);

                if (m_IndexDefragCells.size == 0)
                {
                    // Swap index buffers
                    var oldDefragIndex = m_DefragIndex;
                    m_DefragIndex = m_Index;
                    m_Index = oldDefragIndex;

                    var oldDefragCellIndices = m_DefragCellIndices;
                    m_DefragCellIndices = m_CellIndices;
                    m_CellIndices = oldDefragCellIndices;

                    // Resume streaming
                    m_IndexDefragmentationInProgress = false;
                }
            }
        }

        void OnStreamingComplete(CellStreamingRequest request, CommandBuffer cmd)
        {
            request.cell.streamingInfo.request = null;
            UpdatePoolAndIndex(request.cell, request.scratchBuffer, request.scratchBufferLayout, request.poolIndex, cmd);
        }

        void OnBlendingStreamingComplete(CellStreamingRequest request, CommandBuffer cmd)
        {
            UpdatePool(cmd, request.cell.blendingInfo.chunkList, request.scratchBuffer, request.scratchBufferLayout, request.poolIndex);

            if (request.poolIndex == 0)
                request.cell.streamingInfo.blendingRequest0 = null;
            else
                request.cell.streamingInfo.blendingRequest1 = null;

            // Streaming of both scenario is over, we can update the index and start blending.
            if (request.cell.streamingInfo.blendingRequest0 == null && request.cell.streamingInfo.blendingRequest1 == null && !request.cell.indexInfo.indexUpdated)
                UpdateCellIndex(request.cell);
        }

        void PushDiskStreamingRequest(Cell cell, string scenario, int poolIndex, CellStreamingRequest.OnStreamingCompleteDelegate onStreamingComplete)
        {
            var streamingRequest = m_StreamingRequestsPool.Get();
            streamingRequest.cell = cell;
            streamingRequest.state = CellStreamingRequest.State.Pending;
            streamingRequest.scenarioData = m_CurrentBakingSet.scenarios[scenario];
            streamingRequest.poolIndex = poolIndex;
            streamingRequest.onStreamingComplete = onStreamingComplete;

            // Only stream shared data for a regular streaming request (index -1 : no streaming)
            // or the first scenario of the two blending scenarios (index 0)
            if (poolIndex == -1 || poolIndex == 0)
                streamingRequest.streamSharedData = true;

            if (probeVolumeDebug.verboseStreamingLog)
            {
                if (poolIndex == -1)
                    LogStreaming($"Push streaming request for cell {cell.desc.index}.");
                else
                    LogStreaming($"Push streaming request for blending cell {cell.desc.index}.");
            }

            switch (poolIndex)
            {
                case -1:
                    cell.streamingInfo.request = streamingRequest;
                    break;
                case 0:
                    cell.streamingInfo.blendingRequest0 = streamingRequest;
                    break;
                case 1:
                    cell.streamingInfo.blendingRequest1 = streamingRequest;
                    break;
            }

            // Enqueue request.
            m_StreamingQueue.Enqueue(streamingRequest);
        }

        void CancelStreamingRequest(Cell cell)
        {
            m_Index.RemoveBricks(cell.indexInfo);
            m_Pool.Deallocate(cell.poolInfo.chunkList);

            if (cell.streamingInfo.request != null)
                cell.streamingInfo.request.Cancel();
        }

        void CancelBlendingStreamingRequest(Cell cell)
        {
            if (cell.streamingInfo.blendingRequest0 != null)
                cell.streamingInfo.blendingRequest0.Cancel();
            if (cell.streamingInfo.blendingRequest1 != null)
                cell.streamingInfo.blendingRequest1.Cancel();
        }

        unsafe bool ProcessDiskStreamingRequest(CellStreamingRequest request)
        {
            var cellIndex = request.cell.desc.index;
            var cell = cells[cellIndex];
            var cellDesc = cell.desc;
            var cellData = cell.data;

            if (!m_ScratchBufferPool.AllocateScratchBuffer(cellDesc.shChunkCount, out var cellStreamingScratchBuffer, out var layout, m_DiskStreamingUseCompute))
                return false;

            if (!m_CurrentBakingSet.HasValidSharedData())
            {
                Debug.LogError($"One or more data file missing for baking set {m_CurrentBakingSet.name}. Cannot load shared data.");
                return false;
            }

            if (!request.scenarioData.HasValidData(m_SHBands))
            {
                Debug.LogError($"One or more data file missing for baking set {m_CurrentBakingSet.name} scenario {lightingScenario}. Cannot load scenario data.");
                return false;
            }

            if (probeVolumeDebug.verboseStreamingLog)
            {
                if (request.poolIndex == -1)
                    LogStreaming($"Running disk streaming request for cell {cellDesc.index} ({cellDesc.shChunkCount} chunks)");
                else
                    LogStreaming($"Running disk streaming request for cell {cellDesc.index} ({cellDesc.shChunkCount} chunks) for scenario {request.poolIndex}");
            }

            // Note: We allocate new NativeArrays here.
            // This will not generate GCAlloc since NativeArrays are value types but it will allocate on the native side.
            // This is probably ok as the frequency should be pretty low but we need to keep an eye on this.

            // GPU Data
            request.scratchBuffer = cellStreamingScratchBuffer;
            request.scratchBufferLayout = layout;
            request.bytesWritten = 0;

            var mappedBuffer = request.scratchBuffer.stagingBuffer;

            var mappedBufferBaseAddr = (byte*)mappedBuffer.GetUnsafePtr();
            var mappedBufferAddr = mappedBufferBaseAddr;

            // Write destination chunk coordinates for SH data
            var destChunkAddr = (uint*)mappedBufferAddr;
            // Pool -1 is regular pool and 0/1 are blending pools.
            var destChunks = request.poolIndex == -1 ? request.cell.poolInfo.chunkList : request.cell.blendingInfo.chunkList;
            var destChunkCount = destChunks.Count;
            for (int i = 0; i < destChunkCount ; ++i)
            {
                var destChunk = destChunks[i];
                destChunkAddr[i * 4] = (uint)destChunk.x;
                destChunkAddr[i * 4 + 1] = (uint)destChunk.y;
                destChunkAddr[i * 4 + 2] = (uint)destChunk.z;
                destChunkAddr[i * 4 + 3] = 0;
            }
            mappedBufferAddr += (destChunkCount * sizeof(uint) * 4);

            // Write destination chunk coordinates for Shared data (always in main pool)
            destChunkAddr = (uint*)mappedBufferAddr;
            destChunks = request.cell.poolInfo.chunkList;
            Debug.Assert(destChunks.Count == destChunkCount);
            for (int i = 0; i < destChunkCount; ++i)
            {
                var destChunk = destChunks[i];
                destChunkAddr[i * 4] = (uint)destChunk.x;
                destChunkAddr[i * 4 + 1] = (uint)destChunk.y;
                destChunkAddr[i * 4 + 2] = (uint)destChunk.z;
                destChunkAddr[i * 4 + 3] = 0;
            }
            mappedBufferAddr += (destChunkCount * sizeof(uint) * 4);

            var shL0L1DataAsset = request.scenarioData.cellDataAsset;
            var cellStreamingDesc = shL0L1DataAsset.streamableCellDescs[cellIndex];
            var chunkCount = cellDesc.shChunkCount;
            var L0L1Size = m_CurrentBakingSet.L0ChunkSize * chunkCount;
            var L1Size = m_CurrentBakingSet.L1ChunkSize * chunkCount;

            var L0L1ReadSize = L0L1Size + 2 * L1Size;
            request.cellDataStreamingRequest.AddReadCommand(cellStreamingDesc.offset, L0L1ReadSize, mappedBufferAddr);
            mappedBufferAddr += L0L1ReadSize;
            request.bytesWritten += request.cellDataStreamingRequest.RunCommands(shL0L1DataAsset.OpenFile());

            if (request.streamSharedData)
            {
                var sharedDataAsset = m_CurrentBakingSet.cellSharedDataAsset;
                cellStreamingDesc = sharedDataAsset.streamableCellDescs[cellIndex];
                var sharedChunkSize = m_CurrentBakingSet.sharedDataChunkSize;

                request.cellSharedDataStreamingRequest.AddReadCommand(cellStreamingDesc.offset, sharedChunkSize * chunkCount, mappedBufferAddr);
                mappedBufferAddr += (sharedChunkSize * chunkCount);
                request.bytesWritten += request.cellSharedDataStreamingRequest.RunCommands(sharedDataAsset.OpenFile());
            }

            if (m_SHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                var optionalDataAsset = request.scenarioData.cellOptionalDataAsset;
                cellStreamingDesc = optionalDataAsset.streamableCellDescs[cellIndex];
                var L2ReadSize = m_CurrentBakingSet.L2TextureChunkSize * chunkCount * 4; // 4 textures
                request.cellOptionalDataStreamingRequest.AddReadCommand(cellStreamingDesc.offset, L2ReadSize, mappedBufferAddr);
                mappedBufferAddr += L2ReadSize;
                request.bytesWritten += request.cellOptionalDataStreamingRequest.RunCommands(optionalDataAsset.OpenFile());
            }

            if (m_CurrentBakingSet.bakedProbeOcclusion)
            {
                var probeOcclusionDataAsset = request.scenarioData.cellProbeOcclusionDataAsset;
                cellStreamingDesc = probeOcclusionDataAsset.streamableCellDescs[cellIndex];
                var probeOcclusionReadSize = m_CurrentBakingSet.ProbeOcclusionChunkSize * chunkCount;
                request.cellProbeOcclusionDataStreamingRequest.AddReadCommand(cellStreamingDesc.offset, probeOcclusionReadSize, mappedBufferAddr);
                mappedBufferAddr += probeOcclusionReadSize;
                request.bytesWritten += request.cellProbeOcclusionDataStreamingRequest.RunCommands(probeOcclusionDataAsset.OpenFile());
            }

            // Bricks Data
            cellData.bricks = new NativeArray<ProbeBrickIndex.Brick>(cellDesc.bricksCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            var brickDataAsset = m_CurrentBakingSet.cellBricksDataAsset;
            cellStreamingDesc = brickDataAsset.streamableCellDescs[cellIndex];
            request.brickStreamingRequest.AddReadCommand(cellStreamingDesc.offset, brickDataAsset.elementSize * cellStreamingDesc.elementCount, (byte*)cellData.bricks.GetUnsafePtr());
            request.brickStreamingRequest.RunCommands(brickDataAsset.OpenFile());

            // Support Data
            if (m_CurrentBakingSet.HasSupportData())
            {
                var supportDataAsset = m_CurrentBakingSet.cellSupportDataAsset;
                cellStreamingDesc = supportDataAsset.streamableCellDescs[cellIndex];

                var supportOffset = cellStreamingDesc.offset;
                var positionSize = cellStreamingDesc.elementCount * m_CurrentBakingSet.supportPositionChunkSize;
                var touchupSize = cellStreamingDesc.elementCount * m_CurrentBakingSet.supportTouchupChunkSize;
                var offsetsSize = cellStreamingDesc.elementCount * m_CurrentBakingSet.supportOffsetsChunkSize;
                var layerSize = cellStreamingDesc.elementCount * m_CurrentBakingSet.supportLayerMaskChunkSize;
                var validitySize = cellStreamingDesc.elementCount * m_CurrentBakingSet.supportValidityChunkSize;

                cellData.probePositions = (new NativeArray<byte>(positionSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)).Reinterpret<Vector3>(1);
                cellData.validity = (new NativeArray<byte>(validitySize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)).Reinterpret<float>(1);
                cellData.layer = (new NativeArray<byte>(layerSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)).Reinterpret<byte>(1);
                cellData.touchupVolumeInteraction = (new NativeArray<byte>(touchupSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)).Reinterpret<float>(1);
                cellData.offsetVectors = (new NativeArray<byte>(offsetsSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory)).Reinterpret<Vector3>(1);

                request.supportStreamingRequest.AddReadCommand(supportOffset, positionSize, (byte*)cellData.probePositions.GetUnsafePtr()); supportOffset += positionSize;
                request.supportStreamingRequest.AddReadCommand(supportOffset, validitySize, (byte*)cellData.validity.GetUnsafePtr()); supportOffset += validitySize;
                request.supportStreamingRequest.AddReadCommand(supportOffset, touchupSize, (byte*)cellData.touchupVolumeInteraction.GetUnsafePtr()); supportOffset += touchupSize;
                request.supportStreamingRequest.AddReadCommand(supportOffset, layerSize, (byte*)cellData.layer.GetUnsafePtr()); supportOffset += layerSize;
                request.supportStreamingRequest.AddReadCommand(supportOffset, offsetsSize, (byte*)cellData.offsetVectors.GetUnsafePtr());
                request.supportStreamingRequest.RunCommands(supportDataAsset.OpenFile());
            }

            request.state = CellStreamingRequest.State.Active;
            m_ActiveStreamingRequests.Add(request);

            return true;
        }

        void AllocateScratchBufferPoolIfNeeded()
        {
            if (m_SupportDiskStreaming)
            {
                int shChunkSize = m_CurrentBakingSet.GetChunkGPUMemory(m_SHBands);
                int maxSHChunkCount = m_CurrentBakingSet.maxSHChunkCount;

                Debug.Assert(shChunkSize % 4 == 0);

                // Recreate if chunk size or max count is different.
                if (m_ScratchBufferPool == null || m_ScratchBufferPool.chunkSize != shChunkSize || m_ScratchBufferPool.maxChunkCount != maxSHChunkCount)
                {
                    if (probeVolumeDebug.verboseStreamingLog)
                        LogStreaming($"Allocating new Scratch Buffer Pool. Chunk size: {shChunkSize}, max SH Chunks: {maxSHChunkCount}");

                    if (m_ScratchBufferPool != null)
                        m_ScratchBufferPool.Cleanup();

                    m_ScratchBufferPool = new ProbeVolumeScratchBufferPool(m_CurrentBakingSet, m_SHBands);
                }
            }
        }

        void UpdateActiveRequests(CommandBuffer cmd)
        {
            if (m_ActiveStreamingRequests.Count > 0)
            {
                for (int i = m_ActiveStreamingRequests.Count - 1; i >= 0; --i)
                {
                    var request = m_ActiveStreamingRequests[i];
                    // Can't String.Format in an assert message without generating garbage :/
                    //Debug.Assert(request.state != CellStreamingRequest.State.Pending, $"Wrong status for request {request.cell.desc.index}: {request.state}");
                    Debug.Assert(request.state != CellStreamingRequest.State.Pending, "Wrong status for request");

                    bool releaseRequest = false;

                    if (request.state == CellStreamingRequest.State.Canceled)
                    {
                        if (probeVolumeDebug.verboseStreamingLog)
                            LogStreaming($"Discarding active request for cell {request.cell.desc.index}");

                        m_ScratchBufferPool.ReleaseScratchBuffer(request.scratchBuffer);
                        releaseRequest = true;
                    }
                    else
                    {
                        request.UpdateState();

                        if (request.state == CellStreamingRequest.State.Complete)
                        {
                            Debug.Assert(cmd != null); // We should not get here during cleanup.

                            if (probeVolumeDebug.verboseStreamingLog)
                            {
                                if (request.poolIndex == -1)
                                    LogStreaming($"Completed disk streaming request for cell {request.cell.desc.index}");
                                else
                                    LogStreaming($"Completed disk streaming request for blending cell {request.cell.desc.index} for scenario {request.poolIndex}");
                            }

                            // Because of limitation of low level device implementation of Lock/Unlock on Graphics Buffers
                            // (the fact that locking over multiple frames isn't really supported)
                            // We need to go through a temporary buffer and copy into the GraphicsBuffer when streaming is done.
                            // This can be a first step to later on, use compressed data on disk to lighten the I/O load and decompress
                            // directly in the graphics buffer.
                            if (request.scratchBuffer.buffer != null)
                            {
                                var mappedBuffer = request.scratchBuffer.buffer.LockBufferForWrite<byte>(0, request.scratchBuffer.stagingBuffer.Length);
                                mappedBuffer.CopyFrom(request.scratchBuffer.stagingBuffer);
                                request.scratchBuffer.buffer.UnlockBufferAfterWrite<byte>(request.scratchBuffer.stagingBuffer.Length);
                            }
                            request.onStreamingComplete(request, cmd);

                            // We can release here because the GraphicsBuffer inside the scratchBuffer is double buffered.
                            // So a new request on next frame won't overlap.
                            m_ScratchBufferPool.ReleaseScratchBuffer(request.scratchBuffer);
                            releaseRequest = true;
                        }
                        else if (request.state == CellStreamingRequest.State.Invalid)
                        {
                            if (probeVolumeDebug.verboseStreamingLog)
                                LogStreaming($"Reseting invalid request for cell {request.cell.desc.index}");

                            // If invalid, try to run it again.
                            m_ScratchBufferPool.ReleaseScratchBuffer(request.scratchBuffer);
                            request.Reset();
                            m_ActiveStreamingRequests.RemoveAt(i);
                            m_StreamingQueue.Enqueue(request);
                        }
                    }

                    if (releaseRequest)
                    {
                        m_ActiveStreamingRequests.RemoveAt(i);
                        m_StreamingRequestsPool.Release(request);
                    }
                }
            }
        }

        unsafe void ProcessNewRequests()
        {
            while (m_StreamingQueue.TryPeek(out var request))
            {
                if (request.state == CellStreamingRequest.State.Canceled)
                {
                    if (probeVolumeDebug.verboseStreamingLog)
                    {
                        if (request.poolIndex == -1)
                            LogStreaming($"Discarding request for cell {request.cell.desc.index}");
                        else
                            LogStreaming($"Discarding request for blending cell {request.cell.desc.index} for scenario {request.poolIndex}");
                    }

                    Debug.Assert(request.scratchBuffer == null);
                    m_StreamingRequestsPool.Release(request);
                    m_StreamingQueue.Dequeue(); // Discard request.
                }
                else
                {
                    Debug.Assert(request.state == CellStreamingRequest.State.Pending);
                    Debug.Assert(request.cell.data != null); // Need data for bricks and support data.

                    if (ProcessDiskStreamingRequest(request))
                    {
                        m_StreamingQueue.Dequeue();
                    }
                    else
                    {
                        // No available scratch buffer for this request.
                        // Since we want to conserve order in the queue, we don't process any more requests this frame.
                        break;
                    }
                }
            }
        }

        void UpdateDiskStreaming(CommandBuffer cmd)
        {
            if (!diskStreamingEnabled)
                return;

            using (new ProfilingScope(ProfilingSampler.Get(CoreProfileId.APVDiskStreamingUpdate)))
            {
                AllocateScratchBufferPoolIfNeeded();
                ProcessNewRequests();
                UpdateActiveRequests(cmd);

                // Close file handles if not needed anymore.
                // Checking cellBricksDataAsset here just to know if any of the files is open. If one if open, all of them should be.
                if (m_ActiveStreamingRequests.Count == 0 && m_StreamingQueue.Count == 0 && m_CurrentBakingSet.cellBricksDataAsset != null && m_CurrentBakingSet.cellBricksDataAsset.IsOpen())
                {
                    if (probeVolumeDebug.verboseStreamingLog)
                        LogStreaming("Closing files open for APV disk streaming.");

                    m_CurrentBakingSet.cellBricksDataAsset.CloseFile();
                    m_CurrentBakingSet.cellSupportDataAsset.CloseFile();
                    m_CurrentBakingSet.cellSharedDataAsset.CloseFile();

                    if (m_CurrentBakingSet.scenarios.TryGetValue(lightingScenario, out var scenarioData))
                    {
                        scenarioData.cellDataAsset.CloseFile();
                        scenarioData.cellOptionalDataAsset.CloseFile();
                        scenarioData.cellProbeOcclusionDataAsset.CloseFile();
                    }

                    if (!string.IsNullOrEmpty(otherScenario) && m_CurrentBakingSet.scenarios.TryGetValue(lightingScenario, out var otherScenarioData))
                    {
                        otherScenarioData.cellDataAsset.CloseFile();
                        otherScenarioData.cellOptionalDataAsset.CloseFile();
                        otherScenarioData.cellProbeOcclusionDataAsset.CloseFile();
                    }
                }
            }

            // Debug flag to force unload/reload of cells to be able to debug streaming shader code.
            if (probeVolumeDebug.debugStreaming)
            {
                if (m_ToBeLoadedCells.size == 0 && m_ActiveStreamingRequests.Count == 0)
                    UnloadAllCells();
            }
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        void LogStreaming(string log)
        {
            Debug.Log(log);
        }
    }
}
