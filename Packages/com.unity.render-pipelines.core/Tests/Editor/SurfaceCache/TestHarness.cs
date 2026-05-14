using System;
using System.Reflection;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering.Tests
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHRGBL1
    {
        public Vector3 L0;
        public Vector3 L10;
        public Vector3 L11;
        public Vector3 L12;
    }

    // Each fixture owns one of these.
    internal sealed class TestHarness : IDisposable
    {
        sealed class WorldUpdatePassData
        {
            internal SurfaceCacheWorld World;
            internal uint EnvCubemapResolution;
            internal UnityEngine.Light Sun;
        }

        readonly RayTracingContext _rtContext;
        readonly SurfaceCacheResourceSet _coreResources;
        readonly SurfaceCache _cache;
        readonly SurfaceCacheWorld _world;
        readonly RenderGraph _renderGraph;

        CommandBuffer _cmd;
        GraphicsBuffer _worldScratch;
        uint _frameIdx;
        uint _outputIrradianceBufferIdx;

        public SurfaceCacheWorld World => _world;
        public SurfaceCacheResourceSet Resources => _coreResources;

        public TestHarness(RayTracingBackend backend = RayTracingBackend.Compute)
        {
            Assume.That(RayTracingContext.IsBackendSupported(backend), Is.True,
                $"SurfaceCache Core tests require ray-tracing backend '{backend}' to be supported.");
            Assume.That(SystemInfo.computeSubGroupSize, Is.GreaterThan(0),
                "SurfaceCache Core tests require real wave-size / sub-group support.");

            var volParams = new SurfaceCacheVolumeParameterSet
            {
                Resolution = 4,
                CascadeCount = 1,
                Size = 1f
            };

            var rtResources = new RayTracingResources();
            rtResources.Load();
            _rtContext = new RayTracingContext(backend, rtResources);

            var shaders = UnityEngine.Resources.Load<TestShaderAsset>("TestShaders");
            Assert.That(shaders, Is.Not.Null,
                "TestShaders.asset not found under any Resources/ folder. "
                + "Expected at Packages/com.unity.render-pipelines.core/Tests/Editor/SurfaceCache/Resources/.");

            Object punctualLightSamplingUnifiedObj;
            Object estimationUnifiedObj;
            if (backend == RayTracingBackend.Compute)
            {
                punctualLightSamplingUnifiedObj = shaders.punctualLightSamplingComputeShader;
                estimationUnifiedObj = shaders.estimationComputeShader;
            }
            else
            {
                punctualLightSamplingUnifiedObj = shaders.punctualLightSamplingRayTracingShader;
                estimationUnifiedObj = shaders.estimationRayTracingShader;
            }
            IRayTracingShader punctualLightSamplingShader = _rtContext.CreateRayTracingShader(punctualLightSamplingUnifiedObj);
            IRayTracingShader estimationShader = _rtContext.CreateRayTracingShader(estimationUnifiedObj);

            _coreResources = new SurfaceCacheResourceSet((uint)SystemInfo.computeSubGroupSize);
            _coreResources.Load(
                shaders.scrolling, shaders.eviction, shaders.patchAllocation,
                shaders.spatialFiltering, shaders.temporalFiltering, shaders.defrag,
                punctualLightSamplingShader, estimationShader);
            foreach (var field in typeof(SurfaceCacheResourceSet).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var value = field.GetValue(_coreResources);
                if (typeof(Object).IsAssignableFrom(field.FieldType) || typeof(IRayTracingShader).IsAssignableFrom(field.FieldType))
                    Assert.That(value, Is.Not.Null, $"SurfaceCacheResourceSet.{field.Name} is null after Load().");
                else if (field.FieldType == typeof(int) && field.Name.EndsWith("Kernel"))
                    Assert.That((int)value, Is.GreaterThanOrEqualTo(0), $"SurfaceCacheResourceSet.{field.Name} is -1 after Load().");
            }

            _cache = new SurfaceCache(_coreResources, volParams);
            _cache.SetEstimationParams(new SurfaceCacheEstimationParameterSet
            {
                SampleCount = 1,
                MultiBounce = false,
                BouncePatchAllocation = false
            });
            _cache.SetPatchFilteringParams(new SurfaceCachePatchFilteringParameterSet
            {
                TemporalSmoothing = 0f,
                SpatialFilterEnabled = false,
                SpatialFilterSampleCount = 0,
                SpatialFilterRadius = 0f,
                TemporalPostFilterEnabled = false
            });

            var worldResources = new WorldResourceSet();
            worldResources.LoadFromAssetDatabase();
            _world = new SurfaceCacheWorld();
            _world.Init(_rtContext, worldResources);
            _renderGraph = new RenderGraph("TestHarness");
        }

        public void BeginFrame()
        {
            // Fresh CommandBuffer per frame: avoids any cross-frame buffer-lifecycle race that
            // could occur if Graphics.ExecuteCommandBuffer's deep-copy contract is unreliable
            // under -force-gfx-jobs split (worker-thread submission).
            _cmd = new CommandBuffer { name = $"TestHarness Frame {_frameIdx}" };
            var rgParams = new RenderGraphParameters
            {
                commandBuffer = _cmd,
                invalidContextForTesting = true,
                currentFrameIndex = (int)_frameIdx,
            };
            _renderGraph.BeginRecording(rgParams);
        }

        public void CommitWorld(UnityEngine.Light sun = null)
        {
            using (var builder = _renderGraph.AddUnsafePass("Surface Cache World Update", out WorldUpdatePassData passData))
            {
                passData.World = _world;
                passData.EnvCubemapResolution = 32;
                passData.Sun = sun;

                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((WorldUpdatePassData data, UnsafeGraphContext graphCtx) => UpdateWorld(data, graphCtx, ref _worldScratch));
            }
        }

        public void Estimate()
        {
            _cache.RecordPreparation(_renderGraph, _frameIdx);
            _outputIrradianceBufferIdx = _cache.RecordPatchUpdate(_renderGraph, _frameIdx, _world);
        }

        public void EndFrame()
        {
            _renderGraph.EndRecordingAndExecute();
            Graphics.ExecuteCommandBuffer(_cmd);
            _cmd.Dispose();
            _cmd = null;
            _frameIdx++;
        }

        static void UpdateWorld(WorldUpdatePassData data, UnsafeGraphContext graphCtx, ref GraphicsBuffer scratch)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphCtx.cmd);
            data.World.Commit(cmd, ref scratch, data.EnvCubemapResolution, data.Sun, out _);
        }

        public SHRGBL1 ReadPatchIrradiance(uint patchIndex)
        {
            var data = new SHRGBL1[1];
            _cache.Patches.Irradiances[_outputIrradianceBufferIdx].GetData(
                data, 0, (int)patchIndex, 1);
            return data[0];
        }

        // Configure patches; seeds geometry, ring config, and irradiance buffers (0 and 2) directly.
        public void SetPatches(
            Vector3[] worldPositions,
            Vector3[] worldNormals,
            uint[] cellIndices,
            SHRGBL1[] irradiances)
        {
            Debug.Assert(worldPositions.Length == worldNormals.Length);
            Debug.Assert(worldPositions.Length == cellIndices.Length);
            Debug.Assert(worldPositions.Length == irradiances.Length);

            int count = worldPositions.Length;

            var geometries = new float[count * 6];
            for (int i = 0; i < count; ++i)
            {
                geometries[i * 6 + 0] = worldPositions[i].x;
                geometries[i * 6 + 1] = worldPositions[i].y;
                geometries[i * 6 + 2] = worldPositions[i].z;
                geometries[i * 6 + 3] = worldNormals[i].x;
                geometries[i * 6 + 4] = worldNormals[i].y;
                geometries[i * 6 + 5] = worldNormals[i].z;
            }
            _cache.Patches.Geometries.SetData(geometries, 0, 0, geometries.Length);
            _cache.Patches.CellIndices.SetData(cellIndices, 0, 0, count);

            for (int i = 0; i < count; ++i)
                _cache.Volume.CellPatchIndices.SetData(new[] { (uint)i }, 0, (int)cellIndices[i], 1);

            _cache.RingConfig.Buffer.SetData(
                new uint[] { (uint)count, 0, (uint)count }, 0, (int)_cache.RingConfig.OffsetA, 3);

            _cache.Patches.Irradiances[0].SetData(irradiances, 0, 0, count);
            _cache.Patches.Irradiances[2].SetData(irradiances, 0, 0, count);
        }

        public static void AssertL0IrradianceApproximatelyEqual(SHRGBL1 expected, SHRGBL1 actual, float epsilon)
        {
            AssertVector3ApproximatelyEqual(expected.L0,   actual.L0,   epsilon, "L0");
        }

        static void AssertVector3ApproximatelyEqual(Vector3 expected, Vector3 actual, float epsilon, string label)
        {
            Assert.AreEqual(expected.x, actual.x, epsilon, label + ".x");
            Assert.AreEqual(expected.y, actual.y, epsilon, label + ".y");
            Assert.AreEqual(expected.z, actual.z, epsilon, label + ".z");
        }

        public void Dispose()
        {
            _renderGraph.Cleanup();
            _cache.Dispose();
            _world.Dispose();
            _rtContext.Dispose();
            _worldScratch?.Dispose();
            _cmd?.Dispose();
        }
    }
}
