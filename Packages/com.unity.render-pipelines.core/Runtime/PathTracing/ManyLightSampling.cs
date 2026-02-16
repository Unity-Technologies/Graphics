using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;
using static UnityEngine.PathTracing.Core.World;

namespace UnityEngine.PathTracing.Core
{

    // Interface for many light sampling
    internal interface IManyLightSampling : IDisposable
    {
        void Build(CommandBuffer cmd, World.LightState lightState, Bounds sceneBounds, SamplingResources samplingResources);
        void Bind(CommandBuffer cmd, IRayTracingShader shader);
    }

    internal enum GridMemLayout { Sparse, Dense };
    internal enum GridSizingStrategy { Uniform, FitToSceneBounds };

    internal static class LightGridUtils
    {
        public static Vector3Int ComputeLightGridDims(Vector3 sceneBounds, int maxLightGridCellCount, GridSizingStrategy lightGridSizingStrategy)
        {
            if (lightGridSizingStrategy == GridSizingStrategy.Uniform)
            {
                int volumeSide = (int)Math.Pow((double)maxLightGridCellCount, 1.0 / 3.0);
                if ((volumeSide + 1) * (volumeSide + 1) * (volumeSide + 1) <= maxLightGridCellCount)
                    volumeSide++;

                return new Vector3Int(volumeSide, volumeSide, volumeSide);
            }

            // Fix scene bounds if the ratio between 2 dims is too important
            float maxSceneDim = math.max(sceneBounds.x, math.max(sceneBounds.y, sceneBounds.z));

            if (sceneBounds.x * Mathf.Sqrt(maxLightGridCellCount) < maxSceneDim)
                sceneBounds.x = maxSceneDim / Mathf.Sqrt(maxLightGridCellCount);

            if (sceneBounds.y * Mathf.Sqrt(maxLightGridCellCount) < maxSceneDim)
                sceneBounds.y = maxSceneDim / Mathf.Sqrt(maxLightGridCellCount);

            if (sceneBounds.z * Mathf.Sqrt(maxLightGridCellCount) < maxSceneDim)
                sceneBounds.z = maxSceneDim / Mathf.Sqrt(maxLightGridCellCount);

            // Compute ideal cell width (we aim for for cells having the same width along all 3 axes)
            float idealCellWidth = Mathf.Pow(sceneBounds.x * sceneBounds.y * sceneBounds.z / ((float)maxLightGridCellCount), 1.0f / 3.0f);

            // Use ideal cell width to compute grid dims
            Vector3Int gridDims = new Vector3Int(
                Math.Max((int)(sceneBounds.x / idealCellWidth), 1),
                Math.Max((int)(sceneBounds.y / idealCellWidth), 1),
                Math.Max((int)(sceneBounds.z / idealCellWidth), 1));

            Debug.Assert(gridDims.x * gridDims.y * gridDims.z <= maxLightGridCellCount);
            return gridDims;
        }

        public static int ComputeMaxLightsInAnyCell(int2[] grid, Vector3Int gridDims)
        {
            int maxLightsInAnyCell = 0;
            for (int i = 0; i < gridDims.x * gridDims.y * gridDims.z; i++)
            {
                int lightCount = grid[i].y;
                maxLightsInAnyCell = Math.Max(maxLightsInAnyCell, lightCount);
            }
            return maxLightsInAnyCell;
        }
    }


    internal class ConservativeLightGrid : IManyLightSampling
    {
        // Light grid parameters
        public int LightGridCellCount = 64 * 64 * 64;
        public int MaxLightsPerCell = 64; // Only used with GridMemLayout.Dense
        public GridSizingStrategy LightGridSizingStrategy = GridSizingStrategy.FitToSceneBounds;
        public GridMemLayout GridMemLayout = GridMemLayout.Sparse;
        public int MaxLightsInAnyCell => _maxLightsInAnyCell;

        public ConservativeLightGrid(ComputeShader shader)
        {
            _shader = shader;
            _buildLightGridlKernel = _shader.FindKernel("BuildConservativeLightGrid");
        }

        public void Init()
        {
            if (GridMemLayout == GridMemLayout.Dense && (_lightGridCellsDataBuffer == null || _lightGridCellsDataBuffer.count <= 1))
            {
                int count = LightGridCellCount * MaxLightsPerCell;
                int stride = Marshal.SizeOf<World.ThinReservoir>();
                _lightGridCellsDataBuffer?.Dispose();
                _lightGridCellsDataBuffer = new ComputeBuffer(count, stride);
            }

            if (_lightGridBuffer == null || _lightGridBuffer.count <= 1)
            {
                int count = LightGridCellCount;
                int stride = Marshal.SizeOf<int2>();
                _lightGridBuffer?.Dispose();
                _lightGridBuffer = new ComputeBuffer(count, stride);
            }

            if (_totalLightsInGridCountBuffer == null)
            {
                _totalLightsInGridCountBuffer = new ComputeBuffer(1, sizeof(int));
            }
        }

        protected void BindComputeResources(CommandBuffer cmd, World.LightState lightState, Bounds sceneBounds, SamplingResources samplingResources)
        {
            SamplingResources.Bind(cmd, samplingResources);

            // Set the input lighting state.Note that this is a subset, as we evaluate without light cookies
            cmd.SetComputeIntParam(_shader, ShaderProperties.NumLights, lightState.LightCount);
            cmd.SetComputeIntParam(_shader, ShaderProperties.MaxLightsPerCell, MaxLightsPerCell);
            cmd.SetComputeIntParam(_shader, ShaderProperties.NumEmissiveMeshes, lightState.MeshLightCount);
            cmd.SetComputeIntParam(_shader, ShaderProperties.GridDimX, _lightGridDims.x);
            cmd.SetComputeIntParam(_shader, ShaderProperties.GridDimY, _lightGridDims.y);
            cmd.SetComputeIntParam(_shader, ShaderProperties.GridDimZ, _lightGridDims.z);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.GridMin, sceneBounds.min);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.GridSize, sceneBounds.size);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.CellSize, _cellSize);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.InvCellSize, _invCellSize);

            cmd.SetComputeBufferParam(_shader, _buildLightGridlKernel, ShaderProperties.LightList, lightState.LightListBuffer);

            // Set the output buffer
            cmd.SetComputeBufferParam(_shader, _buildLightGridlKernel, ShaderProperties.LightGrid, _lightGridBuffer);
            cmd.SetComputeBufferParam(_shader, _buildLightGridlKernel, ShaderProperties.TotalReservoirCount, _totalLightsInGridCountBuffer);
        }

        public void Build(CommandBuffer cmd, World.LightState lightState, Bounds sceneBounds, SamplingResources samplingResources)
        {
            if (lightState.LightListBuffer == null)
                return;

            Init();

            _sceneBounds = sceneBounds;

            _lightGridDims = LightGridUtils.ComputeLightGridDims(sceneBounds.size, LightGridCellCount, LightGridSizingStrategy);
            Vector3 div = new Vector3(1.0f / _lightGridDims.x, 1.0f / _lightGridDims.y, 1.0f / _lightGridDims.z);
            Vector3 cellSize = Vector3.Scale(sceneBounds.size, div);
            _cellSize = cellSize;
            // The length of the diagonal
            _cellSize.w = Mathf.Sqrt(cellSize.x * cellSize.x + cellSize.y * cellSize.y + cellSize.z * cellSize.z);
            _invCellSize = new Vector4(1.0f / _cellSize.x, 1.0f / _cellSize.y, 1.0f / _cellSize.z, 1.0f / _cellSize.w);

            BindComputeResources(cmd, lightState, sceneBounds, samplingResources);

            // If the grid is sparse, do a first dispatch to determine the total light count for all cells
            // And allocate the _lightGridBuffer based on that number
            if (GridMemLayout == GridMemLayout.Sparse)
            {
                _shader.EnableKeyword("SPARSE_GRID");
                cmd.SetComputeBufferParam(_shader, _buildLightGridlKernel, ShaderProperties.LightGridCellsData, _lightGridBuffer); // dummy bind
                DispatchBuild(cmd, 0);

                GraphicsHelpers.Flush(cmd);
                var requiredLightCount = new int[1];
                _totalLightsInGridCountBuffer.GetData(requiredLightCount);
                if (_lightGridCellsDataBuffer == null || _lightGridCellsDataBuffer.count < requiredLightCount[0])
                {
                    _lightGridCellsDataBuffer?.Dispose();
                    _lightGridCellsDataBuffer = new ComputeBuffer(math.max(requiredLightCount[0], 1), Marshal.SizeOf<World.ThinReservoir>());
                }

                // Need to re-bind everything after flush
                BindComputeResources(cmd, lightState, sceneBounds, samplingResources);
            }
            else
            {
                _shader.DisableKeyword("SPARSE_GRID");
            }

            // Build the grid
            cmd.SetComputeBufferParam(_shader, _buildLightGridlKernel, ShaderProperties.LightGridCellsData, _lightGridCellsDataBuffer);
            DispatchBuild(cmd, 1);

            GraphicsHelpers.Flush(cmd);
            int2[] grid = new int2[_lightGridBuffer.count];
            _lightGridBuffer.GetData(grid);
            _maxLightsInAnyCell = LightGridUtils.ComputeMaxLightsInAnyCell(grid, _lightGridDims);
        }

        public void Bind(CommandBuffer cmd, IRayTracingShader shader)
        {
            if (_lightGridCellsDataBuffer == null)
            {
                // dummy buffer, when the feature is disabled
                int stride = Marshal.SizeOf<World.ThinReservoir>();
                _lightGridCellsDataBuffer = new ComputeBuffer(1, stride);
            }

            if (_lightGridBuffer == null)
            {
                // dummy buffer, when the feature is disabled
                _lightGridBuffer = new ComputeBuffer(1, Marshal.SizeOf<int2>());
            }

            shader.SetIntParam(cmd, ShaderProperties.GridDimX, _lightGridDims.x);
            shader.SetIntParam(cmd, ShaderProperties.GridDimY, _lightGridDims.y);
            shader.SetIntParam(cmd, ShaderProperties.GridDimZ, _lightGridDims.z);
            shader.SetIntParam(cmd, ShaderProperties.NumReservoirs, MaxLightsPerCell);

            shader.SetVectorParam(cmd, ShaderProperties.GridMin, _sceneBounds.min);
            shader.SetVectorParam(cmd, ShaderProperties.GridSize, _sceneBounds.size);
            shader.SetVectorParam(cmd, ShaderProperties.CellSize, _cellSize);
            shader.SetVectorParam(cmd, ShaderProperties.InvCellSize, _invCellSize);

            shader.SetBufferParam(cmd, ShaderProperties.LightGridCellsData, _lightGridCellsDataBuffer);
            shader.SetBufferParam(cmd, ShaderProperties.LightGrid, _lightGridBuffer);
        }

        public void Dispose()
        {
            _lightGridCellsDataBuffer?.Dispose();
            _lightGridBuffer?.Dispose();
            _totalLightsInGridCountBuffer?.Dispose();
        }

        void DispatchBuild(CommandBuffer cmd, int buildPass)
        {
            const int groupDim = 4;
            cmd.SetComputeIntParam(_shader, ShaderProperties.BuildPass, buildPass);
            cmd.SetBufferData(_totalLightsInGridCountBuffer, new uint[] { 0 });
            cmd.DispatchCompute(_shader, _buildLightGridlKernel,
                GraphicsHelpers.DivUp(_lightGridDims.x, groupDim),
                GraphicsHelpers.DivUp(_lightGridDims.y, groupDim),
                GraphicsHelpers.DivUp(_lightGridDims.z, groupDim));
        }

        readonly ComputeShader _shader;
        readonly int _buildLightGridlKernel;
        ComputeBuffer _lightGridCellsDataBuffer;
        ComputeBuffer _lightGridBuffer;
        ComputeBuffer _totalLightsInGridCountBuffer;
        Bounds _sceneBounds;
        Vector4 _cellSize;
        Vector4 _invCellSize;
        Vector3Int _lightGridDims;
        int _maxLightsInAnyCell;
    }

    internal class RegirLightGrid : IManyLightSampling
    {
        // Light grid parameters
        public int LightGridCellCount = 64 * 64 * 64;
        public int MaxLightsPerCell = 64;
        public int NumCandidates = -1; // -1 means we iterate over all the lights
        public GridSizingStrategy LightGridSizingStrategy = GridSizingStrategy.Uniform;
        public int MaxLightsInAnyCell => _maxLightsInAnyCell;

        public RegirLightGrid(ComputeShader shader)
        {
            _shader = shader;
            _buildRegirLightGridlKernel = _shader.FindKernel("BuildRegirLightGrid");
        }

        public void Init()
        {
            if (_lightGridCellsDataBuffer == null || _lightGridCellsDataBuffer.count <= 1)
            {
                int count = LightGridCellCount * MaxLightsPerCell;
                int stride = Marshal.SizeOf<World.ThinReservoir>();
                _lightGridCellsDataBuffer?.Dispose();
                _lightGridCellsDataBuffer = new ComputeBuffer(count, stride);
            }

            if (_lightGridBuffer == null || _lightGridBuffer.count <= 1)
            {
                int count = LightGridCellCount;
                int stride = Marshal.SizeOf<int2>();
                _lightGridBuffer?.Dispose();
                _lightGridBuffer = new ComputeBuffer(count, stride);
            }
        }

        public void Build(CommandBuffer cmd, World.LightState lightState, Bounds sceneBounds, SamplingResources samplingResources)
        {
            if (lightState.LightListBuffer == null)
                return;

            Init();

            _sceneBounds = sceneBounds;

            // The number of RIS candidates cannot exceed the number of light sources
            int activeCandidates = NumCandidates == -1 ? lightState.LightCount : Mathf.Min(NumCandidates, lightState.LightCount);

            _lightGridDims = LightGridUtils.ComputeLightGridDims(sceneBounds.size, LightGridCellCount, LightGridSizingStrategy);
            Vector3 div = new Vector3(1.0f / _lightGridDims.x, 1.0f / _lightGridDims.y, 1.0f / _lightGridDims.z);
            Vector3 cellSize = Vector3.Scale(sceneBounds.size, div);
            _cellSize = cellSize;
            // The length of the diagonal
            _cellSize.w = Mathf.Sqrt(cellSize.x * cellSize.x + cellSize.y * cellSize.y + cellSize.z * cellSize.z);
            _invCellSize = new Vector4(1.0f / _cellSize.x, 1.0f / _cellSize.y, 1.0f / _cellSize.z, 1.0f / _cellSize.w);

            SamplingResources.Bind(cmd, samplingResources);

            // Set the input lighting state.Note that this is a subset, as we evaluate without light cookies
            cmd.SetComputeIntParam(_shader, ShaderProperties.NumLights, lightState.LightCount);
            cmd.SetComputeIntParam(_shader, ShaderProperties.NumCandidates, activeCandidates);
            cmd.SetComputeIntParam(_shader, ShaderProperties.NumReservoirs, MaxLightsPerCell);
            cmd.SetComputeIntParam(_shader, ShaderProperties.NumEmissiveMeshes, lightState.MeshLightCount);
            cmd.SetComputeIntParam(_shader, ShaderProperties.GridDimX, _lightGridDims.x);
            cmd.SetComputeIntParam(_shader, ShaderProperties.GridDimY, _lightGridDims.y);
            cmd.SetComputeIntParam(_shader, ShaderProperties.GridDimZ, _lightGridDims.z);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.GridMin, sceneBounds.min);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.GridSize, sceneBounds.size);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.CellSize, _cellSize);
            cmd.SetComputeVectorParam(_shader, ShaderProperties.InvCellSize, _invCellSize);

            cmd.SetComputeBufferParam(_shader, _buildRegirLightGridlKernel, ShaderProperties.LightList, lightState.LightListBuffer);

            // Set the output buffer
            cmd.SetComputeBufferParam(_shader, _buildRegirLightGridlKernel, ShaderProperties.LightGrid, _lightGridBuffer);
            cmd.SetComputeBufferParam(_shader, _buildRegirLightGridlKernel, ShaderProperties.LightGridCellsData, _lightGridCellsDataBuffer);

            // Build the grid
            const int groupDim = 4;
            cmd.DispatchCompute(_shader, _buildRegirLightGridlKernel,
                GraphicsHelpers.DivUp(_lightGridDims.x, groupDim),
                GraphicsHelpers.DivUp(_lightGridDims.y, groupDim),
                GraphicsHelpers.DivUp(_lightGridDims.z, groupDim));

            GraphicsHelpers.Flush(cmd);
            int2[] grid = new int2[_lightGridBuffer.count];
            _lightGridBuffer.GetData(grid);
            _maxLightsInAnyCell = LightGridUtils.ComputeMaxLightsInAnyCell(grid, _lightGridDims);
        }

        public void Bind(CommandBuffer cmd, IRayTracingShader shader)
        {
            if (_lightGridCellsDataBuffer == null)
            {
                // dummy buffer, when the feature is disabled
                int stride = Marshal.SizeOf<World.ThinReservoir>();
                _lightGridCellsDataBuffer = new ComputeBuffer(1, stride);
            }

            if (_lightGridBuffer == null)
            {
                // dummy buffer, when the feature is disabled
                _lightGridBuffer = new ComputeBuffer(1, Marshal.SizeOf<int2>());
            }

            shader.SetIntParam(cmd, ShaderProperties.GridDimX, _lightGridDims.x);
            shader.SetIntParam(cmd, ShaderProperties.GridDimY, _lightGridDims.y);
            shader.SetIntParam(cmd, ShaderProperties.GridDimZ, _lightGridDims.z);
            shader.SetIntParam(cmd, ShaderProperties.NumReservoirs, MaxLightsPerCell);

            shader.SetVectorParam(cmd, ShaderProperties.GridMin, _sceneBounds.min);
            shader.SetVectorParam(cmd, ShaderProperties.GridSize, _sceneBounds.size);
            shader.SetVectorParam(cmd, ShaderProperties.CellSize, _cellSize);
            shader.SetVectorParam(cmd, ShaderProperties.InvCellSize, _invCellSize);

            shader.SetBufferParam(cmd, ShaderProperties.LightGridCellsData, _lightGridCellsDataBuffer);
            shader.SetBufferParam(cmd, ShaderProperties.LightGrid, _lightGridBuffer);
        }

        public void Dispose()
        {
            _lightGridCellsDataBuffer?.Dispose();
            _lightGridBuffer?.Dispose();
        }


        readonly ComputeShader _shader;
        readonly int _buildRegirLightGridlKernel;
        ComputeBuffer _lightGridCellsDataBuffer;
        ComputeBuffer _lightGridBuffer;
        Bounds _sceneBounds;
        Vector4 _cellSize;
        Vector4 _invCellSize;
        Vector3Int _lightGridDims;
        int _maxLightsInAnyCell;
    }
}
